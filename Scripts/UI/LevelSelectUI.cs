using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Core;
using SnakeRescue.Data;
using SnakeRescue.Managers;
using SnakeRescue.Systems;
using SnakeRescue.Utils;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Controls the Level Select screen.
    ///
    /// Generates a grid of level buttons.
    /// Each button shows:
    /// - Level Number
    /// - Lock Status
    /// - Stars Earned
    ///
    /// Clicking a level loads the game scene.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Grid")]
        [SerializeField] private Transform _contentParent;
        [SerializeField] private GameObject _levelButtonPrefab;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        // ─── Runtime ──────────────────────────────────────────
        private Button[] _levelButtons;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            _backButton?.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            BuildLevelGrid();
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        // ─── Grid Building ────────────────────────────────────

        private void BuildLevelGrid()
        {
            if (_contentParent == null || _levelButtonPrefab == null)
            {
                Debug.LogError("[LevelSelectUI] Missing references.");
                return;
            }

            // Clear existing
            foreach (Transform child in _contentParent)
                Destroy(child.gameObject);

            int totalLevels = Constants.TOTAL_MVP_LEVELS;

            for (int i = 0; i < totalLevels; i++)
            {
                GameObject btnObj = Instantiate(_levelButtonPrefab, _contentParent);
                LevelButton button = btnObj.GetComponent<LevelButton>();

                if (button != null)
                {
                    button.Initialize(i, OnLevelClicked);
                }
            }
        }

        // ─── Button Handlers ──────────────────────────────────

        private void OnBackClicked()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            SceneController.Instance?.LoadMainMenu();
        }

        private void OnLevelClicked(int levelIndex)
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");

            if (ProgressManager.Instance?.IsLevelUnlocked(levelIndex) ?? false)
            {
                SceneController.Instance?.LoadGameScene(levelIndex);
            }
            else
            {
                // Locked feedback
                Debug.Log("[LevelSelect] Level locked.");
            }
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            gameObject.SetActive(state == GameState.LevelSelect);
        }

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _backButton?.onClick.RemoveListener(OnBackClicked);
        }
    }
}

// ─── Helper Component ─────────────────────────────────────────

/// <summary>
/// Individual level button component.
/// Attached to the prefab used by LevelSelectUI.
/// </summary>
public class LevelButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text _levelNumberText;
    [SerializeField] private Text _starsText;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private Button _button;
    [SerializeField] private Image _buttonImage;

    private int  _levelIndex;
    private bool _isUnlocked;

    public void Initialize(int levelIndex, System.Action<int> onClick)
    {
        _levelIndex = levelIndex;
        _isUnlocked = ProgressManager.Instance?.IsLevelUnlocked(levelIndex) ?? false;

        if (_levelNumberText != null)
            _levelNumberText.text = (levelIndex + 1).ToString();

        if (_button != null)
            _button.onClick.AddListener(() => onClick.Invoke(_levelIndex));

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_lockIcon != null)
            _lockIcon.enabled = !_isUnlocked;

        if (_buttonImage != null)
            _buttonImage.color = _isUnlocked ? Color.white : Color.gray;

        if (_starsText != null)
        {
            if (_isUnlocked)
            {
                int stars = ProgressManager.Instance?.GetLevelStars(_levelIndex) ?? 0;
                _starsText.text = new string('★', stars);
            }
            else
            {
                _starsText.text = "";
            }
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
    }
}
