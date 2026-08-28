using System.Collections.Generic;
using System.Linq;
using LetterHunter.SkillTree;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.EditorTools
{
    /// <summary>
    /// Source-style authoring tool for skill trees.
    /// It edits only definitions and layout; purchase, currency and save remain runtime responsibilities.
    /// </summary>
    public sealed class SkillTreeEditorWindow : EditorWindow
    {
        private const string DefaultProfessionPath = "Assets/_Game/Data/Professions/Warrior/WarriorProfession.asset";

        private ProfessionDefinitionSO _profession;
        private SkillNodeDefinitionSO _selectedNode;
        private Vector2 _canvasScroll;
        private bool _showConnections = true;
        private string _status = "Select a profession to begin.";
        private MessageType _statusType = MessageType.Info;
        private GUIStyle _nodeStyle;
        private GUIStyle _selectedNodeStyle;
        private SkillNodeDefinitionSO _draggedNode;
        private Vector2 _lastMousePosition;
        private Vector2 _dragOffset;
        private Rect _canvasRect;

        [MenuItem("Everrealm/Skill Tree/Editor")]
        public static void Open()
        {
            var window = GetWindow<SkillTreeEditorWindow>("Everrealm Skill Tree Editor");
            window._profession = AssetDatabase.LoadAssetAtPath<ProfessionDefinitionSO>(DefaultProfessionPath);
            window._selectedNode = window.FirstNode();
            window.SetStatus(window._profession == null ? "Select a profession to begin." : "Warrior profession loaded.", MessageType.Info);
        }

        [MenuItem("CONTEXT/ProfessionDefinitionSO/Edit Skill Tree")]
        private static void OpenFromProfession(MenuCommand command)
        {
            var window = GetWindow<SkillTreeEditorWindow>("Everrealm Skill Tree Editor");
            window._profession = command.context as ProfessionDefinitionSO;
            window._selectedNode = window.FirstNode();
            window.Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (_profession == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a ProfessionDefinitionSO or create a new profession to edit its skill tree.",
                    MessageType.Info);
                DrawStatus();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCanvas();
                DrawInspector();
            }

            DrawStatus();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _profession = (ProfessionDefinitionSO)EditorGUILayout.ObjectField(
                    new GUIContent("Profession"), _profession, typeof(ProfessionDefinitionSO), false,
                    GUILayout.Width(320f));
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedNode = FirstNode();
                    SetStatus(_profession == null ? "Select a profession to begin." : "Profession loaded.", MessageType.Info);
                }

                if (GUILayout.Button("New Profession", EditorStyles.toolbarButton, GUILayout.Width(105f)))
                    CreateProfession();

                using (new EditorGUI.DisabledScope(_profession == null))
                {
                    if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(75f)))
                        CreateNode();
                    if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                        ValidateProfession();
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(55f)))
                        SaveProfession();
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(_profession == null ? string.Empty : $"Nodes: {_profession.SkillNodes?.Count ?? 0}", EditorStyles.miniLabel);
            }
        }

        private void DrawCanvas()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Tree layout", EditorStyles.boldLabel);
                _showConnections = EditorGUILayout.ToggleLeft("Show prerequisite connections", _showConnections);
                _canvasScroll = EditorGUILayout.BeginScrollView(_canvasScroll, "box", GUILayout.ExpandHeight(true));

                var nodes = _profession.SkillNodes?.Where(node => node != null).ToList() ?? new List<SkillNodeDefinitionSO>();
                var canvasSize = EverrealmSkillTreeLayout.CalculateEditorCanvasSize(nodes, position.width - 330f, position.height);
                var canvasRect = GUILayoutUtility.GetRect(canvasSize.x, canvasSize.y);
                _canvasRect = canvasRect;
                var metrics = EverrealmSkillTreeLayout.CalculateEditorMetrics(nodes, canvasRect.width, position.height);
                var positions = BuildPositions(nodes, canvasRect, metrics);

                if (_showConnections)
                    DrawConnections(nodes, positions);

                foreach (var node in nodes)
                {
                    var rect = new Rect(positions[node], new Vector2(EverrealmSkillTreeLayout.NodeWidth, EverrealmSkillTreeLayout.NodeHeight));
                    rect.position -= new Vector2(EverrealmSkillTreeLayout.NodeWidth * .5f, EverrealmSkillTreeLayout.NodeHeight * .5f);
                    DrawNode(node, rect);
                }

                if (Event.current.type == EventType.MouseUp)
                    _draggedNode = null;

                EditorGUILayout.EndScrollView();
            }
        }

        private Dictionary<SkillNodeDefinitionSO, Vector2> BuildPositions(IReadOnlyList<SkillNodeDefinitionSO> nodes,
            Rect canvas, EverrealmSkillTreeLayout.Metrics metrics)
        {
            var positions = new Dictionary<SkillNodeDefinitionSO, Vector2>();
            foreach (var node in nodes)
            {
                positions[node] = EverrealmSkillTreeLayout.ToEditorPosition(node.UiPosition, canvas, metrics);
            }
            return positions;
        }

        private void DrawNode(SkillNodeDefinitionSO node, Rect rect)
        {
            var selected = node == _selectedNode;
            var style = selected ? _selectedNodeStyle : _nodeStyle;
            var current = Event.current;
            if ((rect.Contains(current.mousePosition) || _draggedNode == node) && current.button == 0)
            {
                if (current.type == EventType.MouseDown)
                {
                    _selectedNode = node;
                    _draggedNode = node;
                    _lastMousePosition = current.mousePosition;
                    _dragOffset = current.mousePosition - rect.center;
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    current.Use();
                }
                else if (current.type == EventType.MouseDrag && _draggedNode == node)
                {
                    var nodes = _profession.SkillNodes?.Where(item => item != null).ToList() ?? new List<SkillNodeDefinitionSO>();
                    var metrics = EverrealmSkillTreeLayout.CalculateEditorMetrics(nodes, _canvasRect.width, position.height);
                    var canvasPosition = current.mousePosition - _dragOffset - _canvasRect.position;
                    canvasPosition.y = _canvasRect.height - canvasPosition.y;
                    Undo.RecordObject(node, "Move Skill Tree Node");
                    var serialized = new SerializedObject(node);
                    var uiPositionProperty = serialized.FindProperty("uiPosition");
                    if (uiPositionProperty != null)
                    {
                        uiPositionProperty.vector2Value = EverrealmSkillTreeLayout.ToAuthoredPosition(canvasPosition, metrics);
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(node);
                    _lastMousePosition = current.mousePosition;
                    current.Use();
                    Repaint();
                }
                else if (current.type == EventType.MouseUp && _draggedNode == node)
                {
                    _draggedNode = null;
                    GUIUtility.hotControl = 0;
                    current.Use();
                }
            }

            GUI.Box(rect, GUIContent.none, style);

            var title = node.AbilityToGrant != null ? node.AbilityToGrant.DisplayName :
                (string.IsNullOrWhiteSpace(node.DisplayName) ? node.name : node.DisplayName);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 20f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 31f, rect.width - 16f, 18f),
                $"Lv {node.RequiredLevel}   Price {node.Price}", EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 50f, rect.width - 16f, 18f),
                node.AbilityToGrant == null ? "No skill assigned" : node.AbilityToGrant.DisplayName,
                EditorStyles.miniLabel);
        }

        private void DrawConnections(IReadOnlyList<SkillNodeDefinitionSO> nodes,
            IReadOnlyDictionary<SkillNodeDefinitionSO, Vector2> positions)
        {
            Handles.BeginGUI();
            foreach (var child in nodes)
            {
                foreach (var parent in child.ParentNodes ?? new SkillNodeDefinitionSO[0])
                {
                    if (parent == null || !positions.TryGetValue(parent, out var from) ||
                        !positions.TryGetValue(child, out var to)) continue;
                    Handles.color = new Color(.25f, .65f, .85f, .85f);
                    Handles.DrawAAPolyLine(3f, from, to);
                    var direction = (to - from).normalized;
                    var tip = to - direction * 12f;
                    Handles.DrawAAConvexPolygon(tip,
                        tip - direction * 12f + Vector2.Perpendicular(direction) * 5f,
                        tip - direction * 12f - Vector2.Perpendicular(direction) * 5f);
                }
            }
            Handles.EndGUI();
        }

        private void DrawInspector()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(310f), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Profession", EditorStyles.boldLabel);
                DrawProperty(_profession, "professionId", "Id");
                DrawProperty(_profession, "displayName", "Display name");
                DrawProperty(_profession, "description", "Description");
                DrawProperty(_profession, "icon", "Icon");

                DrawProfessionNodeList();

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Selected skill node", EditorStyles.boldLabel);
                if (_selectedNode == null)
                {
                    EditorGUILayout.HelpBox("Select a node on the canvas.", MessageType.Info);
                    return;
                }

                var serialized = new SerializedObject(_selectedNode);
                serialized.Update();
                DrawSerialized(serialized, "nodeId", "Node id");
                DrawSerialized(serialized, "displayName", "Display name");
                DrawSerialized(serialized, "description", "Description");
                DrawSerialized(serialized, "icon", "Icon");
                DrawSerialized(serialized, "abilityToGrant", "Skill");
                EditorGUILayout.LabelField("Granted skill", _selectedNode.AbilityToGrant != null
                    ? $"{_selectedNode.AbilityToGrant.DisplayName} ({_selectedNode.AbilityToGrant.SkillId})"
                    : "Not assigned", EditorStyles.boldLabel);
                DrawSerialized(serialized, "requiredLevel", "Required level");
                DrawSerialized(serialized, "price", "Price");
                DrawSerialized(serialized, "parentNodes", "Parent skills");
                DrawSerialized(serialized, "uiPosition", "Layout position");
                if (serialized.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(_selectedNode);
                    Repaint();
                }

                if (GUILayout.Button("Focus selected node"))
                    _canvasScroll = Vector2.zero;
            }
        }

        private void DrawProfessionNodeList()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"Profession nodes ({_profession.SkillNodes?.Count ?? 0})", EditorStyles.boldLabel);
            foreach (var node in _profession.SkillNodes?.Where(item => item != null) ?? Enumerable.Empty<SkillNodeDefinitionSO>())
            {
                string title = node.AbilityToGrant != null ? node.AbilityToGrant.DisplayName : node.DisplayName;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(title, node == _selectedNode ? EditorStyles.toolbarButton : GUI.skin.button))
                    {
                        _selectedNode = node;
                        Repaint();
                    }
                    EditorGUILayout.LabelField($"({node.UiPosition.x:0.###}, {node.UiPosition.y:0.###})", EditorStyles.miniLabel,
                        GUILayout.Width(95f));
                }
            }
        }

        private static void DrawProperty(Object target, string propertyName, string label)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            DrawSerialized(serialized, propertyName, label);
            if (serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(target);
        }

        private static void DrawSerialized(SerializedObject serialized, string propertyName, string label)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private void CreateProfession()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create profession", "Profession", "asset", "Choose a location");
            if (string.IsNullOrEmpty(path)) return;
            var profession = CreateInstance<ProfessionDefinitionSO>();
            AssetDatabase.CreateAsset(profession, path);
            AssetDatabase.SaveAssets();
            _profession = profession;
            _selectedNode = null;
            SetStatus("Profession created.", MessageType.Info);
        }

        private void CreateNode()
        {
            var folder = AssetDatabase.GetAssetPath(_profession);
            folder = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var path = EditorUtility.SaveFilePanelInProject("Create skill node", "SkillNode", "asset", "Choose a location", folder);
            if (string.IsNullOrEmpty(path)) return;

            var node = CreateInstance<SkillNodeDefinitionSO>();
            AssetDatabase.CreateAsset(node, path);
            var serialized = new SerializedObject(_profession);
            var nodes = serialized.FindProperty("skillNodes");
            nodes.arraySize++;
            nodes.GetArrayElementAtIndex(nodes.arraySize - 1).objectReferenceValue = node;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_profession);
            AssetDatabase.SaveAssets();
            _selectedNode = node;
            SetStatus("Node created.", MessageType.Info);
        }

        private void ValidateProfession()
        {
            var errors = new List<string>();
            var nodes = _profession.SkillNodes?.Where(node => node != null).ToList() ?? new List<SkillNodeDefinitionSO>();
            var ids = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node.NodeId)) errors.Add($"{node.name}: missing node id");
                else if (!ids.Add(node.NodeId)) errors.Add($"Duplicate node id: {node.NodeId}");
                if (node.AbilityToGrant == null) errors.Add($"{node.NodeId}: skill is not assigned");
                foreach (var parent in node.ParentNodes ?? new SkillNodeDefinitionSO[0])
                    if (parent != null && !nodes.Contains(parent)) errors.Add($"{node.NodeId}: parent is outside this profession");
            }

            if (errors.Count == 0)
            {
                SetStatus("Validation passed.", MessageType.Info);
                EditorUtility.DisplayDialog("Skill Tree", "Validation passed.", "OK");
            }
            else
            {
                SetStatus(string.Join(" | ", errors), MessageType.Error);
                EditorUtility.DisplayDialog("Skill Tree validation", string.Join("\n", errors), "OK");
            }
        }

        private void SaveProfession()
        {
            EditorUtility.SetDirty(_profession);
            foreach (var node in _profession.SkillNodes ?? new SkillNodeDefinitionSO[0])
                if (node != null) EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets();
            SetStatus("Changes saved.", MessageType.Info);
        }

        private SkillNodeDefinitionSO FirstNode() =>
            _profession?.SkillNodes?.FirstOrDefault(node => node != null);

        private void EnsureStyles()
        {
            if (_nodeStyle != null) return;
            _nodeStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(8, 8, 8, 8) };
            _nodeStyle.normal.background = MakeTexture(new Color(.14f, .2f, .25f, 1f));
            _selectedNodeStyle = new GUIStyle(_nodeStyle);
            _selectedNodeStyle.normal.background = MakeTexture(new Color(.7f, .34f, .08f, 1f));
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrWhiteSpace(_status))
                EditorGUILayout.HelpBox(_status, _statusType);
        }

        private void SetStatus(string status, MessageType type)
        {
            _status = status;
            _statusType = type;
            Repaint();
        }
    }
}
