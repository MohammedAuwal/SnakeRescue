using UnityEngine;
using SnakeRescue.Utils;

namespace SnakeRescue.Characters.Snake
{
    /// <summary>
    /// Determines if the snake can reach the princess.
    ///
    /// MVP Approach:
    /// Instead of complex A* navmesh, we use Raycasts.
    /// If a wall blocks the direct line to princess → Path Blocked.
    /// If an object blocks the line → Path Blocked (until object moves).
    ///
    /// This fits the physics puzzle design:
    /// Snake waits until player clears the path.
    /// </summary>
    public class SnakePathfinder : MonoBehaviour
    {
        // ─── Settings ─────────────────────────────────────────
        [Header("Pathfinding")]
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private LayerMask _objectLayer;
        [SerializeField] private int   _raycastCount   = 3;
        [SerializeField] private float _raycastSpread  = 0.5f;

        // ─── Cache ────────────────────────────────────────────
        private RaycastHit2D[] _hits;

        // ─── Public API ───────────────────────────────────────

        /// <summary>
        /// Checks if there is a clear path from snake to princess.
        /// Returns false if walls or static objects block the way.
        /// </summary>
        public bool HasPathToPrincess(Vector2 start, Vector2 target)
        {
            Vector2 direction = (target - start).normalized;
            float distance    = Vector2.Distance(start, target);

            // Cast multiple rays to cover snake width
            for (int i = 0; i < _raycastCount; i++)
            {
                float offset = Mathf.Lerp(
                    -_raycastSpread, _raycastSpread,
                    (float)i / (_raycastCount - 1));

                Vector2 offsetDir = Quaternion.Euler(0, 0, offset * 90f) * direction;

                RaycastHit2D hit = Physics2D.Raycast(
                    start,
                    offsetDir,
                    distance,
                    _wallLayer | _objectLayer);

                // If we hit something before reaching target
                if (hit.collider != null && hit.distance < distance * 0.95f)
                {
                    // Check if what we hit is movable
                    if (IsBlocker(hit.collider))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the next movement direction toward target.
        /// </summary>
        public Vector2 GetNextDirection(Vector2 start, Vector2 target)
        {
            return (target - start).normalized;
        }

        // ─── Helpers ──────────────────────────────────────────

        private bool IsBlocker(Collider2D collider)
        {
            // Walls are always blockers
            if (((1 << collider.gameObject.layer) & _wallLayer) != 0)
                return true;

            // Objects are blockers if they are heavy/static
            Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // If object is kinematic or heavy, it blocks
                if (rb.bodyType == RigidbodyType2D.Kinematic)
                    return true;

                if (rb.mass > 5f)
                    return true;
            }

            return false;
        }

        // ─── Gizmos ───────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * 2f);
        }
    }
}
