using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Systems;
using SnakeRescue.Managers;
using SnakeRescue.Utils;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Controls the Main Menu screen.
    ///
    /// Buttons:
    /// - Play (Loads Level Select)
    /// - Settings (Opens settings panel)
    /// - Quit (Exits game)
    ///
    /// Displays:
    /// - Game Title
    /// - Total Stars Collected
    /// - Version Number
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Text")]
        [SerializeField] private Text _starsText;
        [SerializeField] private Text _versionText;

        [Header("Panels")]
        [SerializeField] private GameObject _settingsPanel;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            SetupButtons();
            UpdateDisplay();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        // ─── Setup ────────────────────────────────────────────

        private void SetupButtons()
        {
            _playButton?.onClick.AddListener(OnPlayClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);
        }

        // ─── Button Handlers ──────────────────────────────────

        private void OnPlayClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            SceneController.Instance?.LoadLevelSelect();
        }

        private void OnSettingsClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            _settingsPanel?.SetActive(true);
        }

        private void OnQuitClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CloseSettings()
        {
            _settingsPanel?.SetActive(false);
        }

        // ─── Display Updates ──────────────────────────────────

        private void UpdateDisplay()
        {
            if (_starsText != null)
            {
                int stars = ProgressManager.Instance?.GetTotalStars() ?? 0;
                _starsText.text = $"Stars: {stars}";
            }

            if (_versionText != null)
            {
                string version = GameConfig.Instance?.Version ?? "1.0.0";
                _versionText.text = $"v{version}";
            }
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            // Ensure menu is only active in MainMenu state
            gameObject.SetActive(state == GameState.MainMenu);
        }

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _playButton?.onClick.RemoveListener(OnPlayClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsClicked);
            _quitButton?.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
