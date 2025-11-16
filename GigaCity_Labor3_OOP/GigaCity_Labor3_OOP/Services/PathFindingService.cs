using System;
using System.Collections.Generic;
using System.Linq;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.Services
{
    public class PathFindingService
    {
        private readonly HashSet<(int, int)> _roadCoordinates;

        public PathFindingService(HashSet<(int, int)> roadCoordinates)
        {
            _roadCoordinates = roadCoordinates;
        }

        public List<(int X, int Y)> FindPath(int startX, int startY, int endX, int endY)
        {
            var openSet = new List<PathNode>();
            var closedSet = new HashSet<PathNode>();
            var startNode = new PathNode(startX, startY, null, 0, GetHeuristic(startX, startY, endX, endY));

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var currentNode = openSet.OrderBy(node => node.F).First();
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode.X == endX && currentNode.Y == endY)
                    return ReconstructPath(currentNode);

                var neighbors = GetNeighbors(currentNode.X, currentNode.Y);

                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Any(n => n.X == neighbor.X && n.Y == neighbor.Y))
                        continue;

                    var g = currentNode.G + 1;
                    var h = GetHeuristic(neighbor.X, neighbor.Y, endX, endY);

                    var existingNode = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);

                    if (existingNode == null)
                    {
                        openSet.Add(new PathNode(neighbor.X, neighbor.Y, currentNode, g, h));
                    }
                    else if (g < existingNode.G)
                    {
                        existingNode.G = g;
                        existingNode.Parent = currentNode;
                    }
                }
            }

            return new List<(int, int)>();
        }

        private List<(int X, int Y)> GetNeighbors(int x, int y)
        {
            var neighbors = new List<(int X, int Y)>();
            var directions = new (int dx, int dy)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

            foreach (var (dx, dy) in directions)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < 100 && ny >= 0 && ny < 100)
                {
                    if (_roadCoordinates.Contains((nx, ny)))
                    {
                        neighbors.Add((nx, ny));
                    }
                }
            }

            return neighbors;
        }

        private List<(int X, int Y)> ReconstructPath(PathNode endNode)
        {
            var path = new List<(int X, int Y)>();
            var currentNode = endNode;
            while (currentNode != null)
            {
                path.Add((currentNode.X, currentNode.Y));
                currentNode = currentNode.Parent;
            }
            path.Reverse();
            return path;
        }

        private double GetDistance(int x1, int y1, int x2, int y2) => Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
        private double GetHeuristic(int x1, int y1, int x2, int y2) => GetDistance(x1, y1, x2, y2);

        private class PathNode
        {
            public int X { get; }
            public int Y { get; }
            public PathNode Parent { get; set; }
            public double G { get; set; }
            public double H { get; }
            public double F => G + H;

            public PathNode(int x, int y, PathNode parent, double g, double h)
            {
                X = x; Y = y; Parent = parent; G = g; H = h;
            }
        }
    }
}