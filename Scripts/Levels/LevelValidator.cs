using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Data;

namespace SnakeRescue.Levels
{
    /// <summary>
    /// Evaluates win and fail conditions every frame.
    ///
    /// Completely separated from LevelManager on purpose.
    /// LevelManager drives level flow.
    /// LevelValidator only answers ONE question:
    ///
    /// "Has the level been won or lost right now?"
    ///
    /// This separation makes it easy to:
    /// - Add new win conditions without touching LevelManager
    /// - Unit test win conditions independently
    /// - Support different level types cleanly
    /// </summary>
    public class LevelValidator : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("References")]
        [SerializeField] private LevelManager _levelManager;

        // ─── State ────────────────────────────────────────────
        private LevelConfig _config;
        private bool        _validating = false;
        private float       _checkRate  = 0.1f;
        private float       _nextCheck  = 0f;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_validating) return;
            if (Time.time < _nextCheck) return;

            _nextCheck = Time.time + _checkRate;
            ValidateCurrentState();
        }

        // ─── Setup ────────────────────────────────────────────

        public void Initialize(LevelConfig config)
        {
            _config     = config;
            _validating = false;
        }

        public void StartValidating()
        {
            if (_config == null)
            {
                Debug.LogError(
                    "[LevelValidator] Cannot start — no config set.");
                return;
            }

            _validating = true;
            Debug.Log("[LevelValidator] Validation active.");
        }

        public void StopValidating()
        {
            _validating = false;
        }

        // ─── Core Validation ──────────────────────────────────

        private void ValidateCurrentState()
        {
            if (_levelManager == null) return;

            // Check fail conditions first — fail takes priority
            ValidationResult failResult = CheckFailConditions();
            if (failResult.IsTriggered)
            {
                HandleFail(failResult);
                return;
            }

            // Then check win conditions
            ValidationResult winResult = CheckWinConditions();
            if (winResult.IsTriggered)
            {
                HandleWin(winResult);
            }
        }

        // ─── Win Conditions ───────────────────────────────────

        private ValidationResult CheckWinConditions()
        {
            if (_config == null)
                return ValidationResult.NotTriggered();

            switch (_config.Type)
            {
                case LevelType.KillSnake:
                    return CheckKillSnakeWin();

                case LevelType.OpenPath:
                    return CheckOpenPathWin();

                case LevelType.RiskChoice:
                    return CheckRiskChoiceWin();

                case LevelType.TimedRescue:
                    return CheckTimedRescueWin();

                case LevelType.BossLevel:
                    return CheckBossLevelWin();

                default:
                    return CheckKillSnakeWin();
            }
        }

        private ValidationResult CheckKillSnakeWin()
        {
            bool snakeGone    = _levelManager.IsSnakeDefeated();
            bool princessOk   = _levelManager.IsPrincessSafe();

            if (!_config.MustKillSnake)
                snakeGone = true;

            if (!_config.MustSavePrincess)
                princessOk = true;

            if (snakeGone && princessOk)
            {
                return ValidationResult.Triggered(
                    LevelResult.Victory,
                    "Snake defeated and princess safe.");
            }

            return ValidationResult.NotTriggered();
        }

        private ValidationResult CheckOpenPathWin()
        {
            // Princess must reach safe zone
            // Snake must not be blocking
            bool princessSafe = _levelManager.IsPrincessSafe();

            if (princessSafe)
            {
                return ValidationResult.Triggered(
                    LevelResult.Victory,
                    "Princess escaped through open path.");
            }

            return ValidationResult.NotTriggered();
        }

        private ValidationResult CheckRiskChoiceWin()
        {
            // Any rescue counts as win
            // Bonus treasure is optional
            bool princessSafe = _levelManager.IsPrincessSafe();

            if (princessSafe)
            {
                return ValidationResult.Triggered(
                    LevelResult.Victory,
                    "Princess rescued. Bonus optional.");
            }

            return ValidationResult.NotTriggered();
        }

        private ValidationResult CheckTimedRescueWin()
        {
            bool princessSafe = _levelManager.IsPrincessSafe();

            if (princessSafe)
            {
                return ValidationResult.Triggered(
                    LevelResult.Victory,
                    "Timed rescue successful.");
            }

            // Check timeout fail
            if (_levelManager.ElapsedTime > 30f)
            {
                return ValidationResult.NotTriggered();
                // Timeout is handled as fail below
            }

            return ValidationResult.NotTriggered();
        }

        private ValidationResult CheckBossLevelWin()
        {
            bool bossDefeated = _levelManager.IsSnakeDefeated();
            bool princessSafe = _levelManager.IsPrincessSafe();

            if (bossDefeated && princessSafe)
            {
                return ValidationResult.Triggered(
                    LevelResult.Victory,
                    "Boss defeated. Kingdom saved.");
            }

            return ValidationResult.NotTriggered();
        }

        // ─── Fail Conditions ──────────────────────────────────

        private ValidationResult CheckFailConditions()
        {
            // Timed rescue timeout
            if (_config.Type == LevelType.TimedRescue &&
                _levelManager.ElapsedTime > 30f)
            {
                return ValidationResult.Triggered(
                    LevelResult.Failed_Timeout,
                    "Time ran out.");
            }

            return ValidationResult.NotTriggered();
        }

        // ─── Handlers ─────────────────────────────────────────

        private void HandleWin(ValidationResult result)
        {
            StopValidating();
            Debug.Log($"[LevelValidator] WIN: {result.Reason}");
            GameEvents.TriggerPrincessSaved();
        }

        private void HandleFail(ValidationResult result)
        {
            StopValidating();
            Debug.Log($"[LevelValidator] FAIL: {result.Reason}");

            switch (result.FailType)
            {
                case LevelResult.Failed_Timeout:
                    GameManager.Instance?.FailLevel(
                        LevelResult.Failed_Timeout);
                    break;

                default:
                    GameManager.Instance?.FailLevel(result.FailType);
                    break;
            }
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    StartValidating();
                    break;

                case GameState.Paused:
                case GameState.LevelComplete:
                case GameState.LevelFailed:
                case GameState.ChainReacting:
                    StopValidating();
                    break;
            }
        }
    }

    // ─── Validation Result ────────────────────────────────────

    /// <summary>
    /// Returned by every condition check.
    /// Clean and allocation-free using struct.
    /// </summary>
    public struct ValidationResult
    {
        public bool        IsTriggered;
        public LevelResult FailType;
        public string      Reason;

        public static ValidationResult Triggered(
            LevelResult result, string reason)
        {
            return new ValidationResult
            {
                IsTriggered = true,
                FailType    = result,
                Reason      = reason
            };
        }

        public static ValidationResult NotTriggered()
        {
            return new ValidationResult
            {
                IsTriggered = false,
                FailType    = LevelResult.None,
                Reason      = string.Empty
            };
        }
    }
}
