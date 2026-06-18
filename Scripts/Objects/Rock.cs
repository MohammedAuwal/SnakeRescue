using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Objects;

namespace SnakeRescue.Objects
{
    /// <summary>
    /// A heavy physics object.
    ///
    /// Behavior:
    /// - Low bounciness
    /// - High mass (pushes other objects)
    /// - High damage on impact (kills Snake easily)
    /// - Can break certain hazards (like weak walls later)
    ///
    /// Use Rock for:
    /// - Crushing enemies
    /// - Weighting down switches
    /// - Heavy chain reactions
    /// </summary>
    public class Rock : PhysicsObject
    {
        // ─── Settings ─────────────────────────────────────────
        [Header("Rock Specifics")]
        [SerializeField] private float _rockMass      = 5.0f;
        [SerializeField] private float _damageForce   = 8f;
        [SerializeField] private AudioClip _crushSound;

        // ─── Unity Lifecycle ──────────────────────────────────

        protected override void Awake()
        {
            _objectType = ObjectType.Rock;
            base.Awake();
        }

        // ─── Initialization ───────────────────────────────────

        protected override void InitializePhysics()
        {
            base.InitializePhysics();

            _rigidbody.mass = _rockMass;

            // Low bounce, high friction
            PhysicsMaterial2D mat = new PhysicsMaterial2D("RockMat");
            mat.bounciness = 0.1f;
            mat.friction = 0.6f;

            _collider.sharedMaterial = mat;
        }

        // ─── Collision Overrides ──────────────────────────────

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);

            // Check if hitting Snake
            if (collision.gameObject.CompareTag(Constants.TAG_SNAKE))
            {
                SnakeController snake = collision.gameObject.GetComponent<SnakeController>();
                if (snake != null && snake.IsAlive)
                {
                    float impact = collision.relativeVelocity.magnitude;
                    if (impact >= _damageForce)
                    {
                        snake.TakeDamage(impact, transform.position);
                        GameEvents.TriggerPlaySFX("SFX_SnakeDead");
                    }
                }
            }

            // Heavy impact sound
            float impactMag = collision.relativeVelocity.magnitude;
            if (impactMag > 3f)
            {
                GameEvents.TriggerPlaySFX("SFX_RockFall");
            }
        }

        // ─── Interaction ──────────────────────────────────────

        public override void OnInteract()
        {
            // Rock is too heavy to tap effectively
            // Only released by Pin
            Debug.Log("[Rock] Too heavy to move by hand.");
        }
    }
}
