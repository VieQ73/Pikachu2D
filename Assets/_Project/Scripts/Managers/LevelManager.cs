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
            {
                levelData.Width = int.Parse(line.Split(':')[1].Trim());
            }
            else if (line.StartsWith("ROWS:", System.StringComparison.OrdinalIgnoreCase))
            {
                levelData.Height = int.Parse(line.Split(':')[1].Trim());
            }
            else if (line.StartsWith("TIME:", System.StringComparison.OrdinalIgnoreCase))
            {
                levelData.Time = int.Parse(line.Split(':')[1].Trim());
            }
            else if (line.StartsWith("TILE_TYPES:", System.StringComparison.OrdinalIgnoreCase))
            {
                levelData.TileTypes = int.Parse(line.Split(':')[1].Trim());
            }
        }

        if (levelData.Width <= 0 || levelData.Height <= 0)
        {
            Debug.LogError("[LevelManager] LỖI: Width hoặc Height không hợp lệ trong metadata.");
            return null;
        }

        // 2. Thu thập các dòng dữ liệu lưới
        var gridLines = new List<string>();
        bool isGridSection = false;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("//"))
            {
                if (line.Contains("Grid Layout")) isGridSection = true;
                continue;
            }
            if (line.StartsWith("COLUMNS:") || line.StartsWith("ROWS:") ||
                line.StartsWith("TIME:") || line.StartsWith("TILE_TYPES:")) continue;

            if (isGridSection)
            {
                var tokens = Regex.Split(line, @"\s+").Where(s => s.Length > 0).ToArray();
                if (tokens.Length > 0 && tokens.All(t =>
                    t == "ST" || t == "BB" || t == "00" ||
                    (t.EndsWith("FZ") && Regex.IsMatch(t.Replace("FZ", ""), @"^(ST|BB|00|\d+)$")) ||
                    Regex.IsMatch(t, @"^\d+$")))
                {
                    gridLines.Add(line);
                }
                else
                {
                    Debug.LogWarning($"[LevelManager] Bỏ qua dòng không hợp lệ trong phần grid: {line}");
                }
            }
        }

        // 3. Đọc lưới vào Layout
        levelData.Layout = new string[levelData.Height, levelData.Width];

        for (int row = 0; row < levelData.Height; row++)
        {
            if (row < gridLines.Count)
            {
                var tokens = Regex.Split(gridLines[row], @"\s+").Where(s => s.Length > 0).ToList();

                if (tokens.Count < levelData.Width)
                {
                    while (tokens.Count < levelData.Width) tokens.Add("00");
                }
                else if (tokens.Count > levelData.Width)
                {
                    tokens = tokens.Take(levelData.Width).ToList();
                }

                for (int col = 0; col < levelData.Width; col++)
                {
                    string token = tokens[col];
                    if (token.EndsWith("FZ"))
                    {
                        string baseCode = token.Replace("FZ", "");
                        if (baseCode == "ST" || baseCode == "BB" || baseCode == "00" ||
                            (int.TryParse(baseCode, out int type) && type >= 0 && type <= levelData.TileTypes))
                        {
                            levelData.Layout[row, col] = token;
                            continue;
                        }
                    }
                    else if (token == "ST" || token == "BB" || token == "00" ||
                             (int.TryParse(token, out int type2) && type2 >= 0 && type2 <= levelData.TileTypes))
                    {
                        levelData.Layout[row, col] = token;
                        continue;
                    }

                    Debug.LogWarning($"[LevelManager] Giá trị không hợp lệ tại [{row},{col}]: {token}. Gán mặc định '00'.");
                    levelData.Layout[row, col] = "00";
                }
            }
            else
            {
                for (int col = 0; col < levelData.Width; col++)
                    levelData.Layout[row, col] = "00";
            }
        }

        // 4. Debug preview
        for (int r = 0; r < levelData.Height; r++)
        {
            string rowStr = string.Join(" ", Enumerable.Range(0, levelData.Width).Select(c => levelData.Layout[r, c]));
            Debug.Log($"[LevelManager] layout row {r}: {rowStr}");
        }

        return levelData;
    }
}