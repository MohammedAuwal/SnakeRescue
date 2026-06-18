using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Utils;

namespace SnakeRescue.Characters.Snake
{
    /// <summary>
    /// The main brain of the Snake character.
    /// Coordinates all snake subsystems:
    /// - SnakeStateManager  (current behavior state)
    /// - SnakePathfinder    (how it navigates to princess)
    /// - SnakeAnimator      (visuals)
    ///
    /// Snake Design Rules:
    /// 1. Snake is NOT intelligent — it is a pressure system
    /// 2. Snake only moves when a path to princess is open
    /// 3. Snake is destroyed when physics objects hit it
    /// 4. Snake creates fear — not complex gameplay
    ///
    /// The snake is a TIMER in disguise.
    /// If the player does nothing, snake reaches princess.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class SnakeController : MonoBehaviour
    {
        // ─── Subsystems ───────────────────────────────────────
        [Header("Subsystems")]
        [SerializeField] private SnakeStateManager _stateManager;
        [SerializeField] private SnakePathfinder   _pathfinder;
        [SerializeField] private SnakeAnimator     _snakeAnimator;

        // ─── Settings ─────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float _patrolSpeed      = 1.0f;
        [SerializeField] private float _chaseSpeed       = 2.5f;
        [SerializeField] private float _detectionRadius  = 5.0f;
        [SerializeField] private float _attackRadius     = 0.8f;

        [Header("Behavior")]
        [SerializeField] private float _activationDelay  = 0.5f;
        [SerializeField] private float _pathUpdateRate   = 0.3f;
        [SerializeField] private bool  _startActive      = false;

        [Header("Death")]
        [SerializeField] private float _deathForceThreshold = 3.0f;
        [SerializeField] private ParticleSystem _deathParticles;

        // ─── Components ───────────────────────────────────────
        private Rigidbody2D _rigidbody;
        private Collider2D  _collider;

        // ─── Runtime ──────────────────────────────────────────
        public  SnakeState CurrentState  => _stateManager?.CurrentState
                                           ?? SnakeState.Idle;
        public  bool       IsAlive       { get; private set; } = true;
        public  Transform  TargetPrincess { get; private set; }

        private float _pathUpdateTimer  = 0f;
        private float _activationTimer  = 0f;
        private bool  _activated        = false;
        private Vector2 _moveDirection  = Vector2.zero;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider  = GetComponent<Collider2D>();

            ValidateSubsystems();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (!GameManager.Instance?.IsPlaying() ?? true) return;

            HandleActivation();
            HandleBehavior();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || !_activated) return;

