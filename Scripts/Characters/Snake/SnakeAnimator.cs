using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Characters.Snake
{
    /// <summary>
    /// Controls all Snake animations and visual feedback.
    ///
    /// Handles:
    /// - Slithering animation speed
    /// - Attack lunge
    /// - Death collapse
    /// - Direction flipping
    /// - Color tint (stunned vs normal)
    ///
    /// This script only does visuals.
    /// It never changes game state.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SnakeAnimator : MonoBehaviour
    {
        // ─── Components ───────────────────────────────────────
        private Animator       _animator;
        private SpriteRenderer _spriteRenderer;

        // ─── Settings ─────────────────────────────────────────
        [Header("Visuals")]
        [SerializeField] private Color _colorNormal  = Color.white;
        [SerializeField] private Color _colorStunned = new Color(0.5f, 0.5f, 1f);
        [SerializeField] private Color _colorDead    = Color.gray;

        [Header("Animation Speed")]
        [SerializeField] private float _idleSpeed    = 1f;
        [SerializeField] private float _chaseSpeed   = 2f;

        // ─── Animator Parameter Hashes ────────────────────────
        private static readonly int PARAM_STATE      = Animator.StringToHash("State");
        private static readonly int PARAM_SPEED      = Animator.StringToHash("Speed");
        private static readonly int PARAM_IS_ALIVE   = Animator.StringToHash("IsAlive");
        private static readonly int TRIGGER_ATTACK   = Animator.StringToHash("Attack");
        private static readonly int TRIGGER_DEATH    = Animator.StringToHash("Death");

        // ─── Runtime ──────────────────────────────────────────
        private SnakeState _currentState = SnakeState.Idle;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _animator       = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // ─── Initialization ───────────────────────────────────

        public void Initialize()
        {
            _currentState = SnakeState.Idle;

            if (_animator != null)
            {
                _animator.SetBool(PARAM_IS_ALIVE, true);
                _animator.SetInteger(PARAM_STATE, 0);
                _animator.SetFloat(PARAM_SPEED, _idleSpeed);
            }

            ApplyColor(_colorNormal);
        }

        // ─── State Change ─────────────────────────────────────

        public void OnStateChanged(SnakeState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;

            UpdateAnimatorState(newState);
            ApplyStateVisuals(newState);
        }

        // ─── Animator Updates ─────────────────────────────────

        private void UpdateAnimatorState(SnakeState state)
        {
            if (_animator == null) return;

            _animator.SetInteger(PARAM_STATE, (int)state);

            switch (state)
            {
                case SnakeState.Chasing:
                    _animator.SetFloat(PARAM_SPEED, _chaseSpeed);
                    break;

                case SnakeState.Idle:
                case SnakeState.Patrolling:
                    _animator.SetFloat(PARAM_SPEED, _idleSpeed);
                    break;

                case SnakeState.Attacking:
                    _animator.SetTrigger(TRIGGER_ATTACK);
                    break;

                case SnakeState.Dead:
                    _animator.SetBool(PARAM_IS_ALIVE, false);
                    _animator.SetTrigger(TRIGGER_DEATH);
                    break;

                case SnakeState.Stunned:
                    _animator.SetFloat(PARAM_SPEED, 0f);
                    break;
            }
        }

        // ─── Visuals ──────────────────────────────────────────

        private void ApplyStateVisuals(SnakeState state)
        {
            switch (state)
            {
                case SnakeState.Stunned:
                    ApplyColor(_colorStunned);
                    break;

                case SnakeState.Dead:
                    ApplyColor(_colorDead);
                    break;

                default:
                    ApplyColor(_colorNormal);
                    break;
            }
        }

        private void ApplyColor(Color color)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }

        // ─── Direction ────────────────────────────────────────

        public void FaceDirection(Vector2 moveDirection)
        {
            if (_spriteRenderer == null) return;
            if (moveDirection == Vector2.zero) return;

            bool faceRight = moveDirection.x > 0f;
            _spriteRenderer.flipX = !faceRight;
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            if (_spriteRenderer == null) return;

            float dirX = targetPosition.x - transform.position.x;
            _spriteRenderer.flipX = dirX < 0f;
        }

        // ─── One-Shot Animations ──────────────────────────────

        public void PlayAttackAnimation()
        {
            _animator?.SetTrigger(TRIGGER_ATTACK);
        }

        public void PlayDeathAnimation()
        {
            _animator?.SetTrigger(TRIGGER_DEATH);
        }
    }
}
