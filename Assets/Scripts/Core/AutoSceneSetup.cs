using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Levels;
using SnakeRescue.Systems;
using SnakeRescue.Managers;
using SnakeRescue.UI;

namespace SnakeRescue.Core
{
    /// <summary>
    /// AUTO SCENE SETUP - NO EDITOR REQUIRED
    /// 
    /// This script creates all required GameObjects at runtime.
    /// Attach this to an empty GameObject in any scene.
    /// 
    /// Creates:
    /// - Cameras (Main Camera)
    /// - EventSystem (UI Input)
    /// - Managers (GameManager, AudioManager, SaveSystem, etc.)
    /// - Canvas (UI Container)
    /// - Scene-specific UI (MainMenu, GameUI, etc.)
    /// 
    /// Usage:
    /// 1. Create empty GameObject named "_AutoSetup"
    /// 2. Attach this script
    /// 3. Done! Everything creates automatically on Start()
    /// </summary>
    public class AutoSceneSetup : MonoBehaviour
    {
        // ─── Settings ─────────────────────────────────────────
        [Header("Auto-Create Options")]
        [SerializeField] private bool _createCamera = true;
        [SerializeField] private bool _createEventSystem = true;
        [SerializeField] private bool _createManagers = true;
        [SerializeField] private bool _createCanvas = true;
        [SerializeField] private bool _createSceneUI = true;

        // ─── Runtime ──────────────────────────────────────────
        private Camera _mainCamera;
        private GameObject _eventSystem;
        private GameObject _canvas;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            Debug.Log("[AutoSceneSetup] Starting automatic scene setup...");

            if (_createCamera)
                CreateMainCamera();

            if (_createEventSystem)
                CreateEventSystem();

            if (_createManagers)
                CreateManagers();

            if (_createCanvas)
                CreateCanvas();

            if (_createSceneUI)
                CreateSceneSpecificUI();

            Debug.Log("[AutoSceneSetup] Scene setup complete!");
        }

        // ─── Camera Setup ─────────────────────────────────────

        private void CreateMainCamera()
        {
            // Check if camera exists
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                Debug.Log("[AutoSceneSetup] Camera already exists.");
                return;
            }

            // Create camera
            GameObject cameraObj = new GameObject("Main Camera");
            _mainCamera = cameraObj.AddComponent<Camera>();

            // Configure camera
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 5f;
            _mainCamera.nearClipPlane = 0.3f;
            _mainCamera.farClipPlane = 1000f;
            _mainCamera.cullingMask = -1; // All layers
            _mainCamera.depth = 0;

            // Set as main camera
            cameraObj.tag = "MainCamera";

            // Position for 2D game
            cameraObj.transform.position = new Vector3(0, 0, -10);

