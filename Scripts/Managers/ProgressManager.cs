using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Data;
using SnakeRescue.Systems;
using SnakeRescue.Utils;

namespace SnakeRescue.Managers
{
    /// <summary>
    /// Single access point for all player progress queries.
    ///
    /// Other systems should never talk to SaveSystem directly.
    /// They ask ProgressManager, which coordinates with SaveSystem.
    ///
    /// Responsibilities:
    /// - Level unlock logic
    /// - Star rating computation
    /// - Progress statistics
    /// - Cosmetic unlock triggers
    /// </summary>
    public class ProgressManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static ProgressManager Instance { get; private set; }

        // ─── Runtime Tracking ─────────────────────────────────
        private int   _currentLevelFailCount  = 0;
        private float _sessionStartTime;

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

            _sessionStartTime = Time.time;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelStarted   += OnLevelStarted;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnLevelFailed    += OnLevelFailed;
            GameEvents.OnLevelReset     += OnLevelReset;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted   -= OnLevelStarted;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnLevelFailed    -= OnLevelFailed;
            GameEvents.OnLevelReset     -= OnLevelReset;
        }

        private void OnApplicationQuit()
        {
            float sessionTime = Time.time - _sessionStartTime;
            SaveSystem.Instance?.AddPlayTime(sessionTime);
        }

        // ─── Level Queries ────────────────────────────────────

        public bool IsLevelUnlocked(int levelIndex)
        {
            // Debug mode unlocks all
            if (GameConfig.Instance != null &&
                GameConfig.Instance.UnlockAllLevels)
                return true;

            if (SaveSystem.Instance == null) return levelIndex == 0;
            return SaveSystem.Instance.IsLevelUnlocked(levelIndex);
        }

        public int GetLevelStars(int levelIndex)
        {
            if (SaveSystem.Instance == null) return 0;
            return SaveSystem.Instance.GetLevelStars(levelIndex);
        }

        public LevelRecord GetLevelRecord(int levelIndex)
        {
            if (SaveSystem.Instance == null) return null;
            return SaveSystem.Instance.GetLevelRecord(levelIndex);
        }

        public bool IsLevelCompleted(int levelIndex)
        {
            LevelRecord record = GetLevelRecord(levelIndex);
            return record?.IsCompleted ?? false;
        }

        public int GetTotalStars()
        {
            if (SaveSystem.Instance == null) return 0;
            return SaveSystem.Instance.GetTotalStars();
        }

        public int GetTotalLevelsCompleted()
        {
            if (SaveSystem.Instance?.CurrentData == null) return 0;
            return SaveSystem.Instance.CurrentData.TotalLevelsCompleted;
        }

        // ─── Star Calculation ─────────────────────────────────

        public int CalculateStars(int levelIndex, float timeTaken, int actions)
        {
            LevelConfig config =
                GameConfig.Instance?.GetLevelConfig(levelIndex);

            if (config == null)
            {
                // Fallback to constants
                return CalculateStarsFallback(timeTaken, actions);
            }

            bool perfect =
                timeTaken <= config.ParTime3Stars &&
                actions   <= config.ParActions3Stars;

            bool good =
                timeTaken <= config.ParTime2Stars &&
                actions   <= config.ParActions2Stars;

            if (perfect) return 3;
            if (good)    return 2;

            return 1;
        }

        private int CalculateStarsFallback(float timeTaken, int actions)
        {
            if (actions <= Constants.STAR_3_MAX_ACTIONS &&
                timeTaken < 8f)  return 3;

            if (actions <= Constants.STAR_2_MAX_ACTIONS ||
                timeTaken < 15f) return 2;

            return 1;
        }

        // ─── Hint System ──────────────────────────────────────

        public bool ShouldShowHint(int levelIndex)
        {
            LevelConfig config =
                GameConfig.Instance?.GetLevelConfig(levelIndex);

            if (config == null)
                return _currentLevelFailCount >= Constants.HINT_UNLOCK_FAILS;

            return config.ShouldShowHint(_currentLevelFailCount);
        }

        public string GetCurrentHint(int levelIndex)
        {
            LevelConfig config =
                GameConfig.Instance?.GetLevelConfig(levelIndex);

            if (config == null)
                return "Think carefully about the chain reaction.";

            return config.GetHint(_currentLevelFailCount);
        }

        public int GetCurrentFailCount()
            => _currentLevelFailCount;

        // ─── Cosmetic Unlocks ─────────────────────────────────

        public void CheckAndGrantCosmeticUnlocks()
        {
            if (SaveSystem.Instance?.CurrentData == null) return;

            PlayerData data = SaveSystem.Instance.CurrentData;
            int totalStars  = data.TotalStars;

            if (GameConfig.Instance?.AvailableSkins == null) return;

            foreach (SkinDefinition skin in GameConfig.Instance.AvailableSkins)
            {
                if (skin.IsPremium) continue;

                if (totalStars >= skin.StarsRequired)
                {
                    bool newUnlock = data.UnlockSkin(skin.SkinID);
                    if (newUnlock)
                    {
                        Debug.Log(
                            $"[ProgressManager] Unlocked skin: {skin.DisplayName}");
                    }
                }
            }
        }

        // ─── Statistics ───────────────────────────────────────

        public float GetCompletionPercentage()
        {
            int total     = Constants.TOTAL_MVP_LEVELS;
            int completed = GetTotalLevelsCompleted();

            if (total == 0) return 0f;
            return (float)completed / total * 100f;
        }

        public int GetMaxPossibleStars()
            => Constants.TOTAL_MVP_LEVELS * Constants.MAX_STARS_PER_LEVEL;

        public string GetFormattedPlayTime()
        {
            if (SaveSystem.Instance?.CurrentData == null) return "0m";

            float seconds = SaveSystem.Instance.CurrentData.TotalPlayTimeSeconds;
            int minutes   = Mathf.FloorToInt(seconds / 60f);
            int hours     = Mathf.FloorToInt(minutes / 60f);

            if (hours > 0)
                return $"{hours}h {minutes % 60}m";

            return $"{minutes}m";
        }

        // ─── Event Handlers ───────────────────────────────────

        private void OnLevelStarted(int levelIndex)
        {
            _currentLevelFailCount = 0;
        }

        private void OnLevelCompleted(LevelResult result, int stars)
        {
            if (GameManager.Instance == null) return;

            int   levelIndex = GameManager.Instance.CurrentLevel;
            float timeTaken  = GameManager.Instance.GetLevelElapsedTime();
            int   actions    = GameManager.Instance.GetActionsCount();

            // Recalculate stars through ProgressManager
            int calculatedStars = CalculateStars(levelIndex, timeTaken, actions);

            SaveSystem.Instance?.RecordLevelComplete(
                levelIndex, calculatedStars, timeTaken, actions);

            CheckAndGrantCosmeticUnlocks();

            // Publish typed event
            EventManager.Publish(new LevelCompletedEvent
            {
                LevelIndex    = levelIndex,
                StarsEarned   = calculatedStars,
                TimeTaken     = timeTaken,
                ActionsTaken  = actions
            });

            EventManager.Publish(new StarEarnedEvent
            {
                LevelIndex = levelIndex,
                Stars      = calculatedStars,
                TotalStars = GetTotalStars()
            });
        }

        private void OnLevelFailed(LevelResult reason)
        {
            _currentLevelFailCount++;

            SaveSystem.Instance?.RecordLevelAttempt(
                GameManager.Instance?.CurrentLevel ?? 0);

            EventManager.Publish(new LevelFailedEvent
            {
                LevelIndex = GameManager.Instance?.CurrentLevel ?? 0,
                FailReason = reason.ToString()
            });
        }

        private void OnLevelReset()
        {
            // Don't reset fail count on manual reset
            // Only reset on new level start
        }
    }
}
