using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class VitalsOrbView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private string label;
        [SerializeField] private bool showNumbers = true;

        public void Render(float current, float max)
        {
            var safeMax = Mathf.Max(1f, max);
            var ratio = Mathf.Clamp01(current / safeMax);

            if (fill != null)
                fill.fillAmount = ratio;

            if (valueText != null)
            {
                valueText.enabled = showNumbers;
                if (showNumbers)
                    valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(safeMax)}";
            }

            if (labelText != null)
                labelText.text = label;
        }
    }
}
