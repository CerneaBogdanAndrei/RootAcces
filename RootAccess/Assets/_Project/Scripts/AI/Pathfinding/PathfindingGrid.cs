using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>
    /// Scene component that rasterizes level colliders into an <see cref="AStarGrid"/> for flying agents. Built in Start
    /// so every tilemap/composite collider is already enabled.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PathfindingGrid : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(60f, 24f);
        [SerializeField, Min(0.1f)] private float cellSize = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float probeScale = 0.9f;
        [SerializeField] private bool drawGizmos = true;

        public AStarGrid Grid { get; private set; }
        public AStarPathfinder Pathfinder { get; private set; }

        private void Awake() => ServiceLocator.Register(this);

        private void Start() => Build();

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Build()
        {
            int width = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
            int height = Mathf.Max(1, Mathf.CeilToInt(size.y / cellSize));
            Vector2 origin = (Vector2)transform.position - size * 0.5f;
            Grid = new AStarGrid(width, height, origin, cellSize);
            Vector2 probe = Vector2.one * cellSize * probeScale;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 center = Grid.CellToWorld(new Vector2Int(x, y));
                bool blocked = Physics2D.OverlapBox(center, probe, 0f, Layers.ObstacleMask) != null;
                Grid.SetWalkable(x, y, !blocked);
            }

            Pathfinder = new AStarPathfinder(Grid);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
            Gizmos.DrawWireCube(transform.position, size);
            if (Grid == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.25f);
            for (int y = 0; y < Grid.Height; y++)
            for (int x = 0; x < Grid.Width; x++)
                if (!Grid.IsWalkable(x, y)) Gizmos.DrawCube(Grid.CellToWorld(new Vector2Int(x, y)), Vector3.one * Grid.CellSize * 0.9f);
        }
    }
}
