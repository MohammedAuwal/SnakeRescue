using System;
using SnakeRescue.Core;

namespace SnakeRescue.Core
{
    /// <summary>
    /// Central event bus for the entire game.
    /// Systems communicate through events — never direct references.
    /// This keeps every system decoupled and testable.
    /// </summary>
    public static class GameEvents
    {
        // ─── Game State Events ────────────────────────────────
        public static event Action<GameState>        OnGameStateChanged;
        public static event Action                   OnGamePaused;
        public static event Action                   OnGameResumed;

        // ─── Level Events ─────────────────────────────────────
        public static event Action<int>              OnLevelStarted;
        public static event Action<LevelResult, int> OnLevelCompleted;
        public static event Action<LevelResult>      OnLevelFailed;
        public static event Action                   OnLevelReset;
        public static event Action                   OnChainReactionStarted;
        public static event Action                   OnChainReactionEnded;

        // ─── Princess Events ──────────────────────────────────
        public static event Action<PrincessState>    OnPrincessStateChanged;
        public static event Action                   OnPrincessSaved;
        public static event Action                   OnPrincessCaught;
        public static event Action                   OnPrincessHazardHit;

        // ─── Snake Events ─────────────────────────────────────
        public static event Action<SnakeState>       OnSnakeStateChanged;
        public static event Action                   OnSnakeActivated;
        public static event Action                   OnSnakeDead;
        public static event Action                   OnSnakeReachedPrincess;

        // ─── Object Events ────────────────────────────────────
        public static event Action<ObjectType>       OnObjectTriggered;
        public static event Action<ObjectType>       OnObjectDestroyed;
        public static event Action<ObjectType, ObjectType> OnObjectCollision;

        // ─── Hazard Events ────────────────────────────────────
        public static event Action<HazardType>       OnHazardActivated;
        public static event Action<HazardType>       OnHazardNeutralized;

        // ─── UI Events ────────────────────────────────────────
        public static event Action                   OnHintRequested;
        public static event Action<int>              OnStarEarned;
        public static event Action                   OnResultScreenOpened;

        // ─── Audio Events ─────────────────────────────────────
        public static event Action<string>           OnPlaySFX;
        public static event Action<string>           OnPlayMusic;
        public static event Action                   OnStopMusic;


        // ─── Invokers ─────────────────────────────────────────
        // Clean invoke methods so callers never touch null-check logic

        public static void TriggerGameStateChanged(GameState state)
            => OnGameStateChanged?.Invoke(state);

        public static void TriggerGamePaused()
            => OnGamePaused?.Invoke();

        public static void TriggerGameResumed()
            => OnGameResumed?.Invoke();

        public static void TriggerLevelStarted(int levelIndex)
            => OnLevelStarted?.Invoke(levelIndex);

        public static void TriggerLevelCompleted(LevelResult result, int stars)
            => OnLevelCompleted?.Invoke(result, stars);

        public static void TriggerLevelFailed(LevelResult reason)
            => OnLevelFailed?.Invoke(reason);

        public static void TriggerLevelReset()
            => OnLevelReset?.Invoke();

        public static void TriggerChainReactionStarted()
            => OnChainReactionStarted?.Invoke();

        public static void TriggerChainReactionEnded()
            => OnChainReactionEnded?.Invoke();

        public static void TriggerPrincessStateChanged(PrincessState state)
            => OnPrincessStateChanged?.Invoke(state);

        public static void TriggerPrincessSaved()
            => OnPrincessSaved?.Invoke();

        public static void TriggerPrincessCaught()
            => OnPrincessCaught?.Invoke();

        public static void TriggerPrincessHazardHit()
            => OnPrincessHazardHit?.Invoke();

        public static void TriggerSnakeStateChanged(SnakeState state)
            => OnSnakeStateChanged?.Invoke(state);

        public static void TriggerSnakeActivated()
            => OnSnakeActivated?.Invoke();

        public static void TriggerSnakeDead()
            => OnSnakeDead?.Invoke();

        public static void TriggerSnakeReachedPrincess()
            => OnSnakeReachedPrincess?.Invoke();

        public static void TriggerObjectTriggered(ObjectType type)
            => OnObjectTriggered?.Invoke(type);

        public static void TriggerObjectDestroyed(ObjectType type)
            => OnObjectDestroyed?.Invoke(type);

        public static void TriggerObjectCollision(ObjectType a, ObjectType b)
            => OnObjectCollision?.Invoke(a, b);

        public static void TriggerHazardActivated(HazardType type)
            => OnHazardActivated?.Invoke(type);

        public static void TriggerHazardNeutralized(HazardType type)
            => OnHazardNeutralized?.Invoke(type);

        public static void TriggerHintRequested()
            => OnHintRequested?.Invoke();

        public static void TriggerStarEarned(int count)
            => OnStarEarned?.Invoke(count);

        public static void TriggerResultScreenOpened()
            => OnResultScreenOpened?.Invoke();

        public static void TriggerPlaySFX(string clipName)
            => OnPlaySFX?.Invoke(clipName);

        public static void TriggerPlayMusic(string clipName)
            => OnPlayMusic?.Invoke(clipName);

        public static void TriggerStopMusic()
            => OnStopMusic?.Invoke();

        /// <summary>
        /// Call this when loading a new scene.
        /// Clears all subscriptions to prevent ghost listeners.
        /// </summary>
        public static void ClearAllEvents()
        {
            OnGameStateChanged       = null;
            OnGamePaused             = null;
            OnGameResumed            = null;
            OnLevelStarted           = null;
            OnLevelCompleted         = null;
            OnLevelFailed            = null;
            OnLevelReset             = null;
            OnChainReactionStarted   = null;
            OnChainReactionEnded     = null;
            OnPrincessStateChanged   = null;
            OnPrincessSaved          = null;
            OnPrincessCaught         = null;
            OnPrincessHazardHit      = null;
            OnSnakeStateChanged      = null;
            OnSnakeActivated         = null;
            OnSnakeDead              = null;
            OnSnakeReachedPrincess   = null;
            OnObjectTriggered        = null;
            OnObjectDestroyed        = null;
            OnObjectCollision        = null;
            OnHazardActivated        = null;
            OnHazardNeutralized      = null;
            OnHintRequested          = null;
            OnStarEarned             = null;
            OnResultScreenOpened     = null;
            OnPlaySFX                = null;
            OnPlayMusic              = null;
            OnStopMusic              = null;
        }
    }
}
