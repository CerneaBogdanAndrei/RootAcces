using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>The level's route, start to exit: the generator's <see cref="RouteStep"/> waypoints in order.</summary>
    public sealed class LevelRoute : MonoBehaviour
    {
        [SerializeField] private RouteStep[] steps = new RouteStep[0];

        public RouteStep[] Steps => steps;

        public void SetSteps(RouteStep[] values) => steps = values ?? new RouteStep[0];

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.6f);
            for (int i = 1; i < steps.Length; i++)
                if (steps[i - 1] != null && steps[i] != null)
                    Gizmos.DrawLine(steps[i - 1].transform.position + Vector3.up * 0.5f, steps[i].transform.position + Vector3.up * 0.5f);
        }
    }
}
