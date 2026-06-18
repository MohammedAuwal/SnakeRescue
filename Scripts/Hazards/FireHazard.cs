using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Hazards;

namespace SnakeRescue.Hazards
{
    /// <summary>
    /// Fire hazard.
    ///
    /// Behavior:
    /// - Damages Princess on contact
    /// - Can be neutralized by Water
    /// - Visuals: Particle system + Light
    ///
    /// Puzzle Role:
    /// - Blocks path until extinguished
    /// - Requires Water object to be released nearby
    /// </summary>
    public class FireHazard : HazardBase
    {
        // ─── References ───────────────────────────────────────
        [Header("Visuals")]
        [SerializeField] private ParticleSystem _fireParticles;
        [SerializeField] private Light          _fireLight;
        [SerializeField] private SpriteRenderer _fireSprite;

        [Header("Neutralization")]
        [SerializeField] private float _extinguishRadius = 2f;
        [SerializeField] private AudioClip _extinguishSound;
        [SerializeField] private AudioClip _burnSound;

        // ─── Runtime ──────────────────────────────────────────
        private bool _isListeningForWater = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        protected override void Awake()
        {
            _hazardType = HazardType.Fire;
            _damagesPrincess = true;
            _damagesSnake = false; // Snake immune to fire for now

            base.Awake();
        }

        private void OnEnable()
        {
            if (IsActive)
                StartBurnSound();
        }

        private void OnDisable()
        {
            StopBurnSound();
        }

        // ─── Activation Overrides ─────────────────────────────

        public override void Activate()
        {
            base.Activate();

            if (_fireParticles != null) _fireParticles.Play();
            if (_fireLight != null) _fireLight.enabled = true;
            if (_fireSprite != null) _fireSprite.enabled = true;

            StartBurnSound();
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (_fireParticles != null) _fireParticles.Stop();
            if (_fireLight != null) _fireLight.enabled = false;
            if (_fireSprite != null) _fireSprite.enabled = false;

            StopBurnSound();
        }

        // ─── Neutralization Logic ─────────────────────────────

        /// <summary>
        /// Called by WaterHazard when water flows near fire.
        /// </summary>
        public void ExtinguishByWater(Vector3 waterPosition)
        {
            if (IsNeutralized) return;

            float dist = Vector3.Distance(transform.position, waterPosition);
            if (dist <= _extinguishRadius)
            {
                Neutralize();
                GameEvents.TriggerPlaySFX("SFX_Splash");
            }
        }

        protected override void UpdateVisuals()
        {
            if (IsNeutralized)
            {
                // Show smoke instead of fire
                if (_fireParticles != null)
                {
                    var main = _fireParticles.main;
                    main.startColor = Color.gray;
                }
                if (_fireLight != null) _fireLight.enabled = false;
            }
            else if (IsActive)
            {
                // Show normal fire
                if (_fireParticles != null)
                {
                    var main = _fireParticles.main;
                    main.startColor = Color.orange;
                }
                if (_fireLight != null) _fireLight.enabled = true;
            }
        }

        // ─── Audio ────────────────────────────────────────────

        private void StartBurnSound()
        {
            // Loop fire sound
            GameEvents.TriggerPlaySFX("SFX_Fire");
        }

        private void StopBurnSound()
        {
            // Stop fire sound
        }

        // ─── Debug ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _extinguishRadius);
        }
    }
}
