using UnityEngine;
using System.Collections.Generic;
using SnakeRescue.Data;

namespace SnakeRescue.Data
{
    /// <summary>
    /// Global game configuration.
    /// Single ScriptableObject that lives in Resources folder.
    /// Loaded once at startup — never changes at runtime.
    ///
    /// Create via:
    /// Right click in Project → Create → SnakeRescue → GameConfig
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "SnakeRescue/GameConfig",
        order = 0)]
    public class GameConfig : ScriptableObject
    {
        // ─── Singleton Access ─────────────────────────────────
        private static GameConfig _instance;

        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");

                    if (_instance == null)
                    {
                        Debug.LogError(
                            "[GameConfig] GameConfig not found in Resources folder. " +
                            "Create one via: Create → SnakeRescue → GameConfig");
                    }
                }
                return _instance;
            }
        }

        // ─── Game Identity ────────────────────────────────────
        [Header("Game Identity")]
        public string GameName    = "Snake Rescue";
        public string Version     = "1.0.0";
        public string BundleID    = "com.studio.snakerescue";

        // ─── Level Registry ───────────────────────────────────
        [Header("Levels")]
        [Tooltip("All level configs in order. Index = level number.")]
        public List<LevelConfig> Levels = new List<LevelConfig>();

        // ─── Physics ──────────────────────────────────────────
        [Header("Physics")]
        public float GlobalGravity          = -9.81f;
        public float BallMass               = 1.5f;
        public float RockMass               = 3.0f;
        public float WeightMass             = 5.0f;
        public float DefaultBounciness      = 0.2f;
        public float DefaultFriction        = 0.4f;
        public float ChainReactionTimeout   = 5.0f;

        // ─── Snake ────────────────────────────────────────────
        [Header("Snake")]
        public float SnakeIdleSpeed         = 0f;
        public float SnakePatrolSpeed       = 1.0f;
        public float SnakeChaseSpeed        = 2.5f;
        public float SnakeDetectionRadius   = 5.0f;
        public float SnakeAttackRadius      = 0.8f;
        public float SnakePathUpdateRate    = 0.3f;
        public float SnakeActivationDelay   = 0.5f;

        // ─── Princess ─────────────────────────────────────────
        [Header("Princess")]
        public float PrincessDangerRadius   = 3.0f;
        public float PrincessPanicRadius    = 1.5f;
        public float PrincessReactionDelay  = 0.2f;

        // ─── Scoring ──────────────────────────────────────────
        [Header("Scoring")]
        public float Star3TimeMultiplier    = 0.6f;
        public float Star2TimeMultiplier    = 0.85f;
        public int   Star3MaxActions        = 1;
        public int   Star2MaxActions        = 2;

        // ─── Audio ────────────────────────────────────────────
        [Header("Audio")]
        public float DefaultMasterVolume    = 1.0f;
        public float DefaultMusicVolume     = 0.7f;
        public float DefaultSFXVolume       = 1.0f;
        public float AudioFadeDuration      = 0.5f;

        // ─── UI ───────────────────────────────────────────────
        [Header("UI")]
        public float UIAnimationDuration    = 0.3f;
        public float ResultScreenDelay      = 1.2f;
        public float LevelResetDelay        = 0.8f;
        public int   HintUnlockAfterFails   = 3;

        // ─── Cosmetics ────────────────────────────────────────
        [Header("Cosmetics")]
        public List<SkinDefinition> AvailableSkins = new List<SkinDefinition>();

        // ─── Ads & Monetization (Placeholder) ─────────────────
        [Header("Monetization")]
        public bool   AdsEnabled              = false;
        public int    LevelsBetweenAds        = 5;
        public bool   RemoveAdsAvailable      = true;

        // ─── Debug ────────────────────────────────────────────
        [Header("Debug")]
        public bool   DebugMode              = false;
        public bool   ShowFPS                = false;
        public bool   UnlockAllLevels        = false;
        public bool   SkipIntros             = false;

        // ─── Accessors ────────────────────────────────────────

        public LevelConfig GetLevelConfig(int index)
        {
            if (index < 0 || index >= Levels.Count)
            {
                Debug.LogError(
                    $"[GameConfig] Level index {index} is out of range. " +
                    $"Total levels: {Levels.Count}");
                return null;
            }

            return Levels[index];
        }

        public int GetTotalLevels()
            => Levels?.Count ?? 0;

        public bool IsValidLevel(int index)
            => index >= 0 && index < Levels.Count;

        public bool IsDebugMode()
        {
#if UNITY_EDITOR
            return DebugMode;
#else
            return false;
#endif
        }
    }

    // ─── Supporting Types ─────────────────────────────────────

    [System.Serializable]
    public class SkinDefinition
    {
        public string SkinID;
        public string DisplayName;
        public Sprite PreviewSprite;
        public int    StarsRequired;
        public bool   IsPremium;
    }
}
