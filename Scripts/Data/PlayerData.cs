using System;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Utils;

namespace SnakeRescue.Data
{
    /// <summary>
    /// Holds all persistent player data.
    /// This is the single source of truth for:
    /// - Level progress
    /// - Stars collected
    /// - Unlocks
    /// - Settings
    ///
    /// This class is serializable so SaveSystem can
    /// convert it to JSON and store it.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        // ─── Identity ─────────────────────────────────────────
        public string PlayerName      = "Player";
        public string DataVersion     = "1.0.0";
        public long   CreatedAt;
        public long   LastSavedAt;

        // ─── Level Progress ───────────────────────────────────

        /// <summary>
        /// Key = level index (0-based)
        /// Value = LevelRecord for that level
        /// </summary>
        public List<LevelRecord> LevelRecords = new List<LevelRecord>();

        /// <summary>
        /// The highest level the player has unlocked.
        /// Level 0 is always unlocked.
        /// </summary>
        public int HighestUnlockedLevel = 0;

        /// <summary>
        /// Total stars collected across all levels.
        /// </summary>
        public int TotalStars = 0;

        // ─── Cosmetics ────────────────────────────────────────
        public string EquippedPrincessSkin = "Default";
        public List<string> UnlockedSkins  = new List<string> { "Default" };

        // ─── Settings ─────────────────────────────────────────
        public float MasterVolume = 1.0f;
        public float MusicVolume  = 0.7f;
        public float SFXVolume    = 1.0f;
        public bool  Vibration    = true;
        public bool  Notifications = true;

        // ─── Stats ────────────────────────────────────────────
        public int   TotalLevelsCompleted = 0;
        public int   TotalSnakesKilled    = 0;
        public int   TotalDeaths          = 0;
        public int   TotalHintsUsed       = 0;
        public float TotalPlayTimeSeconds = 0f;

        // ─── Constructor ──────────────────────────────────────

        public PlayerData()
        {
            CreatedAt   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LastSavedAt = CreatedAt;

            // Pre-fill level records for all MVP levels
            for (int i = 0; i < Constants.TOTAL_MVP_LEVELS; i++)
            {
                LevelRecords.Add(new LevelRecord(i));
            }
        }

        // ─── Level Record Access ──────────────────────────────

        public LevelRecord GetLevelRecord(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelRecords.Count)
            {
                Debug.LogWarning(
                    $"[PlayerData] Level index {levelIndex} out of range.");
                return null;
            }

            return LevelRecords[levelIndex];
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex == 0) return true;
            return levelIndex <= HighestUnlockedLevel;
        }

        public int GetLevelStars(int levelIndex)
        {
            LevelRecord record = GetLevelRecord(levelIndex);
            return record?.BestStars ?? 0;
        }

        // ─── Progress Updates ─────────────────────────────────

        public void RecordLevelComplete(int levelIndex, int stars, float time, int actions)
        {
            LevelRecord record = GetLevelRecord(levelIndex);
            if (record == null) return;

            bool isFirstComplete = !record.IsCompleted;

            // Update record if better result
            if (!record.IsCompleted || stars > record.BestStars)
            {
                record.BestStars = stars;
            }

            if (!record.IsCompleted || time < record.BestTime)
            {
                record.BestTime = time;
            }

            if (!record.IsCompleted || actions < record.BestActions)
            {
                record.BestActions = actions;
            }

            record.IsCompleted   = true;
            record.AttemptCount += 1;
            record.LastPlayedAt  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Unlock next level
            int nextLevel = levelIndex + 1;
            if (nextLevel < Constants.TOTAL_MVP_LEVELS &&
                nextLevel > HighestUnlockedLevel)
            {
                HighestUnlockedLevel = nextLevel;
            }

            // Update totals
            if (isFirstComplete)
            {
                TotalLevelsCompleted++;
            }

            RecalculateTotalStars();

            LastSavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void RecordLevelAttempt(int levelIndex)
        {
            LevelRecord record = GetLevelRecord(levelIndex);
            if (record == null) return;

            record.AttemptCount++;
        }

        public void RecordDeath()
        {
            TotalDeaths++;
        }

        public void RecordSnakeKill()
        {
            TotalSnakesKilled++;
        }

        public void RecordHintUsed()
        {
            TotalHintsUsed++;
        }

        public void AddPlayTime(float seconds)
        {
            TotalPlayTimeSeconds += seconds;
        }

        // ─── Cosmetic Updates ─────────────────────────────────

        public bool UnlockSkin(string skinId)
        {
            if (UnlockedSkins.Contains(skinId)) return false;
            UnlockedSkins.Add(skinId);
            return true;
        }

        public bool EquipSkin(string skinId)
        {
            if (!UnlockedSkins.Contains(skinId)) return false;
            EquippedPrincessSkin = skinId;
            return true;
        }

        // ─── Internal Helpers ─────────────────────────────────

        private void RecalculateTotalStars()
        {
            int total = 0;
            foreach (LevelRecord record in LevelRecords)
            {
                total += record.BestStars;
            }
            TotalStars = total;
        }
    }

    // ─── Level Record ─────────────────────────────────────────

    /// <summary>
    /// Stores completion data for a single level.
    /// </summary>
    [Serializable]
    public class LevelRecord
    {
        public int   LevelIndex;
        public bool  IsCompleted  = false;
        public int   BestStars    = 0;
        public float BestTime     = float.MaxValue;
        public int   BestActions  = int.MaxValue;
        public int   AttemptCount = 0;
        public long  LastPlayedAt = 0;

        public LevelRecord(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }
}
