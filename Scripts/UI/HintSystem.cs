using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Managers;
using SnakeRescue.Data;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Manages the Hint Popup UI specifically.
    ///
    /// Decoupled from GameUI so hints can be triggered
    /// by multiple sources (button, auto-fail, event).
    ///
    /// Queries ProgressManager for hint availability.
    /// Queries LevelConfig for hint content.
    /// </summary>
    public class HintSystem : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject _hintPanel;
        [SerializeField] private Text       _hintText;
        [SerializeField] private Button     _closeButton;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.3f;

        // ─── State ────────────────────────────────────────────
        private bool _isShowing = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            SetupButtons();
            _hintPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnHintRequested += OnHintRequested;
            GameEvents.OnLevelStarted  += OnLevelStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnHintRequested -= OnHintRequested;
            GameEvents.OnLevelStarted  -= OnLevelStarted;
        }

        // ─── Setup ────────────────────────────────────────────

        private void SetupButtons()
        {
            _closeButton?.onClick.AddListener(HideHint);
        }

        // ─── Event Handlers ───────────────────────────────────

        private void OnHintRequested()
        {
            ShowHint();
        }

        private void OnLevelStarted(int levelIndex)
        {
            HideHint();
        }

        // ─── Hint Logic ───────────────────────────────────────

        public void ShowHint()
        {
            if (_isShowing) return;

            int levelIndex = GameManager.Instance?.CurrentLevel ?? 0;

            // Check if hint is available
            if (!(ProgressManager.Instance?.ShouldShowHint(levelIndex) ?? false))
            {
                // Show "Keep Trying" message instead
                ShowGenericMessage("Keep trying! Observe the chain reaction.");
                return;
            }

            // Get actual hint
            string hint = ProgressManager.Instance?.GetCurrentHint(levelIndex) ?? "No hint available.";
            ShowSpecificHint(hint);
        }

        private void ShowSpecificHint(string text)
        {
            if (_hintText != null)
                _hintText.text = text;

            _hintPanel?.SetActive(true);
            _isShowing = true;

            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

            // Auto-hide after 10 seconds
            Invoke(nameof(HideHint), 10f);
        }

        private void ShowGenericMessage(string text)
        {
            if (_hintText != null)
                _hintText.text = text;

            _hintPanel?.SetActive(true);
            _isShowing = true;

            Invoke(nameof(HideHint), 3f);
        }

        public void HideHint()
        {
            CancelInvoke();
            _hintPanel?.SetActive(false);
            _isShowing = false;
        }

        // ─── Public API for GameUI ────────────────────────────

        /// <summary>
        /// Called by GameUI when hint button is pressed.
        /// </summary>
        public void OnHintButtonPressed()
        {
            ShowHint();
        }

        public bool IsShowing()
            => _isShowing;

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _closeButton?.onClick.RemoveListener(HideHint);
        }
    }
}
