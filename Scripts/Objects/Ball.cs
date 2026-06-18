using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Objects;

namespace SnakeRescue.Objects
{
    /// <summary>
    /// A lightweight physics object.
    ///
    /// Behavior:
    /// - High bounciness
    /// - Rolls easily
    /// - Less damage on impact than Rock
    /// - Can trigger chain reactions by hitting other objects
    ///
    /// Use Ball for:
    /// - Triggering switches
    /// - Bouncing off walls to hit targets
    /// - Light puzzles requiring precision
    /// </summary>
    public class Ball : PhysicsObject
    {
        // ─── Settings ─────────────────────────────────────────
        [Header("Ball Specifics")]
        [SerializeField] private float _bounceForce   = 5f;
        [SerializeField] private float _ballMass      = 1.5f;
        [SerializeField] private AudioClip _rollSound;
        [SerializeField] private AudioClip _bounceSound;

        // ─── Runtime ──────────────────────────────────────────
        private float _lastSoundTime = 0f;
        private const float SOUND_COOLDOWN = 0.3f;

        // ─── Unity Lifecycle ──────────────────────────────────

        protected override void Awake()
        {
            _objectType = ObjectType.Ball;
            base.Awake();
        }

        // ─── Initialization ───────────────────────────────────

        protected override void InitializePhysics()
        {
            base.InitializePhysics();

            _rigidbody.mass = _ballMass;

            // Set physics material for bounce if available
            PhysicsMaterial2D mat = new PhysicsMaterial2D("BallBounce");
            mat.bounciness = 0.7f;
            mat.friction = 0.1f;

            _collider.sharedMaterial = mat;
        }

        // ─── Collision Overrides ──────────────────────────────

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);

            // Play bounce sound
            if (Time.time - _lastSoundTime > SOUND_COOLDOWN)
            {
                float impact = collision.relativeVelocity.magnitude;
                if (impact > 1f)
                {
                    GameEvents.TriggerPlaySFX("SFX_BallRoll");
                    _lastSoundTime = Time.time;
                }
            }
        }

        // ─── Interaction ──────────────────────────────────────

        public override void OnInteract()
        {
            // Ball can be tapped to give a small impulse
            if (!IsAlive || IsHeld) return;

            Vector2 impulse = Vector2.up * _bounceForce;
            _rigidbody.AddForce(impulse, ForceMode2D.Impulse);

            GameEvents.TriggerPlaySFX("SFX_BallRoll");

            ChainReactionSystem.Instance?.RegisterStep(
                "Ball tapped",
                transform.position,
                ReactionStepType.Trigger);

            Debug.Log("[Ball] Tapped.");
        }
    }
}
