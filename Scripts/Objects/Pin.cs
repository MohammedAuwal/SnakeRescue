using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Levels;
using SnakeRescue.Objects;

namespace SnakeRescue.Objects
{
    /// <summary>
    /// The primary interaction mechanic.
    ///
    /// Player taps pin → Pin is pulled → Held object is released.
    ///
    /// This triggers the chain reaction.
    /// This is the main "verb" of the game.
    ///
    /// Pin holds an object via parenting + kinematic rigidbody.
    /// When pulled, object becomes dynamic and falls.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Pin : MonoBehaviour, IInteractable
    {
        // ─── Identity ─────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private ObjectType _objectType = ObjectType.Pin;

        // ─── References ───────────────────────────────────────
        [Header("Held Object")]
        [SerializeField] private PhysicsObject _heldObject;
        [SerializeField] private Transform     _holdPoint;

        [Header("Visuals")]
        [SerializeField] private GameObject _pinVisual;
        [SerializeField] private GameObject _pullEffect;

        // ─── Settings ─────────────────────────────────────────
        [Header("Settings")]
        [SerializeField] private float _pullDuration = 0.2f;
        [SerializeField] private AudioClip _pullSound;

        // ─── State ────────────────────────────────────────────
        public  ObjectType ObjectType => _objectType;
        public  bool       IsPulled  { get; private set; } = false;
        private bool       _isPulling = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Start()
        {
            if (_heldObject != null)
            {
                _heldObject.SetHeld(true);

                if (_holdPoint != null)
                    _heldObject.transform.SetParent(_holdPoint);
            }

            gameObject.CompareTag(Constants.TAG_OBJECT);
        }

        // ─── Interaction ──────────────────────────────────────

        public bool CanInteract()
        {
            return !IsPulled && !_isPulling;
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;

            StartCoroutine(PullRoutine());
        }

        // ─── Pull Routine ─────────────────────────────────────

        private System.Collections.IEnumerator PullRoutine()
        {
            _isPulling = true;

            // Register action with level manager
            LevelManager.Instance?.RegisterAction();

            // Start chain reaction tracking
            ChainReactionSystem.Instance?.BeginReaction(
                "Pin Pulled",
                transform.position);

            // Visual pull effect
            if (_pullEffect != null)
            {
                _pullEffect.SetActive(true);
                _pullEffect.GetComponent<ParticleSystem>()?.Play();
            }

            // Play sound
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

            // Wait for pull animation
            yield return new WaitForSeconds(_pullDuration);

            // Release object
            ReleaseHeldObject();

            // Disable pin
            IsPulled = true;
            _isPulling = false;

            if (_pinVisual != null)
                _pinVisual.SetActive(false);

            _collider.enabled = false;

            // Notify systems
            GameEvents.TriggerObjectTriggered(_objectType);
            EventManager.Publish(new ObjectTriggeredEvent
            {
                ObjectName = gameObject.name,
                ObjectType = "Pin",
                Position   = transform.position
            });

            Debug.Log("[Pin] Pulled.");
        }

        // ─── Release Logic ────────────────────────────────────

        private void ReleaseHeldObject()
        {
            if (_heldObject == null) return;

            _heldObject.SetHeld(false);

            if (_holdPoint != null)
                _heldObject.transform.SetParent(null);

            // Add slight random force to prevent stacking glitches
            Vector2 randomForce = Random.insideUnitCircle * 0.5f;
            _heldObject.GetComponent<Rigidbody2D>()?.AddForce(randomForce);
        }

        // ─── Components ───────────────────────────────────────

        private Collider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        // ─── Gizmos ───────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            if (_heldObject != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _heldObject.transform.position);
            }
        }
    }
}
