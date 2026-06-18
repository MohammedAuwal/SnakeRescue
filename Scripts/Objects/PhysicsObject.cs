using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Levels;
using SnakeRescue.Managers;

namespace SnakeRescue.Objects
{
    /// <summary>
    /// Base class for all interactable physics objects.
    /// Ball, Rock, Weight, etc. inherit from this.
    ///
    /// Responsibilities:
    /// - Register collisions with ChainReactionSystem
    /// - Handle being held by a Pin
    /// - Report destruction to EventManager
    /// - Implement IInteractable if tapable
    ///
    /// This ensures all objects behave consistently
    /// within the physics puzzle system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PhysicsObject : MonoBehaviour, IInteractable
    {
        // ─── Identity ─────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] protected ObjectType _objectType;

        // ─── Settings ─────────────────────────────────────────
        [Header("Physics")]
        [SerializeField] protected bool _startKinematic = true;
        [SerializeField] protected float _destroyForce  = 10f;

        // ─── Components ───────────────────────────────────────
        protected Rigidbody2D _rigidbody;
        protected Collider2D  _collider;

        // ─── State ────────────────────────────────────────────
        public  ObjectType ObjectType => _objectType;
        public  bool       IsHeld     { get; protected set; } = false;
        public  bool       IsAlive    { get; private set; }   = true;

        // ─── Unity Lifecycle ──────────────────────────────────

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider  = GetComponent<Collider2D>();

            InitializePhysics();
        }

        protected virtual void OnEnable()
        {
            ChainReactionSystem.Instance?.TrackBody(_rigidbody);
        }

        protected virtual void OnDisable()
        {
            ChainReactionSystem.Instance?.UntrackBody(_rigidbody);
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsAlive) return;

            float impact = collision.relativeVelocity.magnitude;

            // Report collision to chain reaction system
            ChainReactionSystem.Instance?.RegisterStep(
                $"{_objectType} hit {collision.gameObject.name}",
                transform.position,
                ReactionStepType.Collision);

            // Check for destruction
            if (impact >= _destroyForce)
            {
                OnDestroyedByForce(impact);
            }
        }

        // ─── Initialization ───────────────────────────────────

        protected virtual void InitializePhysics()
        {
            if (_startKinematic)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody.gravityScale = 0f;
            }
            else
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody.gravityScale = 1f;
            }

            gameObject.CompareTag(Constants.TAG_OBJECT);
        }

        // ─── Interaction ──────────────────────────────────────

        public virtual bool CanInteract()
        {
            return IsAlive && !IsHeld;
        }

        public virtual void OnInteract()
        {
            // Default: do nothing
            // Subclasses (like Lever) override this
            Debug.Log($"[PhysicsObject] {_objectType} interacted.");
        }

        // ─── Pin Holding ──────────────────────────────────────

        public virtual void SetHeld(bool held)
        {
            IsHeld = held;

            if (held)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody.velocity = Vector2.zero;
            }
            else
            {
                ReleaseObject();
            }
        }

        public virtual void ReleaseObject()
        {
            IsHeld = false;
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = 1f;

            // Wake up physics
            _rigidbody.WakeUp();

            Debug.Log($"[PhysicsObject] {_objectType} released.");
        }

        // ─── Destruction ──────────────────────────────────────

        protected virtual void OnDestroyedByForce(float force)
        {
            if (!IsAlive) return;
            IsAlive = false;

            ChainReactionSystem.Instance?.RegisterStep(
                $"{_objectType} destroyed",
                transform.position,
                ReactionStepType.Destruction);

            GameEvents.TriggerObjectDestroyed(_objectType);
            EventManager.Publish(new ObjectTriggeredEvent
            {
                ObjectName = gameObject.name,
                ObjectType = _objectType.ToString(),
                Position   = transform.position
            });

            // Disable collision before destroy
            _collider.enabled = false;
            _rigidbody.simulated = false;

            // Destroy after brief delay for effects
            Destroy(gameObject, 0.5f);
        }

        // ─── IPoolable Support ────────────────────────────────

        public virtual void OnSpawnedFromPool()
        {
            IsAlive = true;
            IsHeld  = false;
            _collider.enabled = true;
            _rigidbody.simulated = true;
            gameObject.SetActive(true);
        }

        public virtual void OnReturnedToPool()
        {
            IsAlive = false;
            IsHeld  = false;
            _rigidbody.velocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }
}