            Debug.Log("[AutoSceneSetup] Main Camera created.");
        }

        // ─── EventSystem Setup ────────────────────────────────

        private void CreateEventSystem()
        {
            // Check if EventSystem exists
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                Debug.Log("[AutoSceneSetup] EventSystem already exists.");
                return;
            }

            // Create EventSystem
            _eventSystem = new GameObject("EventSystem");
            var eventSystem = _eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Add Standalone Input Module (for mouse/touch)
            var inputModule = _eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            inputModule.horizontalAxis = "Horizontal";
            inputModule.verticalAxis = "Vertical";
            inputModule.submitButton = "Submit";
            inputModule.cancelButton = "Cancel";

            // Add Touch Input Module (for mobile)
            var touchModule = _eventSystem.AddComponent<UnityEngine.EventSystems.TouchInputModule>();

            Debug.Log("[AutoSceneSetup] EventSystem created.");
        }

        // ─── Manager Setup ────────────────────────────────────

        private void CreateManagers()
        {
            // GameManager
            if (FindObjectOfType<GameManager>() == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                managerObj.AddComponent<GameManager>();
                DontDestroyOnLoad(managerObj);
                Debug.Log("[AutoSceneSetup] GameManager created.");
            }

            // SceneController
            if (FindObjectOfType<SceneController>() == null)
            {
                GameObject sceneCtrl = new GameObject("SceneController");
                sceneCtrl.AddComponent<SceneController>();
                DontDestroyOnLoad(sceneCtrl);
                Debug.Log("[AutoSceneSetup] SceneController created.");
            }

            // AudioManager
            if (FindObjectOfType<AudioManager>() == null)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioObj.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioObj);
                Debug.Log("[AutoSceneSetup] AudioManager created.");
            }

            // SaveSystem
            if (FindObjectOfType<SaveSystem>() == null)
            {
                GameObject saveObj = new GameObject("SaveSystem");
                saveObj.AddComponent<SaveSystem>();
                DontDestroyOnLoad(saveObj);
                Debug.Log("[AutoSceneSetup] SaveSystem created.");
            }

            // ProgressManager
            if (FindObjectOfType<ProgressManager>() == null)
            {
                GameObject progressObj = new GameObject("ProgressManager");
                progressObj.AddComponent<ProgressManager>();
                DontDestroyOnLoad(progressObj);
                Debug.Log("[AutoSceneSetup] ProgressManager created.");
            }

            // CosmeticSystem
            if (FindObjectOfType<CosmeticSystem>() == null)
            {
                GameObject cosmeticObj = new GameObject("CosmeticSystem");
                cosmeticObj.AddComponent<CosmeticSystem>();
                DontDestroyOnLoad(cosmeticObj);
                Debug.Log("[AutoSceneSetup] CosmeticSystem created.");
            }

            // ObjectPoolManager
            if (FindObjectOfType<ObjectPoolManager>() == null)
            {
                GameObject poolObj = new GameObject("ObjectPoolManager");
                poolObj.AddComponent<ObjectPoolManager>();
                DontDestroyOnLoad(poolObj);
                Debug.Log("[AutoSceneSetup] ObjectPoolManager created.");
            }
        }

        // ─── Canvas Setup ─────────────────────────────────────

        private void CreateCanvas()
        {
            // Check if Canvas exists
            if (FindObjectOfType<Canvas>() != null)
            {
                Debug.Log("[AutoSceneSetup] Canvas already exists.");
                return;
            }

            // Create Canvas
            _canvas = new GameObject("Canvas");
            var canvas = _canvas.AddComponent<Canvas>();

            // Configure Canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 0;

            // Add CanvasScaler
            var scaler = _canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Shrink;

            // Add GraphicRaycaster (for UI interaction)
            _canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Set layer to UI
            _canvas.layer = LayerMask.NameToLayer("UI");

            Debug.Log("[AutoSceneSetup] Canvas created.");
        }

        // ─── Scene-Specific UI ────────────────────────────────

        private void CreateSceneSpecificUI()
        {
            if (_canvas == null)
            {
                Debug.LogWarning("[AutoSceneSetup] Canvas not found. Skipping UI creation.");
                return;
            }

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            switch (sceneName)
            {
                case "MainMenu":
                    CreateMainMenuUI();
                    break;

                case "GameScene":
                    CreateGameUI();
                    break;

                case "LevelSelect":
                    CreateLevelSelectUI();
                    break;

                case "Loading":
                    CreateLoadingUI();
                    break;

                default:
                    Debug.Log($"[AutoSceneSetup] No UI setup for scene: {sceneName}");
                    break;
            }
        }

        // ─── MainMenu UI ──────────────────────────────────────

        private void CreateMainMenuUI()
        {
            Debug.Log("[AutoSceneSetup] Creating MainMenu UI...");

            // Create MainMenuUI object
            GameObject menuObj = new GameObject("MainMenuUI");
            menuObj.transform.SetParent(_canvas.transform, false);
            menuObj.AddComponent<MainMenuUI>();

            // Create basic buttons (placeholders)
            CreateUIButton(menuObj.transform, "PlayButton", new Vector2(0, 50), OnPlayClicked);
            CreateUIButton(menuObj.transform, "SettingsButton", new Vector2(0, -50), OnSettingsClicked);
            CreateUIButton(menuObj.transform, "QuitButton", new Vector2(0, -150), OnQuitClicked);

            Debug.Log("[AutoSceneSetup] MainMenu UI created.");
        }

        // ─── Game UI ──────────────────────────────────────────

        private void CreateGameUI()
        {
            Debug.Log("[AutoSceneSetup] Creating Game UI...");

            // Create GameUI object
            GameObject gameUIObj = new GameObject("GameUI");
            gameUIObj.transform.SetParent(_canvas.transform, false);
            gameUIObj.AddComponent<GameUI>();

            // Create PauseMenu
            GameObject pauseObj = new GameObject("PauseMenu");
            pauseObj.transform.SetParent(_canvas.transform, false);
            pauseObj.AddComponent<PauseMenuUI>();

            // Create ResultScreen
            GameObject resultObj = new GameObject("ResultScreen");
            resultObj.transform.SetParent(_canvas.transform, false);
            resultObj.AddComponent<ResultScreenUI>();

            // Create basic HUD
            CreateUIText(gameUIObj.transform, "TimerText", new Vector2(-300, 900), "0.0s");
            CreateUIText(gameUIObj.transform, "ActionsText", new Vector2(300, 900), "Moves: 0");

            // Create buttons
            CreateUIButton(gameUIObj.transform, "PauseButton", new Vector2(450, 900), OnPauseClicked);
            CreateUIButton(gameUIObj.transform, "ResetButton", new Vector2(-450, 900), OnResetClicked);

            Debug.Log("[AutoSceneSetup] Game UI created.");
        }

        // ─── Level Select UI ──────────────────────────────────

        private void CreateLevelSelectUI()
        {
            Debug.Log("[AutoSceneSetup] Creating LevelSelect UI...");

            // Create LevelSelectUI object
            GameObject levelObj = new GameObject("LevelSelectUI");
            levelObj.transform.SetParent(_canvas.transform, false);
            levelObj.AddComponent<LevelSelectUI>();

            // Create back button
            CreateUIButton(levelObj.transform, "BackButton", new Vector2(-450, 900), OnBackClicked);

            Debug.Log("[AutoSceneSetup] LevelSelect UI created.");
        }

        // ─── Loading UI ───────────────────────────────────────

        private void CreateLoadingUI()
        {
            Debug.Log("[AutoSceneSetup] Creating Loading UI...");

            // Create LoadingScreenUI object
            GameObject loadingObj = new GameObject("LoadingScreen");
            loadingObj.transform.SetParent(_canvas.transform, false);
            loadingObj.AddComponent<LoadingScreenUI>();

            // Create progress bar
            CreateUIPanel(loadingObj.transform, "LoadingPanel", Vector2.zero, new Vector2(800, 100));
            CreateUIProgressBar(loadingObj.transform, "ProgressBar", Vector2.zero);
            CreateUIText(loadingObj.transform, "ProgressText", new Vector2(0, -100), "Loading... 0%");

            Debug.Log("[AutoSceneSetup] Loading UI created.");
        }

        // ─── UI Helper Methods ────────────────────────────────

        private GameObject CreateUIButton(Transform parent, string name, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.transform.localPosition = position;

            // Add RectTransform
            var rect = btnObj.AddComponent<UnityEngine.RectTransform>();
            rect.sizeDelta = new Vector2(300, 80);

            // Add Image (background)
            var image = btnObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);

            // Add Button component
            var button = btnObj.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(onClick);

            // Add Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<UnityEngine.RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = name.Replace("Button", "");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 24;

            return btnObj;
        }

        private GameObject CreateUIText(Transform parent, string name, Vector2 position, string content)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            textObj.transform.localPosition = position;

            var rect = textObj.AddComponent<UnityEngine.RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);

            var text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = content;
            text.color = Color.white;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;

            return textObj;
        }

        private GameObject CreateUIPanel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            panelObj.transform.localPosition = position;

            var rect = panelObj.AddComponent<UnityEngine.RectTransform>();
            rect.sizeDelta = size;

            var image = panelObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            return panelObj;
        }

        private GameObject CreateUIProgressBar(Transform parent, string name, Vector2 position)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);
            barObj.transform.localPosition = position;

            var rect = barObj.AddComponent<UnityEngine.RectTransform>();
            rect.sizeDelta = new Vector2(600, 40);

            // Background
            var bgImage = barObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            var fillRect = fillObj.AddComponent<UnityEngine.RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            var fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            // Slider component
            var slider = barObj.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fillRect;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            slider.interactable = false;

            return barObj;
        }

        // ─── Button Callbacks ─────────────────────────────────

        private void OnPlayClicked()
        {
            Debug.Log("[AutoSceneSetup] Play clicked!");
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            SceneController.Instance?.LoadLevelSelect();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[AutoSceneSetup] Settings clicked!");
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
        }

        private void OnQuitClicked()
        {
            Debug.Log("[AutoSceneSetup] Quit clicked!");
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnPauseClicked()
        {
            Debug.Log("[AutoSceneSetup] Pause clicked!");
            GameManager.Instance?.PauseGame();
        }

        private void OnResetClicked()
        {
            Debug.Log("[AutoSceneSetup] Reset clicked!");
            LevelManager.Instance?.ResetLevel();
        }

        private void OnBackClicked()
        {
            Debug.Log("[AutoSceneSetup] Back clicked!");
            SceneController.Instance?.LoadMainMenu();
        }
    }
}
