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
        public Vector2Int Direction; // Hướng đi TỪ parent đến node này

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
        Debug.Log($"[Pathfinder] Start {startTile.GridPosition} -> {endTile.GridPosition}");

        int extendedWidth = width + 2;
        int extendedHeight = height + 2;

        // Sanity check: ensure grid dimensions match expected orientation
        if (grid.GetLength(0) != width || grid.GetLength(1) != height)
        {
            Debug.LogWarning($"[Pathfinder] Warning: grid dimensions ({grid.GetLength(0)},{grid.GetLength(1)}) != (width,height)=({width},{height}). Check x/y indexing!");
        }

        Vector2Int startPos = startTile.GridPosition + Vector2Int.one;
        Vector2Int endPos = endTile.GridPosition + Vector2Int.one;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // visited[x,y,dirIndex] = minimal turns to reach (x,y) arriving with direction dirIndex
        int[,,] visited = new int[extendedWidth, extendedHeight, 4];
        for (int x = 0; x < extendedWidth; x++)
            for (int y = 0; y < extendedHeight; y++)
                for (int d = 0; d < 4; d++)
                    visited[x, y, d] = int.MaxValue;

        Queue<Node> queue = new Queue<Node>();

        // Seed: put start position with each possible initial direction (turns = 0)
        for (int d = 0; d < 4; d++)
        {
            visited[startPos.x, startPos.y, d] = 0;
            queue.Enqueue(new Node(startPos, null, 0, directions[d]));
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            // get index of current.Direction
            int curDirIndex = System.Array.FindIndex(directions, v => v == current.Direction);
            if (curDirIndex < 0) curDirIndex = 0;

            for (int nd = 0; nd < 4; nd++)
            {
                Vector2Int dir = directions[nd];
                int newTurns = current.Turns;

                // count a turn if we change direction AND current has a parent (so first move is free)
                if (current.Parent != null && nd != curDirIndex) newTurns++;

                if (newTurns > 2) continue; // keep limit = 2 turns (adjust if needed)

                Vector2Int nextPos = current.Position + dir;

                // bounds
                if (nextPos.x < 0 || nextPos.x >= extendedWidth || nextPos.y < 0 || nextPos.y >= extendedHeight) continue;

                // if next is destination -> reconstruct using current as parent
                if (nextPos == endPos)
                {
                    var finalPath = ReconstructPath(current, endPos, startPos);
                    Debug.Log($"[Pathfinder] Found path with turns={newTurns}");
                    return finalPath;
                }

                // check passable: convert to original grid coords
                Vector2Int gridPos = nextPos - Vector2Int.one;
                bool isWalkable = true;
                // only check walkable if nextPos is inside visible area
                if (gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < height)
                {
                    var tile = grid[gridPos.x, gridPos.y];
                    isWalkable = (tile == null || tile.TileType == 0);
                }
                // else if outside visible area (border) -> treat as passable (that's the point of border)

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
        List<Vector2Int> path = new List<Vector2Int>();
        path.Add(endPos - Vector2Int.one); // Điểm cuối

        Node currentNode = endNodeParent;
        while (currentNode != null)
        {
            // Chỉ thêm các điểm rẽ (góc) và điểm bắt đầu vào path
            if (currentNode.Parent == null || currentNode.Direction != currentNode.Parent.Direction)
            {
                path.Add(currentNode.Position - Vector2Int.one);
            }
            currentNode = currentNode.Parent;
        }

        path.Add(startPos - Vector2Int.one); // Điểm đầu

        path.Reverse(); // Đảo ngược để có thứ tự từ Start -> End
        return path;
    }
}