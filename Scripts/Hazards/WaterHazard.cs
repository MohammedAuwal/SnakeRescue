using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Hazards;

namespace SnakeRescue.Hazards
{
    /// <summary>
    /// Water hazard.
    ///
    /// Behavior:
    /// - Flows when activated (optional animation)
    /// - Neutralizes Fire hazards in range
    /// - Damages Snake (drowning) but NOT Princess (safe passage)
    ///
    /// Puzzle Role:
    /// - Extinguishes fire barriers
    /// - Kills snake if snake flows into it
    /// - Creates safe path for princess
    /// </summary>
    public class WaterHazard : HazardBase
    {
        // ─── References ───────────────────────────────────────
        [Header("Visuals")]
        [SerializeField] private ParticleSystem _waterParticles;
        [SerializeField] private SpriteRenderer _waterSprite;
        [SerializeField] private Animator        _waterAnimator;

        [Header("Interaction")]
        [SerializeField] private float _flowRadius      = 3f;
        [SerializeField] private float _flowDuration    = 2f;
        [SerializeField] private LayerMask _fireLayer;

        // ─── Runtime ──────────────────────────────────────────
        private bool _isFlowing = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        protected override void Awake()
        {
            _hazardType = HazardType.Water;
            _damagesPrincess = false; // Princess can swim/pass
            _damagesSnake = true;     // Snake drowns

            base.Awake();
        }

        // ─── Activation Overrides ─────────────────────────────

        public override void Activate()
        {
            base.Activate();

            if (_waterParticles != null) _waterParticles.Play();
            if (_waterSprite != null) _waterSprite.enabled = true;
            if (_waterAnimator != null) _waterAnimator.SetBool("Flowing", true);

            _isFlowing = true;

            // Check for fires to extinguish
            Invoke(nameof(CheckForFires), 0.5f);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (_waterParticles != null) _waterParticles.Stop();
            if (_waterSprite != null) _waterSprite.enabled = false;
            if (_waterAnimator != null) _waterAnimator.SetBool("Flowing", false);

            _isFlowing = false;
        }

        // ─── Water Logic ──────────────────────────────────────

        private void CheckForFires()
        {
            if (!IsActive || IsNeutralized) return;

            // Find all fire hazards in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                _flowRadius,
                _fireLayer);

            foreach (Collider2D hit in hits)
            {
                FireHazard fire = hit.GetComponent<FireHazard>();
                if (fire != null && !fire.IsNeutralized)
                {
                    fire.ExtinguishByWater(transform.position);
                }
            }

            // Check for snake in water
            Collider2D[] snakeHits = Physics2D.OverlapCircleAll(
                transform.position,
                _flowRadius,
                LayerMask.GetMask(Constants.LAYER_CHARACTERS));

            foreach (Collider2D hit in snakeHits)
            {
                if (hit.CompareTag(Constants.TAG_SNAKE))
                {
                    SnakeController snake = hit.GetComponent<SnakeController>();
                    if (snake != null && snake.IsAlive)
                    {
                        snake.TakeDamage(_damageForce, transform.position);
                    }
                }
            }
        }

        // ─── Collision Overrides ──────────────────────────────

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            base.OnTriggerEnter2D(other);

            // Additional water-specific logic
            if (other.CompareTag(Constants.TAG_SNAKE) && _damagesSnake)
            {
                SnakeController snake = other.GetComponent<SnakeController>();
                snake?.TakeDamage(_damageForce, transform.position);
            }
        }

        // ─── Visuals ──────────────────────────────────────────

        protected override void UpdateVisuals()
        {
            if (IsNeutralized)
            {
                // Show stagnant water
                if (_waterSprite != null)
                    _waterSprite.color = Color.gray;
            }
            else if (IsActive)
            {
                // Show flowing water
                if (_waterSprite != null)
                    _waterSprite.color = Color.blue;
            }
        }

        // ─── Gizmos ───────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _flowRadius);
        }
    }
}
