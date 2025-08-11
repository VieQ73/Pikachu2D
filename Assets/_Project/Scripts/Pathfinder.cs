using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class Pathfinder
{
    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public int Turns;
        public Vector2Int Direction;

        public Node(Vector2Int pos, Node parent, int turns, Vector2Int dir)
        {
            Position = pos;
            Parent = parent;
            Turns = turns;
            Direction = dir;
        }
    }

    public static List<Vector2Int> FindPath(Tile startTile, Tile endTile, Tile[,] grid, int width, int height)
    {
        if (startTile == null || endTile == null || grid == null)
        {
            Debug.LogWarning("[Pathfinder] StartTile, EndTile hoặc grid là null.");
            return null;
        }

        Debug.Log($"[Pathfinder] Start {startTile.GridPosition} -> {endTile.GridPosition}");

        int extendedWidth = width + 2;
        int extendedHeight = height + 2;

        Vector2Int startPos = startTile.GridPosition + Vector2Int.one;
        Vector2Int endPos = endTile.GridPosition + Vector2Int.one;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        int[,,] visited = new int[extendedWidth, extendedHeight, 4];
        for (int x = 0; x < extendedWidth; x++)
            for (int y = 0; y < extendedHeight; y++)
                for (int d = 0; d < 4; d++)
                    visited[x, y, d] = int.MaxValue;

        Queue<Node> queue = new Queue<Node>();

        for (int d = 0; d < 4; d++)
        {
            visited[startPos.x, startPos.y, d] = 0;
            queue.Enqueue(new Node(startPos, null, 0, directions[d]));
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int curDirIndex = System.Array.FindIndex(directions, v => v == current.Direction);
            if (curDirIndex < 0) curDirIndex = 0;

            for (int nd = 0; nd < 4; nd++)
            {
                Vector2Int dir = directions[nd];
                int newTurns = current.Turns;

                if (current.Parent != null && nd != curDirIndex) newTurns++;
                if (newTurns > 2) continue;

                Vector2Int nextPos = current.Position + dir;

                if (nextPos.x < 0 || nextPos.x >= extendedWidth || nextPos.y < 0 || nextPos.y >= extendedHeight) continue;

                if (nextPos == endPos)
                {
                    var finalPath = ReconstructPath(current, endPos, startPos);
                    Debug.Log($"[Pathfinder] Found path with turns={newTurns}");
                    return finalPath;
                }

                Vector2Int gridPos = nextPos - Vector2Int.one;
                bool isWalkable = true;
                if (gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < height)
                {
                    var tile = grid[gridPos.x, gridPos.y];
                    isWalkable = (tile == null || tile.TileType == 0);
                }

                if (!isWalkable) continue;

                if (visited[nextPos.x, nextPos.y, nd] > newTurns)
                {
                    visited[nextPos.x, nextPos.y, nd] = newTurns;
                    Node neighbor = new Node(nextPos, current, newTurns, dir);
                    queue.Enqueue(neighbor);
                }
            }
        }

        Debug.LogWarning("[Pathfinder] No path found.");
        return null;
    }

    private static List<Vector2Int> ReconstructPath(Node endNodeParent, Vector2Int endPos, Vector2Int startPos)
    {
        List<Vector2Int> fullPath = new List<Vector2Int>();
        fullPath.Add(endPos);

        Node current = endNodeParent;
        while (current != null)
        {
            fullPath.Add(current.Position);
            current = current.Parent;
        }

        fullPath.Reverse(); // Extended coords: startPos, ..., endPos

        // Convert to grid coords and simplify to corners
        List<Vector2Int> gridPath = fullPath.Select(p => p - Vector2Int.one).ToList();
        List<Vector2Int> simplified = new List<Vector2Int>();
        if (gridPath.Count == 0) return simplified;

        simplified.Add(gridPath[0]);
        for (int i = 1; i < gridPath.Count - 1; i++)
        {
            Vector2Int dirPrev = gridPath[i] - gridPath[i - 1];
            Vector2Int dirNext = gridPath[i + 1] - gridPath[i];
            if (dirPrev != dirNext)
            {
                simplified.Add(gridPath[i]);
            }
        }
        simplified.Add(gridPath[gridPath.Count - 1]);

        return simplified;
    }
}