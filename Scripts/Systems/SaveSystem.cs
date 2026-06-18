using System;
using System.IO;
using UnityEngine;
using SnakeRescue.Data;
using SnakeRescue.Core;
using SnakeRescue.Utils;

namespace SnakeRescue.Systems
{
    /// <summary>
    /// Handles all saving and loading of player data.
    /// Uses JSON serialization written to persistent data path.
    /// PlayerPrefs is used as a fast-access fallback layer.
    ///
    /// Rules:
    /// - Only SaveSystem reads from / writes to disk
    /// - All other systems ask SaveSystem for data
    /// - Auto-saves after every level completion
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static SaveSystem Instance { get; private set; }

        // ─── File Paths ───────────────────────────────────────
        private string _saveFilePath;
        private string _backupFilePath;

        // ─── State ────────────────────────────────────────────
        public  PlayerData CurrentData   { get; private set; }
        public  bool       IsDataLoaded  { get; private set; } = false;
        private bool       _isDirty      = false;
        private float      _autoSaveTimer = 0f;
        private const float AUTO_SAVE_INTERVAL = 30f;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _saveFilePath   = Path.Combine(
                Application.persistentDataPath, "playerdata.json");
            _backupFilePath = Path.Combine(
                Application.persistentDataPath, "playerdata.backup.json");

            LoadData();
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnLevelFailed    += OnLevelFailed;
            GameEvents.OnSnakeDead      += OnSnakeDead;
            GameEvents.OnHintRequested  += OnHintRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnLevelFailed    -= OnLevelFailed;
            GameEvents.OnSnakeDead      -= OnSnakeDead;
            GameEvents.OnHintRequested  -= OnHintRequested;
        }

        private void Update()
        {
            // Periodic auto-save
            if (_isDirty)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
                {
                    SaveData();
                    _autoSaveTimer = 0f;
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _isDirty)
            {
                SaveData();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isDirty)
            {
                SaveData();
            }
        }

        // ─── Load ─────────────────────────────────────────────

        public void LoadData()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    string json = File.ReadAllText(_saveFilePath);
                    CurrentData = JsonUtility.FromJson<PlayerData>(json);

                    if (CurrentData == null)
                        throw new Exception("Parsed PlayerData is null.");

                    IsDataLoaded = true;
                    Debug.Log("[SaveSystem] Data loaded successfully.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[SaveSystem] Primary load failed: {e.Message}. " +
                    $"Trying backup...");

                TryLoadBackup();
                return;
            }

            // No save file — create fresh
            CreateNewData();
        }

        private void TryLoadBackup()
        {
            try
            {
                if (File.Exists(_backupFilePath))
                {
                    string json = File.ReadAllText(_backupFilePath);
                    CurrentData = JsonUtility.FromJson<PlayerData>(json);

                    if (CurrentData != null)
                    {
                        IsDataLoaded = true;
                        Debug.Log("[SaveSystem] Loaded from backup.");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[SaveSystem] Backup load also failed: {e.Message}");
            }

            // Both failed — fresh start
            CreateNewData();
        }

        private void CreateNewData()
        {
            CurrentData  = new PlayerData();
            IsDataLoaded = true;
            _isDirty     = true;
            SaveData();
            Debug.Log("[SaveSystem] Created new player data.");
        }

        // ─── Save ─────────────────────────────────────────────

        public void SaveData()
        {
            if (CurrentData == null)
            {
                Debug.LogError("[SaveSystem] Cannot save — CurrentData is null.");
                return;
            }

            try
            {
                CurrentData.LastSavedAt =
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string json = JsonUtility.ToJson(CurrentData, prettyPrint: true);

                // Write backup of previous save first
                if (File.Exists(_saveFilePath))
                {
                    File.Copy(_saveFilePath, _backupFilePath, overwrite: true);
                }

                // Write new save
                File.WriteAllText(_saveFilePath, json);

                _isDirty      = false;
                _autoSaveTimer = 0f;

                Debug.Log("[SaveSystem] Data saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        // ─── Data Mutators ────────────────────────────────────

        public void RecordLevelComplete(int levelIndex, int stars,
                                        float time,     int actions)
        {
            if (!IsDataLoaded) return;

            CurrentData.RecordLevelComplete(levelIndex, stars, time, actions);
            _isDirty = true;
            SaveData(); // Always save immediately on level complete
        }

        public void RecordLevelAttempt(int levelIndex)
        {
            if (!IsDataLoaded) return;

            CurrentData.RecordLevelAttempt(levelIndex);
            _isDirty = true;
        }

        public void UpdateSettings(float master, float music,
                                   float sfx,    bool vibration)
        {
            if (!IsDataLoaded) return;

            CurrentData.MasterVolume = master;
            CurrentData.MusicVolume  = music;
            CurrentData.SFXVolume    = sfx;
            CurrentData.Vibration    = vibration;
            _isDirty = true;
            SaveData();
        }

        public void AddPlayTime(float seconds)
        {
            if (!IsDataLoaded) return;

            CurrentData.AddPlayTime(seconds);
            _isDirty = true;
        }

        // ─── Data Queries ─────────────────────────────────────

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (!IsDataLoaded) return levelIndex == 0;
            return CurrentData.IsLevelUnlocked(levelIndex);
        }

        public int GetLevelStars(int levelIndex)
        {
            if (!IsDataLoaded) return 0;
            return CurrentData.GetLevelStars(levelIndex);
        }

        public int GetTotalStars()
        {
            if (!IsDataLoaded) return 0;
            return CurrentData.TotalStars;
        }

        public LevelRecord GetLevelRecord(int levelIndex)
        {
            if (!IsDataLoaded) return null;
            return CurrentData.GetLevelRecord(levelIndex);
        }

        // ─── Delete / Reset ───────────────────────────────────

        public void DeleteAllData()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                    File.Delete(_saveFilePath);

                if (File.Exists(_backupFilePath))
                    File.Delete(_backupFilePath);

                CreateNewData();
                Debug.Log("[SaveSystem] All data deleted.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
            }
        }

        // ─── Event Handlers ───────────────────────────────────

        private void OnLevelCompleted(LevelResult result, int stars)
        {
            if (GameManager.Instance == null) return;

            int   level   = GameManager.Instance.CurrentLevel;
            float time    = GameManager.Instance.GetLevelElapsedTime();
            int   actions = GameManager.Instance.GetActionsCount();

            RecordLevelComplete(level, stars, time, actions);
        }

        private void OnLevelFailed(LevelResult reason)
        {
            if (GameManager.Instance == null) return;

            CurrentData?.RecordDeath();
            RecordLevelAttempt(GameManager.Instance.CurrentLevel);
            _isDirty = true;
        }

        private void OnSnakeDead()
        {
            CurrentData?.RecordSnakeKill();
            _isDirty = true;
        }

        private void OnHintRequested()
        {
            CurrentData?.RecordHintUsed();
            _isDirty = true;
        }
    }
}
