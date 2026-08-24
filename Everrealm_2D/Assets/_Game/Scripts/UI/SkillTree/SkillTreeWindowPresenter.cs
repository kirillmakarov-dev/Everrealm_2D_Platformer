using System.Collections.Generic;
using LetterHunter.SkillTree;
using LetterHunter.UI.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeWindowPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerSkillTreeController controller;
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private Transform nodeRoot;
        [SerializeField] private Transform connectionRoot;
        [SerializeField] private RectTransform scrollContent;
        [Min(1f), SerializeField] private float minimumContentHeight = 270f;
        [Min(1f), SerializeField] private float minimumContentWidth = 700f;
        [SerializeField] private List<SkillTreeNodeView> nodeViews = new();
        [SerializeField] private List<SkillTreeConnectionView> connectionViews = new();
        [SerializeField] private SkillTreeNodeView nodePrefab;
        [SerializeField] private SkillTreeConnectionView connectionPrefab;
        [SerializeField] private SkillBarPresenter skillBar;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private Key toggleKey = Key.K;
        [SerializeField] private bool startVisible;

        private readonly Dictionary<string, SkillTreeNodeView> _views = new();
        private readonly Dictionary<string, SkillTreeConnectionView> _connections = new();
        private string _pendingRefundNodeId;
        private float _refundConfirmationExpiresAt;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<PlayerSkillTreeController>();
            if (windowGroup == null)
                windowGroup = GetComponent<CanvasGroup>();
            if (skillBar == null)
                skillBar = FindFirstObjectByType<SkillBarPresenter>();
            if (scrollContent == null && nodeRoot != null)
                scrollContent = nodeRoot.parent as RectTransform;

            PrepareAuthoredViews();

            SetVisible(startVisible);
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Service.Progress.ProgressChanged += Render;
                if (controller.Wallet != null)
                    controller.Wallet.GoldChanged += OnGoldChanged;
                if (controller.Inventory != null)
                    controller.Inventory.InventoryChanged += Render;
            }
            if (skillBar != null)
                skillBar.AssignmentFeedbackChanged += SetFeedback;

            if (IsVisible)
                Render();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Service.Progress.ProgressChanged -= Render;
                if (controller.Wallet != null)
                    controller.Wallet.GoldChanged -= OnGoldChanged;
                if (controller.Inventory != null)
                    controller.Inventory.InventoryChanged -= Render;
            }
            if (skillBar != null)
                skillBar.AssignmentFeedbackChanged -= SetFeedback;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey]?.wasPressedThisFrame == true)
                SetVisible(!IsVisible);
            if (!string.IsNullOrWhiteSpace(_pendingRefundNodeId) &&
                Time.unscaledTime > _refundConfirmationExpiresAt)
            {
                _pendingRefundNodeId = null;
                Render();
            }
        }

        public void Render()
        {
            if (!IsVisible || controller == null || controller.SkillTree == null || nodeRoot == null)
                return;

            if (titleText != null)
                titleText.text = controller.SkillTree.DisplayName;

            foreach (var node in controller.SkillTree.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                    continue;

                if (!_views.TryGetValue(node.NodeId, out var view) || view == null)
                    continue;

                if (nodeRoot.GetComponent<LayoutGroup>() == null && view.transform is RectTransform nodeRect)
                    nodeRect.anchoredPosition = node.UiPosition;

                controller.Service.CanUnlock(node.NodeId, out var failure);
                var confirmingRefund = _pendingRefundNodeId == node.NodeId &&
                                       Time.unscaledTime <= _refundConfirmationExpiresAt;
                view.Render(node, controller.Service.Progress, failure, confirmingRefund);
            }

            RefreshLayout();
            RenderConnections();
        }

        private void RefreshLayout()
        {
            if (nodeRoot is not RectTransform nodeRect)
                return;

            var graphLayout = nodeRoot.GetComponent<SkillTreeGraphLayoutGroup>();
            if (graphLayout != null)
            {
                var preferredSize = graphLayout.Arrange(controller.SkillTree, _views);
                var contentWidth = Mathf.Max(minimumContentWidth, preferredSize.x);
                var contentHeight = Mathf.Max(minimumContentHeight, preferredSize.y);
                ResizeMapLayers(nodeRect, contentWidth, contentHeight);
                graphLayout.Arrange(controller.SkillTree, _views);
                return;
            }

            if (nodeRoot.GetComponent<LayoutGroup>() == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(nodeRect);
            var preferredHeight = LayoutUtility.GetPreferredHeight(nodeRect);
            var listContentHeight = Mathf.Max(minimumContentHeight, preferredHeight);

            if (scrollContent != null)
                scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listContentHeight);
            nodeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listContentHeight);
            if (connectionRoot is RectTransform connectionRect)
                connectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listContentHeight);

            LayoutRebuilder.ForceRebuildLayoutImmediate(nodeRect);
        }

        private void ResizeMapLayers(RectTransform nodeRect, float width, float height)
        {
            if (scrollContent != null)
            {
                scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            nodeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            nodeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (connectionRoot is RectTransform connectionRect)
            {
                connectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                connectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        private void RenderConnections()
        {
            if (connectionRoot == null)
                return;

            var connectionIndex = 0;

            foreach (var node in controller.SkillTree.Nodes)
            {
                if (node == null)
                    continue;

                foreach (var prerequisiteId in node.PrerequisiteNodeIds)
                {
                    if (!_views.TryGetValue(prerequisiteId, out var prerequisiteView) ||
                        !_views.TryGetValue(node.NodeId, out var nodeView))
                        continue;

                    var connectionId = $"{prerequisiteId}>{node.NodeId}";
                    if (!_connections.TryGetValue(connectionId, out var connection) || connection == null)
                    {
                        if (connectionIndex >= connectionViews.Count)
                            continue;
                        connection = connectionViews[connectionIndex];
                        _connections[connectionId] = connection;
                    }
                    connectionIndex++;

                    var prerequisiteRect = (RectTransform)prerequisiteView.transform;
                    var nodeRect = (RectTransform)nodeView.transform;
                    var start = prerequisiteRect.anchoredPosition + Vector2.right * prerequisiteRect.rect.width * 0.5f;
                    var end = nodeRect.anchoredPosition - Vector2.right * nodeRect.rect.width * 0.5f;
                    connection.Render(start, end, controller.Service.Progress.IsUnlocked(node.NodeId));
                }
            }

            for (; connectionIndex < connectionViews.Count; connectionIndex++)
                connectionViews[connectionIndex]?.gameObject.SetActive(false);
        }

        private void PrepareAuthoredViews()
        {
            _views.Clear();
            foreach (var view in nodeViews)
            {
                if (view == null || string.IsNullOrWhiteSpace(view.ConfiguredNodeId))
                    continue;
                view.gameObject.SetActive(true);
                view.Bind(view.ConfiguredNodeId, TryUnlock, TrySelectSkill, SetNodeHovered, TryRefundRank);
                _views[view.ConfiguredNodeId] = view;
            }

            _connections.Clear();
            foreach (var connection in connectionViews)
                if (connection != null)
                    connection.gameObject.SetActive(false);
        }

        private void TryUnlock(string nodeId)
        {
            if (controller == null)
                return;

            var wasUnlocked = controller.Service.Progress.IsUnlocked(nodeId);
            var result = controller.Service.TryUnlock(nodeId);
            if (result.Success)
            {
                if (!wasUnlocked && TryBeginSkillAssignment(nodeId))
                    SetFeedback("Unlocked. Choose a skill bar slot.");
                else
                    SetFeedback(wasUnlocked ? "Upgraded" : "Unlocked");
            }
            else if (result.Failure == SkillTreeUnlockFailure.AlreadyUnlocked && TryBeginSkillAssignment(nodeId))
                return;
            else
                SetFeedback(result.Failure.ToString());

            Render();
        }

        private void TrySelectSkill(string nodeId)
        {
            if (!TryBeginSkillAssignment(nodeId))
                SetFeedback("This node does not unlock a selectable skill.");
        }

        private void TryRefundRank(string nodeId)
        {
            if (controller == null)
                return;

            if (_pendingRefundNodeId != nodeId || Time.unscaledTime > _refundConfirmationExpiresAt)
            {
                _pendingRefundNodeId = nodeId;
                _refundConfirmationExpiresAt = Time.unscaledTime + 3f;
                SetFeedback("Click Confirm to refund one rank.");
                Render();
                return;
            }

            _pendingRefundNodeId = null;
            var result = controller.Service.TryRefundRank(nodeId);
            if (result.Success)
                SetFeedback($"Rank refunded. +{result.RefundedGold} gold.");
            else
                SetFeedback(RespecFailureText(result.Failure));
            Render();
        }

        private static string RespecFailureText(SkillTreeRespecFailure failure) => failure switch
        {
            SkillTreeRespecFailure.HasUnlockedDependents => "Refund dependent nodes first.",
            SkillTreeRespecFailure.InventoryFull => "Inventory has no room for refunded materials.",
            SkillTreeRespecFailure.NotUnlocked => "This node has no purchased ranks.",
            _ => failure.ToString()
        };

        private bool TryBeginSkillAssignment(string nodeId)
        {
            if (controller == null || controller.SkillTree == null || skillBar == null)
                return false;
            if (!controller.SkillTree.TryGetNode(nodeId, out var node) ||
                node.UnlockAction != SkillTreeUnlockAction.UnlockSkill ||
                node.SkillToUnlock == null)
                return false;

            var started = skillBar.BeginAssignSkill(node.SkillToUnlock);
            if (started)
                Render();
            return started;
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        private void SetNodeHovered(string nodeId, bool hovered)
        {
            if (tooltipRoot == null || tooltipText == null)
                return;
            if (!hovered || controller == null || controller.SkillTree == null ||
                !controller.SkillTree.TryGetNode(nodeId, out var node))
            {
                tooltipRoot.SetActive(false);
                return;
            }

            var rank = controller.Progress.GetRank(nodeId);
            var effects = SkillTreeNodeView.BuildEffectText(node, rank);
            tooltipText.text = $"{node.DisplayName}\n{node.NodeType}  Rank {rank}/{node.MaxRank}\n\n" +
                               $"{node.Description}\n\n{effects}";
            tooltipRoot.SetActive(true);
        }

        private void OnGoldChanged(int gold)
        {
            Render();
        }

        private void SetVisible(bool visible)
        {
            if (windowGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            windowGroup.alpha = visible ? 1f : 0f;
            windowGroup.interactable = visible;
            windowGroup.blocksRaycasts = visible;
            if (visible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (visible)
                Render();
        }

        private bool IsVisible => windowGroup == null ? gameObject.activeSelf : windowGroup.alpha > 0.01f;
    }
}
