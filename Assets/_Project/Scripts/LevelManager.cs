using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public LevelData LoadLevel(int levelNumber)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", $"level_{levelNumber}.txt");
        if (!File.Exists(path))
        {
            Debug.LogError($"[LevelManager] LỖI: Không tìm thấy file level tại: {path}");
            return null;
        }

        var lines = File.ReadAllLines(path)
                        .Select(l => l.Replace("\r", "").Trim())
                        .ToList();

        LevelData levelData = new LevelData();

        // 1. Đọc metadata
        foreach (var line in lines)
        {
            if (line.StartsWith("COLUMNS:", System.StringComparison.OrdinalIgnoreCase))
                levelData.Width = int.Parse(line.Split(':')[1].Trim());
            else if (line.StartsWith("ROWS:", System.StringComparison.OrdinalIgnoreCase))
                levelData.Height = int.Parse(line.Split(':')[1].Trim());
            else if (line.StartsWith("TIME:", System.StringComparison.OrdinalIgnoreCase))
                levelData.Time = int.Parse(line.Split(':')[1].Trim());
            else if (line.StartsWith("TILE_TYPES:", System.StringComparison.OrdinalIgnoreCase))
                levelData.TileTypes = int.Parse(line.Split(':')[1].Trim());
        }

        if (levelData.Width <= 0 || levelData.Height <= 0)
        {
            Debug.LogError("[LevelManager] LỖI: Width hoặc Height không hợp lệ trong metadata.");
            return null;
        }

        // 2. Thu thập các dòng dữ liệu lưới (bỏ qua comment, metadata, dòng rỗng)
        var gridLines = new List<string>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("//")) continue;
            if (line.Any(char.IsLetter)) continue; // bỏ dòng chứa chữ cái

            // chỉ giữ dòng chứa số
            var tokens = Regex.Split(line, @"\s+").Where(s => s.Length > 0).ToArray();
            if (tokens.Length > 0)
                gridLines.Add(line);
        }

        // 3. Đọc lưới vào Layout
        levelData.Layout = new string[levelData.Height, levelData.Width];

        for (int row = 0; row < levelData.Height; row++)
        {
            if (row < gridLines.Count)
            {
                var tokens = Regex.Split(gridLines[row], @"\s+").Where(s => s.Length > 0).ToList();

                // pad hoặc cắt
                if (tokens.Count < levelData.Width)
                {
                    while (tokens.Count < levelData.Width) tokens.Add("00");
                }
                else if (tokens.Count > levelData.Width)
                {
                    tokens = tokens.Take(levelData.Width).ToList();
                }

                for (int col = 0; col < levelData.Width; col++)
                    levelData.Layout[row, col] = tokens[col];
            }
            else
            {
                // nếu thiếu dòng → pad full "00"
                for (int col = 0; col < levelData.Width; col++)
                    levelData.Layout[row, col] = "00";
            }
        }

        // 4. Debug preview
        for (int r = 0; r < Mathf.Min(levelData.Height, 8); r++)
        {
            string rowStr = string.Join(" ", Enumerable.Range(0, levelData.Width).Select(c => levelData.Layout[r, c]));
            Debug.Log($"[LevelManager] layout row {r}: {rowStr}");
        }

        return levelData;
    }
}
