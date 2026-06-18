using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Levels;

namespace SnakeRescue.Hazards
{
    /// <summary>
    /// Base class for all hazards (Fire, Spike, Water, etc.).
    ///
    /// Responsibilities:
    /// - Detect overlap with Princess/Snake
    /// - Handle activation/deactivation
    /// - Handle neutralization (e.g. Water puts out Fire)
    /// - Report events to ChainReactionSystem
    ///
    /// Hazards are static triggers that cause failure
    /// if Princess touches them, or success if they
    /// eliminate the Snake.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardBase : MonoBehaviour
    {
        // ─── Identity ─────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] protected HazardType _hazardType;

        // ─── Settings ─────────────────────────────────────────
        [Header("Behavior")]
        [SerializeField] protected bool _startActive = true;
        [SerializeField] protected bool _damagesPrincess = true;
        [SerializeField] protected bool _damagesSnake    = false;
        [SerializeField] protected float _damageForce    = 5f;

        // ─── Components ───────────────────────────────────────
        protected Collider2D _collider;

        // ─── State ────────────────────────────────────────────
        public  HazardType HazardType => _hazardType;
        public  bool       IsActive   { get; protected set; }
        public  bool       IsNeutralized { get; protected set; }

        // ─── Unity Lifecycle ──────────────────────────────────

        protected virtual void Awake()
        {
            _collider = GetComponent<Collider2D>();
            gameObject.CompareTag(Constants.TAG_HAZARD);

            IsActive = _startActive;
            IsNeutralized = false;

            UpdateVisuals();
        }

        protected virtual void OnEnable()
        {
            if (IsActive)
                GameEvents.TriggerHazardActivated(_hazardType);
        }

        // ─── Activation ───────────────────────────────────────

        public virtual void Activate()
        {
            if (IsNeutralized) return;

            IsActive = true;
            _collider.enabled = true;

            UpdateVisuals();
            GameEvents.TriggerHazardActivated(_hazardType);

            ChainReactionSystem.Instance?.RegisterStep(
                $"{_hazardType} activated",
                transform.position,
                ReactionStepType.HazardActivated);
        }

        public virtual void Deactivate()
        {
            IsActive = false;
            _collider.enabled = false;

            UpdateVisuals();
        }

        // ─── Neutralization ───────────────────────────────────

        /// <summary>
        /// Called when something neutralizes this hazard.
        /// Example: Water neutralizes Fire.
        /// </summary>
        public virtual void Neutralize()
        {
            if (IsNeutralized) return;

            IsNeutralized = true;
            IsActive = false;
            _collider.enabled = false;

            UpdateVisuals();

            GameEvents.TriggerHazardNeutralized(_hazardType);

            ChainReactionSystem.Instance?.RegisterStep(
                $"{_hazardType} neutralized",
                transform.position,
                ReactionStepType.HazardRemoved);

            Debug.Log($"[Hazard] {_hazardType} neutralized.");
        }

        // ─── Collision Handling ───────────────────────────────

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive || IsNeutralized) return;

            // Princess hit hazard
            if (_damagesPrincess && other.CompareTag(Constants.TAG_PRINCESS))
            {
                PrincessController princess = other.GetComponent<PrincessController>();
                if (princess != null && princess.IsAlive)
                {
                    princess.OnHazardHit();
                }
            }

            // Snake hit hazard
            if (_damagesSnake && other.CompareTag(Constants.TAG_SNAKE))
            {
                SnakeController snake = other.GetComponent<SnakeController>();
                if (snake != null && snake.IsAlive)
                {
                    snake.TakeDamage(_damageForce, transform.position);
                }
            }
        }

        // ─── Visuals ──────────────────────────────────────────

        protected virtual void UpdateVisuals()
        {
            // Override in subclasses to toggle particles/sprites
            gameObject.SetActive(IsActive && !IsNeutralized);
        }

        // ─── Interaction ──────────────────────────────────────

        /// <summary>
        /// Some hazards can be interacted with directly.
        /// Override in subclasses if needed.
        /// </summary>
        public virtual void OnInteract()
        {
            // Default: do nothing
        }
    }
}
