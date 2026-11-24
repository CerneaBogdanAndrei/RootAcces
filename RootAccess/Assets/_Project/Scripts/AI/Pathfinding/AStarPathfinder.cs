using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>
    /// A* over an <see cref="AStarGrid"/>. 8-connected by default with no corner cutting (a diagonal step needs both
    /// orthogonal neighbours free). Buffers are reused between searches; generation stamps avoid clearing arrays.
    /// </summary>
    public sealed class AStarPathfinder
    {
        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.41421356f;

        private static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        private readonly AStarGrid _grid;
        private readonly float[] _gCost;
        private readonly int[] _parent;
        private readonly int[] _openStamp;
        private readonly int[] _closedStamp;
        private readonly MinHeap _open;
        private int _stamp;

        public AStarPathfinder(AStarGrid grid)
        {
            _grid = grid;
            int count = grid.CellCount;
            _gCost = new float[count];
            _parent = new int[count];
            _openStamp = new int[count];
            _closedStamp = new int[count];
            _open = new MinHeap(Mathf.Max(16, count / 4));
        }

        public bool AllowDiagonal { get; set; } = true;
        public int LastExpandedNodes { get; private set; }

        /// <summary>Fills <paramref name="result"/> with cells from start to goal (inclusive). Returns false when unreachable.</summary>
        public bool FindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> result)
        {
            result.Clear();
            LastExpandedNodes = 0;
            if (!_grid.IsWalkable(start) || !_grid.IsWalkable(goal)) return false;
            if (start == goal)
            {
                result.Add(start);
                return true;
            }

            NextStamp();
            _open.Clear();
            int startIndex = _grid.ToIndex(start.x, start.y);
            int goalIndex = _grid.ToIndex(goal.x, goal.y);
            _gCost[startIndex] = 0f;
            _parent[startIndex] = -1;
            _openStamp[startIndex] = _stamp;
            _open.Push(startIndex, Heuristic(start, goal));

            int directionCount = AllowDiagonal ? 8 : 4;
            while (_open.Count > 0)
            {
                int current = _open.Pop();
                if (_closedStamp[current] == _stamp) continue;
                _closedStamp[current] = _stamp;
                LastExpandedNodes++;
                if (current == goalIndex)
                {
                    Reconstruct(goalIndex, result);
                    return true;
                }

                Vector2Int cell = _grid.FromIndex(current);
                for (int d = 0; d < directionCount; d++)
                {
                    Vector2Int dir = Directions[d];
                    int nx = cell.x + dir.x;
                    int ny = cell.y + dir.y;
                    if (!_grid.IsWalkable(nx, ny)) continue;

                    bool diagonal = dir.x != 0 && dir.y != 0;
                    if (diagonal && (!_grid.IsWalkable(cell.x + dir.x, cell.y) || !_grid.IsWalkable(cell.x, cell.y + dir.y))) continue;

                    int neighbour = _grid.ToIndex(nx, ny);
                    if (_closedStamp[neighbour] == _stamp) continue;

                    float tentative = _gCost[current] + (diagonal ? DiagonalCost : StraightCost);
                    if (_openStamp[neighbour] == _stamp && tentative >= _gCost[neighbour]) continue;

                    _openStamp[neighbour] = _stamp;
                    _gCost[neighbour] = tentative;
                    _parent[neighbour] = current;
                    _open.Push(neighbour, tentative + Heuristic(new Vector2Int(nx, ny), goal));
                }
            }

            return false;
        }

        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            if (!AllowDiagonal) return (dx + dy) * StraightCost;
            return StraightCost * (dx + dy) + (DiagonalCost - 2f * StraightCost) * Mathf.Min(dx, dy);
        }

        private void Reconstruct(int goalIndex, List<Vector2Int> result)
        {
            for (int index = goalIndex; index != -1; index = _parent[index]) result.Add(_grid.FromIndex(index));
            result.Reverse();
        }

        private void NextStamp()
        {
            _stamp++;
            if (_stamp != int.MaxValue) return;
            System.Array.Clear(_openStamp, 0, _openStamp.Length);
            System.Array.Clear(_closedStamp, 0, _closedStamp.Length);
            _stamp = 1;
        }

        /// <summary>Binary min-heap keyed by f-cost; duplicates are allowed and skipped via the closed set (lazy deletion).</summary>
        private sealed class MinHeap
        {
            private int[] _items;
            private float[] _keys;

            public MinHeap(int capacity)
            {
                _items = new int[capacity];
                _keys = new float[capacity];
            }

            public int Count { get; private set; }

            public void Clear() => Count = 0;

            public void Push(int item, float key)
            {
                if (Count == _items.Length)
                {
                    System.Array.Resize(ref _items, _items.Length * 2);
                    System.Array.Resize(ref _keys, _keys.Length * 2);
                }

                int i = Count++;
                _items[i] = item;
                _keys[i] = key;
                while (i > 0)
                {
                    int parent = (i - 1) >> 1;
                    if (_keys[parent] <= _keys[i]) break;
                    Swap(i, parent);
                    i = parent;
                }
            }

            public int Pop()
            {
                int top = _items[0];
                Count--;
                if (Count > 0)
                {
                    _items[0] = _items[Count];
                    _keys[0] = _keys[Count];
                    int i = 0;
                    while (true)
                    {
                        int left = i * 2 + 1;
                        if (left >= Count) break;
                        int right = left + 1;
                        int smallest = right < Count && _keys[right] < _keys[left] ? right : left;
                        if (_keys[i] <= _keys[smallest]) break;
                        Swap(i, smallest);
                        i = smallest;
                    }
                }

                return top;
            }

            private void Swap(int a, int b)
            {
                (_items[a], _items[b]) = (_items[b], _items[a]);
                (_keys[a], _keys[b]) = (_keys[b], _keys[a]);
            }
        }
    }
}
