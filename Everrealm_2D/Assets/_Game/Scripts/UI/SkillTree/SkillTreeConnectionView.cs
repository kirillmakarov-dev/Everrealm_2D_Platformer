using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeConnectionView : MonoBehaviour
    {
        [SerializeField] private RectTransform line;
        [SerializeField] private Image image;
        [Min(1f), SerializeField] private float thickness = 6f;
        [SerializeField] private Color lockedColor = new(0.25f, 0.27f, 0.32f, 0.9f);
        [SerializeField] private Color unlockedColor = new(0.3f, 0.8f, 0.42f, 0.95f);

        public void Render(Vector2 start, Vector2 end, bool unlocked)
        {
            if (line == null)
                line = transform as RectTransform;
            if (image == null)
                image = GetComponent<Image>();
            if (line == null)
                return;

            var direction = end - start;
            line.anchoredPosition = (start + end) * 0.5f;
            line.sizeDelta = new Vector2(direction.magnitude, thickness);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (image != null)
                image.color = unlocked ? unlockedColor : lockedColor;
        }
    }
}
