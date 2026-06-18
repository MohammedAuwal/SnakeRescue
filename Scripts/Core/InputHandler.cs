using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Core
{
    /// <summary>
    /// Handles all player input for the game.
    /// Translates touch/mouse input into game actions.
    /// All other systems listen to GameEvents — they never read input directly.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        // ─── Settings ─────────────────────────────────────────
        [Header("Input Settings")]
        [SerializeField] private float _tapMaxDuration    = 0.2f;
        [SerializeField] private float _swipeMinDistance  = 50f;
        [SerializeField] private LayerMask _interactableLayers;

        // ─── Runtime ──────────────────────────────────────────
        private Camera   _mainCamera;
        private float    _touchStartTime;
        private Vector2  _touchStartPosition;
        private bool     _isTracking;
        private bool     _inputEnabled = true;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            HandleInput();
        }

        // ─── Input Routing ────────────────────────────────────

        private void HandleInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        // ─── Mouse Input (Editor / PC) ────────────────────────

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartPosition = Input.mousePosition;
                _touchStartTime = Time.time;
                _isTracking = true;

                OnPointerDown(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0) && _isTracking)
            {
                float duration = Time.time - _touchStartTime;
                float distance = Vector2.Distance(
                    Input.mousePosition, _touchStartPosition);

                if (duration <= _tapMaxDuration && distance < _swipeMinDistance)
                {
                    OnTap(Input.mousePosition);
                }

                _isTracking = false;
                OnPointerUp(Input.mousePosition);
            }

            // Escape = Pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnEscapePressed();
            }
        }

        // ─── Touch Input (Mobile) ─────────────────────────────

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartPosition = touch.position;
                    _touchStartTime = Time.time;
                    _isTracking = true;
                    OnPointerDown(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isTracking)
                    {
                        float duration = Time.time - _touchStartTime;
                        float distance = Vector2.Distance(
                            touch.position, _touchStartPosition);

                        if (duration <= _tapMaxDuration &&
                            distance < _swipeMinDistance)
                        {
                            OnTap(touch.position);
                        }

                        _isTracking = false;
                        OnPointerUp(touch.position);
                    }
                    break;
            }
        }

        // ─── Core Input Actions ───────────────────────────────

        private void OnPointerDown(Vector2 screenPosition)
        {
            // Reserved for drag-start logic later
        }

        private void OnPointerUp(Vector2 screenPosition)
        {
            // Reserved for drag-end logic later
        }

        private void OnTap(Vector2 screenPosition)
        {
            if (_mainCamera == null) return;

            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, 0f));

            // Raycast to see what was tapped
            RaycastHit2D hit = Physics2D.Raycast(
                worldPos, Vector2.zero, Mathf.Infinity, _interactableLayers);

            if (hit.collider != null)
            {
                ProcessTapOnObject(hit.collider.gameObject, worldPos);
            }
        }

        private void ProcessTapOnObject(GameObject target, Vector3 worldPosition)
        {
            // Check if target is an interactable object
            IInteractable interactable = target.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract())
            {
                interactable.OnInteract();

                // Tell GameManager the player made an action
                GameManager.Instance?.RegisterPlayerAction();
            }
        }

        private void OnEscapePressed()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.IsPlaying())
                GameManager.Instance.PauseGame();
            else if (GameManager.Instance.IsPaused)
                GameManager.Instance.ResumeGame();
        }

        // ─── Input Enable / Disable ───────────────────────────

        public void EnableInput()
        {
            _inputEnabled = true;
        }

        public void DisableInput()
        {
            _inputEnabled = false;
            _isTracking = false;
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    EnableInput();
                    break;

                case GameState.Paused:
                case GameState.LevelComplete:
                case GameState.LevelFailed:
                case GameState.LevelIntro:
                case GameState.Loading:
                    DisableInput();
                    break;

                case GameState.ChainReacting:
                    // Input disabled during chain reaction
                    // Player cannot interfere while physics resolves
                    DisableInput();
                    break;
            }
        }
    }

    // ─── Interactable Interface ───────────────────────────────

    /// <summary>
    /// Any object in the world that the player can tap must implement this.
    /// Ball, Rock, Pin, Gate, Lever, Rope — all implement this.
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract();
        void OnInteract();
    }
}
