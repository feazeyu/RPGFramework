using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Core
{
    public class RotateTowardsPoint : MonoBehaviour
    {
        public Vector2 aimDirection;
        public float angle;
        public void RotateTowards(Vector2 point, Camera cam = null) {
            // Mouse aiming
            if (point == Vector2.zero)
                return;
            if (Mouse.current != null && Mouse.current.position.IsActuated())
            {
                if(cam == null)
                    cam = Camera.main;
                if (cam == null) { 
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
