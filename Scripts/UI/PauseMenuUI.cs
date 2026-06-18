using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Systems;
using SnakeRescue.Utils;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Controls the Pause Menu overlay.
    ///
    /// Appears when:
    /// - Player taps Pause button
    /// - Player presses Escape (PC)
    /// - Game loses focus (optional)
    ///
    /// Buttons:
    /// - Resume
    /// - Restart Level
    /// - Quit to Menu
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        [Header("Panel")]
        [SerializeField] private GameObject _pausePanel;

        // ─── State ────────────────────────────────────────────
        private bool _isPaused = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            SetupButtons();
            _pausePanel?.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            GameEvents.OnGamePaused       += OnGamePaused;
            GameEvents.OnGameResumed      += OnGameResumed;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            GameEvents.OnGamePaused       -= OnGamePaused;
            GameEvents.OnGameResumed      -= OnGameResumed;
        }

        // ─── Setup ────────────────────────────────────────────

        private void SetupButtons()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _restartButton?.onClick.AddListener(OnRestartClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);
        }

        // ─── Button Handlers ──────────────────────────────────

        private void OnResumeClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            GameManager.Instance?.ResumeGame();
        }

        private void OnRestartClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            GameManager.Instance?.ResumeGame(); // Unpause first
            LevelManager.Instance?.ResetLevel();
            _pausePanel?.SetActive(false);
        }

        private void OnQuitClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            GameManager.Instance?.ResumeGame(); // Unpause first
            SceneController.Instance?.LoadMainMenu();
            _pausePanel?.SetActive(false);
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Paused)
            {
                ShowPauseMenu();
            }
            else
            {
                HidePauseMenu();
            }
        }

        private void OnGamePaused()
        {
            ShowPauseMenu();
        }

        private void OnGameResumed()
        {
            HidePauseMenu();
        }

        // ─── Visual Control ───────────────────────────────────

        private void ShowPauseMenu()
        {
            _isPaused = true;
            _pausePanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        private void HidePauseMenu()
        {
            _isPaused = false;
            _pausePanel?.SetActive(false);
            Time.timeScale = 1f;
        }

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _resumeButton?.onClick.RemoveListener(OnResumeClicked);
            _restartButton?.onClick.RemoveListener(OnRestartClicked);
            _quitButton?.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
