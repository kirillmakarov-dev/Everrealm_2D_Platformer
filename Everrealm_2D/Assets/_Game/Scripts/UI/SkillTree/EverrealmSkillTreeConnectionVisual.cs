using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class EverrealmSkillTreeConnectionVisual : MonoBehaviour
    {
        [SerializeField] private Image _shadow;
        [SerializeField] private Image _line;
        [SerializeField] private Image _directionMarker;

        public void Render(Vector2 start, Vector2 end, SkillTreeNodeState state)
        {
            var rect = (RectTransform)transform;
            var direction = end - start;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = start;
            rect.sizeDelta = new Vector2(direction.magnitude, 12f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var color = state == SkillTreeNodeState.Purchased ? new Color(.3f, .9f, .45f) : new Color(.38f, .65f, .82f);
            _shadow.color = new Color(.015f, .02f, .025f, .96f);
            _line.color = color;
            _directionMarker.color = color;
        }
    }
}
