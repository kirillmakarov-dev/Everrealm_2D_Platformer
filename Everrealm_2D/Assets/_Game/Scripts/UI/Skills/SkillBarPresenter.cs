using System.Collections.Generic;
using System;
using LetterHunter.Characters;
using LetterHunter.Items;
using LetterHunter.UI.Inventory;
using LetterHunter.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LetterHunter.UI.Skills
{
    [DisallowMultipleComponent]
    public sealed class SkillBarPresenter : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerClassController player;
        [SerializeField] private PlayerSkillLoadout loadout;
        [SerializeField] private PlayerInventory inventory;

        [Header("View")]
        [SerializeField] private SkillSlotView slotPrefab;
        [SerializeField] private List<SkillSlotView> authoredSlots = new();
        [SerializeField] private Image assignmentGhost;
        [SerializeField] private Transform slotRoot;
        [Min(1), SerializeField] private int maxSlots = 4;

        [Header("Debug")]
        [SerializeField] private bool logSkillClicks = true;

        private readonly List<SkillSlotView> _views = new();
        private IReadOnlyList<SkillSlotBinding> _runtimeSlots;
        private SkillDefinition _pendingAssignment;
        private Image _assignmentGhost;
        private Transform _assignmentGhostOriginalParent;
        private int _assignmentGhostOriginalSiblingIndex;
        private Canvas _rootCanvas;
        private int _hoveredSlotIndex = -1;
        private int _dragSourceIndex = -1;
        private SkillSlotView _dragSourceView;
        private ItemDefinition[] _consumableSlots = Array.Empty<ItemDefinition>();

        public event Action<string> AssignmentFeedbackChanged;
        public event Action ConsumableSlotsChanged;
        public bool IsAssigningSkill => _pendingAssignment != null;
        public int ConsumableSlotCount => _consumableSlots != null ? _consumableSlots.Length : Mathf.Max(1, maxSlots);

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerClassController>();
            if (loadout == null && player != null)
                loadout = player.GetComponent<PlayerSkillLoadout>();
            if (inventory == null && player != null)
                inventory = player.GetComponent<PlayerInventory>();
            if (slotRoot == null)
                slotRoot = transform;
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>();

            if (assignmentGhost != null)
            {
                _assignmentGhostOriginalParent = assignmentGhost.transform.parent;
                _assignmentGhostOriginalSiblingIndex = assignmentGhost.transform.GetSiblingIndex();
                assignmentGhost.gameObject.SetActive(false);
                assignmentGhost.enabled = false;
                assignmentGhost.raycastTarget = false;
            }

            foreach (var view in authoredSlots)
                WireView(view);

            _consumableSlots = new ItemDefinition[Mathf.Max(1, maxSlots)];
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.InventoryChanged += Render;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.InventoryChanged -= Render;
        }

        private void Start()
        {
            Rebuild();
        }

        private void Update()
        {
            UpdateAssignmentGhost();
            HandlePendingAssignmentPointerDrop();
            HandlePendingAssignmentHotkeys();
            Render();
        }

        public void Rebuild()
        {
            if (player == null)
            {
                Debug.LogWarning("[Skill Bar] Player not found.", this);
                return;
            }

            if (loadout == null)
                loadout = player.GetComponent<PlayerSkillLoadout>();
            if (loadout == null)
                loadout = player.gameObject.AddComponent<PlayerSkillLoadout>();

            _runtimeSlots = loadout.BuildRuntimeSlots(player, maxSlots);
            ResolveConsumableSkillConflicts();
            _runtimeSlots = loadout.BuildRuntimeSlots(player, maxSlots);
            EnsureViews();
            Render();
        }

        public bool BeginAssignSkill(SkillDefinition skill)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                return false;
            if (player == null || !player.IsSkillAvailable(skill)) return false;

            if (loadout == null && player != null)
                loadout = player.GetComponent<PlayerSkillLoadout>();
            if (loadout == null && player != null)
                loadout = player.gameObject.AddComponent<PlayerSkillLoadout>();

            _pendingAssignment = skill;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            CreateAssignmentGhost(skill.Icon);
            AssignmentFeedbackChanged?.Invoke($"Select a skill bar slot for {skill.DisplayName}.");
            Render();
            return true;
        }

        public bool TryAutoAssignSkill(SkillDefinition skill)
        {
            if (skill == null || player == null)
                return false;
            Rebuild();
            if (_runtimeSlots == null)
                return false;

            foreach (var binding in _runtimeSlots)
                if (binding?.Skill != null && binding.Skill.SkillId == skill.SkillId)
                    return true;

            for (var i = 0; i < _runtimeSlots.Count; i++)
            {
                if (_runtimeSlots[i].Skill != null || GetConsumableSlot(i) != null)
                    continue;
                if (!loadout.TryAssignSkill(i, skill, out _))
                    return false;
                Rebuild();
                return true;
            }

            AssignmentFeedbackChanged?.Invoke($"No empty skill bar slot is available for {skill.DisplayName}.");
            return false;
        }

        private void EnsureViews()
        {
            if (slotPrefab == null)
            {
                Debug.LogWarning("[Skill Bar] Slot prefab is missing.", this);
                return;
            }

            _views.Clear();
            _views.AddRange(authoredSlots);

            for (var i = 0; i < _views.Count; i++)
                _views[i].gameObject.SetActive(i < maxSlots);
        }

        private void WireView(SkillSlotView view)
        {
            if (view == null)
                return;
            view.DisableTemplateIconLayers();
            view.Clicked -= OnSlotClicked;
            view.PointerEntered -= OnSlotPointerEntered;
            view.PointerExited -= OnSlotPointerExited;
            view.DragBegan -= OnSlotDragBegan;
            view.DragMoved -= UpdateSlotDrag;
            view.DragEnded -= EndSlotDrag;
            view.Dropped -= DropDraggedSlotOn;
            view.ItemDropped -= DropInventoryItemOn;
            view.Clicked += OnSlotClicked;
            view.PointerEntered += OnSlotPointerEntered;
            view.PointerExited += OnSlotPointerExited;
            view.DragBegan += OnSlotDragBegan;
            view.DragMoved += UpdateSlotDrag;
            view.DragEnded += EndSlotDrag;
            view.Dropped += DropDraggedSlotOn;
            view.ItemDropped += DropInventoryItemOn;
        }

        private void Render()
        {
            if (player == null || _runtimeSlots == null) return;

            for (var i = 0; i < _views.Count && i < _runtimeSlots.Count; i++)
            {
                var binding = _runtimeSlots[i];
                if (i < _consumableSlots.Length && _consumableSlots[i] != null)
                {
                    var consumable = _consumableSlots[i];
                    var amount = inventory != null ? inventory.RuntimeInventory.Count(consumable) : 0;
                    if (amount > 0)
                    {
                        _views[i].RenderConsumable(consumable, amount, binding.InputLabel);
                        continue;
                    }

                    _consumableSlots[i] = null;
                }

                var skill = binding.Skill;
                if (!player.IsSkillAvailable(skill)) skill = null;
                SkillRuntimeState state = null;
                var effectiveManaCost = skill != null ? skill.ManaCost : 0f;
                var effectiveCooldown = skill != null ? skill.Cooldown : 0f;
                if (skill != null)
                {
                    player.TryGetSkillRuntimeState(skill.SkillId, out state);
                    if (player.SkillService != null)
                    {
                        var values = player.SkillService.GetRuntimeValues(skill);
                        effectiveManaCost = values.ManaCost;
                        effectiveCooldown = values.Cooldown;
                    }
                }

                var canAfford = skill == null || player.Stats == null || player.Stats.CurrentMana >= effectiveManaCost;
                _views[i].Render(new SkillSlotViewModel(i, skill, state, canAfford, binding.InputLabel,
                    effectiveCooldown));
                _views[i].SetAssignmentTarget(_pendingAssignment != null);
            }
        }

        private void OnSlotClicked(int index)
        {
            TryActivateSlot(index);
        }

        public bool TryActivateSlot(int index)
        {
            if (player == null || _runtimeSlots == null || index < 0 || index >= _runtimeSlots.Count)
                return false;

            if (index < _consumableSlots.Length && _consumableSlots[index] != null)
            {
                UseConsumable(index);
                return true;
            }

            if (_pendingAssignment != null)
            {
                AssignPendingSkillToSlot(index);
                return true;
            }

            var skill = _runtimeSlots[index].Skill;
            if (skill == null) return false;

            var characterRoot = player.GetComponent<CharacterRoot>();
            if (characterRoot == null)
            {
                Debug.LogError("[Skill Bar] Cannot activate a skill: the player has no CharacterRoot projectile route.", this);
                return false;
            }

            var result = characterRoot.TryUseSkillSlot(index);
            if (logSkillClicks)
            {
                Debug.Log(result.Success
                    ? $"[Skill Bar] Activated {skill.DisplayName}."
                    : $"[Skill Bar] {skill.DisplayName} failed: {result.Failure}.", this);
            }

            return result.Success;
        }

        private void HandlePendingAssignmentHotkeys()
        {
            if (_pendingAssignment == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelPendingAssignment();
                return;
            }

            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(2);
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(3);
            else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(4);
            else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(5);
            else if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(6);
            else if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(7);
            else if (keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(8);
            else if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame)
                AssignPendingSkillToSlot(9);
        }

        private void HandlePendingAssignmentPointerDrop()
        {
            if (_pendingAssignment == null)
                return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                CancelPendingAssignment();
                return;
            }

            if (_hoveredSlotIndex >= 0 && mouse != null && mouse.leftButton.wasReleasedThisFrame)
                AssignPendingSkillToSlot(_hoveredSlotIndex);
        }

        private void AssignPendingSkillToSlot(int index)
        {
            if (GetConsumableSlot(index) != null)
            {
                AssignmentFeedbackChanged?.Invoke($"Slot {index + 1} is occupied by a consumable.");
                return;
            }

            var failure = "Skill loadout is missing.";
            if (loadout != null && _pendingAssignment != null && loadout.TryAssignSkill(index, _pendingAssignment, out failure))
            {
                AssignmentFeedbackChanged?.Invoke($"{_pendingAssignment.DisplayName} assigned to slot {index + 1}.");
                _pendingAssignment = null;
                DestroyAssignmentGhost();
                Rebuild();
            }
            else
            {
                AssignmentFeedbackChanged?.Invoke(failure ?? "Could not assign skill.");
            }
        }

        private void OnSlotPointerEntered(int index)
        {
            _hoveredSlotIndex = index;
        }

        private void OnSlotPointerExited(int index)
        {
            if (_hoveredSlotIndex == index)
                _hoveredSlotIndex = -1;
        }

        private void OnSlotDragBegan(int index, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_pendingAssignment != null || _runtimeSlots == null || index < 0 ||
                index >= _runtimeSlots.Count)
                return;

            var skill = _runtimeSlots[index].Skill;
            var consumable = GetConsumableSlot(index);
            if (skill == null && consumable == null)
                return;

            _dragSourceIndex = index;
            _dragSourceView = index < _views.Count ? _views[index] : null;
            _dragSourceView?.SetDragHidden(true);
            CreateAssignmentGhost(skill != null ? skill.Icon : consumable.Icon);
            AssignmentFeedbackChanged?.Invoke(
                $"Move {(skill != null ? skill.DisplayName : consumable.DisplayName)} to another slot.");
            UpdateAssignmentGhost();
        }

        private void UpdateSlotDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_dragSourceIndex >= 0)
                UpdateAssignmentGhost();
        }

        private void DropDraggedSlotOn(int targetIndex)
        {
            if (_dragSourceIndex < 0 || _runtimeSlots == null || loadout == null ||
                targetIndex < 0 || targetIndex >= _runtimeSlots.Count)
                return;

            var sourceIndex = _dragSourceIndex;
            var sourceConsumable = GetConsumableSlot(sourceIndex);
            if (sourceConsumable != null)
            {
                if (HasAssignedSkill(targetIndex))
                {
                    AssignmentFeedbackChanged?.Invoke($"Slot {targetIndex + 1} is occupied by a skill.");
                    FinishSlotDrag(false);
                    return;
                }

                var moved = TrySwapConsumableSlots(sourceIndex, targetIndex);
                AssignmentFeedbackChanged?.Invoke(sourceIndex == targetIndex
                    ? "Consumable position unchanged."
                    : moved
                        ? $"Moved {sourceConsumable.DisplayName} to slot {targetIndex + 1}."
                        : "Could not move consumable.");
                FinishSlotDrag(false);
                return;
            }

            if (GetConsumableSlot(targetIndex) != null)
            {
                AssignmentFeedbackChanged?.Invoke($"Slot {targetIndex + 1} is occupied by a consumable.");
                FinishSlotDrag(false);
                return;
            }

            var sourceSkill = _runtimeSlots[sourceIndex].Skill;
            var targetSkill = _runtimeSlots[targetIndex].Skill;
            if (loadout.TrySwapSlots(sourceIndex, targetIndex, sourceSkill, targetSkill, out var failure))
            {
                AssignmentFeedbackChanged?.Invoke(sourceIndex == targetIndex
                    ? "Skill position unchanged."
                    : $"Moved {sourceSkill.DisplayName} to slot {targetIndex + 1}.");
                FinishSlotDrag(true);
            }
            else
            {
                AssignmentFeedbackChanged?.Invoke(failure ?? "Could not move skill.");
                FinishSlotDrag(false);
            }
        }

        private void DropInventoryItemOn(int targetIndex, InventorySlotView source)
        {
            if (source == null || source.Item == null || source.Item.ItemType != ItemType.Consumable ||
                inventory == null || inventory.RuntimeInventory.Count(source.Item) <= 0 ||
                targetIndex < 0 || targetIndex >= _consumableSlots.Length)
                return;

            if (!SetConsumableSlot(targetIndex, source.Item))
                AssignmentFeedbackChanged?.Invoke($"Slot {targetIndex + 1} is occupied by a skill.");
        }

        public ItemDefinition GetConsumableSlot(int index)
        {
            EnsureConsumableSlots();
            return index >= 0 && index < _consumableSlots.Length ? _consumableSlots[index] : null;
        }

        public bool TrySwapConsumableSlots(int firstIndex, int secondIndex)
        {
            EnsureConsumableSlots();
            if (firstIndex < 0 || firstIndex >= _consumableSlots.Length ||
                secondIndex < 0 || secondIndex >= _consumableSlots.Length ||
                _consumableSlots[firstIndex] == null || HasAssignedSkill(secondIndex))
                return false;
            if (firstIndex == secondIndex)
                return true;

            (_consumableSlots[firstIndex], _consumableSlots[secondIndex]) =
                (_consumableSlots[secondIndex], _consumableSlots[firstIndex]);
            ConsumableSlotsChanged?.Invoke();
            Render();
            return true;
        }

        public bool SetConsumableSlot(int index, ItemDefinition item, bool notify = true,
            bool allowSkillConflict = false)
        {
            EnsureConsumableSlots();
            if (index < 0 || index >= _consumableSlots.Length ||
                (item != null && item.ItemType != ItemType.Consumable))
                return false;
            if (item != null && !allowSkillConflict && HasAssignedSkill(index))
                return false;

            if (_consumableSlots[index] == item)
                return true;

            _consumableSlots[index] = item;
            if (notify)
                ConsumableSlotsChanged?.Invoke();
            Render();
            return true;
        }

        public void ClearConsumableSlots(bool notify = true)
        {
            EnsureConsumableSlots();
            var changed = false;
            for (var i = 0; i < _consumableSlots.Length; i++)
            {
                changed |= _consumableSlots[i] != null;
                _consumableSlots[i] = null;
            }

            if (changed && notify)
                ConsumableSlotsChanged?.Invoke();
            Render();
        }

        private void EnsureConsumableSlots()
        {
            if (_consumableSlots == null || _consumableSlots.Length != Mathf.Max(1, maxSlots))
                Array.Resize(ref _consumableSlots, Mathf.Max(1, maxSlots));
        }

        private bool HasAssignedSkill(int index)
        {
            if (_runtimeSlots != null && index >= 0 && index < _runtimeSlots.Count)
            {
                if (_runtimeSlots[index]?.Skill != null)
                    return true;
            }
            if (loadout == null)
                return false;

            foreach (var binding in loadout.Slots)
                if (binding != null && binding.SlotIndex == index && binding.Skill != null)
                    return true;
            return false;
        }

        private void ResolveConsumableSkillConflicts()
        {
            EnsureConsumableSlots();
            if (_runtimeSlots == null || loadout == null)
                return;

            var consumablesChanged = false;
            for (var sourceIndex = 0; sourceIndex < _runtimeSlots.Count; sourceIndex++)
            {
                var skill = _runtimeSlots[sourceIndex]?.Skill;
                if (skill == null || GetConsumableSlot(sourceIndex) == null)
                    continue;

                var targetIndex = FindEmptySlot(sourceIndex);
                if (targetIndex >= 0 && loadout.TrySwapSlots(sourceIndex, targetIndex, skill, null, out _))
                {
                    _runtimeSlots = loadout.BuildRuntimeSlots(player, maxSlots);
                    continue;
                }

                // Skills have priority. The consumable stays in inventory and can be assigned again later.
                _consumableSlots[sourceIndex] = null;
                consumablesChanged = true;
            }

            if (consumablesChanged)
                ConsumableSlotsChanged?.Invoke();
        }

        private int FindEmptySlot(int excludedIndex)
        {
            for (var i = 0; i < _runtimeSlots.Count; i++)
                if (i != excludedIndex && _runtimeSlots[i]?.Skill == null && GetConsumableSlot(i) == null)
                    return i;
            return -1;
        }

        private void UseConsumable(int slotIndex)
        {
            var item = _consumableSlots[slotIndex];
            if (item == null || inventory == null || inventory.RuntimeInventory.Count(item) <= 0)
                return;
            if (!player.TryUseConsumable(item))
                return;

            inventory.TryRemove(item, 1);
            if (inventory.RuntimeInventory.Count(item) <= 0)
                SetConsumableSlot(slotIndex, null);
            Render();
        }

        private void EndSlotDrag()
        {
            if (_dragSourceIndex >= 0)
                FinishSlotDrag(false);
        }

        private void FinishSlotDrag(bool rebuild)
        {
            _dragSourceView?.SetDragHidden(false);
            _dragSourceView = null;
            _dragSourceIndex = -1;
            DestroyAssignmentGhost();
            if (rebuild)
                Rebuild();
            else
                Render();
        }

        private void CreateAssignmentGhost(Sprite icon)
        {
            DestroyAssignmentGhost();
            if (assignmentGhost == null)
                return;

            _assignmentGhost = assignmentGhost;
            _assignmentGhost.sprite = icon;
            _assignmentGhost.color = icon != null ? Color.white : new Color(0.95f, 0.78f, 0.25f, 0.9f);
            _assignmentGhost.raycastTarget = false;
            _assignmentGhost.preserveAspect = true;

            var canvasTransform = _rootCanvas != null ? _rootCanvas.transform as RectTransform : null;
            if (canvasTransform != null && _assignmentGhost.transform.parent != canvasTransform)
            {
                _assignmentGhost.transform.SetParent(canvasTransform, false);
                _assignmentGhost.transform.SetAsLastSibling();
            }

            _assignmentGhost.rectTransform.anchorMin = new Vector2(.5f, .5f);
            _assignmentGhost.rectTransform.anchorMax = new Vector2(.5f, .5f);
            _assignmentGhost.rectTransform.pivot = new Vector2(.5f, .5f);
            _assignmentGhost.rectTransform.localScale = Vector3.one;
            _assignmentGhost.rectTransform.sizeDelta = new Vector2(58f, 58f);

            _assignmentGhost.gameObject.SetActive(true);
            _assignmentGhost.enabled = true;
            var group = _assignmentGhost.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.alpha = 0.9f;
            }
            UpdateAssignmentGhost();
        }

        private void UpdateAssignmentGhost()
        {
            if (_assignmentGhost == null || _rootCanvas == null)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var ghostParent = _assignmentGhost.rectTransform.parent as RectTransform;
            if (ghostParent == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ghostParent,
                mouse.position.ReadValue(),
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
                out var localPoint);
            _assignmentGhost.rectTransform.anchoredPosition = localPoint;
        }

        private void DestroyAssignmentGhost()
        {
            if (_assignmentGhost != null)
            {
                _assignmentGhost.enabled = false;
                _assignmentGhost.gameObject.SetActive(false);
                _assignmentGhost.sprite = null;

                if (_assignmentGhostOriginalParent != null)
                {
                    _assignmentGhost.transform.SetParent(_assignmentGhostOriginalParent, false);
                    _assignmentGhost.transform.SetSiblingIndex(_assignmentGhostOriginalSiblingIndex);
                }
            }
            _assignmentGhost = null;
        }

        private void CancelPendingAssignment()
        {
            if (_pendingAssignment == null)
                return;

            AssignmentFeedbackChanged?.Invoke("Skill assignment cancelled.");
            _pendingAssignment = null;
            _hoveredSlotIndex = -1;
            DestroyAssignmentGhost();
            Render();
        }
    }
}
