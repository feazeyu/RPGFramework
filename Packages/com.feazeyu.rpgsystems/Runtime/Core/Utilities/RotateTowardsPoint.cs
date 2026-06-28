using UnityEngine;
using UnityEngine.InputSystem;

namespace Feazeyu.RPGSystems.Core
{
    /// <summary>
    /// Rotates the owning transform to face a screen-space point, driven by mouse aiming.
    /// </summary>
    public class RotateTowardsPoint : MonoBehaviour
    {
        /// <summary>The most recently computed normalized aim direction.</summary>
        public Vector2 aimDirection;

        /// <summary>The most recently computed aim angle, in degrees.</summary>
        public float angle;

        /// <summary>
        /// Rotates the transform so its local right axis points toward <paramref name="point"/>.
        /// No-op when the point is zero or the mouse is inactive.
        /// </summary>
        /// <param name="point">Target position in screen space.</param>
        /// <param name="cam">Camera used to project the transform's world position; falls back to <see cref="Camera.main"/>.</param>
        public void RotateTowards(Vector2 point, Camera cam = null)
        {
            if (point == Vector2.zero)
                return;
            if (Mouse.current != null && Mouse.current.position.IsActuated())
            {
                if (cam == null)
                    cam = Camera.main;
                if (cam == null)
                {
                    Debug.LogError("No camera found. Either correctly tag a MainCamera or pass one as an argument");
                    return;
                }
                Vector2 screenPosition = cam.WorldToScreenPoint(transform.position);
                aimDirection = (point - screenPosition).normalized;
                angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}
