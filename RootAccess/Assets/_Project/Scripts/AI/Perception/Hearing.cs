using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>
    /// Pure hearing rule shared by every listener: a noise carries <c>radius</c> world units, and a wall between the
    /// source and the listener muffles it to a fraction of that. Kept free of physics so it is unit-testable.
    /// </summary>
    public static class Hearing
    {
        /// <summary>Share of the radius that still carries through a wall or floor.</summary>
        public const float OcclusionFactor = 0.45f;

        public static float EffectiveRadius(float radius, bool occluded, float sensitivity = 1f) =>
            Mathf.Max(0f, radius) * Mathf.Max(0f, sensitivity) * (occluded ? OcclusionFactor : 1f);

        public static bool CanHear(Vector2 listener, Vector2 source, float radius, bool occluded, float sensitivity = 1f)
        {
            float effective = EffectiveRadius(radius, occluded, sensitivity);
            return effective > 0f && (listener - source).sqrMagnitude <= effective * effective;
        }
    }
}
