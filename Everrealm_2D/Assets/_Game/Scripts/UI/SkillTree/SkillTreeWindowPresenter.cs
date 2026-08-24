using System;
using System.Collections.Generic;
using LetterHunter.Characters;
using LetterHunter.SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeWindowPresenter : MonoBehaviour
    {
        [Header("Domain")]
        [SerializeField] private PlayerSkillTreeController controller;
        [SerializeField] private Key toggleKey = Key.K;
        [SerializeField] private bool startVisible;

        [Header("Window")]
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Profession")]
        [SerializeField] private Image professionIcon;
        [SerializeField] private TMP_Text professionNameText;
        [SerializeField] private TMP_Text professionDescriptionText;

        [Header("Tree")]
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private Transform connectionRoot;
        [SerializeField] private Transform nodeRoot;
        [SerializeField] private List<SkillTreeNodeView> nodeViews = new();
        [SerializeField] private List<SkillTreeConnectionView> connectionViews = new();

        [Header("Selected skill")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailDescriptionText;
        [SerializeField] private TMP_Text detailRequirementsText;
        [SerializeField] private TMP_Text detailStateText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonText;

        [Header("Modal input")]
        [SerializeField] private CharacterInputRouter characterInput;
        [SerializeField] private Behaviour[] additionalInputsToBlock = Array.Empty<Behaviour>();

        private readonly Dictionary<string, SkillTreeNodeView> _views = new();
        private readonly List<(SkillNodeDefinitionSO parent, SkillNodeDefinitionSO child)> _edges = new();
        private bool[] _previousInputStates = Array.Empty<bool>();
        private SkillNodeDefinitionSO _selectedNode;
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockMode;
        private bool _modalApplied;

        public bool IsVisible => windowGroup == null ? gameObject.activeSelf : windowGroup.alpha > 0.01f;

        private void Awake()
        {
            if (controller == null) controller = FindFirstObjectByType<PlayerSkillTreeController>();
            if (windowGroup == null) windowGroup = GetComponent<CanvasGroup>();
            if (characterInput == null) characterInput = FindFirstObjectByType<CharacterInputRouter>();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (buyButton != null) buyButton.onClick.AddListener(BuySelected);
            PrepareAuthoredViews();
            SetVisible(startVisible);
        }

        private void OnEnable()
        {
            if (controller != null) controller.StateChanged += Render;
            if (IsVisible) Render();
        }

        private void OnDisable()
        {
            if (controller != null) controller.StateChanged -= Render;
            if (!_modalApplied) return;
            characterInput?.SetBlocked(false);
            RestoreAdditionalInputs();
            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _cursorLockMode;
            _modalApplied = false;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard[toggleKey]?.wasPressedThisFrame == true)
                SetVisible(!IsVisible);
            else if (IsVisible && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open() => SetVisible(true);
        public void Close() => SetVisible(false);

        public void Render()
        {
            if (!IsVisible || controller == null || controller.ActiveProfession == null) return;
            var profession = controller.ActiveProfession;
            if (titleText != null) titleText.text = "PROFESSION SKILLS";
            if (levelText != null) levelText.text = $"LEVEL  {controller.CurrentLevel}";
            if (coinsText != null) coinsText.text = $"COINS  {controller.CurrentCoins}";
            if (professionIcon != null)
            {
                professionIcon.sprite = profession.Icon;
                professionIcon.enabled = profession.Icon != null;
            }
            if (professionNameText != null) professionNameText.text = profession.DisplayName;
            if (professionDescriptionText != null) professionDescriptionText.text = profession.Description;

            if (_selectedNode == null || !profession.Contains(_selectedNode))
                _selectedNode = FirstNode(profession);

            foreach (var node in profession.SkillNodes)
                if (node != null && _views.TryGetValue(node.NodeId, out var view))
                    view.Render(controller.Service.GetState(node), node == _selectedNode);

            ArrangeTree(profession);
            RenderConnections();
            RenderDetails();
        }

        private void PrepareAuthoredViews()
        {
            _views.Clear();
            foreach (var view in nodeViews)
            {
                if (view == null || view.ConfiguredNode == null) continue;
                view.Bind(view.ConfiguredNode, SelectNode, OnNodeHovered);
                _views[view.ConfiguredNode.NodeId] = view;
            }
            _edges.Clear();
            var profession = controller != null ? controller.ActiveProfession : null;
            if (profession == null) return;
            foreach (var child in profession.SkillNodes)
                if (child != null)
                    foreach (var parent in child.ParentNodes)
                        if (parent != null) _edges.Add((parent, child));
        }

        private void ArrangeTree(ProfessionDefinitionSO profession)
        {
            if (nodeRoot is not RectTransform nodeRect) return;
            var layout = nodeRoot.GetComponent<SkillTreeGraphLayoutGroup>();
            if (layout == null) return;
            var size = layout.Arrange(profession, _views);
            if (scrollContent != null) scrollContent.sizeDelta = size;
            nodeRect.sizeDelta = size;
            if (connectionRoot is RectTransform connectionRect) connectionRect.sizeDelta = size;
        }

        private void RenderConnections()
        {
            for (var i = 0; i < connectionViews.Count; i++)
                connectionViews[i]?.gameObject.SetActive(i < _edges.Count);
            for (var i = 0; i < _edges.Count && i < connectionViews.Count; i++)
            {
                var edge = _edges[i];
                if (!_views.TryGetValue(edge.parent.NodeId, out var parentView) ||
                    !_views.TryGetValue(edge.child.NodeId, out var childView)) continue;
                var parentRect = (RectTransform)parentView.transform;
                var childRect = (RectTransform)childView.transform;
                var start = parentRect.anchoredPosition + Vector2.right * parentRect.rect.width * 0.5f;
                var end = childRect.anchoredPosition - Vector2.right * childRect.rect.width * 0.5f;
                connectionViews[i].Render(start, end, controller.Service.GetState(edge.child));
            }
        }

        private void RenderDetails()
        {
            if (_selectedNode == null) return;
            var state = controller.Service.GetState(_selectedNode);
            if (detailIcon != null)
            {
                detailIcon.sprite = _selectedNode.Icon;
                detailIcon.enabled = _selectedNode.Icon != null;
            }
            if (detailNameText != null) detailNameText.text = _selectedNode.DisplayName;
            if (detailDescriptionText != null) detailDescriptionText.text = _selectedNode.Description;
            if (detailRequirementsText != null) detailRequirementsText.text = BuildRequirements(_selectedNode);
            if (detailStateText != null) detailStateText.text = StateLabel(state);
            if (buyButton != null) buyButton.interactable = state == SkillTreeNodeState.Available;
            if (buyButtonText != null)
                buyButtonText.text = state == SkillTreeNodeState.Purchased ? "PURCHASED" : $"BUY  {_selectedNode.Price}";
        }

        private void SelectNode(SkillNodeDefinitionSO node)
        {
            _selectedNode = node;
            Render();
        }

        private void OnNodeHovered(SkillNodeDefinitionSO node, bool hovered)
        {
            if (hovered && node != null && feedbackText != null)
                feedbackText.text = node.DisplayName;
            else if (feedbackText != null)
                feedbackText.text = "K TOGGLE   •   ESC CLOSE";
        }

        private void BuySelected()
        {
            if (_selectedNode == null || controller == null) return;
            var result = controller.TryPurchase(_selectedNode);
            if (feedbackText != null)
                feedbackText.text = result.Success ? "SKILL PURCHASED" : FailureLabel(result.Failure);
            Render();
        }

        private void SetVisible(bool visible)
        {
            if (windowGroup == null) gameObject.SetActive(visible);
            else
            {
                windowGroup.alpha = visible ? 1f : 0f;
                windowGroup.interactable = visible;
                windowGroup.blocksRaycasts = visible;
            }

            if (visible)
            {
                if (!_modalApplied)
                {
                    _cursorWasVisible = Cursor.visible;
                    _cursorLockMode = Cursor.lockState;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    characterInput?.SetBlocked(true);
                    BlockAdditionalInputs();
                    _modalApplied = true;
                }
                PrepareAuthoredViews();
                Render();
            }
            else if (_modalApplied)
            {
                characterInput?.SetBlocked(false);
                RestoreAdditionalInputs();
                Cursor.visible = _cursorWasVisible;
                Cursor.lockState = _cursorLockMode;
                _modalApplied = false;
            }
        }

        private void BlockAdditionalInputs()
        {
            _previousInputStates = new bool[additionalInputsToBlock?.Length ?? 0];
            for (var i = 0; i < _previousInputStates.Length; i++)
            {
                var input = additionalInputsToBlock[i];
                if (input == null || input == this) continue;
                _previousInputStates[i] = input.enabled;
                input.enabled = false;
            }
        }

        private void RestoreAdditionalInputs()
        {
            if (additionalInputsToBlock == null) return;
            for (var i = 0; i < additionalInputsToBlock.Length && i < _previousInputStates.Length; i++)
                if (additionalInputsToBlock[i] != null && additionalInputsToBlock[i] != this)
                    additionalInputsToBlock[i].enabled = _previousInputStates[i];
            _previousInputStates = Array.Empty<bool>();
        }

        private static SkillNodeDefinitionSO FirstNode(ProfessionDefinitionSO profession)
        {
            foreach (var node in profession.SkillNodes) if (node != null) return node;
            return null;
        }

        private string BuildRequirements(SkillNodeDefinitionSO node)
        {
            var parents = string.Empty;
            foreach (var parent in node.ParentNodes)
                if (parent != null) parents += (parents.Length == 0 ? string.Empty : ", ") + parent.DisplayName;
            var parentLine = parents.Length > 0 ? $"\nRequires: {parents}" : string.Empty;
            var abilityLine = node.AbilityToGrant != null ? $"\nUnlocks: {node.AbilityToGrant.DisplayName}" : string.Empty;
            return $"Required level: {node.RequiredLevel}\nPrice: {node.Price} coins{parentLine}{abilityLine}";
        }

        private static string StateLabel(SkillTreeNodeState state) => state switch
        {
            SkillTreeNodeState.Available => "AVAILABLE",
            SkillTreeNodeState.Purchased => "PURCHASED",
            SkillTreeNodeState.UnavailableByFunds => "NOT ENOUGH COINS",
            _ => "LOCKED"
        };

        private static string FailureLabel(SkillTreePurchaseFailure failure) => failure switch
        {
            SkillTreePurchaseFailure.RequiredLevel => "LEVEL REQUIREMENT NOT MET",
            SkillTreePurchaseFailure.MissingParent => "PURCHASE PARENT SKILLS FIRST",
            SkillTreePurchaseFailure.NotEnoughCurrency => "NOT ENOUGH COINS",
            SkillTreePurchaseFailure.AlreadyPurchased => "ALREADY PURCHASED",
            _ => "PURCHASE FAILED"
        };
    }
}
