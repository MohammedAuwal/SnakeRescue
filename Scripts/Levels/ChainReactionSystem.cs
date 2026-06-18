using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Utils;

namespace SnakeRescue.Levels
{
    /// <summary>
    /// The heart of the gameplay loop.
    ///
    /// Tracks chain reactions as they happen in the physics world.
    /// A chain reaction is: one event causes another causes another.
    ///
    /// This system:
    /// 1. Detects when a chain reaction starts
    /// 2. Tracks each step in the chain
    /// 3. Detects when it ends (physics settles)
    /// 4. Reports the result
    /// 5. Times out if stuck
    ///
    /// Other systems register "reaction steps" as they happen.
    /// Example: Ball hits Snake → Snake dies → Princess safe
    /// That is a 3-step chain reaction.
    /// </summary>
    public class ChainReactionSystem : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static ChainReactionSystem Instance { get; private set; }

        // ─── Settings ─────────────────────────────────────────
        [Header("Chain Reaction Settings")]
        [SerializeField] private float _settleCheckInterval  = 0.1f;
        [SerializeField] private float _settleThreshold      = 0.05f;
        [SerializeField] private float _timeoutDuration      = 5.0f;
        [SerializeField] private int   _minStepsForCombo     = 3;

        // ─── State ────────────────────────────────────────────
        public  bool  IsReacting       { get; private set; } = false;
        public  int   CurrentStepCount { get; private set; } = 0;
        public  float ReactionDuration { get; private set; } = 0f;

