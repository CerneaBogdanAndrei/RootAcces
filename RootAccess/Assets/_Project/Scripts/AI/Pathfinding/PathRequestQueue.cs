using System;
using System.Collections.Generic;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>
    /// Spreads path searches across frames (budget per frame). Each requester has at most one pending request;
    /// newer requests replace older ones. Results are written into the caller's own buffer (no allocation).
    /// </summary>
    [RequireComponent(typeof(PathfindingGrid))]
    public sealed class PathRequestQueue : MonoBehaviour
    {
        private struct PathRequest
        {
            public int RequesterId;
            public Vector2 From;
            public Vector2 To;
            public List<Vector2> Buffer;
            public Action<bool> Callback;
        }

        [SerializeField, Min(1)] private int maxSearchesPerFrame = 2;
        [SerializeField, Min(0)] private int nearestWalkableRadius = 4;

        private readonly List<PathRequest> _pending = new(16);
        private readonly List<Vector2Int> _cells = new(256);
        private PathfindingGrid _grid;

        public int PendingCount => _pending.Count;

        private void Awake()
        {
            _grid = GetComponent<PathfindingGrid>();
            ServiceLocator.Register(this);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Request(int requesterId, Vector2 from, Vector2 to, List<Vector2> buffer, Action<bool> callback)
        {
            if (buffer == null)
            {
                Debug.LogError("[PathRequestQueue] A result buffer is required.");
                return;
            }

            var request = new PathRequest { RequesterId = requesterId, From = from, To = to, Buffer = buffer, Callback = callback };
            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].RequesterId != requesterId) continue;
                _pending[i] = request;
                return;
            }

            _pending.Add(request);
        }

        public void Cancel(int requesterId)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
                if (_pending[i].RequesterId == requesterId) _pending.RemoveAt(i);
        }

        private void Update()
        {
            int budget = Mathf.Min(maxSearchesPerFrame, _pending.Count);
            for (int i = 0; i < budget; i++)
            {
                PathRequest request = _pending[0];
                _pending.RemoveAt(0);
                bool found = Solve(request.From, request.To, request.Buffer);
                request.Callback?.Invoke(found);
            }
        }

        private bool Solve(Vector2 from, Vector2 to, List<Vector2> buffer)
        {
            buffer.Clear();
            AStarGrid grid = _grid.Grid;
            if (grid == null || _grid.Pathfinder == null) return false;
            if (!grid.TryFindNearestWalkable(grid.WorldToCell(from), nearestWalkableRadius, out Vector2Int start)) return false;
            if (!grid.TryFindNearestWalkable(grid.WorldToCell(to), nearestWalkableRadius, out Vector2Int goal)) return false;
            if (!_grid.Pathfinder.FindPath(start, goal, _cells)) return false;

            for (int i = 1; i < _cells.Count; i++) buffer.Add(grid.CellToWorld(_cells[i]));
            if (buffer.Count == 0) buffer.Add(grid.CellToWorld(goal));
            return true;
        }
    }
}
