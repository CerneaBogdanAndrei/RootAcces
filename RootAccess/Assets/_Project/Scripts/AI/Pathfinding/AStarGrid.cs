using System;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>Walkability grid in world space (cell centers). Pure data, built by <see cref="PathfindingGrid"/> or tests.</summary>
    public sealed class AStarGrid
    {
        private readonly bool[] _walkable;

        public AStarGrid(int width, int height, Vector2 origin, float cellSize)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            Width = width;
            Height = height;
            Origin = origin;
            CellSize = cellSize;
            _walkable = new bool[width * height];
            for (int i = 0; i < _walkable.Length; i++) _walkable[i] = true;
        }

        public int Width { get; }
        public int Height { get; }
        public Vector2 Origin { get; }
        public float CellSize { get; }
        public int CellCount => _walkable.Length;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(Vector2Int cell) => InBounds(cell.x, cell.y);

        public int ToIndex(int x, int y) => y * Width + x;
        public Vector2Int FromIndex(int index) => new(index % Width, index / Width);

        public bool IsWalkable(int x, int y) => InBounds(x, y) && _walkable[ToIndex(x, y)];
        public bool IsWalkable(Vector2Int cell) => IsWalkable(cell.x, cell.y);

        public void SetWalkable(int x, int y, bool walkable)
        {
            if (InBounds(x, y)) _walkable[ToIndex(x, y)] = walkable;
        }

        public Vector2Int WorldToCell(Vector2 world)
        {
            Vector2 local = (world - Origin) / CellSize;
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }

        public Vector2 CellToWorld(Vector2Int cell) => Origin + new Vector2((cell.x + 0.5f) * CellSize, (cell.y + 0.5f) * CellSize);

        /// <summary>Spiral search for the closest walkable cell (used when a target stands inside geometry).</summary>
        public bool TryFindNearestWalkable(Vector2Int cell, int maxRadius, out Vector2Int result)
        {
            if (IsWalkable(cell))
            {
                result = cell;
                return true;
            }

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        var candidate = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (!IsWalkable(candidate)) continue;
                        result = candidate;
                        return true;
                    }
                }
            }

            result = cell;
            return false;
        }
    }
}
