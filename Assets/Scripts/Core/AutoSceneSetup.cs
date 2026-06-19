using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SnakeRescue.Core
{
    /// <summary>
    /// AUTO SCENE SETUP - NO EDITOR REQUIRED
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
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                Debug.Log("[AutoSceneSetup] Camera already exists.");
                return;
            }

            GameObject cameraObj = new GameObject("Main Camera");
            _mainCamera = cameraObj.AddComponent<Camera>();

            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 5f;
            _mainCamera.nearClipPlane = 0.3f;
            _mainCamera.farClipPlane = 1000f;
            _mainCamera.cullingMask = -1;
            _mainCamera.depth = 0;

            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0, 0, -10);

            Debug.Log("[AutoSceneSetup] Main Camera created.");
        }

        // ─── EventSystem Setup ────────────────────────────────

        private void CreateEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                Debug.Log("[AutoSceneSetup] EventSystem already exists.");
                return;
            }

            _eventSystem = new GameObject("EventSystem");
            var eventSystem = _eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            var inputModule = _eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            inputModule.horizontalAxis = "Horizontal";
            inputModule.verticalAxis = "Vertical";
            inputModule.submitButton = "Submit";
            inputModule.cancelButton = "Cancel";

            // Add Touch Input Module (conditional based on obsolete state in newer versions)
#if !UNITY_2022_1_OR_NEWER
            _eventSystem.AddComponent<UnityEngine.EventSystems.TouchInputModule>();
#endif
            Debug.Log("[AutoSceneSetup] EventSystem created.");
        }

        // ─── Manager Setup ────────────────────────────────────

        private void CreateManagers()
        {
            CreateRuntimeManager("GameManager", "SnakeRescue.Managers.GameManager");
            CreateRuntimeManager("SceneController", "SnakeRescue.Managers.SceneController");
            CreateRuntimeManager("AudioManager", "SnakeRescue.Managers.AudioManager");
            CreateRuntimeManager("SaveSystem", "SnakeRescue.Systems.SaveSystem");
            CreateRuntimeManager("ProgressManager", "SnakeRescue.Managers.ProgressManager");
            CreateRuntimeManager("CosmeticSystem", "SnakeRescue.Systems.CosmeticSystem");
            CreateRuntimeManager("ObjectPoolManager", "SnakeRescue.Managers.ObjectPoolManager");
        }

        private void CreateRuntimeManager(string gameObjectName, string componentTypeName)
        {
            System.Type type = System.Type.GetType(componentTypeName + ", Assembly-CSharp");
            if (type == null)
            {
                Debug.LogWarning($"[AutoSceneSetup] Component type not found: {componentTypeName}");
                return;
            }

            if (FindObjectOfType(type) == null)
            {
                GameObject managerObj = new GameObject(gameObjectName);
                managerObj.AddComponent(type);
                DontDestroyOnLoad(managerObj);
                Debug.Log($"[AutoSceneSetup] {gameObjectName} created.");
            }
        }

        // ─── Canvas Setup ─────────────────────────────────────

        private void CreateCanvas()
        {
            if (FindObjectOfType<Canvas>() != null)
            {
                Debug.Log("[AutoSceneSetup] Canvas already exists.");
                return;
            }

            _canvas = new GameObject("Canvas");
            var canvas = _canvas.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 0;

            var scaler = _canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Shrink;

            _canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
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
                    CreateRuntimeUI("MainMenuUI", "SnakeRescue.UI.MainMenuUI");
                    break;
                case "GameScene":
                    CreateRuntimeUI("GameUI", "SnakeRescue.UI.GameUI");
                    CreateRuntimeUI("PauseMenu", "SnakeRescue.UI.PauseMenuUI");
                    CreateRuntimeUI("ResultScreen", "SnakeRescue.UI.ResultScreenUI");
                    break;
                case "LevelSelect":
                    CreateRuntimeUI("LevelSelectUI", "SnakeRescue.UI.LevelSelectUI");
                    break;
                case "Loading":
                    CreateRuntimeUI("LoadingScreen", "SnakeRescue.UI.LoadingScreenUI");
                    break;
                default:
                    Debug.Log($"[AutoSceneSetup] No UI setup for scene: {sceneName}");
                    break;
            }
        }

        private void CreateRuntimeUI(string gameObjectName, string componentTypeName)
        {
            System.Type type = System.Type.GetType(componentTypeName + ", Assembly-CSharp");
            if (type == null) return;

            GameObject uiObj = new GameObject(gameObjectName);
            uiObj.transform.SetParent(_canvas.transform, false);
            uiObj.AddComponent(type);
        }

        // ─── UI Helper Methods ────────────────────────────────

        private GameObject CreateUIButton(Transform parent, string name, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.transform.localPosition = position;

            var rect = btnObj.AddComponent<UnityEngine.RectTransform>();
            rect.sizeDelta = new Vector2(300, 80);

            var image = btnObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);

            var button = btnObj.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(onClick);

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

        private void OnPlayClicked() {}
        private void OnSettingsClicked() {}
        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
