using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Hazards;

namespace SnakeRescue.Hazards
{
    /// <summary>
    /// Spike hazard.
    ///
    /// Behavior:
    /// - Static damage zone
    /// - Damages both Princess and Snake
    /// - Can be retracted (optional)
    ///
    /// Puzzle Role:
    /// - Blocks path permanently unless covered
    /// - Kills snake if pushed onto it
    /// - Kills princess if she falls onto it
    /// </summary>
    public class SpikeHazard : HazardBase
    {
        // ─── References ───────────────────────────────────────
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spikeSprite;
        [SerializeField] private Animator        _spikeAnimator;

        [Header("Behavior")]
        [SerializeField] private bool _retractable = false;
        [SerializeField] private float _retractDelay = 2f;

        // ─── State ────────────────────────────────────────────
        private bool _isRetracted = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        protected override void Awake()
        {
            _hazardType = HazardType.Spike;
            _damagesPrincess = true;
            _damagesSnake = true;

            base.Awake();
        }

        // ─── Activation Overrides ─────────────────────────────

        public override void Activate()
        {
            base.Activate();
            _isRetracted = false;
            UpdateVisuals();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            _isRetracted = true;
            UpdateVisuals();
        }

        // ─── Retract Logic ────────────────────────────────────

        public void Retract()
        {
            if (!_retractable || IsNeutralized) return;

            _isRetracted = true;
            _collider.enabled = false;

            UpdateVisuals();

            ChainReactionSystem.Instance?.RegisterStep(
                "Spikes retracted",
                transform.position,
                ReactionStepType.HazardRemoved);

            // Auto extend after delay
            Invoke(nameof(Extend), _retractDelay);
        }

        public void Extend()
        {
            if (!_retractable || IsNeutralized) return;

            _isRetracted = false;
            _collider.enabled = true;

            UpdateVisuals();

            ChainReactionSystem.Instance?.RegisterStep(
                "Spikes extended",
                transform.position,
                ReactionStepType.HazardActivated);
        }

        // ─── Visuals ──────────────────────────────────────────

        protected override void UpdateVisuals()
        {
            if (_spikeSprite == null) return;

            if (_isRetracted || IsNeutralized)
            {
                _spikeSprite.enabled = false;
            }
            else if (IsActive)
            {
                _spikeSprite.enabled = true;
            }

            if (_spikeAnimator != null)
                _spikeAnimator.SetBool("Active", IsActive && !_isRetracted);
        }

        // ─── Interaction ──────────────────────────────────────

        public override void OnInteract()
        {
            if (_retractable)
            {
                if (_isRetracted)
                    Extend();
                else
                    Retract();
            }
        }
    }
}
