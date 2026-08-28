using System;
using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class EverrealmSkillTreeNodeVisual : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private RawImage _icon;
        [SerializeField] private Image _frame;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject _selectedOutline;
        [SerializeField] private Text _label;
        [SerializeField] private Text _stateLabel;
        private SkillNodeDefinitionSO _node;
        private Action<SkillNodeDefinitionSO> _select;

        public void Bind(SkillNodeDefinitionSO node, Action<SkillNodeDefinitionSO> select)
        {
            _node = node;
            _select = select;
            _button?.onClick.RemoveAllListeners();
            _button?.onClick.AddListener(() => _select?.Invoke(_node));
        }

        public void Render(SkillTreeNodeState state, bool selected)
        {
            if (_node == null) return;
            _label.text = _node.AbilityToGrant != null ? _node.AbilityToGrant.DisplayName : _node.DisplayName;
            _icon.texture = _node.Icon != null ? _node.Icon.texture : null;
            _icon.enabled = _icon.texture != null;
            _stateLabel.text = state == SkillTreeNodeState.Purchased ? "PURCHASED" : state == SkillTreeNodeState.Available ? "AVAILABLE" : $"LEVEL {_node.RequiredLevel}";
            _lockedOverlay?.SetActive(state == SkillTreeNodeState.Locked);
            _selectedOutline?.SetActive(selected);
            _frame.color = selected ? new Color(1f, .66f, .19f) : Color.white;
            _background.color = state == SkillTreeNodeState.Purchased ? new Color(.17f, .42f, .28f) : new Color(.14f, .2f, .25f);
        }
    }
}
