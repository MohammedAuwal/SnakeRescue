using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Utils;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Displays the loading overlay during scene transitions.
    ///
    /// Listens to SceneController events:
    /// - OnFadeOutStarted
    /// - OnLoadingProgress
    /// - OnFadeInStarted
    ///
    /// Shows:
    /// - Loading Bar
    /// - Percentage Text
    /// - Tip Text (Optional)
    /// </summary>
    public class LoadingScreenUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("UI")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private Slider     _progressSlider;
        [SerializeField] private Text       _progressText;
        [SerializeField] private Text       _tipText;

        [Header("Tips")]
        [SerializeField] private string[] _loadingTips;

        // ─── State ────────────────────────────────────────────
        private bool _isLoading = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _loadingPanel?.SetActive(false);
            ShowRandomTip();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        // ─── Event Subscription ───────────────────────────────

        private void SubscribeToEvents()
        {
            SceneController.OnFadeOutStarted  += OnFadeOutStarted;
            SceneController.OnLoadingProgress += OnLoadingProgress;
            SceneController.OnFadeInStarted   += OnFadeInStarted;
            SceneController.OnSceneReady      += OnSceneReady;
        }

        private void UnsubscribeFromEvents()
        {
            SceneController.OnFadeOutStarted  -= OnFadeOutStarted;
            SceneController.OnLoadingProgress -= OnLoadingProgress;
            SceneController.OnFadeInStarted   -= OnFadeInStarted;
            SceneController.OnSceneReady      -= OnSceneReady;
        }

        // ─── Event Handlers ───────────────────────────────────

        private void OnFadeOutStarted()
        {
            _loadingPanel?.SetActive(true);
            _isLoading = true;
            UpdateProgress(0f);
        }

        private void OnLoadingProgress(float progress)
        {
            UpdateProgress(progress);
        }

        private void OnFadeInStarted()
        {
            _isLoading = false;
            // Keep panel visible during fade in
        }

        private void OnSceneReady()
        {
            _loadingPanel?.SetActive(false);
        }

        // ─── UI Updates ───────────────────────────────────────

        private void UpdateProgress(float progress)
        {
            if (_progressSlider != null)
                _progressSlider.value = progress;

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void ShowRandomTip()
        {
            if (_tipText == null || _loadingTips == null || _loadingTips.Length == 0)
                return;

            string tip = _loadingTips[Random.Range(0, _loadingTips.Length)];
            _tipText.text = $"Tip: {tip}";
        }

        // ─── Public API ───────────────────────────────────────

        public void SetCustomTip(string tip)
        {
            if (_tipText != null)
                _tipText.text = tip;
        }
    }
}
