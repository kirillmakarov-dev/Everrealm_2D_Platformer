using System.Collections.Generic;
using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class EverrealmSkillTreeVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform _nodeLayer;
        [SerializeField] private RectTransform _connectionLayer;
        [SerializeField] private ScrollRect _treeScrollRect;
        [SerializeField] private Text _balanceText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _detailsText;
        [SerializeField] private Text _professionNameText;
        [SerializeField] private Text _professionDescriptionText;
        [SerializeField] private RawImage _professionIcon;
        [SerializeField] private RawImage _selectedIcon;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _buyButton;
        [SerializeField] private CanvasGroup _windowGroup;

        [Header("Everrealm integration")]
        [SerializeField] private PlayerSkillTreeController _controller;
        [SerializeField] private GameObject _nodePrefab;
        [SerializeField] private GameObject _connectionPrefab;
        [SerializeField] private KeyCode toggleKey = KeyCode.K;
        [Header("Skill Tree Zoom")]
        [SerializeField, Min(.1f)] private float _minZoom = .75f;
        [SerializeField, Min(.1f)] private float _maxZoom = 2f;
        [SerializeField, Min(.01f)] private float _zoomStep = .1f;

        private readonly Dictionary<string, EverrealmSkillTreeNodeVisual> _nodes = new();
        private readonly List<EverrealmSkillTreeConnectionVisual> _connections = new();
        private readonly Dictionary<SkillNodeDefinitionSO, Vector2> _layoutPositions = new();
        private SkillNodeDefinitionSO _selected;
        private bool _isOpen;
        private float _zoom = 1f;

        private void OnValidate()
        {
            _nodeLayer ??= FindChild<RectTransform>("NodeLayer");
            _connectionLayer ??= FindChild<RectTransform>("ConnectionLayer");
            _treeScrollRect ??= FindFirst<ScrollRect>();
            _balanceText ??= FindChild<Text>("Balance");
            _levelText ??= FindChild<Text>("Level");
            _detailsText ??= FindChild<Text>("DetailsText");
            _professionNameText ??= FindChild<Text>("ProfessionName");
            _professionDescriptionText ??= FindChild<Text>("ProfessionDescription");
            _professionIcon ??= FindChild<RawImage>("ProfessionIcon");
            _selectedIcon ??= FindChild<RawImage>("SelectedIcon");
            _closeButton ??= FindChild<Button>("CloseButton");
            _buyButton ??= FindChild<Button>("BuyButton");
            _windowGroup ??= GetComponent<CanvasGroup>();
            _nodePrefab ??= Resources.Load<GameObject>("EnglishKingdomSkillTree/SkillTreeNode");
            _connectionPrefab ??= Resources.Load<GameObject>("EnglishKingdomSkillTree/SkillTreeConnection");
#if UNITY_EDITOR
            if (!UnityEditor.EditorUtility.IsPersistent(gameObject))
                _controller ??= FindFirstObjectByType<PlayerSkillTreeController>(FindObjectsInactive.Include);
#endif
        }

        private T FindChild<T>(string objectName) where T : Component
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == objectName && child.TryGetComponent<T>(out var component))
                    return component;
            return null;
        }

        private T FindFirst<T>() where T : Component
        {
            foreach (var component in GetComponentsInChildren<T>(true))
                return component;
            return null;
        }

        private void Awake()
        {
            ResolveRuntimeReferences();
            _windowGroup ??= GetComponent<CanvasGroup>();
            if (_windowGroup == null)
                _windowGroup = gameObject.AddComponent<CanvasGroup>();
            if (_controller == null)
            {
                var player = FindFirstObjectByType<LetterHunter.Characters.PlayerClassController>();
                _controller = player != null ? player.GetComponent<PlayerSkillTreeController>() : null;
                _controller ??= FindFirstObjectByType<PlayerSkillTreeController>();
            }
            _nodePrefab ??= Resources.Load<GameObject>("EnglishKingdomSkillTree/SkillTreeNode");
            _connectionPrefab ??= Resources.Load<GameObject>("EnglishKingdomSkillTree/SkillTreeConnection");
            ConfigureTreeScroll();
            _closeButton?.onClick.AddListener(Close);
            _buyButton?.onClick.AddListener(BuySelected);
            if (_controller != null) _controller.StateChanged += Refresh;
            _isOpen = false;
            ApplyVisibility();
        }

        private void ResolveRuntimeReferences()
        {
            _nodeLayer ??= FindChild<RectTransform>("NodeLayer");
            _connectionLayer ??= FindChild<RectTransform>("ConnectionLayer");
            _treeScrollRect ??= FindFirst<ScrollRect>();
            _balanceText ??= FindChild<Text>("Balance");
            _levelText ??= FindChild<Text>("Level");
            _detailsText ??= FindChild<Text>("DetailsText");
            _professionNameText ??= FindChild<Text>("ProfessionName");
            _professionDescriptionText ??= FindChild<Text>("ProfessionDescription");
            _professionIcon ??= FindChild<RawImage>("ProfessionIcon");
            _selectedIcon ??= FindChild<RawImage>("SelectedIcon");
            _closeButton ??= FindChild<Button>("CloseButton");
            _buyButton ??= FindChild<Button>("BuyButton");
            _windowGroup ??= GetComponent<CanvasGroup>();
            _nodePrefab ??= Resources.Load<GameObject>("EnglishKingdomSkillTree/SkillTreeNode");
            _connectionPrefab ??= Resources.Load<GameObject>("EnglishKingdomSkillTree/SkillTreeConnection");
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.StateChanged -= Refresh;
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                if (_isOpen) Close();
                else Open();
            }
            else if (_isOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
            if (_isOpen) HandleZoomInput();
        }

        private void HandleZoomInput()
        {
            if (_treeScrollRect == null || _treeScrollRect.content == null ||
                _treeScrollRect.viewport == null || Mouse.current == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(_treeScrollRect.viewport,
                    Mouse.current.position.ReadValue(), null)) return;
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(wheel, 0f)) return;
            float min = Mathf.Min(_minZoom, _maxZoom);
            float max = Mathf.Max(_minZoom, _maxZoom);
            float next = Mathf.Clamp(_zoom + (wheel > 0f ? _zoomStep : -_zoomStep), min, max);
            if (Mathf.Approximately(next, _zoom)) return;
            _zoom = next;
            _treeScrollRect.content.localScale = Vector3.one * _zoom;
            Canvas.ForceUpdateCanvases();
            CenterTreeScroll();
        }

        private void ConfigureTreeScroll()
        {
            if (_treeScrollRect == null) return;
            _treeScrollRect.horizontal = true;
            _treeScrollRect.vertical = true;
            _treeScrollRect.inertia = true;
            _treeScrollRect.movementType = ScrollRect.MovementType.Elastic;
            _treeScrollRect.decelerationRate = .12f;
            // Wheel input is reserved for zoom; pointer dragging still pans the tree.
            _treeScrollRect.scrollSensitivity = 0f;
        }

        private bool WasTogglePressed()
        {
            if (Keyboard.current == null) return false;
            return toggleKey == KeyCode.K && Keyboard.current.kKey.wasPressedThisFrame;
        }

        public void Open()
        {
            _isOpen = true;
            _zoom = 1f;
            ApplyVisibility();
            Canvas.ForceUpdateCanvases();
            Refresh();
            Canvas.ForceUpdateCanvases();
            CenterTreeScroll();
        }

        public void Close()
        {
            _isOpen = false;
            ApplyVisibility();
        }

        private void Refresh()
        {
            if (!_isOpen) return;
            ResolveRuntimeReferences();
            if (_controller?.ActiveProfession == null)
            {
                Debug.LogError("Everrealm Skill Tree has no active profession on the player's controller.", this);
                return;
            }
            if (_nodeLayer == null || _connectionLayer == null || _nodePrefab == null || _connectionPrefab == null)
            {
                Debug.LogError("Everrealm Skill Tree visual references are incomplete; node rendering was skipped.", this);
                return;
            }
            var profession = _controller.ActiveProfession;
            _balanceText.text = $"COINS  {_controller.CurrentCoins:N0}";
            _levelText.text = $"LEVEL  {_controller.CurrentLevel}";
            _professionNameText.text = profession.DisplayName;
            _professionDescriptionText.text = profession.Description;
            SetTexture(_professionIcon, profession.Icon);

            if (_selected == null || !profession.Contains(_selected))
                _selected = FirstNode(profession);

            ClearRuntimeViews();
            var nodes = new List<SkillNodeDefinitionSO>(profession.SkillNodes);
            var viewport = _treeScrollRect != null && _treeScrollRect.viewport != null
                ? _treeScrollRect.viewport.rect.size : new Vector2(900f, 650f);
            var metrics = EverrealmSkillTreeLayout.Calculate(nodes, viewport.x, viewport.y);
            if (_treeScrollRect != null && _treeScrollRect.content != null)
            {
                _treeScrollRect.content.sizeDelta = metrics.ContentSize;
                ConfigureLayer(_nodeLayer);
                ConfigureLayer(_connectionLayer);
            }
            var positions = BuildLayout(profession, metrics);
            _layoutPositions.Clear();
            foreach (var pair in positions) _layoutPositions[pair.Key] = pair.Value;
            foreach (var node in profession.SkillNodes)
            {
                if (node == null || _nodePrefab == null) continue;
                var viewObject = Instantiate(_nodePrefab, _nodeLayer, false);
                var view = viewObject.GetComponent<EverrealmSkillTreeNodeVisual>();
                if (view == null)
                {
                    Debug.LogError($"Everrealm node prefab '{_nodePrefab.name}' has no EverrealmSkillTreeNodeVisual component.", _nodePrefab);
                    Destroy(viewObject);
                    continue;
                }
                view.Bind(node, SelectNode);
                var nodeRect = viewObject.GetComponent<RectTransform>();
                nodeRect.anchorMin = nodeRect.anchorMax = Vector2.zero;
                nodeRect.pivot = new Vector2(.5f, .5f);
                nodeRect.anchoredPosition = positions[node];
                view.Render(_controller.Service.GetState(node), node == _selected);
                _nodes[node.NodeId] = view;
            }

            foreach (var child in profession.SkillNodes)
            foreach (var parent in child?.ParentNodes ?? System.Array.Empty<SkillNodeDefinitionSO>())
            {
                if (parent == null || !_nodes.TryGetValue(parent.NodeId, out var parentView) ||
                    !_nodes.TryGetValue(child.NodeId, out var childView) || _connectionPrefab == null) continue;
                var lineObject = Instantiate(_connectionPrefab, _connectionLayer, false);
                var line = lineObject.GetComponent<EverrealmSkillTreeConnectionVisual>();
                if (line == null)
                {
                    Debug.LogError($"Everrealm connection prefab '{_connectionPrefab.name}' has no EverrealmSkillTreeConnectionVisual component.", _connectionPrefab);
                    Destroy(lineObject);
                    continue;
                }
                line.Render(parentView.GetComponent<RectTransform>().anchoredPosition,
                    childView.GetComponent<RectTransform>().anchoredPosition,
                    _controller.Service.GetState(child));
                _connections.Add(line);
            }
            RefreshDetails();
        }

        private Dictionary<SkillNodeDefinitionSO, Vector2> BuildLayout(ProfessionDefinitionSO profession,
            EverrealmSkillTreeLayout.Metrics metrics)
        {
            var result = new Dictionary<SkillNodeDefinitionSO, Vector2>();
            foreach (var node in profession.SkillNodes)
                if (node != null) result[node] = EverrealmSkillTreeLayout.ToCanvasPosition(node.UiPosition, metrics);
            return result;
        }

        private void CenterTreeScroll()
        {
            if (_treeScrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            if (_treeScrollRect.content != null)
                _treeScrollRect.content.localScale = Vector3.one * _zoom;
            if (_treeScrollRect.content == null || _layoutPositions.Count == 0)
                return;
            Vector2 halfNode = new(EverrealmSkillTreeLayout.RuntimeNodeWidth * .5f,
                EverrealmSkillTreeLayout.RuntimeNodeHeight * .5f);
            Vector2 minimum = new(float.MaxValue, float.MaxValue);
            Vector2 maximum = new(float.MinValue, float.MinValue);
            foreach (var position in _layoutPositions.Values)
            {
                minimum = Vector2.Min(minimum, position - halfNode);
                maximum = Vector2.Max(maximum, position + halfNode);
            }
            Vector2 target = (minimum + maximum) * .5f * _zoom;
            Vector2 contentSize = _treeScrollRect.content.rect.size * _zoom;
            Vector2 viewportSize = _treeScrollRect.viewport.rect.size;
            Vector2 desired = viewportSize * .5f - target;
            float minX = Mathf.Min(0f, viewportSize.x - contentSize.x);
            float minY = Mathf.Min(0f, viewportSize.y - contentSize.y);
            _treeScrollRect.content.anchoredPosition = new Vector2(
                Mathf.Clamp(desired.x, minX, 0f),
                Mathf.Clamp(desired.y, minY, 0f));
        }

        private static void ConfigureLayer(RectTransform layer)
        {
            if (layer == null) return;
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.pivot = Vector2.zero;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
        }

        private void RefreshDetails()
        {
            if (_selected == null) return;
            SetTexture(_selectedIcon, _selected.Icon);
            var grantedSkill = _selected.AbilityToGrant;
            var grantedName = grantedSkill != null ? grantedSkill.DisplayName : "Not assigned";
            _detailsText.text = $"<b>{_selected.DisplayName}</b>\nSkill: {grantedName}\n\n{_selected.Description}\n\n" +
                $"Required level: {_selected.RequiredLevel}\nPrice: {_selected.Price} coins\nStatus: {_controller.Service.GetState(_selected)}";
            _buyButton.interactable = _controller.Service.GetState(_selected) == SkillTreeNodeState.Available;
        }

        private void SelectNode(SkillNodeDefinitionSO node) { _selected = node; Refresh(); }

        private void BuySelected()
        {
            if (_selected != null) _controller.TryPurchase(_selected);
            Refresh();
        }

        private void ClearRuntimeViews()
        {
            foreach (var node in _nodes.Values) if (node != null) Destroy(node.gameObject);
            foreach (var connection in _connections) if (connection != null) Destroy(connection.gameObject);
            _nodes.Clear();
            _connections.Clear();
        }

        private void ApplyVisibility()
        {
            if (_windowGroup == null) return;
            _windowGroup.alpha = _isOpen ? 1f : 0f;
            _windowGroup.interactable = _isOpen;
            _windowGroup.blocksRaycasts = _isOpen;
        }

        private static SkillNodeDefinitionSO FirstNode(ProfessionDefinitionSO profession)
        {
            foreach (var node in profession.SkillNodes) if (node != null) return node;
            return null;
        }

        private static void SetTexture(RawImage image, Sprite sprite)
        {
            if (image == null) return;
            image.texture = sprite != null ? sprite.texture : null;
            image.enabled = image.texture != null;
        }
    }

}
