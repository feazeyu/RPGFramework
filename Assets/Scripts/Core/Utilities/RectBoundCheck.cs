
using UnityEngine;

namespace Game.Core.Utilities
{
    public static class RectBoundCheck
    {
        public static bool IsElementWithinAnother(RectTransform encapsulatingElement, RectTransform encapsulatedElement)
        {
            Vector3[] encapsulatedCorners = new Vector3[4];
            encapsulatedElement.GetWorldCorners(encapsulatedCorners);

            Vector3[] encapsulatingCorners = new Vector3[4];
            encapsulatingElement.GetWorldCorners(encapsulatingCorners);

            Vector2 canvasMin = encapsulatingCorners[0];
            Vector2 canvasMax = encapsulatingCorners[2];

            foreach (Vector3 corner in encapsulatedCorners)
            {
                if (corner.x < canvasMin.x || corner.x > canvasMax.x ||
                    corner.y < canvasMin.y || corner.y > canvasMax.y)
                {
                    return false; // At least one corner is outside
                }
            }

            return true; // All corners are within the canvas
        }
    }   
}
