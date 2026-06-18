using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeRescue.Managers
{
    /// <summary>
    /// Type-safe generic event manager.
    /// Works alongside GameEvents (which handles game-specific events).
    ///
    /// This handles dynamic runtime events where the
    /// event type is not known at compile time.
    ///
    /// Usage:
    ///   EventManager.Subscribe<MyEventData>(OnMyEvent);
    ///   EventManager.Publish(new MyEventData { Value = 5 });
    ///   EventManager.Unsubscribe<MyEventData>(OnMyEvent);
    /// </summary>
    public static class EventManager
    {
        // ─── Registry ─────────────────────────────────────────
        private static Dictionary<Type, List<Delegate>> _handlers
            = new Dictionary<Type, List<Delegate>>();

        private static Dictionary<Type, object> _lastEvents
            = new Dictionary<Type, object>();

        // ─── Subscribe ────────────────────────────────────────

        public static void Subscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);

            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();

            if (!_handlers[type].Contains(handler))
            {
                _handlers[type].Add(handler);
            }
        }

        /// <summary>
        /// Subscribe and immediately receive last published event
        /// if one exists. Useful for UI that initializes late.
        /// </summary>
        public static void SubscribeAndGetLast<T>(Action<T> handler)
        {
            Subscribe(handler);

            Type type = typeof(T);
            if (_lastEvents.TryGetValue(type, out object lastEvent))
            {
                handler.Invoke((T)lastEvent);
            }
        }

        // ─── Unsubscribe ──────────────────────────────────────

        public static void Unsubscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);

            if (!_handlers.ContainsKey(type)) return;

            _handlers[type].Remove(handler);

            if (_handlers[type].Count == 0)
                _handlers.Remove(type);
        }

        // ─── Publish ──────────────────────────────────────────

        public static void Publish<T>(T eventData)
        {
            Type type = typeof(T);

            // Store as last event for late subscribers
            _lastEvents[type] = eventData;

            if (!_handlers.TryGetValue(type, out List<Delegate> handlers))
                return;

            // Copy to avoid modification during iteration
            Delegate[] snapshot = handlers.ToArray();

            foreach (Delegate handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[EventManager] Handler error for {type.Name}: {e.Message}");
                }
            }
        }

        // ─── Clear ────────────────────────────────────────────

        public static void ClearAll()
        {
            _handlers.Clear();
            _lastEvents.Clear();
        }

        public static void ClearEvent<T>()
        {
            Type type = typeof(T);
            _handlers.Remove(type);
            _lastEvents.Remove(type);
        }

        // ─── Diagnostics ──────────────────────────────────────

        public static int GetSubscriberCount<T>()
        {
            Type type = typeof(T);
            if (_handlers.TryGetValue(type, out List<Delegate> handlers))
                return handlers.Count;
            return 0;
        }

        public static void LogAllSubscriptions()
        {
            Debug.Log(
                $"[EventManager] Active event types: {_handlers.Count}");

            foreach (var kvp in _handlers)
            {
                Debug.Log(
                    $"  {kvp.Key.Name}: {kvp.Value.Count} subscriber(s)");
            }
        }
    }

    // ─── Built-in Event Data Types ────────────────────────────
    // Define typed event payloads here.
    // Other scripts publish/subscribe using these types.

    public struct LevelStartedEvent
    {
        public int   LevelIndex;
        public string LevelName;
    }

    public struct LevelCompletedEvent
    {
        public int   LevelIndex;
        public int   StarsEarned;
        public float TimeTaken;
        public int   ActionsTaken;
    }

    public struct LevelFailedEvent
    {
        public int    LevelIndex;
        public string FailReason;
    }

    public struct ChainReactionEvent
    {
        public int     StepCount;
        public float   Duration;
        public bool    IsComplete;
    }

    public struct ObjectTriggeredEvent
    {
        public string ObjectName;
        public string ObjectType;
        public Vector3 Position;
    }

    public struct HazardEvent
    {
        public string HazardType;
        public bool   IsNeutralized;
        public Vector3 Position;
    }

    public struct PrincessStateEvent
    {
        public string PreviousState;
        public string NewState;
    }

    public struct SnakeStateEvent
    {
        public string PreviousState;
        public string NewState;
        public Vector3 Position;
    }

    public struct StarEarnedEvent
    {
        public int LevelIndex;
        public int Stars;
        public int TotalStars;
    }

    public struct VolumeChangedEvent
    {
        public float Master;
        public float Music;
        public float SFX;
    }
}
