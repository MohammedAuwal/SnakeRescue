using System.Collections;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Data;
using SnakeRescue.Systems;
using SnakeRescue.Utils;

namespace SnakeRescue.Levels
{
    /// <summary>
    /// Controls everything that happens inside a level.
    /// Lives in the GameScene — created fresh every level load.
    ///
    /// Responsibilities:
    /// - Start / stop level timer
    /// - Track win and fail conditions
    /// - Coordinate LevelLoader and ChainReactionSystem
    /// - Tell GameManager when level is done
    ///
    /// One LevelManager per scene.
    /// It does NOT persist between scenes.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // ─── Singleton (Scene-scoped) ─────────────────────────
        public static LevelManager Instance { get; private set; }

        // ─── References ───────────────────────────────────────
        [Header("References")]
        [SerializeField] private LevelLoader         _levelLoader;
        [SerializeField] private ChainReactionSystem _chainReaction;

        // ─── State ────────────────────────────────────────────
        public  LevelConfig CurrentConfig  { get; private set; }
        public  bool        LevelActive    { get; private set; } = false;
        public  float       ElapsedTime    { get; private set; } = 0f;
        public  int         ActionsTaken   { get; private set; } = 0;

        private bool  _snakeDefeated     = false;
        private bool  _princessSafe      = false;
        private bool  _levelEnded        = false;
        private int   _failCount         = 0;
        private float _levelStartTime    = 0f;

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
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!LevelActive) return;

            ElapsedTime = Time.time - _levelStartTime;
        }

        // ─── Initialization ───────────────────────────────────

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[LevelManager] GameManager not found.");
                return;
            }

            int levelIndex = GameManager.Instance.CurrentLevel;
            InitializeLevel(levelIndex);
        }

        public void InitializeLevel(int levelIndex)
        {
            // Load config
            CurrentConfig = GameConfig.Instance?.GetLevelConfig(levelIndex);

            if (CurrentConfig == null)
            {
                Debug.LogError(
                    $"[LevelManager] No config for level {levelIndex}");
                return;
            }

            if (!CurrentConfig.IsValid())
            {
                Debug.LogError(
                    $"[LevelManager] Invalid config for level {levelIndex}");
                return;
            }

            // Reset state
            _snakeDefeated  = false;
            _princessSafe   = false;
            _levelEnded     = false;
            ActionsTaken    = 0;
            ElapsedTime     = 0f;

            Debug.Log(
                $"[LevelManager] Initializing level {levelIndex}: " +
                $"{CurrentConfig.LevelName}");

            // Load objects into scene
            _levelLoader?.LoadLevel(CurrentConfig);

            // Begin intro sequence then start
            StartCoroutine(LevelIntroSequence());
        }

        // ─── Intro Sequence ───────────────────────────────────

        private IEnumerator LevelIntroSequence()
        {
            GameManager.Instance.SetState(GameState.LevelIntro);

            // Show level name / objective briefly
            yield return new WaitForSeconds(1.5f);

            // Skip intro in debug mode
            if (GameConfig.Instance != null &&
                GameConfig.Instance.SkipIntros)
            {
                StartLevel();
                yield break;
            }

            // Show intro dialogue if exists
            if (!string.IsNullOrEmpty(CurrentConfig.IntroDialogue))
            {
                yield return new WaitForSeconds(2.0f);
            }

            StartLevel();
        }

        // ─── Level Start ──────────────────────────────────────

        public void StartLevel()
        {
            LevelActive     = true;
            _levelStartTime = Time.time;

            GameManager.Instance.SetState(GameState.Playing);
            GameEvents.TriggerLevelStarted(CurrentConfig.LevelIndex);

            // Play ambient sound if defined
            if (!string.IsNullOrEmpty(CurrentConfig.AmbientSound))
            {
                EventManager.Publish(new LevelStartedEvent
                {
                    LevelIndex = CurrentConfig.LevelIndex,
                    LevelName  = CurrentConfig.LevelName
                });
            }

            Debug.Log(
                $"[LevelManager] Level started: {CurrentConfig.LevelName}");
        }

        // ─── Action Registration ──────────────────────────────

        /// <summary>
        /// Called every time the player interacts with an object.
        /// Drives star rating and hint system.
        /// </summary>
        public void RegisterAction()
        {
            if (!LevelActive) return;

            ActionsTaken++;
            GameManager.Instance?.RegisterPlayerAction();

            // Start chain reaction tracking
            _chainReaction?.BeginReaction(
                "PlayerAction",
                Vector3.zero);

            Debug.Log(
                $"[LevelManager] Action #{ActionsTaken} registered.");
        }

        // ─── Win Condition Checks ─────────────────────────────

        private void CheckWinConditions()
        {
            if (_levelEnded) return;

            bool snakeConditionMet =
                !CurrentConfig.MustKillSnake || _snakeDefeated;

            bool princessConditionMet =
                !CurrentConfig.MustSavePrincess || _princessSafe;

            if (snakeConditionMet && princessConditionMet)
            {
                TriggerLevelComplete();
            }
        }

        // ─── Level Complete ───────────────────────────────────

        private void TriggerLevelComplete()
        {
            if (_levelEnded) return;

            _levelEnded = true;
            LevelActive = false;

            _chainReaction?.ForceEnd();

            StartCoroutine(LevelCompleteSequence());
        }

        private IEnumerator LevelCompleteSequence()
        {
            // Brief pause to let victory animation play
            yield return new WaitForSeconds(0.5f);

            // Show outro if exists
            if (!string.IsNullOrEmpty(CurrentConfig.OutroDialogue))
            {
                yield return new WaitForSeconds(1.5f);
            }

            // Final delay before result screen
            yield return new WaitForSeconds(
                Constants.UI_RESULT_DELAY);

            GameManager.Instance?.CompleteLevel();

            Debug.Log("[LevelManager] Level complete triggered.");
        }

        // ─── Level Fail ───────────────────────────────────────

        private void TriggerLevelFail(LevelResult reason)
        {
            if (_levelEnded) return;

            _levelEnded = true;
            LevelActive = false;
            _failCount++;

            _chainReaction?.ForceEnd();

            StartCoroutine(LevelFailSequence(reason));
        }

        private IEnumerator LevelFailSequence(LevelResult reason)
        {
            // Let fail animation play
            yield return new WaitForSeconds(0.8f);

            GameManager.Instance?.FailLevel(reason);

            Debug.Log($"[LevelManager] Level failed: {reason}");
        }

        // ─── Level Reset ──────────────────────────────────────

        public void ResetLevel()
        {
            StopAllCoroutines();

            _snakeDefeated = false;
            _princessSafe  = false;
            _levelEnded    = false;
            LevelActive    = false;
            ActionsTaken   = 0;
            ElapsedTime    = 0f;

            _chainReaction?.ResetSystem();
            _levelLoader?.ClearLevel();

            GameEvents.TriggerLevelReset();

            // Re-initialize after short delay
            StartCoroutine(DelayedReinit());
        }

        private IEnumerator DelayedReinit()
        {
            yield return new WaitForSeconds(Constants.LEVEL_RESET_DELAY);
            InitializeLevel(GameManager.Instance.CurrentLevel);
        }

        // ─── Hint ─────────────────────────────────────────────

        public bool ShouldShowHint()
        {
            if (CurrentConfig == null) return false;
            return CurrentConfig.ShouldShowHint(_failCount);
        }

        public string GetCurrentHint()
        {
            if (CurrentConfig == null)
                return "Think about the chain reaction.";

            return CurrentConfig.GetHint(_failCount);
        }

        // ─── Event Subscriptions ──────────────────────────────

        private void SubscribeToEvents()
        {
            GameEvents.OnSnakeDead          += OnSnakeDead;
            GameEvents.OnPrincessSaved      += OnPrincessSaved;
            GameEvents.OnPrincessCaught     += OnPrincessCaught;
            GameEvents.OnPrincessHazardHit  += OnPrincessHazardHit;
            GameEvents.OnLevelReset         += ResetLevel;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnSnakeDead          -= OnSnakeDead;
            GameEvents.OnPrincessSaved      -= OnPrincessSaved;
            GameEvents.OnPrincessCaught     -= OnPrincessCaught;
            GameEvents.OnPrincessHazardHit  -= OnPrincessHazardHit;
            GameEvents.OnLevelReset         -= ResetLevel;
        }

        // ─── Event Handlers ───────────────────────────────────

        private void OnSnakeDead()
        {
            _snakeDefeated = true;

            _chainReaction?.RegisterStep(
                "Snake defeated",
                Vector3.zero,
                ReactionStepType.EnemyDead);

            CheckWinConditions();
        }

        private void OnPrincessSaved()
        {
            _princessSafe = true;

            _chainReaction?.RegisterStep(
                "Princess safe",
                Vector3.zero,
                ReactionStepType.PrincessSafe);

            CheckWinConditions();
        }

        private void OnPrincessCaught()
        {
            TriggerLevelFail(LevelResult.Failed_PrincessCaught);
        }

        private void OnPrincessHazardHit()
        {
            TriggerLevelFail(LevelResult.Failed_PrincessHazard);
        }

        // ─── Getters ──────────────────────────────────────────

        public int GetFailCount()         => _failCount;
        public bool IsSnakeDefeated()     => _snakeDefeated;
        public bool IsPrincessSafe()      => _princessSafe;
        public LevelType GetLevelType()   => CurrentConfig?.Type
                                            ?? LevelType.KillSnake;
    }
}
