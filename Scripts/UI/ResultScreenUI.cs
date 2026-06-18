using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Systems;
using SnakeRescue.Managers;
using SnakeRescue.Utils;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Displays level completion or failure results.
    ///
    /// Shows:
    /// - Victory/Failure Message
    /// - Star Rating (animated)
    /// - Time Taken
    /// - Actions Used
    /// - Grade Text (Perfect, Well Done, etc.)
    ///
    /// Buttons:
    /// - Next Level (Victory only)
    /// - Retry
    /// - Level Select
    /// </summary>
    public class ResultScreenUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private GameObject _failurePanel;

        [Header("Stats")]
        [SerializeField] private Text _timeText;
        [SerializeField] private Text _actionsText;
        [SerializeField] private Text _gradeText;

        [Header("Stars")]
        [SerializeField] private Image _star1;
        [SerializeField] private Image _star2;
        [SerializeField] private Image _star3;
        [SerializeField] private GameObject _starContainer;

        [Header("Buttons")]
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _levelSelectButton;

        // ─── Runtime ──────────────────────────────────────────
        private int _currentStars = 0;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            SetupButtons();
            gameObject.SetActive(false);
            _starContainer?.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnLevelFailed    += OnLevelFailed;
            StarRatingSystem.OnStarRevealed += OnStarRevealed;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnLevelFailed    -= OnLevelFailed;
            StarRatingSystem.OnStarRevealed -= OnStarRevealed;
        }

        // ─── Setup ────────────────────────────────────────────

        private void SetupButtons()
        {
            _nextLevelButton?.onClick.AddListener(OnNextLevelClicked);
            _retryButton?.onClick.AddListener(OnRetryClicked);
            _levelSelectButton?.onClick.AddListener(OnLevelSelectClicked);
        }

        // ─── Event Handlers ───────────────────────────────────

        private void OnLevelCompleted(LevelResult result, int stars)
        {
            if (result != LevelResult.Victory) return;

            _currentStars = stars;
            ShowVictory();
            UpdateStats();
            ResetStars();

            // Trigger star animation via StarRatingSystem
            StarRatingSystem.Instance?.RevealStars(_currentStars);
        }

        private void OnLevelFailed(LevelResult reason)
        {
            ShowFailure();
            UpdateStats();
            _starContainer?.SetActive(false);
        }

        private void OnStarRevealed(int starIndex)
        {
            if (starIndex >= 1 && _star1 != null) _star1.enabled = true;
            if (starIndex >= 2 && _star2 != null) _star2.enabled = true;
            if (starIndex >= 3 && _star3 != null) _star3.enabled = true;
        }

        // ─── Display Logic ────────────────────────────────────

        private void ShowVictory()
        {
            _victoryPanel?.SetActive(true);
            _failurePanel?.SetActive(false);
            _starContainer?.SetActive(true);
            _nextLevelButton?.gameObject.SetActive(true);

            if (_gradeText != null)
            {
                _gradeText.text = StarRatingSystem.Instance?.GetGradeText(_currentStars) ?? "Well Done!";
                _gradeText.color = Color.yellow;
            }

            gameObject.SetActive(true);
        }

        private void ShowFailure()
        {
            _victoryPanel?.SetActive(false);
            _failurePanel?.SetActive(true);
            _starContainer?.SetActive(false);
            _nextLevelButton?.gameObject.SetActive(false);

            if (_gradeText != null)
            {
                _gradeText.text = "Try Again";
                _gradeText.color = Color.red;
            }

            gameObject.SetActive(true);
        }

        private void UpdateStats()
        {
            if (LevelManager.Instance == null) return;

            if (_timeText != null)
            {
                float time = LevelManager.Instance.ElapsedTime;
                _timeText.text = $"Time: {time:F1}s";
            }

            if (_actionsText != null)
            {
                int actions = LevelManager.Instance.ActionsTaken;
                _actionsText.text = $"Moves: {actions}";
            }
        }

        private void ResetStars()
        {
            if (_star1 != null) _star1.enabled = false;
            if (_star2 != null) _star2.enabled = false;
            if (_star3 != null) _star3.enabled = false;
        }

        // ─── Button Handlers ──────────────────────────────────

        private void OnNextLevelClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            SceneController.Instance?.LoadNextLevel();
        }

        private void OnRetryClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            LevelManager.Instance?.ResetLevel();
            gameObject.SetActive(false);
        }

        private void OnLevelSelectClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            SceneController.Instance?.LoadLevelSelect();
        }

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _nextLevelButton?.onClick.RemoveListener(OnNextLevelClicked);
            _retryButton?.onClick.RemoveListener(OnRetryClicked);
            _levelSelectButton?.onClick.RemoveListener(OnLevelSelectClicked);
        }
    }
}
