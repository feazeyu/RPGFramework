using UnityEngine;

namespace Feazeyu.RPGSystems.Core.Utilities
{
    /// <summary>Geometric containment tests for UI <see cref="RectTransform"/>s.</summary>
    public static class RectBoundCheck
    {
        /// <summary>
        /// Determines whether one UI element is fully contained within another.
        /// </summary>
        /// <param name="encapsulatingElement">The outer element expected to contain the other.</param>
        /// <param name="encapsulatedElement">The inner element tested for containment.</param>
        /// <returns>
        /// <c>true</c> if every world corner of <paramref name="encapsulatedElement"/> lies
        /// within the axis-aligned bounds of <paramref name="encapsulatingElement"/>;
        /// otherwise <c>false</c>.
        /// </returns>
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
                    return false;
                }
            }

            return true;
        }
    }
}
