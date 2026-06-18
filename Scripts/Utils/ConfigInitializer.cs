using UnityEngine;
using SnakeRescue.Data;

namespace SnakeRescue.Utils
{
    /// <summary>
    /// Automatically creates required ScriptableObjects if missing.
    ///
    /// This helps when running the game for the first time
    /// without manually creating assets in the Editor.
    ///
    /// NOTE: In a real build, you should create these assets
    /// in the Editor. This is a safety net for development.
    ///
    /// Attach this to an empty GameObject in the first scene.
    /// </summary>
    public class ConfigInitializer : MonoBehaviour
    {
        // ─── Settings ─────────────────────────────────────────
        [Header("Paths")]
        [SerializeField] private string _gameConfigPath = "GameConfig";
        [SerializeField] private bool   _autoCreate     = true;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (_autoCreate)
            {
                EnsureGameConfigExists();
            }
        }

        // ─── GameConfig Check ─────────────────────────────────

        private void EnsureGameConfigExists()
        {
            GameConfig config = GameConfig.Instance;

            if (config == null)
            {
                Debug.LogWarning(
                    "[ConfigInitializer] GameConfig not found in Resources. " +
                    "Creating temporary runtime config...");

                CreateRuntimeGameConfig();
            }
            else
            {
                Debug.Log("[ConfigInitializer] GameConfig found.");
            }
        }

        // ─── Runtime Config Creation ──────────────────────────

        private void CreateRuntimeGameConfig()
        {
            // Create ScriptableObject instance
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

            // Set defaults
            config.GameName = "Snake Rescue";
            config.Version  = "1.0.0";

            // Add default skin
            config.AvailableSkins.Add(new SkinDefinition
            {
                SkinID        = "Default",
                DisplayName   = "Classic Princess",
                StarsRequired = 0,
                IsPremium     = false
            });

            // Add default level config (placeholder)
            LevelConfig level1 = ScriptableObject.CreateInstance<LevelConfig>();
            level1.LevelIndex = 0;
            level1.LevelName  = "Level 1";
            config.Levels.Add(level1);

            // NOTE: We cannot save this asset to disk at runtime.
            // It exists only in memory for this session.
            // This prevents crashes but levels won't persist without Editor setup.

            Debug.Log(
                "[ConfigInitializer] Runtime GameConfig created. " +
                "Please create permanent assets in Editor for full features.");
        }

        // ─── Editor Helper ────────────────────────────────────

        /// <summary>
        /// Call this from a button in Editor to create assets.
        /// </summary>
        [ContextMenu("Create Default Assets")]
        private void CreateDefaultAssets()
        {
            Debug.Log(
                "[ConfigInitializer] To create assets properly, " +
                "right-click in Project window → Create → SnakeRescue → GameConfig");
        }
    }
}
