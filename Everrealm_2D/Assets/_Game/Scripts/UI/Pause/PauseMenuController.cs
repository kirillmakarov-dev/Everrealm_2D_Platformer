using LetterHunter.UI.SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetterHunter.UI.Pause
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main menu")]
        [SerializeField] private Button exitButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;

        [Header("Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeValueText;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button backButton;

        private EverrealmSkillTreeVisual _skillTreeVisual;

        private bool _isOpen;
        private float _timeScaleBeforePause = 1f;
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockMode;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (windowGroup == null)
                windowGroup = GetComponent<CanvasGroup>();
            _skillTreeVisual = FindFirstObjectByType<EverrealmSkillTreeVisual>();

            exitButton?.onClick.AddListener(ExitGame);
            restartButton?.onClick.AddListener(RestartLevel);
            settingsButton?.onClick.AddListener(OpenSettings);
            backButton?.onClick.AddListener(ShowMainMenu);
            masterVolumeSlider?.onValueChanged.AddListener(SetMasterVolume);
            fullscreenToggle?.onValueChanged.AddListener(SetFullscreen);

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            UpdateVolumeLabel(AudioListener.volume);
            SetWindowVisible(false);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return;

            if (_isOpen)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                    ShowMainMenu();
                else
                    Close();
                return;
            }

            // The skill tree owns Escape while it is visible. This component runs first,
            // so one key press closes that window without also opening the pause menu.
            if (_skillTreeVisual != null && _skillTreeVisual.IsOpen)
                return;

            Open();
        }

        private void OnDisable()
        {
            if (_isOpen)
                RestoreGameState();
        }

        public void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            _cursorWasVisible = Cursor.visible;
            _cursorLockMode = Cursor.lockState;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SetWindowVisible(true);
            ShowMainMenu();
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            RestoreGameState();
            SetWindowVisible(false);
        }

        public void RestartLevel()
        {
            RestoreGameState();
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else
                SceneManager.LoadScene(scene.name);
        }

        public void ExitGame()
        {
            RestoreGameState();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OpenSettings()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            Select(masterVolumeSlider != null ? masterVolumeSlider.gameObject : backButton?.gameObject);
        }

        public void ShowMainMenu()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            Select(restartButton != null ? restartButton.gameObject : settingsButton?.gameObject);
        }

        public void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            UpdateVolumeLabel(AudioListener.volume);
        }

        public void SetFullscreen(bool value) => Screen.fullScreen = value;

        private void RestoreGameState()
        {
            Time.timeScale = _timeScaleBeforePause;
            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _cursorLockMode;
            _isOpen = false;
        }

        private void SetWindowVisible(bool visible)
        {
            if (windowGroup == null)
                return;

            windowGroup.alpha = visible ? 1f : 0f;
            windowGroup.interactable = visible;
            windowGroup.blocksRaycasts = visible;
        }

        private void UpdateVolumeLabel(float value)
        {
            if (masterVolumeValueText != null)
                masterVolumeValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
