using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;

namespace SnakeRescue.Managers
{
    /// <summary>
    /// Generic object pool manager.
    /// Reuses GameObjects instead of Instantiate/Destroy.
    ///
    /// Why this matters:
    /// - Physics objects spawn and die frequently
    /// - Mobile cannot afford constant GC pressure
    /// - Pool keeps objects inactive instead of destroying them
    ///
    /// Usage:
    ///   ObjectPoolManager.Instance.GetObject("Ball", position, rotation)
    ///   ObjectPoolManager.Instance.ReturnObject("Ball", gameObject)
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static ObjectPoolManager Instance { get; private set; }

        // ─── Pool Definition ──────────────────────────────────
        [System.Serializable]
        public class PoolEntry
        {
            public string     PoolID;
            public GameObject Prefab;
            public int        InitialSize = 5;
            public int        MaxSize     = 20;
            public bool       Expandable  = true;
        }

        // ─── Inspector ────────────────────────────────────────
        [Header("Pool Definitions")]
        [SerializeField] private List<PoolEntry> _poolDefinitions
            = new List<PoolEntry>();

        // ─── Runtime ──────────────────────────────────────────
        private Dictionary<string, Queue<GameObject>>  _available
            = new Dictionary<string, Queue<GameObject>>();

        private Dictionary<string, List<GameObject>>   _allObjects
            = new Dictionary<string, List<GameObject>>();

        private Dictionary<string, PoolEntry>          _definitions
            = new Dictionary<string, PoolEntry>();

        private Dictionary<string, Transform>           _poolParents
            = new Dictionary<string, Transform>();

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

            InitializePools();
        }

        private void OnEnable()
        {
            GameEvents.OnLevelReset += OnLevelReset;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelReset -= OnLevelReset;
        }

        // ─── Initialization ───────────────────────────────────

        private void InitializePools()
        {
            foreach (PoolEntry entry in _poolDefinitions)
            {
                if (string.IsNullOrEmpty(entry.PoolID) || entry.Prefab == null)
                {
                    Debug.LogWarning(
                        "[ObjectPoolManager] Skipping invalid pool entry.");
                    continue;
                }

                CreatePool(entry);
            }

            Debug.Log(
                $"[ObjectPoolManager] Initialized {_definitions.Count} pools.");
        }

        private void CreatePool(PoolEntry entry)
        {
            _definitions[entry.PoolID] = entry;
            _available[entry.PoolID]   = new Queue<GameObject>();
            _allObjects[entry.PoolID]  = new List<GameObject>();

            // Create parent transform to keep hierarchy clean
            GameObject parent = new GameObject($"Pool_{entry.PoolID}");
            parent.transform.SetParent(transform);
            _poolParents[entry.PoolID] = parent.transform;

            // Pre-warm pool
            for (int i = 0; i < entry.InitialSize; i++)
            {
                CreateNewObject(entry.PoolID);
            }
        }

        private GameObject CreateNewObject(string poolID)
        {
            if (!_definitions.TryGetValue(poolID, out PoolEntry entry))
                return null;

            GameObject obj = Instantiate(
                entry.Prefab,
                Vector3.zero,
                Quaternion.identity,
                _poolParents[poolID]);

            obj.name = $"{poolID}_{_allObjects[poolID].Count}";
            obj.SetActive(false);

            _allObjects[poolID].Add(obj);
            _available[poolID].Enqueue(obj);

            return obj;
        }

        // ─── Get Object ───────────────────────────────────────

        public GameObject GetObject(string poolID,
                                    Vector3    position,
                                    Quaternion rotation)
        {
            if (!_available.TryGetValue(poolID, out Queue<GameObject> queue))
            {
                Debug.LogError(
                    $"[ObjectPoolManager] Pool not found: {poolID}");
                return null;
            }

            GameObject obj = null;

            // Try to get from available queue
            while (queue.Count > 0)
            {
                obj = queue.Dequeue();
                if (obj != null) break;
            }

            // Queue was empty or all objects were null
            if (obj == null)
            {
                if (_definitions[poolID].Expandable &&
                    _allObjects[poolID].Count < _definitions[poolID].MaxSize)
                {
                    obj = CreateNewObject(poolID);
                    // Remove from available since we're about to use it
                    if (_available[poolID].Count > 0)
                        _available[poolID].Dequeue();
                }
                else
                {
                    Debug.LogWarning(
                        $"[ObjectPoolManager] Pool '{poolID}' is at max capacity.");
                    return null;
                }
            }

            // Set up the object
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Notify the object it has been retrieved
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawnedFromPool();

            return obj;
        }

        public GameObject GetObject(string poolID, Vector3 position)
            => GetObject(poolID, position, Quaternion.identity);

        // ─── Return Object ────────────────────────────────────

        public void ReturnObject(string poolID, GameObject obj)
        {
            if (obj == null) return;

            if (!_available.ContainsKey(poolID))
            {
                Debug.LogWarning(
                    $"[ObjectPoolManager] Returning to unknown pool: {poolID}");
                Destroy(obj);
                return;
            }

            // Notify the object it is being returned
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturnedToPool();

            obj.SetActive(false);
            obj.transform.SetParent(_poolParents[poolID]);
            obj.transform.position = Vector3.zero;

            _available[poolID].Enqueue(obj);
        }

        public void ReturnObjectDelayed(string poolID,
                                        GameObject obj,
                                        float delay)
        {
            if (obj == null) return;
            StartCoroutine(ReturnAfterDelay(poolID, obj, delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(
            string poolID, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnObject(poolID, obj);
        }

        // ─── Pool Registration (Runtime) ──────────────────────

        /// <summary>
        /// Register a new pool at runtime if needed.
        /// </summary>
        public void RegisterPool(string poolID,
                                 GameObject prefab,
                                 int initialSize  = 5,
                                 int maxSize      = 20)
        {
            if (_definitions.ContainsKey(poolID))
            {
                Debug.LogWarning(
                    $"[ObjectPoolManager] Pool already exists: {poolID}");
                return;
            }

            PoolEntry entry = new PoolEntry
            {
                PoolID      = poolID,
                Prefab      = prefab,
                InitialSize = initialSize,
                MaxSize     = maxSize,
                Expandable  = true
            };

            CreatePool(entry);
        }

        // ─── Level Reset ──────────────────────────────────────

        private void OnLevelReset()
        {
            ReturnAllActiveObjects();
        }

        public void ReturnAllActiveObjects()
        {
            foreach (var kvp in _allObjects)
            {
                string         poolID  = kvp.Key;
                List<GameObject> objs  = kvp.Value;

                foreach (GameObject obj in objs)
                {
                    if (obj != null && obj.activeInHierarchy)
                    {
                        ReturnObject(poolID, obj);
                    }
                }
            }
        }

        // ─── Info ─────────────────────────────────────────────

        public int GetAvailableCount(string poolID)
        {
            if (_available.TryGetValue(poolID, out Queue<GameObject> q))
                return q.Count;
            return 0;
        }

        public int GetTotalCount(string poolID)
        {
            if (_allObjects.TryGetValue(poolID, out List<GameObject> list))
                return list.Count;
            return 0;
        }
    }

    // ─── IPoolable Interface ──────────────────────────────────

    /// <summary>
    /// Any pooled object can implement this to react
    /// when it is spawned or returned.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnedFromPool();
        void OnReturnedToPool();
    }
}
