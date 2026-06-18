using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Utils;

namespace SnakeRescue.Characters.Princess
{
    /// <summary>
    /// The main brain of the Princess character.
    /// Coordinates all princess subsystems:
    /// - PrincessStateManager  (what state she is in)
    /// - PrincessAnimator      (how she looks)
    /// - PrincessReactionSystem (how she reacts to events)
    ///
    /// This script owns the princess GameObject.
    /// All other princess scripts report to this one.
    ///
    /// The princess does NOT move by player input.
    /// She reacts to the world around her.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PrincessController : MonoBehaviour
    {
        // ─── Subsystems ───────────────────────────────────────
        [Header("Subsystems")]
        [SerializeField] private PrincessStateManager   _stateManager;
        [SerializeField] private PrincessAnimator       _animator;
        [SerializeField] private PrincessReactionSystem _reactionSystem;

        // ─── Settings ─────────────────────────────────────────
        [Header("Detection")]
        [SerializeField] private float _dangerRadius  = 3.0f;
        [SerializeField] private float _panicRadius   = 1.5f;
        [SerializeField] private LayerMask _snakeLayer;
        [SerializeField] private LayerMask _hazardLayer;

        [Header("Safe Zone")]
        [SerializeField] private Transform _safeZoneTarget;
        [SerializeField] private bool      _hasSafeZone = false;

        // ─── Components ───────────────────────────────────────
        private Rigidbody2D _rigidbody;
        private Collider2D  _collider;

        // ─── Runtime State ────────────────────────────────────
        public  PrincessState CurrentState   => _stateManager?.CurrentState
                                               ?? PrincessState.Idle;
        public  bool          IsAlive        { get; private set; } = true;
        public  bool          IsSaved        { get; private set; } = false;
        private float         _nearestThreat = float.MaxValue;
        private float         _detectionTimer = 0f;
        private const float   DETECTION_RATE  = 0.15f;

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
            if (!IsAlive || IsSaved) return;
            if (!GameManager.Instance?.IsPlaying() ?? true) return;

            _detectionTimer += Time.deltaTime;

            if (_detectionTimer >= DETECTION_RATE)
            {
                _detectionTimer = 0f;
                ScanForThreats();
            }
        }

        // ─── Initialization ───────────────────────────────────

        private void Initialize()
        {
            IsAlive = true;
            IsSaved = false;

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.gravityScale = 0f;

            _stateManager?.Initialize();
            _animator?.Initialize();
            _reactionSystem?.Initialize(this);

            SetState(PrincessState.Idle);

            Debug.Log("[Princess] Initialized.");
        }

        private void ValidateSubsystems()
        {
            if (_stateManager == null)
                _stateManager = GetComponent<PrincessStateManager>();

            if (_animator == null)
                _animator = GetComponent<PrincessAnimator>();

            if (_reactionSystem == null)
                _reactionSystem = GetComponent<PrincessReactionSystem>();

            if (_stateManager == null)
                Debug.LogError("[Princess] Missing PrincessStateManager.");

            if (_animator == null)
                Debug.LogError("[Princess] Missing PrincessAnimator.");
        }

        // ─── Threat Detection ─────────────────────────────────

        private void ScanForThreats()
        {
            _nearestThreat = float.MaxValue;

            // Check for snake in range
            Collider2D[] snakeHits = Physics2D.OverlapCircleAll(
                transform.position,
                _dangerRadius,
                _snakeLayer);

            foreach (Collider2D hit in snakeHits)
            {
                float dist = Vector2.Distance(
                    transform.position, hit.transform.position);

                if (dist < _nearestThreat)
                    _nearestThreat = dist;
            }

            // Check for hazards in range
            Collider2D[] hazardHits = Physics2D.OverlapCircleAll(
                transform.position,
                _dangerRadius,
                _hazardLayer);

            foreach (Collider2D hit in hazardHits)
            {
                float dist = Vector2.Distance(
                    transform.position, hit.transform.position);

                if (dist < _nearestThreat)
                    _nearestThreat = dist;
            }

            // Update state based on nearest threat
            UpdateThreatState();
        }

