using System.Collections;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Levels;
using SnakeRescue.Objects;

namespace SnakeRescue.Objects
{
    /// <summary>
    /// A barrier that can open or close.
    ///
    /// Behavior:
    /// - Blocks path when closed
    /// - Opens when interacted (tapped or triggered)
    /// - Can be held by a Pin (optional)
    ///
    /// Use Gate for:
    /// - Controlling Snake movement
    /// - Creating safe paths for Princess
    /// - Timing puzzles (open then close)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Gate : MonoBehaviour, IInteractable
    {
        // ─── Identity ─────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private ObjectType _objectType = ObjectType.Gate;

        // ─── Settings ─────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private Vector2 _openOffset   = new Vector2(3f, 0f);
        [SerializeField] private float   _openDuration = 0.5f;
        [SerializeField] private bool    _autoClose    = false;
        [SerializeField] private float   _closeDelay   = 3f;

        [Header("State")]
        [SerializeField] private bool _startOpen = false;

        // ─── Components ───────────────────────────────────────
        private Rigidbody2D _rigidbody;
        private Collider2D  _collider;
        private Vector2     _closedPosition;
        private Vector2     _openPosition;

        // ─── State ────────────────────────────────────────────
        public  ObjectType ObjectType => _objectType;
        public  bool       IsOpen     { get; private set; }
        private bool       _isMoving  = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider  = GetComponent<Collider2D>();

            _closedPosition = transform.position;
            _openPosition   = _closedPosition + _openOffset;

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.gravityScale = 0f;

            gameObject.CompareTag(Constants.TAG_OBJECT);

            if (_startOpen)
            {
                IsOpen = true;
                transform.position = _openPosition;
                _collider.enabled = false;
            }
            else
            {
                IsOpen = false;
            }
        }

        // ─── Interaction ──────────────────────────────────────

        public bool CanInteract()
        {
            return !_isMoving;
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;

            ToggleGate();
        }

        // ─── Gate Logic ───────────────────────────────────────

        public void ToggleGate()
        {
            if (_isMoving) return;

            LevelManager.Instance?.RegisterAction();

            if (IsOpen)
                CloseGate();
            else
                OpenGate();
        }

        public void OpenGate()
        {
            if (IsOpen) return;

            IsOpen = true;
            _isMoving = true;

            ChainReactionSystem.Instance?.RegisterStep(
                "Gate opened",
                transform.position,
                ReactionStepType.Trigger);

            GameEvents.TriggerObjectTriggered(_objectType);
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

            StartCoroutine(MoveGateRoutine(_openPosition));
        }

        public void CloseGate()
        {
            if (!IsOpen) return;

            IsOpen = false;
            _isMoving = true;

            ChainReactionSystem.Instance?.RegisterStep(
                "Gate closed",
                transform.position,
                ReactionStepType.Trigger);

            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

            StartCoroutine(MoveGateRoutine(_closedPosition));
        }

        private IEnumerator MoveGateRoutine(Vector3 target)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;

            // Enable collider during move
            _collider.enabled = true;

            while (elapsed < _openDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _openDuration;

                transform.position = Vector3.Lerp(start, target, EaseInOut(t));
                yield return null;
            }

            transform.position = target;
            _isMoving = false;

            // Disable collider if open (so characters can pass)
            if (IsOpen)
                _collider.enabled = false;

            // Auto close logic
            if (IsOpen && _autoClose)
            {
                yield return new WaitForSeconds(_closeDelay);
                CloseGate();
            }
        }

        // ─── Helpers ──────────────────────────────────────────

        private float EaseInOut(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : -1f + (4f - 2f * t) * t;
        }

        // ─── Trigger by Other Objects ─────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Optional: Open gate when heavy object hits it
            if (other.CompareTag(Constants.TAG_OBJECT))
            {
                Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
                if (rb != null && rb.velocity.magnitude > 2f)
                {
                    if (!IsOpen) OpenGate();
                }
            }
        }
    }
}
