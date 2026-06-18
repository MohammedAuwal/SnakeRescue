using System;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Characters.Princess
{
    /// <summary>
    /// Manages state transitions for the Princess.
    ///
    /// Enforces valid transitions — not every state
    /// can transition to every other state.
    ///
    /// For example:
    /// Dead → cannot go back to Idle
    /// Celebrating → cannot go to Panic
    ///
    /// This keeps princess behavior consistent and
    /// prevents impossible animation sequences.
    /// </summary>
    public class PrincessStateManager : MonoBehaviour
    {
        // ─── State ────────────────────────────────────────────
        public  PrincessState CurrentState  { get; private set; }
            = PrincessState.Idle;

        public  PrincessState PreviousState { get; private set; }
            = PrincessState.Idle;

        public  float StateDuration         { get; private set; } = 0f;
        private float _stateEnterTime       = 0f;

        // ─── Transition Rules ─────────────────────────────────
        // Key   = current state
        // Value = list of valid states to transition TO
        private static readonly Dictionary<PrincessState, List<PrincessState>>
            _validTransitions = new Dictionary<PrincessState, List<PrincessState>>
        {
            {
                PrincessState.Idle, new List<PrincessState>
                {
                    PrincessState.Alert,
                    PrincessState.Fear,
                    PrincessState.Panic,
                    PrincessState.Relief,
                    PrincessState.Celebrating,
                    PrincessState.Dead
                }
            },
            {
                PrincessState.Alert, new List<PrincessState>
                {
                    PrincessState.Idle,
                    PrincessState.Fear,
                    PrincessState.Panic,
                    PrincessState.Relief,
                    PrincessState.Dead
                }
            },
            {
                PrincessState.Fear, new List<PrincessState>
                {
                    PrincessState.Idle,
                    PrincessState.Alert,
                    PrincessState.Panic,
                    PrincessState.Relief,
                    PrincessState.Dead
                }
            },
            {
                PrincessState.Panic, new List<PrincessState>
                {
                    PrincessState.Fear,
                    PrincessState.Relief,
                    PrincessState.Dead
                }
                // Cannot go directly Panic → Idle
                // Must go through Relief first
            },
            {
                PrincessState.Relief, new List<PrincessState>
                {
                    PrincessState.Idle,
                    PrincessState.Celebrating,
                    PrincessState.Alert
                }
            },
            {
                PrincessState.Celebrating, new List<PrincessState>
                {
                    PrincessState.Idle
                    // Celebrating is near-final state
                    // Can only return to Idle if level resets
                }
            },
            {
                PrincessState.Dead, new List<PrincessState>
                {
                    // Dead is terminal — no transitions out
                    // Only a full level reset can revive her
                }
            }
        };

        // ─── Events ───────────────────────────────────────────
        public event Action<PrincessState, PrincessState> OnTransition;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Update()
        {
            StateDuration = Time.time - _stateEnterTime;
        }

        // ─── Initialization ───────────────────────────────────

        public void Initialize()
        {
            CurrentState   = PrincessState.Idle;
            PreviousState  = PrincessState.Idle;
            _stateEnterTime = Time.time;
            StateDuration  = 0f;
        }

        // ─── State Transition ─────────────────────────────────

        public bool SetState(PrincessState newState)
        {
            if (CurrentState == newState) return false;

            if (!IsTransitionValid(CurrentState, newState))
            {
                Debug.LogWarning(
                    $"[PrincessState] Invalid transition: " +
                    $"{CurrentState} → {newState}. Ignored.");
                return false;
            }

            PreviousState  = CurrentState;
            CurrentState   = newState;
            _stateEnterTime = Time.time;
            StateDuration  = 0f;

            OnTransition?.Invoke(PreviousState, CurrentState);

            Debug.Log(
                $"[PrincessState] {PreviousState} → {CurrentState}");

            return true;
        }

        // ─── Transition Validation ────────────────────────────

        public bool IsTransitionValid(PrincessState from, PrincessState to)
        {
            if (!_validTransitions.TryGetValue(
                from, out List<PrincessState> allowed))
            {
                return false;
            }

            return allowed.Contains(to);
        }

        public bool CanTransitionTo(PrincessState target)
            => IsTransitionValid(CurrentState, target);

        // ─── State Queries ────────────────────────────────────

        public bool IsInDangerState()
        {
            return CurrentState == PrincessState.Alert  ||
                   CurrentState == PrincessState.Fear   ||
                   CurrentState == PrincessState.Panic;
        }

        public bool IsTerminalState()
        {
            return CurrentState == PrincessState.Dead        ||
                   CurrentState == PrincessState.Celebrating;
        }

        public bool HasBeenInStateFor(float seconds)
            => StateDuration >= seconds;

        public string GetStateDisplayName()
        {
            switch (CurrentState)
            {
                case PrincessState.Idle:        return "Safe";
                case PrincessState.Alert:       return "Alert";
                case PrincessState.Fear:        return "Afraid";
                case PrincessState.Panic:       return "Panicking";
                case PrincessState.Relief:      return "Relieved";
                case PrincessState.Celebrating: return "Celebrating";
                case PrincessState.Dead:        return "Caught";
                default:                        return "Unknown";
            }
        }

        // ─── Force Override ───────────────────────────────────

        /// <summary>
        /// Bypasses transition rules.
        /// ONLY use this for level reset.
        /// </summary>
        public void ForceState(PrincessState state)
        {
            PreviousState  = CurrentState;
            CurrentState   = state;
            _stateEnterTime = Time.time;
            StateDuration  = 0f;

            Debug.Log(
                $"[PrincessState] FORCED: {PreviousState} → {CurrentState}");
        }
    }
}
