using System.Collections;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Levels;

namespace SnakeRescue.Characters.Princess
{
    /// <summary>
    /// Handles all Princess emotional and physical reactions
    /// to events happening in the world around her.
    ///
    /// This is the system that makes the princess feel ALIVE.
    ///
    /// Reactions include:
    /// - Screaming when snake gets close
    /// - Stepping back from approaching danger
    /// - Covering face when rocks fall nearby
    /// - Jumping with joy when saved
    /// - Looking toward sounds/events
    ///
    /// All reactions are cosmetic — they never affect gameplay.
    /// But they create emotional connection with the player.
    /// </summary>
    public class PrincessReactionSystem : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        private PrincessController _controller;
        private PrincessAnimator   _animator;

        // ─── Settings ─────────────────────────────────────────
        [Header("Reaction Settings")]
        [SerializeField] private float _reactionCooldown   = 0.5f;
        [SerializeField] private float _stepBackDistance   = 0.3f;
        [SerializeField] private float _stepBackDuration   = 0.2f;
        [SerializeField] private float _lookAtDelay        = 0.1f;

        // ─── Runtime ──────────────────────────────────────────
        private float     _lastReactionTime   = 0f;
        private bool      _isReacting         = false;
        private Vector3   _basePosition;
        private Coroutine _reactionCoroutine;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        // ─── Initialization ───────────────────────────────────

        public void Initialize(PrincessController controller)
        {
            _controller  = controller;
            _animator    = GetComponent<PrincessAnimator>();
            _basePosition = transform.position;

            _isReacting       = false;
            _lastReactionTime = 0f;
        }

        // ─── State Change Reactions ───────────────────────────

        public void OnStateChanged(PrincessState previous, PrincessState newState)
        {
            if (!CanReact()) return;

            switch (newState)
            {
                case PrincessState.Alert:
                    ReactToAlert();
                    break;

                case PrincessState.Fear:
                    ReactToFear();
                    break;

                case PrincessState.Panic:
                    ReactToPanic();
                    break;

                case PrincessState.Relief:
                    ReactToRelief();
                    break;

                case PrincessState.Celebrating:
                    ReactToCelebration();
                    break;
            }
        }

        // ─── Threat Reactions ─────────────────────────────────

        public void ReactToSnake(Vector3 snakePosition)
        {
            if (!CanReact()) return;

            _lastReactionTime = Time.time;

            // Face toward snake
            _animator?.FaceToward(snakePosition);

            // Step back away from snake
            Vector3 awayDirection =
                (transform.position - snakePosition).normalized;

            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            _reactionCoroutine =
                StartCoroutine(StepBackRoutine(awayDirection));
        }

        public void ReactToHazard(Vector3 hazardPosition)
        {
            if (!CanReact()) return;

            _lastReactionTime = Time.time;
            _animator?.FaceToward(hazardPosition);
            _animator?.PlayReactionAnimation();
        }

        public void ReactToFallingObject(Vector3 objectPosition)
        {
            if (!CanReact()) return;

            _lastReactionTime = Time.time;

            // Look at where rock/ball is coming from
            StartCoroutine(DelayedLookAt(objectPosition));
        }

        public void ReactToExplosion(Vector3 explosionPosition)
        {
            if (!CanReact()) return;

            _lastReactionTime = Time.time;
            _animator?.PlayReactionAnimation();

            // Step away from explosion
            Vector3 awayDirection =
                (transform.position - explosionPosition).normalized;

            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            _reactionCoroutine =
                StartCoroutine(StepBackRoutine(awayDirection));
        }

        // ─── State-Based Reactions ────────────────────────────

        private void ReactToAlert()
        {
            // Small look-around
            _animator?.PlayReactionAnimation();
        }

        private void ReactToFear()
        {
            // Back away slightly
            Vector3 backDirection = -transform.right;

            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            _reactionCoroutine =
                StartCoroutine(StepBackRoutine(backDirection));
        }

        private void ReactToPanic()
        {
            GameEvents.TriggerPlaySFX("SFX_PrincessScream");
            _animator?.PlayReactionAnimation();
        }

        private void ReactToRelief()
        {
            // Sigh of relief — face forward
            _animator?.FaceRight();
        }

        private void ReactToCelebration()
        {
            GameEvents.TriggerPlaySFX("SFX_PrincessCheer");
            _animator?.PlayCelebrationAnimation();

            if (_reactionCoroutine != null)
                StopCoroutine(_reactionCoroutine);

            _reactionCoroutine = StartCoroutine(CelebrationRoutine());
        }

        // ─── Movement Routines ────────────────────────────────

        private IEnumerator StepBackRoutine(Vector3 direction)
        {
            _isReacting = true;

            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos +
                direction.normalized * _stepBackDistance;

            float elapsed = 0f;

            while (elapsed < _stepBackDuration)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / _stepBackDuration;

                transform.position = Vector3.Lerp(startPos, targetPos,
                    EaseOut(t));

                yield return null;
            }

            transform.position = targetPos;
            _isReacting = false;
        }

        private IEnumerator CelebrationRoutine()
        {
            _isReacting = true;

            // Small bounce up
            Vector3 startPos  = transform.position;
            Vector3 upPos     = startPos + Vector3.up * 0.2f;
            float   elapsed   = 0f;
            float   duration  = 0.3f;

            // Bounce up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(
                    startPos, upPos, EaseOut(elapsed / duration));
                yield return null;
            }

            elapsed = 0f;

            // Bounce back down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(
                    upPos, startPos, EaseOut(elapsed / duration));
                yield return null;
            }

            transform.position = startPos;
            _isReacting = false;
        }

        private IEnumerator DelayedLookAt(Vector3 target)
        {
            yield return new WaitForSeconds(_lookAtDelay);
            _animator?.FaceToward(target);
        }

        // ─── Event Subscriptions ──────────────────────────────

        private void SubscribeToEvents()
        {
            GameEvents.OnSnakeActivated    += OnSnakeActivated;
            GameEvents.OnChainReactionStarted += OnChainReactionStarted;
            GameEvents.OnHazardActivated   += OnHazardActivated;
            GameEvents.OnObjectTriggered   += OnObjectTriggered;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnSnakeActivated    -= OnSnakeActivated;
            GameEvents.OnChainReactionStarted -= OnChainReactionStarted;
            GameEvents.OnHazardActivated   -= OnHazardActivated;
            GameEvents.OnObjectTriggered   -= OnObjectTriggered;
        }

        private void OnSnakeActivated()
        {
            if (_controller?.CurrentState == PrincessState.Idle)
                _controller?.SetState(PrincessState.Alert);
        }

        private void OnChainReactionStarted()
        {
            // Princess looks toward the action
            _animator?.PlayReactionAnimation();
        }

        private void OnHazardActivated(HazardType type)
        {
            ReactToAlert();
        }

        private void OnObjectTriggered(ObjectType type)
        {
            // React to nearby object trigger
            if (type == ObjectType.Rock || type == ObjectType.Ball)
            {
                ReactToFallingObject(transform.position + Vector3.up);
            }
        }

        // ─── Helpers ──────────────────────────────────────────

        private bool CanReact()
        {
            if (_controller == null)        return false;
            if (!_controller.IsAlive)       return false;
            if (_controller.IsSaved)        return false;

            return Time.time - _lastReactionTime >= _reactionCooldown;
        }

        private float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
