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
        private Vector2 _inspectorScroll;
        private bool _showParents = true;
        private GUIStyle _nodeTitleStyle;
        private GUIStyle _nodeLevelStyle;
        private Sprite _nodeFrameSprite;
        private Sprite _nodeBackgroundSprite;
        private bool _dragUndoRecorded;
        private bool _showConnections = true;
        private string _status = "Select a profession to begin.";
        private MessageType _statusType = MessageType.Info;
        private SkillNodeDefinitionSO _draggedNode;
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
                if (_selectedNode != null)
                    GUILayout.Label($"EDITING: {_selectedNode.NodeId}", EditorStyles.boldLabel);
                GUILayout.Label(_profession == null ? string.Empty : $"Nodes: {_profession.SkillNodes?.Count ?? 0}", EditorStyles.miniLabel);
            }
        }

        private void DrawCanvas()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Tree layout", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Drag cards to place nodes. Select a card or a row to edit it; dependency checkboxes only assign parents.", MessageType.None);
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
            var current = Event.current;
            if ((rect.Contains(current.mousePosition) || _draggedNode == node) && current.button == 0)
            {
                if (current.type == EventType.MouseDown)
                {
                    _selectedNode = node;
                    _draggedNode = node;
                    _dragUndoRecorded = false;
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
                    if (!_dragUndoRecorded)
                    {
                        Undo.RecordObject(node, "Move Skill Tree Node");
                        _dragUndoRecorded = true;
                    }
                    var serialized = new SerializedObject(node);
                    var uiPositionProperty = serialized.FindProperty("uiPosition");
                    if (uiPositionProperty != null)
                    {
                        uiPositionProperty.vector2Value = EverrealmSkillTreeLayout.ToAuthoredPosition(canvasPosition, metrics);
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(node);
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

            // Match the game's compact portrait card: icon, display name, then status.
            var border = selected ? new Color(1f, .55f, .08f) : new Color(.75f, .78f, .78f);
            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f),
                new Color(.035f, .045f, .045f));
            DrawSlicedSprite(rect, _nodeBackgroundSprite, new Color(.14f, .2f, .25f));
            DrawSlicedSprite(rect, _nodeFrameSprite, selected ? new Color(1f, .66f, .19f) : Color.white);
            if (node.Icon != null)
            {
                var sprite = node.Icon;
                var uv = sprite.textureRect;
                uv.x /= sprite.texture.width;
                uv.width /= sprite.texture.width;
                uv.y /= sprite.texture.height;
                uv.height /= sprite.texture.height;
                float iconSize = rect.width * .71f;
                GUI.DrawTextureWithTexCoords(new Rect(rect.center.x - iconSize * .5f, rect.y + 6f,
                    iconSize, iconSize), sprite.texture, uv);
            }
            string title = node.AbilityToGrant != null ? node.AbilityToGrant.DisplayName : node.DisplayName;
            var previousContentColor = GUI.contentColor;
            GUI.contentColor = new Color(.96f, .94f, .87f, 1f);
            GUI.Label(new Rect(rect.x + 3f, rect.y + rect.height * .71f, rect.width - 6f, rect.height * .19f),
                new GUIContent(title, $"{node.NodeId} | Price {node.Price} | X {node.UiPosition.x:0.##} Y {node.UiPosition.y:0.##}"),
                _nodeTitleStyle);
            GUI.contentColor = new Color(1f, .6f, .1f);
            GUI.Label(new Rect(rect.x + 3f, rect.y + rect.height * .88f, rect.width - 6f, rect.height * .12f),
                $"LEVEL {node.RequiredLevel}", _nodeLevelStyle);
            GUI.contentColor = previousContentColor;
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
                    Handles.color = child == _selectedNode ? new Color(1f, .65f, .1f) : new Color(.25f, .65f, .85f, .85f);
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

        // Draw the prefab's sliced UI sprites in IMGUI without stretching the corner artwork.
        private static void DrawSlicedSprite(Rect destination, Sprite sprite, Color tint)
        {
            if (sprite == null) return;
            Rect source = sprite.textureRect;
            Vector4 border = sprite.border;
            float scale = EverrealmSkillTreeLayout.PreviewScale;
            float[] sx = { source.xMin, source.xMin + border.x, source.xMax - border.z, source.xMax };
            float[] sy = { source.yMin, source.yMin + border.y, source.yMax - border.w, source.yMax };
            float[] dx = { destination.xMin, destination.xMin + border.x * scale,
                destination.xMax - border.z * scale, destination.xMax };
            float[] dy = { destination.yMax, destination.yMax - border.y * scale,
                destination.yMin + border.w * scale, destination.yMin };
            var previous = GUI.color;
            GUI.color = tint;
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                if (sx[x + 1] <= sx[x] || sy[y + 1] <= sy[y]) continue;
                var uv = new Rect(sx[x] / sprite.texture.width, sy[y] / sprite.texture.height,
                    (sx[x + 1] - sx[x]) / sprite.texture.width, (sy[y + 1] - sy[y]) / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(new Rect(dx[x], dy[y + 1], dx[x + 1] - dx[x], dy[y] - dy[y + 1]),
                    sprite.texture, uv);
            }
            GUI.color = previous;
        }

        private void DrawInspector()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(310f), GUILayout.ExpandHeight(true)))
            {
                using var inspectorScroll = new EditorGUILayout.ScrollViewScope(_inspectorScroll);
                _inspectorScroll = inspectorScroll.scrollPosition;
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
                if (_selectedNode.AbilityToGrant != null)
                    EditorGUILayout.HelpBox(LetterHunter.Skills.SkillDescription.Build(_selectedNode.AbilityToGrant), MessageType.None);
                if (GUILayout.Button("All Skills — Effects and Types")) SkillCatalogWindow.Open();
                EditorGUILayout.LabelField("Granted skill", _selectedNode.AbilityToGrant != null
                    ? $"{_selectedNode.AbilityToGrant.DisplayName} ({_selectedNode.AbilityToGrant.SkillId})"
                    : "Not assigned", EditorStyles.boldLabel);
                DrawSerialized(serialized, "requiredLevel", "Required level");
                DrawSerialized(serialized, "price", "Price");
                DrawSerialized(serialized, "uiPosition", "Layout position");
                if (serialized.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(_selectedNode);
                    Repaint();
                }

                _showParents = EditorGUILayout.Foldout(_showParents, "Required parent nodes", true);
                if (_showParents) DrawParentSelector();
                if (GUILayout.Button("Sync Icon From Skill") && _selectedNode.AbilityToGrant != null)
                {
                    serialized.Update();
                    serialized.FindProperty("icon").objectReferenceValue = _selectedNode.AbilityToGrant.Icon;
                    serialized.ApplyModifiedProperties();
                }
                if (GUILayout.Button("Focus selected node")) FocusSelectedNode();
                if (GUILayout.Button("Remove Node From Tree")) RemoveSelectedNode();
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
                    var previousColor = GUI.backgroundColor;
                    if (node == _selectedNode) GUI.backgroundColor = new Color(1f, .55f, .08f);
                    bool clicked = GUILayout.Button(new GUIContent(node.NodeId, title), GUI.skin.button);
                    GUI.backgroundColor = previousColor;
                    if (clicked)
                    {
                        _selectedNode = node;
                        Repaint();
                    }
                    EditorGUILayout.LabelField($"({node.UiPosition.x:0.###}, {node.UiPosition.y:0.###})", EditorStyles.miniLabel,
                        GUILayout.Width(95f));
                    int index = _profession.SkillNodes.ToList().IndexOf(node);
                    using (new EditorGUI.DisabledScope(index == 0))
                        if (GUILayout.Button("▲", GUILayout.Width(24f))) MoveNode(index, -1);
                    using (new EditorGUI.DisabledScope(index == _profession.SkillNodes.Count - 1))
                        if (GUILayout.Button("▼", GUILayout.Width(24f))) MoveNode(index, 1);
                }
            }
        }

        private void MoveNode(int index, int direction)
        {
            var serialized = new SerializedObject(_profession);
            serialized.FindProperty("skillNodes").MoveArrayElement(index, index + direction);
            serialized.ApplyModifiedProperties();
            Repaint();
        }

        private void RemoveSelectedNode()
        {
            if (!EditorUtility.DisplayDialog("Remove skill node",
                    $"Remove '{_selectedNode.NodeId}' from this profession and its parent references? The node asset is kept.",
                    "Remove", "Cancel")) return;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Skill Tree Node");
            foreach (var node in _profession.SkillNodes.Where(node => node != null && node != _selectedNode))
            {
                var serialized = new SerializedObject(node);
                var parents = serialized.FindProperty("parentNodes");
                for (int i = parents.arraySize - 1; i >= 0; i--)
                    if (parents.GetArrayElementAtIndex(i).objectReferenceValue == _selectedNode)
                    {
                        parents.GetArrayElementAtIndex(i).objectReferenceValue = null;
                        parents.DeleteArrayElementAtIndex(i);
                    }
                serialized.ApplyModifiedProperties();
            }
            var definition = new SerializedObject(_profession);
            var nodes = definition.FindProperty("skillNodes");
            for (int i = nodes.arraySize - 1; i >= 0; i--)
                if (nodes.GetArrayElementAtIndex(i).objectReferenceValue == _selectedNode)
                {
                    nodes.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    nodes.DeleteArrayElementAtIndex(i);
                }
            definition.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(undoGroup);
            _selectedNode = FirstNode();
            SetStatus("Node removed from this tree. Its asset was kept.", MessageType.Info);
        }

        private void DrawParentSelector()
        {
            foreach (var candidate in _profession.SkillNodes.Where(node => node != null && node != _selectedNode))
            {
                bool assigned = _selectedNode.ParentNodes.Contains(candidate);
                bool next = EditorGUILayout.ToggleLeft($"{candidate.NodeId} ({candidate.DisplayName})", assigned);
                if (next == assigned) continue;
                var parents = _selectedNode.ParentNodes.ToList();
                if (next) parents.Add(candidate);
                else parents.RemoveAll(parent => parent == candidate);
                var serialized = new SerializedObject(_selectedNode);
                var property = serialized.FindProperty("parentNodes");
                property.arraySize = parents.Count;
                for (int i = 0; i < parents.Count; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = parents[i];
                serialized.ApplyModifiedProperties();
                Repaint();
            }
        }

        private void FocusSelectedNode()
        {
            var metrics = EverrealmSkillTreeLayout.CalculateEditorMetrics(_profession.SkillNodes, _canvasRect.width, position.height);
            var point = EverrealmSkillTreeLayout.ToEditorPosition(_selectedNode.UiPosition, _canvasRect, metrics);
            _canvasScroll = Vector2.Max(Vector2.zero, point - _canvasRect.position -
                new Vector2(Mathf.Max(100f, position.width - 350f), Mathf.Max(100f, position.height - 160f)) * .5f);
            Repaint();
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
            var nodeData = new SerializedObject(node);
            string baseId = System.IO.Path.GetFileNameWithoutExtension(path);
            string id = baseId;
            int suffix = 1;
            while (_profession.SkillNodes.Any(item => item != null && item.NodeId == id)) id = $"{baseId}_{suffix++}";
            nodeData.FindProperty("nodeId").stringValue = id;
            nodeData.FindProperty("displayName").stringValue = baseId;
            float nextX = _profession.SkillNodes.Where(item => item != null)
                .Select(item => item.UiPosition.x).DefaultIfEmpty(EverrealmSkillTreeLayout.LegacyMinimumX - EverrealmSkillTreeLayout.LegacyXStep).Max()
                + EverrealmSkillTreeLayout.LegacyXStep;
            nodeData.FindProperty("uiPosition").vector2Value = new Vector2(nextX, EverrealmSkillTreeLayout.LegacyMaximumY);
            nodeData.ApplyModifiedPropertiesWithoutUndo();
            var serialized = new SerializedObject(_profession);
            var nodes = serialized.FindProperty("skillNodes");
            nodes.arraySize++;
            nodes.GetArrayElementAtIndex(nodes.arraySize - 1).objectReferenceValue = node;
            serialized.ApplyModifiedProperties();
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
            var visited = new HashSet<SkillNodeDefinitionSO>();
            foreach (var node in nodes)
                if (HasCycle(node, new HashSet<SkillNodeDefinitionSO>(), visited))
                {
                    errors.Add("Circular parent dependencies found.");
                    break;
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

        private static bool HasCycle(SkillNodeDefinitionSO node, HashSet<SkillNodeDefinitionSO> visiting,
            HashSet<SkillNodeDefinitionSO> visited)
        {
            if (node == null || visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            foreach (var parent in node.ParentNodes)
                if (HasCycle(parent, visiting, visited)) return true;
            visiting.Remove(node);
            visited.Add(node);
            return false;
        }

        private void OnEnable() => Undo.undoRedoPerformed += Repaint;

        private SkillNodeDefinitionSO FirstNode() =>
            _profession?.SkillNodes?.FirstOrDefault(node => node != null);

        private void EnsureStyles()
        {
            if (_nodeTitleStyle != null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/EnglishKingdomSkillTree/SkillTreeNode.prefab");
            if (prefab != null)
            {
                var visual = prefab.GetComponent<LetterHunter.UI.SkillTree.EverrealmSkillTreeNodeVisual>();
                if (visual != null)
                {
                    var data = new SerializedObject(visual);
                    _nodeFrameSprite = (data.FindProperty("_frame").objectReferenceValue as UnityEngine.UI.Image)?.sprite;
                    _nodeBackgroundSprite = (data.FindProperty("_background").objectReferenceValue as UnityEngine.UI.Image)?.sprite;
                }
            }
            _nodeTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 10,
                clipping = TextClipping.Clip
            };
            SetNodeTextColors(_nodeTitleStyle, new Color(.96f, .94f, .87f, 1f));

            _nodeLevelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                fontSize = 9,
                clipping = TextClipping.Clip
            };
            SetNodeTextColors(_nodeLevelStyle, new Color(1f, .64f, .16f, 1f));
        }

        private static void SetNodeTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            _nodeTitleStyle = null;
            _nodeLevelStyle = null;
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
