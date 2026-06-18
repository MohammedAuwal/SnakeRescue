using UnityEngine;
using System;
using System.Text;

namespace SnakeRescue.Utils
{
    /// <summary>
    /// Static helper functions for common operations.
    /// Unlike Extensions, these are called directly.
    ///
    /// Usage:
    ///   string time = Helpers.FormatTime(125.5f);
    ///   Color c = Helpers.GetRandomPastelColor();
    /// </summary>
    public static class Helpers
    {
        // ─── Time Formatting ──────────────────────────────────

        /// <summary>
        /// Formats seconds into mm:ss.ff format.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int ms   = Mathf.FloorToInt((seconds % 1f) * 100f);

            return $"{mins:D2}:{secs:D2}:{ms:D2}";
        }

        /// <summary>
        /// Formats seconds into simple s.ff format.
        /// </summary>
        public static string FormatSeconds(float seconds)
        {
            return $"{seconds:F2}s";
        }

        // ─── Color Helpers ────────────────────────────────────

        /// <summary>
        /// Returns a random pastel color.
        /// </summary>
        public static Color GetRandomPastelColor()
        {
            float r = UnityEngine.Random.Range(0.5f, 1f);
            float g = UnityEngine.Random.Range(0.5f, 1f);
            float b = UnityEngine.Random.Range(0.5f, 1f);
            return new Color(r, g, b);
        }

        /// <summary>
        /// Returns a color from hex string.
        /// </summary>
        public static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        /// <summary>
        /// Linear interpolation between two colors.
        /// </summary>
        public static Color LerpColor(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.Clamp01(t));
        }

        // ─── Math Helpers ─────────────────────────────────────

        /// <summary>
        /// Remaps value from one range to another.
        /// </summary>
        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        /// <summary>
        /// Returns true if value is within range (inclusive).
        /// </summary>
        public static bool InRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Clamps value but returns original if already in range.
        /// Useful for detecting if clamping occurred.
        /// </summary>
        public static bool TryClamp(ref float value, float min, float max)
        {
            float old = value;
            value = Mathf.Clamp(value, min, max);
            return !Mathf.Approximately(old, value);
        }

        // ─── Debug Helpers ────────────────────────────────────

        /// <summary>
        /// Conditional debug log. Only works in Debug mode.
        /// </summary>
        public static void DebugLog(string message, Object context = null)
        {
#if UNITY_EDITOR
            Debug.Log(message, context);
#endif
        }

        /// <summary>
        /// Conditional debug warning.
        /// </summary>
        public static void DebugWarn(string message, Object context = null)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message, context);
#endif
        }

        // ─── String Helpers ───────────────────────────────────

        /// <summary>
        /// Creates a string of repeated characters.
        /// Example: RepeatChar('★', 3) → "★★★"
        /// </summary>
        public static string RepeatChar(char c, int count)
        {
            StringBuilder sb = new StringBuilder(count);
            for (int i = 0; i < count; i++)
                sb.Append(c);
            return sb.ToString();
        }

        /// <summary>
        /// Capitalizes first letter of string.
        /// </summary>
        public static string CapitalizeFirst(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        // ─── Physics Helpers ──────────────────────────────────

        /// <summary>
        /// Calculates impact force from velocity and mass.
        /// </summary>
        public static float CalculateImpactForce(Vector2 velocity, float mass)
        {
            return velocity.magnitude * mass;
        }

        /// <summary>
        /// Returns angle in degrees between two points.
        /// </summary>
        public static float GetAngle(Vector2 from, Vector2 to)
        {
            return Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        }
    }
}
