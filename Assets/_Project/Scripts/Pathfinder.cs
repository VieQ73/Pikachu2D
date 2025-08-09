using UnityEngine;
using System.Collections.Generic;

public static class Pathfinder
{
    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public int Turns;
        public Vector2Int Direction; // Hướng đi từ Parent tới Node này

        public Node(Vector2Int pos, Node parent, int turns, Vector2Int dir)
        {
            Position = pos;
            Parent = parent;
            Turns = turns;
            Direction = dir;
        }
    }

    // Tìm đường đi giữa 2 tile
    public static List<Vector2Int> FindPath(Tile startTile, Tile endTile, Tile[,] grid, int width, int height)
    {
        // Tạo một lưới logic lớn hơn bàn cờ để có thể đi vòng ngoài
        int extendedWidth = width + 2;
        int extendedHeight = height + 2;
        bool[,] walkableGrid = new bool[extendedWidth, extendedHeight];

        for (int y = 0; y < extendedHeight; y++)
        {
            for (int x = 0; x < extendedWidth; x++)
            {
                // Vị trí trên bàn cờ gốc
                Vector2Int gridPos = new Vector2Int(x - 1, y - 1);

                // Các ô rìa ngoài và ô trống trên bàn cờ là đi được
                if (x == 0 || x == extendedWidth - 1 || y == 0 || y == extendedHeight - 1 || grid[gridPos.x, gridPos.y] == null)
                {
                    walkableGrid[x, y] = true;
                }
            }
        }

        Vector2Int startPos = startTile.GridPosition + Vector2Int.one;
        Vector2Int endPos = endTile.GridPosition + Vector2Int.one;

        // Ô bắt đầu và kết thúc cũng phải đi được
        walkableGrid[startPos.x, startPos.y] = true;
        walkableGrid[endPos.x, endPos.y] = true;

        Queue<Node> queue = new Queue<Node>();
        Dictionary<Vector2Int, Node> visited = new Dictionary<Vector2Int, Node>();

        Node startNode = new Node(startPos, null, -1, Vector2Int.zero);
        queue.Enqueue(startNode);
        visited[startPos] = startNode;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Node currentNode = queue.Dequeue();

            if (currentNode.Position == endPos)
            {
                // Tìm thấy đường, tái tạo lại đường đi
                List<Vector2Int> path = new List<Vector2Int>();
                Node pathNode = currentNode;
                while (pathNode != null)
                {
                    path.Add(pathNode.Position - Vector2Int.one); // Chuyển về tọa độ bàn cờ gốc
                    pathNode = pathNode.Parent;
                }
                path.Reverse();
                return path;
            }

            foreach (var dir in directions)
            {
                Vector2Int nextPos = currentNode.Position + dir;
                int newTurns = currentNode.Turns + (dir == currentNode.Direction ? 0 : 1);

                if (newTurns > 2) continue; // Vượt quá 2 lần rẽ

                // Kiểm tra biên
                if (nextPos.x < 0 || nextPos.x >= extendedWidth || nextPos.y < 0 || nextPos.y >= extendedHeight) continue;

                // Chỉ đi vào ô đi được
                if (!walkableGrid[nextPos.x, nextPos.y] && nextPos != endPos) continue;

                // Kiểm tra đã ghé thăm và có đường đi tốt hơn không
                if (visited.ContainsKey(nextPos) && visited[nextPos].Turns <= newTurns) continue;

                Node neighborNode = new Node(nextPos, currentNode, newTurns, dir);
                visited[nextPos] = neighborNode;
                queue.Enqueue(neighborNode);
            }
        }

        return null; // Không tìm thấy đường
    }
}