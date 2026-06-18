using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Characters.Princess
{
    /// <summary>
    /// Controls all Princess animations and visual feedback.
    ///
    /// Reads PrincessState and drives the Animator accordingly.
    /// Also handles:
    /// - Sprite flipping (face toward threat)
    /// - Shake effect when panicking
    /// - Color tint for different states
    /// - Particle effects (celebration sparkles etc)
    ///
    /// This script only does visuals.
    /// It never changes game state.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PrincessAnimator : MonoBehaviour
    {
        // ─── Components ───────────────────────────────────────
        private Animator       _animator;
        private SpriteRenderer _spriteRenderer;

        // ─── Particle Systems ─────────────────────────────────
        [Header("Particles")]
        [SerializeField] private ParticleSystem _celebrationParticles;
        [SerializeField] private ParticleSystem _fearParticles;
        [SerializeField] private ParticleSystem _heartParticles;

        // ─── Shake Settings ───────────────────────────────────
        [Header("Panic Shake")]
        [SerializeField] private float _shakeIntensity = 0.05f;
        [SerializeField] private float _shakeSpeed     = 20f;

        // ─── Color Tints ──────────────────────────────────────
        [Header("State Colors")]
        [SerializeField] private Color _colorIdle        = Color.white;
        [SerializeField] private Color _colorAlert       = new Color(1f, 0.9f, 0.7f);
        [SerializeField] private Color _colorFear        = new Color(1f, 0.7f, 0.7f);
        [SerializeField] private Color _colorPanic       = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private Color _colorRelief      = new Color(0.7f, 1f, 0.7f);
        [SerializeField] private Color _colorCelebrating = new Color(1f, 1f, 0.6f);

        // ─── Animator Parameter Hashes ────────────────────────
        // Cached for performance — never use string at runtime
        private static readonly int PARAM_STATE    = Animator.StringToHash("State");
        private static readonly int PARAM_IS_ALIVE = Animator.StringToHash("IsAlive");
        private static readonly int PARAM_IS_SAVED = Animator.StringToHash("IsSaved");
        private static readonly int TRIGGER_REACT  = Animator.StringToHash("React");
        private static readonly int TRIGGER_CHEER  = Animator.StringToHash("Cheer");

        // ─── Runtime ──────────────────────────────────────────
        private Vector3   _basePosition;
        private bool      _isShaking      = false;
        private float     _shakeTimer     = 0f;
        private PrincessState _currentState = PrincessState.Idle;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _animator       = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_isShaking)
                ApplyShake();
        }

        // ─── Initialization ───────────────────────────────────

        public void Initialize()
        {
            _basePosition = transform.localPosition;
            _currentState = PrincessState.Idle;
            _isShaking    = false;

            ApplyStateVisuals(PrincessState.Idle);

            if (_animator != null)
            {
                _animator.SetBool(PARAM_IS_ALIVE, true);
                _animator.SetBool(PARAM_IS_SAVED, false);
                _animator.SetInteger(PARAM_STATE, 0);
            }
        }

        // ─── State Change ─────────────────────────────────────

        public void OnStateChanged(PrincessState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;

            UpdateAnimatorState(newState);
            ApplyStateVisuals(newState);
            HandleStateEffects(newState);

            Debug.Log($"[PrincessAnimator] State → {newState}");
        }

        // ─── Animator Updates ─────────────────────────────────

        private void UpdateAnimatorState(PrincessState state)
        {
            if (_animator == null) return;

            _animator.SetInteger(PARAM_STATE, (int)state);

            switch (state)
            {
                case PrincessState.Celebrating:
                    _animator.SetBool(PARAM_IS_SAVED, true);
                    _animator.SetTrigger(TRIGGER_CHEER);
                    break;

                case PrincessState.Dead:
                    _animator.SetBool(PARAM_IS_ALIVE, false);
                    break;

                case PrincessState.Fear:
                case PrincessState.Panic:
                    _animator.SetTrigger(TRIGGER_REACT);
                    break;
            }
        }

        // ─── Visuals ──────────────────────────────────────────

        private void ApplyStateVisuals(PrincessState state)
        {
            if (_spriteRenderer == null) return;

            Color target = GetColorForState(state);
            _spriteRenderer.color = target;
        }

        private Color GetColorForState(PrincessState state)
        {
            switch (state)
            {
                case PrincessState.Idle:        return _colorIdle;
                case PrincessState.Alert:       return _colorAlert;
                case PrincessState.Fear:        return _colorFear;
                case PrincessState.Panic:       return _colorPanic;
                case PrincessState.Relief:      return _colorRelief;
                case PrincessState.Celebrating: return _colorCelebrating;
                default:                        return Color.white;
            }
        }

        // ─── State Effects ────────────────────────────────────

        private void HandleStateEffects(PrincessState state)
        {
            // Stop all effects first
            StopAllEffects();

            switch (state)
            {
                case PrincessState.Panic:
                    StartShake();
                    _fearParticles?.Play();
                    break;

                case PrincessState.Fear:
                    _fearParticles?.Play();
                    break;

                case PrincessState.Celebrating:
                    StopShake();
                    _celebrationParticles?.Play();
                    _heartParticles?.Play();
                    break;

                case PrincessState.Relief:
                    StopShake();
                    break;

                case PrincessState.Idle:
                    StopShake();
                    break;
            }
        }

        private void StopAllEffects()
        {
            _fearParticles?.Stop();
        }

        // ─── Shake Effect ─────────────────────────────────────

        private void StartShake()
        {
            _isShaking  = true;
            _shakeTimer = 0f;
        }

        private void StopShake()
        {
            _isShaking = false;
            transform.localPosition = _basePosition;
        }

        private void ApplyShake()
        {
            _shakeTimer += Time.deltaTime * _shakeSpeed;

            float offsetX = Mathf.Sin(_shakeTimer)       * _shakeIntensity;
            float offsetY = Mathf.Sin(_shakeTimer * 1.3f) * _shakeIntensity * 0.5f;

            transform.localPosition = _basePosition + new Vector3(offsetX, offsetY, 0f);
        }

        // ─── Facing ───────────────────────────────────────────

        /// <summary>
        /// Make princess face toward a world position.
        /// Used when reacting to nearby threat.
        /// </summary>
        public void FaceToward(Vector3 worldPosition)
        {
            if (_spriteRenderer == null) return;

            float dirX = worldPosition.x - transform.position.x;

            if (Mathf.Abs(dirX) > 0.1f)
            {
                _spriteRenderer.flipX = dirX < 0f;
            }
        }

        public void FaceRight()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = false;
        }

        public void FaceLeft()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = true;
        }

        // ─── One-Shot Animations ──────────────────────────────

        public void PlayReactionAnimation()
        {
            _animator?.SetTrigger(TRIGGER_REACT);
        }

        public void PlayCelebrationAnimation()
        {
            _animator?.SetTrigger(TRIGGER_CHEER);
            _celebrationParticles?.Play();
        }
    }
}
