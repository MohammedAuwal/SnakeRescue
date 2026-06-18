using UnityEngine;
using UnityEditor;
using SnakeRescue.Data;
using System.IO;

public class AutoGenerateConfigs
{
    [MenuItem("Tools/SnakeRescue/Generate Default Assets")]
    public static void Generate()
    {
        // Create GameConfig
        string path = "Assets/Resources/GameConfig.asset";
        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(config, path);
            Debug.Log("Created GameConfig.asset");
        }

        // Create Level 1
        string levelPath = "Assets/ScriptableObjects/Levels/Level_0.asset";
        LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(levelPath);

        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelConfig>();
            level.LevelIndex = 0;
            level.LevelName = "Level 1";
            Directory.CreateDirectory("Assets/ScriptableObjects/Levels");
            AssetDatabase.CreateAsset(level, levelPath);
            Debug.Log("Created Level_0.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Default Assets Generated Successfully.");
    }
}
