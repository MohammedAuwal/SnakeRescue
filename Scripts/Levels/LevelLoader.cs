using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Data;
using SnakeRescue.Managers;

namespace SnakeRescue.Levels
{
    /// <summary>
    /// Handles spawning and clearing all objects in a level.
    ///
    /// Reads a LevelConfig and places:
    /// - Physics objects (balls, rocks, weights)
    /// - Control objects (pins, gates, levers)
    /// - Hazards (fire, spikes, water)
    /// - Characters (princess, snake)
    ///
    /// All spawned objects are tracked so they can
    /// be cleanly destroyed on reset or level end.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────
        [Header("Character Prefabs")]
        [SerializeField] private GameObject _princessPrefab;
        [SerializeField] private GameObject _snakePrefab;

        [Header("Object Prefabs")]
        [SerializeField] private GameObject _ballPrefab;
        [SerializeField] private GameObject _rockPrefab;
        [SerializeField] private GameObject _weightPrefab;
        [SerializeField] private GameObject _gatePrefab;
        [SerializeField] private GameObject _pinPrefab;
        [SerializeField] private GameObject _ropePrefab;
        [SerializeField] private GameObject _leverPrefab;

        [Header("Hazard Prefabs")]
        [SerializeField] private GameObject _firePrefab;
        [SerializeField] private GameObject _spikePrefab;
        [SerializeField] private GameObject _waterPrefab;
        [SerializeField] private GameObject _trapPrefab;

        [Header("Spawn Parents")]
        [SerializeField] private Transform _charactersParent;
        [SerializeField] private Transform _objectsParent;
        [SerializeField] private Transform _hazardsParent;

        // ─── Runtime ──────────────────────────────────────────
        private List<GameObject> _spawnedObjects  = new List<GameObject>();
        private GameObject       _spawnedPrincess;
        private GameObject       _spawnedSnake;

        // ─── Load Level ───────────────────────────────────────

        public void LoadLevel(LevelConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[LevelLoader] Cannot load null config.");
                return;
            }

            ClearLevel();
            StartCoroutine(SpawnLevelRoutine(config));
        }

        private IEnumerator SpawnLevelRoutine(LevelConfig config)
        {
            // Spawn all configured objects
            foreach (LevelObjectEntry entry in config.Objects)
            {
                SpawnObject(entry);

                // Spread spawning across frames on mobile
                // to avoid frame spike
                yield return null;
            }

            Debug.Log(
                $"[LevelLoader] Spawned {_spawnedObjects.Count} objects " +
                $"for level {config.LevelIndex}");
        }

        // ─── Spawn Object ─────────────────────────────────────

        private void SpawnObject(LevelObjectEntry entry)
        {
            GameObject prefab = GetPrefabForType(entry.Type);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[LevelLoader] No prefab for type: {entry.Type}");
                return;
            }

            Transform parent = GetParentForType(entry.Type);

            GameObject obj = Instantiate(
                prefab,
                entry.Position,
                Quaternion.Euler(entry.Rotation),
                parent);

            obj.transform.localScale = entry.Scale;
            obj.name = $"{entry.Type}_{_spawnedObjects.Count}";

            // Apply interactable state
            SetInteractable(obj, entry.IsInteractable);

            _spawnedObjects.Add(obj);

            // Track characters separately
            if (entry.Type == ObjectType.Ball ||
                entry.Type == ObjectType.Rock)
            {
                // Register with pool if using pool system
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null)
                    ChainReactionSystem.Instance?.TrackBody(rb);
            }
        }

        // ─── Prefab Lookup ────────────────────────────────────

        private GameObject GetPrefabForType(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Ball:   return _ballPrefab;
                case ObjectType.Rock:   return _rockPrefab;
                case ObjectType.Weight: return _weightPrefab;
                case ObjectType.Gate:   return _gatePrefab;
                case ObjectType.Pin:    return _pinPrefab;
                case ObjectType.Rope:   return _ropePrefab;
                case ObjectType.Lever:  return _leverPrefab;
                case ObjectType.Fire:   return _firePrefab;
                case ObjectType.Spike:  return _spikePrefab;
                case ObjectType.Water:  return _waterPrefab;
                case ObjectType.Trap:   return _trapPrefab;

                default:
                    Debug.LogWarning(
                        $"[LevelLoader] Unknown type: {type}");
                    return null;
            }
        }

        private Transform GetParentForType(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Fire:
                case ObjectType.Spike:
                case ObjectType.Water:
                case ObjectType.Trap:
                    return _hazardsParent ?? transform;

                default:
                    return _objectsParent ?? transform;
            }
        }

        // ─── Interactable Setup ───────────────────────────────

        private void SetInteractable(GameObject obj, bool interactable)
        {
            // Get all IInteractable on object and children
            IInteractable[] components =
                obj.GetComponentsInChildren<IInteractable>();

            // We cast to MonoBehaviour to enable/disable
            // since IInteractable itself has no enabled state
            foreach (IInteractable component in components)
            {
                MonoBehaviour mono = component as MonoBehaviour;
                if (mono != null)
                    mono.enabled = interactable;
            }
        }

        // ─── Character Spawning ───────────────────────────────

        public GameObject SpawnPrincess(Vector3 position)
        {
            if (_princessPrefab == null)
            {
                Debug.LogError("[LevelLoader] Princess prefab not assigned.");
                return null;
            }

            if (_spawnedPrincess != null)
                Destroy(_spawnedPrincess);

            _spawnedPrincess = Instantiate(
                _princessPrefab,
                position,
                Quaternion.identity,
                _charactersParent ?? transform);

            _spawnedObjects.Add(_spawnedPrincess);
            return _spawnedPrincess;
        }

        public GameObject SpawnSnake(Vector3 position)
        {
            if (_snakePrefab == null)
            {
                Debug.LogError("[LevelLoader] Snake prefab not assigned.");
                return null;
            }

            if (_spawnedSnake != null)
                Destroy(_spawnedSnake);

            _spawnedSnake = Instantiate(
                _snakePrefab,
                position,
                Quaternion.identity,
                _charactersParent ?? transform);

            _spawnedObjects.Add(_spawnedSnake);
            return _spawnedSnake;
        }

        // ─── Clear Level ──────────────────────────────────────

        public void ClearLevel()
        {
            ChainReactionSystem.Instance?.ResetSystem();

            foreach (GameObject obj in _spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }

            _spawnedObjects.Clear();
            _spawnedPrincess = null;
            _spawnedSnake    = null;

            Debug.Log("[LevelLoader] Level cleared.");
        }

        // ─── Getters ──────────────────────────────────────────

        public GameObject GetPrincess() => _spawnedPrincess;
        public GameObject GetSnake()    => _spawnedSnake;

        public List<GameObject> GetAllSpawnedObjects()
            => new List<GameObject>(_spawnedObjects);

        public List<T> GetAllOfType<T>() where T : Component
        {
            List<T> result = new List<T>();
            foreach (GameObject obj in _spawnedObjects)
            {
                if (obj == null) continue;
                T component = obj.GetComponent<T>();
                if (component != null)
                    result.Add(component);
            }
            return result;
        }
    }
}
