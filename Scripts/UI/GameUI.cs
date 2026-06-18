using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Levels;
using SnakeRescue.Managers;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Heads-Up Display during gameplay.
    ///
    /// Shows:
    /// - Level Name
    /// - Timer
    /// - Action Count
    /// - Pause Button
    /// - Hint Button (contextual)
    ///
    /// Hides automatically when level ends or pauses.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Info")]
        [SerializeField] private Text _levelNameText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _actionsText;

        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _hintButton;
        [SerializeField] private Button _resetButton;

        [Header("Hint Panel")]
        [SerializeField] private GameObject _hintPanel;
        [SerializeField] private Text       _hintText;

        // ─── Runtime ──────────────────────────────────────────
        private bool _isPaused = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            SetupButtons();
            _hintPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            GameEvents.OnLevelStarted     += OnLevelStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            GameEvents.OnLevelStarted     -= OnLevelStarted;
        }

        private void Update()
        {
            if (!_isPaused && LevelManager.Instance != null && LevelManager.Instance.LevelActive)
            {
                UpdateTimer();
                UpdateActions();
            }
        }

        // ─── Setup ────────────────────────────────────────────

        private void SetupButtons()
        {
            _pauseButton?.onClick.AddListener(OnPauseClicked);
            _hintButton?.onClick.AddListener(OnHintClicked);
            _resetButton?.onClick.AddListener(OnResetClicked);
        }

        // ─── Updates ──────────────────────────────────────────

        private void UpdateTimer()
        {
            if (_timerText == null) return;

            float time = LevelManager.Instance.ElapsedTime;
            _timerText.text = $"{time:F1}s";
        }

        private void UpdateActions()
        {
            if (_actionsText == null) return;

            int actions = LevelManager.Instance.ActionsTaken;
            _actionsText.text = $"Moves: {actions}";
        }

        // ─── Button Handlers ──────────────────────────────────

        private void OnPauseClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

            if (_isPaused)
                GameManager.Instance?.ResumeGame();
            else
                GameManager.Instance?.PauseGame();

            _isPaused = !_isPaused;
        }

        private void OnHintClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            GameEvents.TriggerHintRequested();

            ShowHint();
        }

        private void OnResetClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            LevelManager.Instance?.ResetLevel();
        }

        // ─── Hint System ──────────────────────────────────────

        private void ShowHint()
        {
            if (LevelManager.Instance == null) return;

            if (LevelManager.Instance.ShouldShowHint())
            {
                string hint = LevelManager.Instance.GetCurrentHint();
                _hintText.text = hint;
                _hintPanel?.SetActive(true);
            }
            else
            {
                _hintText.text = "Keep trying!";
                _hintPanel?.SetActive(true);
            }

            // Hide hint after 5 seconds
            Invoke(nameof(HideHint), 5f);
        }

        private void HideHint()
        {
            _hintPanel?.SetActive(false);
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            bool showHUD = (state == GameState.Playing || state == GameState.ChainReacting);
            gameObject.SetActive(showHUD);

            if (state == GameState.Paused)
                _isPaused = true;
            else
                _isPaused = false;
        }

        private void OnLevelStarted(int levelIndex)
        {
            LevelConfig config = GameConfig.Instance?.GetLevelConfig(levelIndex);
            if (config != null && _levelNameText != null)
            {
                _levelNameText.text = config.LevelName;
            }

            _hintPanel?.SetActive(false);
        }

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _pauseButton?.onClick.RemoveListener(OnPauseClicked);
            _hintButton?.onClick.RemoveListener(OnHintClicked);
            _resetButton?.onClick.RemoveListener(OnResetClicked);
        }
    }
}
