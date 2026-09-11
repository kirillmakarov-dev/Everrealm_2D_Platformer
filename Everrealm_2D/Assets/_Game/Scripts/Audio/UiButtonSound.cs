using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LetterHunter.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
    {
        private Button _button;
        private SoundManager _soundManager;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _soundManager = FindFirstObjectByType<SoundManager>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
                _soundManager?.PlayButtonHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
                _soundManager?.PlayButtonClick();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_button != null && _button.interactable)
                _soundManager?.PlayButtonClick();
        }
    }
}
