using UnityEngine;
using SnakeRescue.Utils;

namespace SnakeRescue.Core
{
    /// <summary>
    /// The central brain of the game.
    /// Controls game state, coordinates all major systems.
    /// Singleton — persists across scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ─── State ────────────────────────────────────────────
        public GameState CurrentState { get; private set; } = GameState.None;
        public int       CurrentLevel { get; private set; } = 0;
        public bool      IsPaused     { get; private set; } = false;

        // ─── Runtime Tracking ─────────────────────────────────
        private float _levelStartTime;
        private int   _actionsTakenThisLevel;
        private bool  _levelActive;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        // ─── Initialization ───────────────────────────────────

        private void Initialize()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            SetState(GameState.Loading);

            Debug.Log("[GameManager] Initialized");
        }

        // ─── State Machine ────────────────────────────────────

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previousState = CurrentState;
            CurrentState = newState;

            HandleStateTransition(previousState, newState);
            GameEvents.TriggerGameStateChanged(newState);

            Debug.Log($"[GameManager] State: {previousState} → {newState}");
        }

        private void HandleStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    OnEnterMainMenu();
                    break;

                case GameState.LevelSelect:
                    OnEnterLevelSelect();
                    break;

                case GameState.LevelIntro:
                    OnEnterLevelIntro();
                    break;

                case GameState.Playing:
                    OnEnterPlaying();
                    break;

                case GameState.Paused:
                    OnEnterPaused();
                    break;

                case GameState.ChainReacting:
                    OnEnterChainReacting();
                    break;

                case GameState.LevelComplete:
                    OnEnterLevelComplete();
                    break;

                case GameState.LevelFailed:
                    OnEnterLevelFailed();
                    break;
            }
        }

        // ─── State Handlers ───────────────────────────────────

        private void OnEnterMainMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            GameEvents.TriggerPlayMusic("Music_MainMenu");
        }

        private void OnEnterLevelSelect()
        {
            Time.timeScale = 1f;
        }

        private void OnEnterLevelIntro()
        {
            Time.timeScale = 1f;
            _actionsTakenThisLevel = 0;
        }

        private void OnEnterPlaying()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            _levelActive = true;
            _levelStartTime = Time.time;
            GameEvents.TriggerPlayMusic("Music_Gameplay");
        }

        private void OnEnterPaused()
        {
            Time.timeScale = 0f;
            IsPaused = true;
            GameEvents.TriggerGamePaused();
        }

        private void OnEnterChainReacting()
        {
            // Physics is running — do not freeze time
            // Just track that a chain reaction is happening
        }

        private void OnEnterLevelComplete()
        {
            _levelActive = false;
            Time.timeScale = 1f;

            float timeTaken = Time.time - _levelStartTime;
            int stars = CalculateStars(timeTaken, _actionsTakenThisLevel);

            GameEvents.TriggerLevelCompleted(LevelResult.Victory, stars);
            GameEvents.TriggerStarEarned(stars);
            GameEvents.TriggerPlaySFX("SFX_LevelComplete");
        }

        private void OnEnterLevelFailed()
        {
            _levelActive = false;
            Time.timeScale = 1f;
            GameEvents.TriggerPlaySFX("SFX_LevelFailed");
        }

        // ─── Level Control ────────────────────────────────────

        public void StartLevel(int levelIndex)
        {
            CurrentLevel = levelIndex;
            SetState(GameState.LevelIntro);
            GameEvents.TriggerLevelStarted(levelIndex);
        }

        public void CompleteLevel()
        {
            if (!_levelActive) return;
            SetState(GameState.LevelComplete);
        }

        public void FailLevel(LevelResult reason)
        {
            if (!_levelActive) return;
            SetState(GameState.LevelFailed);
            GameEvents.TriggerLevelFailed(reason);
        }

        public void ResetLevel()
        {
            _actionsTakenThisLevel = 0;
            _levelActive = false;
            GameEvents.TriggerLevelReset();
            StartLevel(CurrentLevel);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            SetState(GameState.Playing);
            GameEvents.TriggerGameResumed();
        }

        // ─── Action Tracking ──────────────────────────────────

        /// <summary>
        /// Call this every time the player makes an interactive action.
        /// Used for star rating calculation.
        /// </summary>
        public void RegisterPlayerAction()
        {
            if (!_levelActive) return;
            _actionsTakenThisLevel++;
        }

        // ─── Star Rating ──────────────────────────────────────

        private int CalculateStars(float timeTaken, int actions)
        {
            // Placeholder logic — LevelConfig will supply par time
            // For now using constants

            bool fastEnough = timeTaken < 10f;
            bool efficientEnough = actions <= Constants.STAR_2_MAX_ACTIONS;
            bool perfect = actions <= Constants.STAR_3_MAX_ACTIONS && timeTaken < 8f;

            if (perfect)       return 3;
            if (fastEnough)    return 2;
            if (efficientEnough) return 2;

            return 1;
        }

        // ─── Events ───────────────────────────────────────────

        private void SubscribeToEvents()
        {
            GameEvents.OnPrincessSaved        += OnPrincessSaved;
            GameEvents.OnPrincessCaught       += OnPrincessCaught;
            GameEvents.OnPrincessHazardHit    += OnPrincessHazardHit;
            GameEvents.OnChainReactionStarted += OnChainReactionStarted;
            GameEvents.OnChainReactionEnded   += OnChainReactionEnded;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPrincessSaved        -= OnPrincessSaved;
            GameEvents.OnPrincessCaught       -= OnPrincessCaught;
            GameEvents.OnPrincessHazardHit    -= OnPrincessHazardHit;
            GameEvents.OnChainReactionStarted -= OnChainReactionStarted;
            GameEvents.OnChainReactionEnded   -= OnChainReactionEnded;
        }

        private void OnPrincessSaved()
        {
            CompleteLevel();
        }

        private void OnPrincessCaught()
        {
            FailLevel(LevelResult.Failed_PrincessCaught);
        }

        private void OnPrincessHazardHit()
        {
            FailLevel(LevelResult.Failed_PrincessHazard);
        }

        private void OnChainReactionStarted()
        {
            if (CurrentState == GameState.Playing)
                SetState(GameState.ChainReacting);
        }

        private void OnChainReactionEnded()
        {
            if (CurrentState == GameState.ChainReacting)
                SetState(GameState.Playing);
        }

        // ─── Getters ──────────────────────────────────────────

        public float GetLevelElapsedTime()
            => _levelActive ? Time.time - _levelStartTime : 0f;

        public int GetActionsCount()
            => _actionsTakenThisLevel;

        public bool IsPlaying()
            => CurrentState == GameState.Playing ||
               CurrentState == GameState.ChainReacting;
    }
}
