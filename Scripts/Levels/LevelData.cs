using System;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Levels
{
    /// <summary>
    /// Runtime snapshot of a level's current state.
    ///
    /// While LevelConfig is the design-time definition,
    /// LevelData is what changes at runtime as the level plays.
    ///
    /// Tracks:
    /// - What objects are alive
    /// - What events have happened
    /// - Current threat state
    /// - Current rescue state
    ///
    /// Used by LevelValidator to evaluate win/fail conditions.
    /// </summary>
    public class LevelData
    {
        // ─── Identity ─────────────────────────────────────────
        public int    LevelIndex   { get; private set; }
        public string LevelName    { get; private set; }
        public float  StartTime    { get; private set; }

        // ─── Character States ─────────────────────────────────
        public PrincessState PrincessState { get; set; } = PrincessState.Idle;
        public SnakeState    SnakeState    { get; set; } = SnakeState.Idle;
        public bool          SnakeAlive    { get; set; } = true;
        public bool          PrincessAlive { get; set; } = true;
        public bool          PrincessSaved { get; set; } = false;

        // ─── Object Tracking ──────────────────────────────────
        public List<RuntimeObjectData> ActiveObjects { get; private set; }
            = new List<RuntimeObjectData>();

        public List<RuntimeHazardData> ActiveHazards { get; private set; }
            = new List<RuntimeHazardData>();

        // ─── Event Log ────────────────────────────────────────
        public List<LevelEvent> EventLog { get; private set; }
            = new List<LevelEvent>();

        // ─── Metrics ──────────────────────────────────────────
        public int   TotalActions       { get; set; } = 0;
        public int   TotalCollisions    { get; set; } = 0;
        public int   ChainReactionSteps { get; set; } = 0;
        public float LongestChain      { get; set; } = 0f;

        // ─── Constructor ──────────────────────────────────────

        public LevelData(int levelIndex, string levelName)
        {
            LevelIndex = levelIndex;
            LevelName  = levelName;
            StartTime  = Time.time;
        }

        // ─── Object Management ────────────────────────────────

        public void RegisterObject(GameObject obj, ObjectType type)
        {
            if (obj == null) return;

            ActiveObjects.Add(new RuntimeObjectData
            {
                ObjectRef    = obj,
                Type         = type,
                IsAlive      = true,
                SpawnTime    = Time.time,
                SpawnPosition = obj.transform.position
            });
        }

        public void RegisterHazard(GameObject obj, HazardType type)
        {
            if (obj == null) return;

            ActiveHazards.Add(new RuntimeHazardData
            {
                HazardRef     = obj,
                Type          = type,
                IsActive      = true,
                IsNeutralized = false
            });
        }

        public void MarkObjectDestroyed(GameObject obj)
        {
            foreach (RuntimeObjectData data in ActiveObjects)
            {
                if (data.ObjectRef == obj)
                {
                    data.IsAlive       = false;
                    data.DestroyTime   = Time.time;
                    break;
                }
            }
        }

        public void MarkHazardNeutralized(GameObject obj)
        {
            foreach (RuntimeHazardData data in ActiveHazards)
            {
                if (data.HazardRef == obj)
                {
                    data.IsNeutralized = true;
                    data.IsActive      = false;
                    break;
                }
            }
        }

        // ─── Event Logging ────────────────────────────────────

        public void LogEvent(string description,
                             LevelEventType type,
                             Vector3 position = default)
        {
            EventLog.Add(new LevelEvent
            {
                Description = description,
                Type        = type,
                Position    = position,
                Timestamp   = Time.time - StartTime
            });
        }

        // ─── Queries ──────────────────────────────────────────

        public float GetElapsedTime()
            => Time.time - StartTime;

        public int GetAliveObjectCount()
        {
            int count = 0;
            foreach (RuntimeObjectData obj in ActiveObjects)
            {
                if (obj.IsAlive) count++;
            }
            return count;
        }

        public int GetActiveHazardCount()
        {
            int count = 0;
            foreach (RuntimeHazardData hazard in ActiveHazards)
            {
                if (hazard.IsActive) count++;
            }
            return count;
        }

        public bool AreAllHazardsNeutralized()
        {
            foreach (RuntimeHazardData hazard in ActiveHazards)
            {
                if (!hazard.IsNeutralized) return false;
            }
            return true;
        }

        public bool HasEventOccurred(LevelEventType type)
        {
            foreach (LevelEvent evt in EventLog)
            {
                if (evt.Type == type) return true;
            }
            return false;
        }

        // ─── Summary ──────────────────────────────────────────

        public string GetSummary()
        {
            return
                $"Level {LevelIndex}: {LevelName}\n" +
                $"Time: {GetElapsedTime():F1}s\n" +
                $"Actions: {TotalActions}\n" +
                $"Chain Steps: {ChainReactionSteps}\n" +
                $"Snake Alive: {SnakeAlive}\n" +
                $"Princess Saved: {PrincessSaved}\n" +
                $"Events Logged: {EventLog.Count}";
        }
    }

    // ─── Supporting Data Types ────────────────────────────────

    [Serializable]
    public class RuntimeObjectData
    {
        public GameObject ObjectRef;
        public ObjectType Type;
        public bool       IsAlive;
        public float      SpawnTime;
        public float      DestroyTime;
        public Vector3    SpawnPosition;
    }

    [Serializable]
    public class RuntimeHazardData
    {
        public GameObject HazardRef;
        public HazardType Type;
        public bool       IsActive;
        public bool       IsNeutralized;
    }

    [Serializable]
    public class LevelEvent
    {
        public string         Description;
        public LevelEventType Type;
        public Vector3        Position;
        public float          Timestamp;
    }

    public enum LevelEventType
    {
        PlayerAction,
        ObjectTriggered,
        ObjectDestroyed,
        CollisionOccurred,
        HazardActivated,
        HazardNeutralized,
        ChainReactionStarted,
        ChainReactionEnded,
        SnakeActivated,
        SnakeDefeated,
        PrincessAlert,
        PrincessPanicked,
        PrincessSaved,
        PrincessCaught,
        LevelComplete,
        LevelFailed
    }
}