        private void UpdateThreatState()
        {
            if (IsSaved || !IsAlive) return;

            if (_nearestThreat <= _panicRadius)
            {
                SetState(PrincessState.Panic);
            }
            else if (_nearestThreat <= _dangerRadius)
            {
                SetState(PrincessState.Fear);
            }
            else if (_nearestThreat < float.MaxValue)
            {
                SetState(PrincessState.Alert);
            }
            else
            {
                // No threats nearby
                if (CurrentState == PrincessState.Alert ||
                    CurrentState == PrincessState.Fear  ||
                    CurrentState == PrincessState.Panic)
                {
                    SetState(PrincessState.Idle);
                }
            }
        }

        // ─── State Control ────────────────────────────────────

        public void SetState(PrincessState newState)
        {
            if (!IsAlive) return;
            if (CurrentState == newState) return;

            PrincessState previous = CurrentState;
            _stateManager?.SetState(newState);
            _animator?.OnStateChanged(newState);
            _reactionSystem?.OnStateChanged(previous, newState);

            GameEvents.TriggerPrincessStateChanged(newState);
        }

        // ─── External Events ──────────────────────────────────

        public void OnSnakeApproaching(Vector3 snakePosition)
        {
            if (!IsAlive || IsSaved) return;

            float dist = Vector2.Distance(
                transform.position, snakePosition);

            if (dist <= _panicRadius)
                SetState(PrincessState.Panic);
            else if (dist <= _dangerRadius)
                SetState(PrincessState.Fear);

            _reactionSystem?.ReactToSnake(snakePosition);
        }

        public void OnHazardNearby(Vector3 hazardPosition)
        {
            if (!IsAlive || IsSaved) return;

            _reactionSystem?.ReactToHazard(hazardPosition);
        }

        public void OnSaved()
        {
            if (IsSaved) return;

            IsSaved = true;
            SetState(PrincessState.Celebrating);

            GameEvents.TriggerPrincessSaved();
            GameEvents.TriggerPlaySFX("SFX_PrincessCheer");

            Debug.Log("[Princess] Saved!");
        }

        public void OnCaughtBySnake()
        {
            if (!IsAlive) return;

            IsAlive = false;
            SetState(PrincessState.Dead);

            GameEvents.TriggerPrincessCaught();
            GameEvents.TriggerPlaySFX("SFX_PrincessScream");

            Debug.Log("[Princess] Caught by snake.");
        }

        public void OnHazardHit()
        {
            if (!IsAlive) return;

            IsAlive = false;
            SetState(PrincessState.Dead);

            GameEvents.TriggerPrincessHazardHit();
            GameEvents.TriggerPlaySFX("SFX_PrincessScream");

            Debug.Log("[Princess] Hit by hazard.");
        }

        // ─── Collision Handling ───────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAlive || IsSaved) return;

            // Snake caught princess
            if (other.CompareTag(Constants.TAG_SNAKE))
            {
                OnCaughtBySnake();
                return;
            }

            // Hazard hit princess
            if (other.CompareTag(Constants.TAG_HAZARD))
            {
                OnHazardHit();
                return;
            }

            // Princess reached safe zone
            if (_hasSafeZone && _safeZoneTarget != null)
            {
                float distToSafe = Vector2.Distance(
                    transform.position,
                    _safeZoneTarget.position);

                if (distToSafe < 0.5f)
                {
                    OnSaved();
                }
            }
        }

        // ─── Event Subscriptions ──────────────────────────────

        private void SubscribeToEvents()
        {
            GameEvents.OnSnakeDead   += OnSnakeDefeated;
            GameEvents.OnLevelReset  += ResetPrincess;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnSnakeDead   -= OnSnakeDefeated;
            GameEvents.OnLevelReset  -= ResetPrincess;
        }

        private void OnSnakeDefeated()
        {
            if (!IsAlive || IsSaved) return;

            // Snake is dead — princess is relieved
            SetState(PrincessState.Relief);

            // After brief relief, trigger saved
            Invoke(nameof(OnSaved), 1.0f);
        }

        private void ResetPrincess()
        {
            CancelInvoke();
            Initialize();
        }

        // ─── Gizmos ───────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // Danger radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _dangerRadius);

            // Panic radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _panicRadius);
        }

        // ─── Getters ──────────────────────────────────────────

        public float GetNearestThreatDistance() => _nearestThreat;
        public bool  IsInDanger()
            => _nearestThreat <= _dangerRadius;
        public bool  IsInPanic()
            => _nearestThreat <= _panicRadius;
    }
}
