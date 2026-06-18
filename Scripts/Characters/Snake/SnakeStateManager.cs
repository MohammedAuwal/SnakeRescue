using System;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Characters.Snake
{
    /// <summary>
    /// Manages state transitions for the Snake.
    ///
    /// Enforces valid transitions — prevents impossible
    /// behavior sequences (e.g. Dead → Chasing).
    ///
    /// Snake states are simpler than Princess states
    /// because the snake is a pressure system, not an
    /// emotional character.
    /// </summary>
    public class SnakeStateManager : MonoBehaviour
    {
        // ─── State ────────────────────────────────────────────
        public  SnakeState CurrentState  { get; private set; }
            = SnakeState.Idle;

        public  SnakeState PreviousState { get; private set; }
            = SnakeState.Idle;

        public  float StateDuration      { get; private set; } = 0f;
        private float _stateEnterTime    = 0f;

        // ─── Transition Rules ─────────────────────────────────
        private static readonly Dictionary<SnakeState, List<SnakeState>>
            _validTransitions = new Dictionary<SnakeState, List<SnakeState>>
        {
            {
                SnakeState.Idle, new List<SnakeState>
                {
                    SnakeState.Patrolling,
                    SnakeState.Detecting,
                    SnakeState.Chasing,
                    SnakeState.Stunned,
                    SnakeState.Dead
                }
            },
            {
                SnakeState.Patrolling, new List<SnakeState>
                {
                    SnakeState.Idle,
                    SnakeState.Detecting,
                    SnakeState.Chasing,
                    SnakeState.Stunned,
                    SnakeState.Dead
                }
            },
            {
                SnakeState.Detecting, new List<SnakeState>
                {
                    SnakeState.Idle,
                    SnakeState.Patrolling,
                    SnakeState.Chasing,
                    SnakeState.Dead
                }
            },
            {
                SnakeState.Chasing, new List<SnakeState>
                {
                    SnakeState.Patrolling,
                    SnakeState.Attacking,
                    SnakeState.Stunned,
                    SnakeState.Dead
                }
            },
            {
                SnakeState.Attacking, new List<SnakeState>
                {
                    SnakeState.Dead,
                    SnakeState.Idle // If attack misses
                }
            },
            {
                SnakeState.Stunned, new List<SnakeState>
                {
                    SnakeState.Idle,
                    SnakeState.Patrolling,
                    SnakeState.Dead
                }
            },
            {
                SnakeState.Dead, new List<SnakeState>
                {
                    // Terminal state
                }
            }
        };

        // ─── Events ───────────────────────────────────────────
        public event Action<SnakeState, SnakeState> OnTransition;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Update()
        {
            StateDuration = Time.time - _stateEnterTime;
        }

        // ─── Initialization ───────────────────────────────────

        public void Initialize()
        {
            CurrentState   = SnakeState.Idle;
            PreviousState  = SnakeState.Idle;
            _stateEnterTime = Time.time;
            StateDuration  = 0f;
        }

        // ─── State Transition ─────────────────────────────────

        public bool SetState(SnakeState newState)
        {
            if (CurrentState == newState) return false;

            if (!IsTransitionValid(CurrentState, newState))
            {
                Debug.LogWarning(
                    $"[SnakeState] Invalid transition: " +
                    $"{CurrentState} → {newState}. Ignored.");
                return false;
            }

            PreviousState  = CurrentState;
            CurrentState   = newState;
            _stateEnterTime = Time.time;
            StateDuration  = 0f;

            OnTransition?.Invoke(PreviousState, CurrentState);

            Debug.Log(
                $"[SnakeState] {PreviousState} → {CurrentState}");

            return true;
        }

        // ─── Transition Validation ────────────────────────────

        public bool IsTransitionValid(SnakeState from, SnakeState to)
        {
            if (!_validTransitions.TryGetValue(
                from, out List<SnakeState> allowed))
            {
                return false;
            }

            return allowed.Contains(to);
        }

        public bool CanTransitionTo(SnakeState target)
            => IsTransitionValid(CurrentState, target);

        // ─── State Queries ────────────────────────────────────

        public bool IsActive()
        {
            return CurrentState == SnakeState.Patrolling  ||
                   CurrentState == SnakeState.Chasing     ||
                   CurrentState == SnakeState.Attacking;
        }

        public bool IsTerminalState()
            => CurrentState == SnakeState.Dead;

        public bool HasBeenInStateFor(float seconds)
            => StateDuration >= seconds;

        // ─── Force Override ───────────────────────────────────

        public void ForceState(SnakeState state)
        {
            PreviousState  = CurrentState;
            CurrentState   = state;
            _stateEnterTime = Time.time;
            StateDuration  = 0f;

            Debug.Log(
                $"[SnakeState] FORCED: {PreviousState} → {CurrentState}");
        }
    }
}