            ApplyMovement();
        }

        // ─── Initialization ───────────────────────────────────

        private void Initialize()
        {
            IsAlive        = true;
            _activated     = _startActive;
            _activationTimer = 0f;
            _pathUpdateTimer = 0f;

            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints  = RigidbodyConstraints2D.FreezeRotation;

            _stateManager?.Initialize();
            _snakeAnimator?.Initialize();

            SetState(_startActive ? SnakeState.Patrolling : SnakeState.Idle);

            // Find princess automatically
            FindPrincess();

            Debug.Log("[Snake] Initialized.");
        }

        private void ValidateSubsystems()
        {
            if (_stateManager == null)
                _stateManager = GetComponent<SnakeStateManager>();

            if (_pathfinder == null)
                _pathfinder = GetComponent<SnakePathfinder>();

            if (_snakeAnimator == null)
                _snakeAnimator = GetComponent<SnakeAnimator>();
        }

        private void FindPrincess()
        {
            GameObject princess =
                GameObject.FindGameObjectWithTag(Constants.TAG_PRINCESS);

            if (princess != null)
                TargetPrincess = princess.transform;
        }

        // ─── Activation ───────────────────────────────────────

        private void HandleActivation()
        {
            if (_activated) return;

            _activationTimer += Time.deltaTime;

            if (_activationTimer >= _activationDelay)
            {
                Activate();
            }
        }

        public void Activate()
        {
            if (_activated || !IsAlive) return;

            _activated = true;
            SetState(SnakeState.Patrolling);
            GameEvents.TriggerSnakeActivated();

            Debug.Log("[Snake] Activated.");
        }

        // ─── Behavior Loop ────────────────────────────────────

        private void HandleBehavior()
        {
            if (!_activated) return;

            _pathUpdateTimer += Time.deltaTime;

            bool shouldUpdate = _pathUpdateTimer >= _pathUpdateRate;

            if (shouldUpdate)
            {
                _pathUpdateTimer = 0f;
                EvaluateSituation();
            }
        }

        private void EvaluateSituation()
        {
            if (TargetPrincess == null)
            {
                FindPrincess();
                return;
            }

            float distToPrincess = Vector2.Distance(
                transform.position, TargetPrincess.position);

            // Attack range — princess caught
            if (distToPrincess <= _attackRadius)
            {
                SetState(SnakeState.Attacking);
                OnReachedPrincess();
                return;
            }

            // Can snake reach princess?
            bool pathExists = _pathfinder?.HasPathToPrincess(
                transform.position,
                TargetPrincess.position) ?? false;

            if (pathExists)
            {
                if (distToPrincess <= _detectionRadius)
                {
                    SetState(SnakeState.Chasing);
                    _moveDirection = _pathfinder.GetNextDirection(
                        transform.position, TargetPrincess.position);
                }
                else
                {
                    SetState(SnakeState.Patrolling);
                    _moveDirection = Vector2.zero;
                }
            }
            else
            {
                // Path blocked — snake waits
                SetState(SnakeState.Idle);
                _moveDirection = Vector2.zero;
            }
        }

        // ─── Movement ─────────────────────────────────────────

        private void ApplyMovement()
        {
            if (_moveDirection == Vector2.zero) return;

            float speed = CurrentState == SnakeState.Chasing
                ? _chaseSpeed
                : _patrolSpeed;

            Vector2 velocity = _moveDirection.normalized * speed;
            _rigidbody.MovePosition(
                _rigidbody.position + velocity * Time.fixedDeltaTime);

            // Face movement direction
            _snakeAnimator?.FaceDirection(_moveDirection);
        }

        // ─── State Control ────────────────────────────────────

        public void SetState(SnakeState newState)
        {
            if (!IsAlive) return;
            if (CurrentState == newState) return;

            _stateManager?.SetState(newState);
            _snakeAnimator?.OnStateChanged(newState);

            GameEvents.TriggerSnakeStateChanged(newState);
        }

        // ─── Death System ─────────────────────────────────────

        public void TakeDamage(float impactForce, Vector3 impactPosition)
        {
            if (!IsAlive) return;

            if (impactForce >= _deathForceThreshold)
            {
                Die(impactPosition);
            }
            else
            {
                // Stun briefly
                SetState(SnakeState.Stunned);
                Invoke(nameof(RecoverFromStun), 1.0f);
            }
        }

        private void RecoverFromStun()
        {
            if (!IsAlive) return;
            SetState(SnakeState.Patrolling);
        }

        public void Die(Vector3 impactPosition)
        {
            if (!IsAlive) return;

            IsAlive    = false;
            _activated = false;

            SetState(SnakeState.Dead);

            // Visual death effect
            if (_deathParticles != null)
            {
                _deathParticles.transform.position = impactPosition;
                _deathParticles.Play();
            }

            // Disable physics
            _rigidbody.velocity        = Vector2.zero;
            _rigidbody.simulated       = false;
            _collider.enabled          = false;

            GameEvents.TriggerSnakeDead();
            GameEvents.TriggerPlaySFX("SFX_SnakeDead");

            // Register with chain reaction system
            ChainReactionSystem.Instance?.RegisterStep(
                "Snake killed",
                transform.position,
                ReactionStepType.EnemyDead);

            Debug.Log("[Snake] Dead.");

            // Destroy after animation plays
            Destroy(gameObject, 1.5f);
        }

        // ─── Reached Princess ─────────────────────────────────

        private void OnReachedPrincess()
        {
            GameEvents.TriggerSnakeReachedPrincess();

            // Tell princess she was caught
            PrincessController princess =
                TargetPrincess?.GetComponent<PrincessController>();

            princess?.OnCaughtBySnake();
        }

        // ─── Collision — Physics Objects Hit Snake ────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsAlive) return;

            if (!collision.gameObject.CompareTag(Constants.TAG_OBJECT))
                return;

            // Calculate impact force from collision
            float impactForce = collision.relativeVelocity.magnitude;

            // Register collision in chain reaction
            ChainReactionSystem.Instance?.RegisterStep(
                $"Object hit snake: {collision.gameObject.name}",
                collision.contacts[0].point,
                ReactionStepType.EnemyHit);

            TakeDamage(impactForce, collision.contacts[0].point);
        }

        // ─── Event Subscriptions ──────────────────────────────

        private void SubscribeToEvents()
        {
            GameEvents.OnLevelReset     += ResetSnake;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelReset     -= ResetSnake;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (!IsAlive) return;

            switch (state)
            {
                case GameState.Paused:
                    _rigidbody.simulated = false;
                    break;

                case GameState.Playing:
                    _rigidbody.simulated = true;
                    break;
            }
        }

        private void ResetSnake()
        {
            CancelInvoke();
            Initialize();
        }

        // ─── Gizmos ───────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _attackRadius);
        }

        // ─── Getters ──────────────────────────────────────────

        public float GetDistanceToPrincess()
        {
            if (TargetPrincess == null) return float.MaxValue;
            return Vector2.Distance(
                transform.position, TargetPrincess.position);
        }

        public bool IsChasing()
            => CurrentState == SnakeState.Chasing;

        public bool IsInAttackRange()
            => GetDistanceToPrincess() <= _attackRadius;
    }
}
