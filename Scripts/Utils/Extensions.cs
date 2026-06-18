using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace SnakeRescue.Utils
{
    /// <summary>
    /// Extension methods for common Unity and C# operations.
    /// Keeps core code clean and readable.
    ///
    /// Usage:
    ///   transform.SetLayer("Objects");
    ///   list.DestroyChildren();
    /// </summary>
    public static class Extensions
    {
        // ─── Transform ────────────────────────────────────────

        /// <summary>
        /// Sets the layer of a transform and all its children.
        /// </summary>
        public static void SetLayer(this Transform transform, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                Debug.LogWarning($"[Extensions] Layer '{layerName}' not found.");
                return;
            }

            SetLayerRecursive(transform, layer);
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
            {
                SetLayerRecursive(child, layer);
            }
        }

        /// <summary>
        /// Destroys all children of this transform.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            if (transform == null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Resets local position, rotation, and scale to identity.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale    = Vector3.one;
        }

        // ─── GameObject ───────────────────────────────────────

        /// <summary>
        /// Sets active state and returns the GameObject for chaining.
        /// </summary>
        public static GameObject SetActive(this GameObject go, bool active)
        {
            go.SetActive(active);
            return go;
        }

        /// <summary>
        /// Gets or adds a component of type T.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        // ─── Collections ──────────────────────────────────────

        /// <summary>
        /// Adds a range of items to a list.
        /// </summary>
        public static void AddRange<T>(this List<T> list, IEnumerable<T> collection)
        {
            if (list == null || collection == null) return;
            list.AddRange(collection);
        }

        /// <summary>
        /// Removes all null entries from a list.
        /// </summary>
        public static void RemoveNulls<T>(this List<T> list) where T : class
        {
            list.RemoveAll(item => item == null);
        }

        // ─── String ───────────────────────────────────────────

        /// <summary>
        /// Truncates string to max length with ellipsis.
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        // ─── Vector ───────────────────────────────────────────

        /// <summary>
        /// Returns a vector with Z set to 0.
        /// </summary>
        public static Vector3 ToFlat(this Vector3 v)
        {
            return new Vector3(v.x, v.y, 0f);
        }

        /// <summary>
        /// Returns random point inside circle radius.
        /// </summary>
        public static Vector2 RandomInCircle(this Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle * radius;
        }
    }
}
