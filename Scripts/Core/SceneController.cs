using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SnakeRescue.Utils;

namespace SnakeRescue.Core
{
    /// <summary>
    /// Handles all scene loading and transitions.
    /// Uses async loading with a loading screen.
    /// No other script should call SceneManager directly.
    /// Always go through SceneController.
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static SceneController Instance { get; private set; }

        // ─── Settings ─────────────────────────────────────────
        [Header("Transition Settings")]
        [SerializeField] private float _minLoadingTime  = 1.0f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _fadeInDuration  = 0.3f;

        // ─── State ────────────────────────────────────────────
        public bool IsLoading { get; private set; } = false;

        // ─── Events ───────────────────────────────────────────
        public static event System.Action<float> OnLoadingProgress;
        public static event System.Action         OnFadeOutStarted;
        public static event System.Action         OnFadeInStarted;
        public static event System.Action         OnSceneReady;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ─── Public Scene Load Methods ────────────────────────

        public void LoadMainMenu()
        {
            LoadScene(Constants.SCENE_MAIN_MENU);
        }

        public void LoadLevelSelect()
        {
            LoadScene(Constants.SCENE_LEVEL_SELECT);
        }

        public void LoadGameScene(int levelIndex)
        {
            // Store the level index before loading
            GameManager.Instance?.StartLevel(levelIndex);
            LoadScene(Constants.SCENE_GAME);
        }

        public void ReloadCurrentScene()
        {
            string current = SceneManager.GetActiveScene().name;
            LoadScene(current);
        }

        public void LoadNextLevel()
        {
            if (GameManager.Instance == null) return;

            int nextLevel = GameManager.Instance.CurrentLevel + 1;

            if (nextLevel >= Constants.TOTAL_MVP_LEVELS)
            {
                // No more levels — go back to level select
                LoadLevelSelect();
                return;
            }

            LoadGameScene(nextLevel);
        }

        // ─── Core Load Logic ──────────────────────────────────

        private void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning(
                    $"[SceneController] Already loading. Ignored: {sceneName}");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;

            // Step 1 — Fade out
            OnFadeOutStarted?.Invoke();
            yield return new WaitForSecondsRealtime(_fadeOutDuration);

            // Step 2 — Clear events before loading new scene
            GameEvents.ClearAllEvents();

            // Step 3 — Start async load
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            float elapsedTime  = 0f;
            float reportedProgress = 0f;

            while (!operation.isDone)
            {
                elapsedTime += Time.unscaledDeltaTime;

                // Unity async load goes to 0.9 then waits for allowSceneActivation
                float rawProgress = Mathf.Clamp01(operation.progress / 0.9f);

                // Make sure we show at least _minLoadingTime worth of progress
                float timeProgress = Mathf.Clamp01(elapsedTime / _minLoadingTime);
                reportedProgress   = Mathf.Min(rawProgress, timeProgress);

                OnLoadingProgress?.Invoke(reportedProgress);

                if (rawProgress >= 1f && elapsedTime >= _minLoadingTime)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            // Step 4 — Scene is loaded
            OnLoadingProgress?.Invoke(1f);

            // Step 5 — Fade in
            OnFadeInStarted?.Invoke();
            yield return new WaitForSecondsRealtime(_fadeInDuration);

            IsLoading = false;
            OnSceneReady?.Invoke();

            Debug.Log($"[SceneController] Loaded: {sceneName}");
        }

        // ─── Utility ──────────────────────────────────────────

        public string GetCurrentSceneName()
            => SceneManager.GetActiveScene().name;

        public bool IsCurrentScene(string sceneName)
            => GetCurrentSceneName() == sceneName;
    }
}
