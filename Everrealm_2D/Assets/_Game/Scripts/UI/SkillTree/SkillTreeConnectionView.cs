using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeConnectionView : MonoBehaviour
    {
        [SerializeField] private RectTransform lineRoot;
        [SerializeField] private Image shadow;
        [SerializeField] private Image foreground;
        [SerializeField] private Image directionMarker;
        [Min(3f), SerializeField] private float thickness = 9f;
        [SerializeField] private Color lockedColor = new(0.22f, 0.23f, 0.25f, 0.9f);
        [SerializeField] private Color availableColor = new(0.25f, 0.65f, 0.8f, 1f);
        [SerializeField] private Color purchasedColor = new(0.95f, 0.62f, 0.18f, 1f);

        public void Render(Vector2 start, Vector2 end, SkillTreeNodeState state)
        {
            if (lineRoot == null) lineRoot = transform as RectTransform;
            if (lineRoot == null) return;
            gameObject.SetActive(true);
            var direction = end - start;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineRoot.anchoredPosition = (start + end) * 0.5f;
            lineRoot.sizeDelta = new Vector2(direction.magnitude, thickness + 8f);
            lineRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (shadow != null)
            {
                shadow.rectTransform.sizeDelta = new Vector2(direction.magnitude, thickness + 6f);
                shadow.color = new Color(0f, 0f, 0f, 0.82f);
            }
            if (foreground != null)
            {
                foreground.rectTransform.sizeDelta = new Vector2(direction.magnitude, thickness);
                foreground.color = state switch
                {
                    SkillTreeNodeState.Purchased => purchasedColor,
                    SkillTreeNodeState.Available => availableColor,
                    _ => lockedColor
                };
            }
            if (directionMarker != null)
            {
                directionMarker.rectTransform.anchoredPosition = new Vector2(direction.magnitude * 0.5f - 8f, 0f);
                directionMarker.color = foreground != null ? foreground.color : lockedColor;
            }
        }
    }
}
