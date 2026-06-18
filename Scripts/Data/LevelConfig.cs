using UnityEngine;
using System.Collections.Generic;
using SnakeRescue.Core;

namespace SnakeRescue.Data
{
    /// <summary>
    /// ScriptableObject that defines everything about one level.
    /// One LevelConfig per level — created in Unity Editor.
    /// Stores layout data, objectives, par conditions, hints.
    ///
    /// Create via:
    /// Right click in Project → Create → SnakeRescue → LevelConfig
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelConfig",
        menuName = "SnakeRescue/LevelConfig",
        order = 1)]
    public class LevelConfig : ScriptableObject
    {
        // ─── Identity ─────────────────────────────────────────
        [Header("Identity")]
        public int    LevelIndex;
        public string LevelName        = "Level 1";
        public string LevelDescription = "";

        // ─── Level Type ───────────────────────────────────────
        [Header("Level Type")]
        public LevelType Type = LevelType.KillSnake;

        // ─── Scene & Layout ───────────────────────────────────
        [Header("Scene")]
        public string SceneName = "GameScene";

        // ─── Star Rating Conditions ───────────────────────────
        [Header("Star Rating")]
        [Tooltip("Complete within this many seconds for 3 stars")]
        public float ParTime3Stars    = 8f;

        [Tooltip("Complete within this many seconds for 2 stars")]
        public float ParTime2Stars    = 15f;

        [Tooltip("Use this many or fewer actions for 3 stars")]
        public int   ParActions3Stars = 1;

        [Tooltip("Use this many or fewer actions for 2 stars")]
        public int   ParActions2Stars = 2;

        // ─── Objectives ───────────────────────────────────────
        [Header("Objectives")]
        public bool MustKillSnake     = true;
        public bool MustSavePrincess  = true;
        public bool HasBonusTreasure  = false;

        // ─── Objects in Level ─────────────────────────────────
        [Header("Level Objects")]
        public List<LevelObjectEntry> Objects = new List<LevelObjectEntry>();

        // ─── Hints ────────────────────────────────────────────
        [Header("Hints")]
        [Tooltip("Hint shown after player fails this many times")]
        public int         HintUnlockAfterFails = 3;
        public List<string> HintMessages        = new List<string>();

        // ─── Audio ────────────────────────────────────────────
        [Header("Audio")]
        public string BackgroundMusic = "Music_Gameplay";
        public string AmbientSound    = "";

        // ─── Narrative ────────────────────────────────────────
        [Header("Narrative (Optional)")]
        public string IntroDialogue   = "";
        public string OutroDialogue   = "";

        // ─── Validation ───────────────────────────────────────

        public bool IsValid()
        {
            if (LevelIndex < 0)
            {
                Debug.LogError($"[LevelConfig] {name}: LevelIndex is negative.");
                return false;
            }

            if (string.IsNullOrEmpty(LevelName))
            {
                Debug.LogError($"[LevelConfig] {name}: LevelName is empty.");
                return false;
            }

            return true;
        }

        public string GetHint(int failCount)
        {
            if (HintMessages == null || HintMessages.Count == 0)
                return "Think about what happens when you release the object.";

            // Cycle through hints as player keeps failing
            int index = Mathf.Min(
                failCount - HintUnlockAfterFails,
                HintMessages.Count - 1);

            return index >= 0 ? HintMessages[index] : string.Empty;
        }

        public bool ShouldShowHint(int failCount)
        {
            return failCount >= HintUnlockAfterFails &&
                   HintMessages != null &&
                   HintMessages.Count > 0;
        }
    }

    // ─── Supporting Types ─────────────────────────────────────

    /// <summary>
    /// Defines one object placed in the level.
    /// Used by LevelLoader to spawn and position objects.
    /// </summary>
    [System.Serializable]
    public class LevelObjectEntry
    {
        public ObjectType  Type;
        public Vector3     Position;
        public Vector3     Rotation;
        public Vector3     Scale        = Vector3.one;
        public bool        IsInteractable = true;
        public string      PrefabOverride = "";
    }

    /// <summary>
    /// What kind of level this is.
    /// Drives objective display and win condition logic.
    /// </summary>
    public enum LevelType
    {
        KillSnake,      // Kill the snake → princess survives
        OpenPath,       // Remove obstacles → create escape route
        RiskChoice,     // Safe rescue OR bonus reward
        TimedRescue,    // Save princess before timer expires
        BossLevel       // Large scale chain reaction puzzle
    }
}
