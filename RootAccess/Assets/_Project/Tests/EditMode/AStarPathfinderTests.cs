using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.AI;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class AStarPathfinderTests
    {
        private static AStarGrid OpenGrid(int width = 10, int height = 10) => new(width, height, Vector2.zero, 1f);

        private static void AssertContiguous(List<Vector2Int> path, bool allowDiagonal)
        {
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int step = path[i] - path[i - 1];
                int dx = Mathf.Abs(step.x);
                int dy = Mathf.Abs(step.y);
                Assert.That(dx <= 1 && dy <= 1 && dx + dy > 0, $"Step {i} jumps from {path[i - 1]} to {path[i]}.");
                if (!allowDiagonal) Assert.That(dx + dy, Is.EqualTo(1), $"Step {i} is diagonal although diagonals are disabled.");
            }
        }

        [Test]
        public void StraightLine_ReturnsShortestPath()
        {
            var finder = new AStarPathfinder(OpenGrid());
            var path = new List<Vector2Int>();

            bool found = finder.FindPath(new Vector2Int(0, 0), new Vector2Int(6, 0), path);

            Assert.IsTrue(found);
            Assert.AreEqual(7, path.Count);
            Assert.AreEqual(new Vector2Int(0, 0), path[0]);
            Assert.AreEqual(new Vector2Int(6, 0), path[^1]);
            Assert.That(path.TrueForAll(c => c.y == 0), "A straight corridor should not leave the row.");
        }

        [Test]
        public void StartEqualsGoal_ReturnsSingleCell()
        {
            var finder = new AStarPathfinder(OpenGrid());
            var path = new List<Vector2Int>();

            Assert.IsTrue(finder.FindPath(new Vector2Int(3, 3), new Vector2Int(3, 3), path));
            CollectionAssert.AreEqual(new[] { new Vector2Int(3, 3) }, path);
        }

        [Test]
        public void Obstacle_PathDetoursAroundWall()
        {
            AStarGrid grid = OpenGrid();
            for (int y = 0; y < 8; y++) grid.SetWalkable(5, y, false);
            var finder = new AStarPathfinder(grid);
            var path = new List<Vector2Int>();

            bool found = finder.FindPath(new Vector2Int(2, 2), new Vector2Int(8, 2), path);

            Assert.IsTrue(found);
            AssertContiguous(path, allowDiagonal: true);
            Assert.That(path.TrueForAll(grid.IsWalkable), "Path must only use walkable cells.");
            Assert.That(path.Exists(c => c.x == 5 && c.y >= 8), "Path must go over the top of the wall.");
        }

        [Test]
        public void EnclosedGoal_ReturnsFalseAndEmptyPath()
        {
            AStarGrid grid = OpenGrid();
            for (int x = 6; x <= 8; x++)
            for (int y = 6; y <= 8; y++)
                if (x != 7 || y != 7) grid.SetWalkable(x, y, false);
            var finder = new AStarPathfinder(grid);
            var path = new List<Vector2Int> { new(1, 1) };

            bool found = finder.FindPath(new Vector2Int(0, 0), new Vector2Int(7, 7), path);

            Assert.IsFalse(found);
            Assert.IsEmpty(path);
        }

        [Test]
        public void BlockedStartOrGoal_ReturnsFalse()
        {
            AStarGrid grid = OpenGrid();
            grid.SetWalkable(9, 9, false);
            var finder = new AStarPathfinder(grid);
            var path = new List<Vector2Int>();

            Assert.IsFalse(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(9, 9), path));
            Assert.IsFalse(finder.FindPath(new Vector2Int(9, 9), new Vector2Int(0, 0), path));
            Assert.IsFalse(finder.FindPath(new Vector2Int(-1, 0), new Vector2Int(0, 0), path), "Out-of-bounds start is not walkable.");
        }

        [Test]
        public void Diagonal_UsedInOpenSpace()
        {
            var finder = new AStarPathfinder(OpenGrid());
            var path = new List<Vector2Int>();

            Assert.IsTrue(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5), path));
            Assert.AreEqual(6, path.Count, "A pure diagonal should take one step per cell.");
            AssertContiguous(path, allowDiagonal: true);
        }

        [Test]
        public void Diagonal_DoesNotCutCorners()
        {
            AStarGrid grid = OpenGrid(3, 3);
            grid.SetWalkable(1, 0, false);
            var finder = new AStarPathfinder(grid);
            var path = new List<Vector2Int>();

            Assert.IsTrue(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(1, 1), path));
            Assert.AreEqual(3, path.Count, "The diagonal (0,0)->(1,1) touches a blocked corner and must be split into two steps.");
            Assert.AreEqual(new Vector2Int(0, 1), path[1]);
        }

        [Test]
        public void Diagonal_SqueezeBetweenTwoBlockedCornersIsImpossible()
        {
            AStarGrid grid = OpenGrid(2, 2);
            grid.SetWalkable(1, 0, false);
            grid.SetWalkable(0, 1, false);
            var finder = new AStarPathfinder(grid);

            Assert.IsFalse(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(1, 1), new List<Vector2Int>()));
        }

        [Test]
        public void DiagonalsDisabled_UsesOnlyOrthogonalSteps()
        {
            var finder = new AStarPathfinder(OpenGrid()) { AllowDiagonal = false };
            var path = new List<Vector2Int>();

            Assert.IsTrue(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 3), path));
            Assert.AreEqual(8, path.Count, "Manhattan distance 7 means 8 cells.");
            AssertContiguous(path, allowDiagonal: false);
        }

        [Test]
        public void FinderIsReusable_BetweenSearches()
        {
            AStarGrid grid = OpenGrid();
            var finder = new AStarPathfinder(grid);
            var path = new List<Vector2Int>();

            Assert.IsTrue(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(9, 9), path));
            grid.SetWalkable(5, 5, false);
            Assert.IsTrue(finder.FindPath(new Vector2Int(0, 0), new Vector2Int(9, 9), path));
            Assert.That(path.TrueForAll(grid.IsWalkable));
        }

        [Test]
        public void Grid_WorldCellConversionRoundTrips()
        {
            var grid = new AStarGrid(20, 10, new Vector2(-5f, -2f), 0.5f);
            Vector2Int cell = grid.WorldToCell(new Vector2(-4.1f, -1.6f));

            Assert.AreEqual(new Vector2Int(1, 0), cell);
            Assert.AreEqual(cell, grid.WorldToCell(grid.CellToWorld(cell)));
        }

        [Test]
        public void Grid_NearestWalkableFindsClosestFreeCell()
        {
            AStarGrid grid = OpenGrid();
            grid.SetWalkable(4, 4, false);

            Assert.IsTrue(grid.TryFindNearestWalkable(new Vector2Int(4, 4), 2, out Vector2Int result));
            Assert.IsTrue(grid.IsWalkable(result));
            Assert.That(Mathf.Max(Mathf.Abs(result.x - 4), Mathf.Abs(result.y - 4)), Is.EqualTo(1));
        }
    }
}
