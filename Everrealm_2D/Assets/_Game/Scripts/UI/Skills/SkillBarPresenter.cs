using System.Collections.Generic;
using System;
using LetterHunter.Characters;
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
        private Canvas _rootCanvas;
        private int _hoveredSlotIndex = -1;
        private int _dragSourceIndex = -1;
        private SkillSlotView _dragSourceView;

        public event Action<string> AssignmentFeedbackChanged;
        public bool IsAssigningSkill => _pendingAssignment != null;

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerClassController>();
            if (loadout == null && player != null)
                loadout = player.GetComponent<PlayerSkillLoadout>();
            if (slotRoot == null)
                slotRoot = transform;
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>();

            if (assignmentGhost != null)
            {
                assignmentGhost.enabled = false;
                assignmentGhost.raycastTarget = false;
            }

            foreach (var view in authoredSlots)
                WireView(view);
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
            EnsureViews();
            Render();
        }

        public bool BeginAssignSkill(SkillDefinition skill)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                return false;

            if (loadout == null && player != null)
                loadout = player.GetComponent<PlayerSkillLoadout>();
            if (loadout == null && player != null)
                loadout = player.gameObject.AddComponent<PlayerSkillLoadout>();

            _pendingAssignment = skill;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            CreateAssignmentGhost(skill);
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
                if (_runtimeSlots[i].Skill != null)
                    continue;
                if (!loadout.TryAssignSkill(i, skill, out _))
                    return false;
                Rebuild();
                return true;
            }
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
            view.Clicked -= OnSlotClicked;
            view.PointerEntered -= OnSlotPointerEntered;
            view.PointerExited -= OnSlotPointerExited;
            view.DragBegan -= OnSlotDragBegan;
            view.DragMoved -= UpdateSlotDrag;
            view.DragEnded -= EndSlotDrag;
            view.Dropped -= DropDraggedSlotOn;
            view.Clicked += OnSlotClicked;
            view.PointerEntered += OnSlotPointerEntered;
            view.PointerExited += OnSlotPointerExited;
            view.DragBegan += OnSlotDragBegan;
            view.DragMoved += UpdateSlotDrag;
            view.DragEnded += EndSlotDrag;
            view.Dropped += DropDraggedSlotOn;
        }

        private void Render()
        {
            if (player == null || _runtimeSlots == null) return;

            for (var i = 0; i < _views.Count && i < _runtimeSlots.Count; i++)
            {
                var binding = _runtimeSlots[i];
                var skill = binding.Skill;
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
            if (player == null || _runtimeSlots == null || index < 0 || index >= _runtimeSlots.Count) return;

            if (_pendingAssignment != null)
            {
                AssignPendingSkillToSlot(index);
                return;
            }

            var skill = _runtimeSlots[index].Skill;
            if (skill == null) return;

            var result = player.UseSkill(skill.SkillId);
            if (logSkillClicks)
            {
                Debug.Log(result.Success
                    ? $"[Skill Bar] Activated {skill.DisplayName}."
                    : $"[Skill Bar] {skill.DisplayName} failed: {result.Failure}.", this);
            }
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
                index >= _runtimeSlots.Count || _runtimeSlots[index].Skill == null)
                return;

            _dragSourceIndex = index;
            _dragSourceView = index < _views.Count ? _views[index] : null;
            _dragSourceView?.SetDragHidden(true);
            CreateAssignmentGhost(_runtimeSlots[index].Skill);
            AssignmentFeedbackChanged?.Invoke(
                $"Move {_runtimeSlots[index].Skill.DisplayName} to another slot.");
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

        private void CreateAssignmentGhost(SkillDefinition skill)
        {
            DestroyAssignmentGhost();
            if (assignmentGhost == null)
                return;

            _assignmentGhost = assignmentGhost;
            _assignmentGhost.sprite = skill.Icon;
            _assignmentGhost.color = skill.Icon != null ? Color.white : new Color(0.95f, 0.78f, 0.25f, 0.9f);
            _assignmentGhost.raycastTarget = false;
            _assignmentGhost.preserveAspect = true;
            _assignmentGhost.rectTransform.sizeDelta = new Vector2(58f, 58f);

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

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
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
                _assignmentGhost.sprite = null;
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