        private List<ReactionStep>    _steps      = new List<ReactionStep>();
        private List<Rigidbody2D>     _trackedBodies = new List<Rigidbody2D>();
        private Coroutine             _reactionCoroutine;
        private float                 _reactionStartTime;

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
            GameEvents.OnLevelReset += ResetSystem;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelReset -= ResetSystem;
        }

        // ─── Start Chain Reaction ─────────────────────────────

        /// <summary>
        /// Call this when the player triggers an object.
        /// This begins chain reaction tracking.
        /// </summary>
        public void BeginReaction(string triggerName, Vector3 triggerPosition)
        {
            if (IsReacting)
            {
                // Already in reaction — just add a new step
                RegisterStep(triggerName, triggerPosition, ReactionStepType.Trigger);
                return;
            }

            IsReacting        = true;
            CurrentStepCount  = 0;
            ReactionDuration  = 0f;
            _steps.Clear();
            _reactionStartTime = Time.time;

            RegisterStep(triggerName, triggerPosition, ReactionStepType.Trigger);

            // Notify all systems
            GameEvents.TriggerChainReactionStarted();
            GameEvents.TriggerPlaySFX("SFX_ChainReaction");

            Debug.Log($"[ChainReaction] Started by: {triggerName}");

            // Begin settle monitoring
            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            _reactionCoroutine = StartCoroutine(MonitorReaction());
        }

        // ─── Register Steps ───────────────────────────────────

        /// <summary>
        /// Register a meaningful event that happened during the chain.
        /// Call this from Ball, Rock, Hazard, Snake, etc.
        /// </summary>
        public void RegisterStep(string description,
                                 Vector3 position,
                                 ReactionStepType stepType)
        {
            if (!IsReacting) return;

            CurrentStepCount++;

            ReactionStep step = new ReactionStep
            {
                StepNumber  = CurrentStepCount,
                Description = description,
                Position    = position,
                Type        = stepType,
                Timestamp   = Time.time - _reactionStartTime
            };

            _steps.Add(step);

            Debug.Log(
                $"[ChainReaction] Step {CurrentStepCount}: " +
                $"{stepType} — {description}");
        }

        // ─── Track Physics Bodies ─────────────────────────────

        /// <summary>
        /// Register a Rigidbody2D to monitor for settling.
        /// When all tracked bodies stop moving, reaction ends.
        /// </summary>
        public void TrackBody(Rigidbody2D body)
        {
            if (body != null && !_trackedBodies.Contains(body))
                _trackedBodies.Add(body);
        }

        public void UntrackBody(Rigidbody2D body)
        {
            _trackedBodies.Remove(body);
        }

        // ─── Monitor Loop ─────────────────────────────────────

        private IEnumerator MonitorReaction()
        {
            float timeout = _timeoutDuration;

            // Give physics a frame to start moving
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            while (IsReacting)
            {
                yield return new WaitForSeconds(_settleCheckInterval);

                timeout -= _settleCheckInterval;

                // Timeout failsafe
                if (timeout <= 0f)
                {
                    Debug.LogWarning("[ChainReaction] Timed out.");
                    EndReaction(ChainReactionEndReason.Timeout);
                    yield break;
                }

                // Check if all physics bodies have settled
                if (AllBodiesSettled())
                {
                    // Wait one more interval to be sure
                    yield return new WaitForSeconds(_settleCheckInterval);

                    if (AllBodiesSettled())
                    {
                        EndReaction(ChainReactionEndReason.Settled);
                        yield break;
                    }
                }
            }
        }

        private bool AllBodiesSettled()
        {
            // Remove destroyed bodies
            _trackedBodies.RemoveAll(b => b == null);

            if (_trackedBodies.Count == 0)
                return true;

            foreach (Rigidbody2D body in _trackedBodies)
            {
                if (body == null) continue;

                bool moving =
                    body.velocity.magnitude > _settleThreshold ||
                    Mathf.Abs(body.angularVelocity) > _settleThreshold;

                if (moving) return false;
            }

            return true;
        }

        // ─── End Reaction ─────────────────────────────────────

        private void EndReaction(ChainReactionEndReason reason)
        {
            IsReacting       = false;
            ReactionDuration = Time.time - _reactionStartTime;

            Debug.Log(
                $"[ChainReaction] Ended. " +
                $"Steps: {CurrentStepCount} | " +
                $"Duration: {ReactionDuration:F2}s | " +
                $"Reason: {reason}");

            // Publish chain reaction summary
            EventManager.Publish(new ChainReactionEvent
            {
                StepCount  = CurrentStepCount,
                Duration   = ReactionDuration,
                IsComplete = reason == ChainReactionEndReason.Settled
            });

            GameEvents.TriggerChainReactionEnded();

            // Combo reward for long chains
            if (CurrentStepCount >= _minStepsForCombo)
            {
                OnComboChainAchieved();
            }

            _trackedBodies.Clear();
        }

        // ─── Combo Logic ──────────────────────────────────────

        private void OnComboChainAchieved()
        {
            Debug.Log(
                $"[ChainReaction] COMBO! {CurrentStepCount} steps!");

            // Visual and audio feedback for combos
            GameEvents.TriggerPlaySFX("SFX_ChainReaction");
        }

        // ─── Manual Force End ─────────────────────────────────

        /// <summary>
        /// Force-end the reaction immediately.
        /// Call this when level ends before physics settles.
        /// </summary>
        public void ForceEnd()
        {
            if (!IsReacting) return;

            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            EndReaction(ChainReactionEndReason.Forced);
        }

        // ─── Reset ────────────────────────────────────────────

        public void ResetSystem()
        {
            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            IsReacting       = false;
            CurrentStepCount = 0;
            ReactionDuration = 0f;
            _steps.Clear();
            _trackedBodies.Clear();
        }

        // ─── Read Data ────────────────────────────────────────

        public List<ReactionStep> GetSteps()
            => new List<ReactionStep>(_steps);

        public ReactionStep GetLastStep()
            => _steps.Count > 0 ? _steps[_steps.Count - 1] : null;

        public bool WasCombo()
            => CurrentStepCount >= _minStepsForCombo;
    }

    // ─── Supporting Types ─────────────────────────────────────

    [System.Serializable]
    public class ReactionStep
    {
        public int              StepNumber;
        public string           Description;
        public Vector3          Position;
        public ReactionStepType Type;
        public float            Timestamp;
    }

    public enum ReactionStepType
    {
        Trigger,        // Player interaction
        Collision,      // Object hit something
        Destruction,    // Object was destroyed
        HazardActivated,// Hazard triggered
        HazardRemoved,  // Hazard neutralized
        EnemyHit,       // Snake or enemy hit
        EnemyDead,      // Enemy killed
        PrincessAlert,  // Princess reacted
        PrincessSafe    // Princess reached safety
    }

    public enum ChainReactionEndReason
    {
        Settled,   // Physics naturally came to rest
        Timeout,   // Took too long
        Forced     // Manually ended
    }
}
