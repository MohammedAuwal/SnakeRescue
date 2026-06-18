using System.Collections;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Data;
using SnakeRescue.Managers;
using SnakeRescue.Utils;

namespace SnakeRescue.Systems
{
    /// <summary>
    /// Calculates and delivers star ratings at level end.
    ///
    /// Stars are based on:
    /// - Time taken vs par time
    /// - Actions used vs par actions
    /// - Bonus: chain reaction length
    ///
    /// Stars are displayed one by one with delay
    /// for satisfying UI animation timing.
    ///
    /// This system does NOT store stars.
    /// That is SaveSystem's job.
    /// This system only calculates and announces them.
    /// </summary>
    public class StarRatingSystem : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static StarRatingSystem Instance { get; private set; }

        // ─── Settings ─────────────────────────────────────────
        [Header("Display Settings")]
        [SerializeField] private float _starRevealDelay    = 0.4f;
        [SerializeField] private float _starRevealInterval = 0.3f;

        // ─── Events ───────────────────────────────────────────
        public static event System.Action<int>  OnStarsCalculated;
        public static event System.Action<int>  OnStarRevealed;
        public static event System.Action       OnAllStarsRevealed;

        // ─── Runtime ──────────────────────────────────────────
        private int       _lastCalculatedStars = 0;
        private Coroutine _revealCoroutine;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
        }

        // ─── Calculation ──────────────────────────────────────

        public int CalculateStars(int     levelIndex,
                                  float   timeTaken,
                                  int     actionsTaken,
                                  int     chainSteps = 0)
        {
            LevelConfig config =
                GameConfig.Instance?.GetLevelConfig(levelIndex);

            int stars = 1; // Minimum for completing

            if (config != null)
            {
                stars = CalculateWithConfig(
                    config, timeTaken, actionsTaken);
            }
            else
            {
                stars = CalculateFallback(timeTaken, actionsTaken);
            }

            // Chain reaction bonus
            // Long chain can upgrade from 2 to 3 stars
            if (stars == 2 && chainSteps >= 5)
            {
                stars = 3;
                Debug.Log(
                    "[StarRating] Chain reaction bonus: upgraded to 3 stars.");
            }

            stars = Mathf.Clamp(stars, 1, Constants.MAX_STARS_PER_LEVEL);

            _lastCalculatedStars = stars;

            LogStarBreakdown(
                levelIndex, timeTaken, actionsTaken, chainSteps, stars);

            return stars;
        }

        private int CalculateWithConfig(LevelConfig config,
                                        float       timeTaken,
                                        int         actionsTaken)
        {
            bool timePerfect   = timeTaken    <= config.ParTime3Stars;
            bool actionPerfect = actionsTaken <= config.ParActions3Stars;
            bool timeGood      = timeTaken    <= config.ParTime2Stars;
            bool actionGood    = actionsTaken <= config.ParActions2Stars;

            // 3 stars: both time AND actions perfect
            if (timePerfect && actionPerfect)
                return 3;

            // 2 stars: either time or actions good
            if (timeGood || actionGood)
                return 2;

            // 1 star: completed but not efficiently
            return 1;
        }

        private int CalculateFallback(float timeTaken, int actionsTaken)
        {
            bool timePerfect   = timeTaken    < 8f;
            bool actionPerfect = actionsTaken <= Constants.STAR_3_MAX_ACTIONS;
            bool timeGood      = timeTaken    < 15f;
            bool actionGood    = actionsTaken <= Constants.STAR_2_MAX_ACTIONS;

            if (timePerfect && actionPerfect) return 3;
            if (timeGood    || actionGood)    return 2;

            return 1;
        }

        // ─── Star Reveal Sequence ─────────────────────────────

        /// <summary>
        /// Triggers the animated star reveal.
        /// Stars appear one at a time with satisfying delay.
        /// </summary>
        public void RevealStars(int stars)
        {
            if (_revealCoroutine != null)
                StopCoroutine(_revealCoroutine);

            _revealCoroutine = StartCoroutine(RevealStarsRoutine(stars));
        }

        private IEnumerator RevealStarsRoutine(int totalStars)
        {
            // Initial delay before first star
            yield return new WaitForSecondsRealtime(_starRevealDelay);

            for (int i = 1; i <= totalStars; i++)
            {
                OnStarRevealed?.Invoke(i);
                GameEvents.TriggerStarEarned(i);
                GameEvents.TriggerPlaySFX("SFX_StarEarned");

                if (i < totalStars)
                {
                    yield return new WaitForSecondsRealtime(
                        _starRevealInterval);
                }
            }

            // All stars revealed
            yield return new WaitForSecondsRealtime(_starRevealInterval);

            OnAllStarsRevealed?.Invoke();

            Debug.Log(
                $"[StarRating] Revealed {totalStars} star(s).");
        }

        // ─── Grade Text ───────────────────────────────────────

        /// <summary>
        /// Returns a human-readable performance label.
        /// Shown on result screen alongside stars.
        /// </summary>
        public string GetGradeText(int stars)
        {
            switch (stars)
            {
                case 3:  return "Perfect!";
                case 2:  return "Well Done!";
                case 1:  return "Complete!";
                default: return "Try Again";
            }
        }

        /// <summary>
        /// Returns feedback on what could be improved.
        /// Shown below grade text to guide replay.
        /// </summary>
        public string GetImprovementHint(int     stars,
                                          float   timeTaken,
                                          int     actionsTaken,
                                          int     levelIndex)
        {
            if (stars >= 3)
                return "Flawless execution!";

            LevelConfig config =
                GameConfig.Instance?.GetLevelConfig(levelIndex);

            if (config == null)
                return "Try to be faster and use fewer actions.";

            bool timeOk   = timeTaken    <= config.ParTime2Stars;
            bool actionOk = actionsTaken <= config.ParActions2Stars;

            if (!timeOk && !actionOk)
                return "Try to act faster and use fewer moves.";

            if (!timeOk)
                return "Try to solve it faster!";

            if (!actionOk)
                return "Try to solve it in fewer moves!";

            return "So close! Try the chain reaction approach.";
        }

        // ─── Personal Best ────────────────────────────────────

        public bool IsPersonalBest(int levelIndex, int stars)
        {
            int previous = SaveSystem.Instance?.GetLevelStars(levelIndex) ?? 0;
            return stars > previous;
        }

        // ─── Event Handler ────────────────────────────────────

        private void OnLevelCompleted(LevelResult result, int rawStars)
        {
            if (result != LevelResult.Victory) return;

            if (GameManager.Instance == null) return;

            int   levelIndex  = GameManager.Instance.CurrentLevel;
            float timeTaken   = GameManager.Instance.GetLevelElapsedTime();
            int   actions     = GameManager.Instance.GetActionsCount();
            int   chainSteps  = ChainReactionSystem.Instance?.CurrentStepCount
                                ?? 0;

            int finalStars = CalculateStars(
                levelIndex, timeTaken, actions, chainSteps);

            OnStarsCalculated?.Invoke(finalStars);

            // Start visual reveal after short delay
            StartCoroutine(DelayedReveal(finalStars));
        }

        private IEnumerator DelayedReveal(int stars)
        {
            yield return new WaitForSecondsRealtime(
                Constants.UI_RESULT_DELAY);

            RevealStars(stars);
        }

        // ─── Diagnostics ──────────────────────────────────────

        private void LogStarBreakdown(int   levelIndex,
                                       float timeTaken,
                                       int   actions,
                                       int   chainSteps,
                                       int   stars)
        {
            Debug.Log(
                $"[StarRating] Level {levelIndex} | " +
                $"Time: {timeTaken:F1}s | " +
                $"Actions: {actions} | " +
                $"Chain: {chainSteps} steps | " +
                $"Stars: {stars}");
        }

        public int GetLastCalculatedStars()
            => _lastCalculatedStars;
    }
}
