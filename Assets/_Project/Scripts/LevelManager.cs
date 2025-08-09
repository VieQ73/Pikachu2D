// File: _Project/Scripts/Managers/LevelManager.cs
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
        Debug.Log($"[LevelManager] Bắt đầu tải level từ: {path}");

        if (!File.Exists(path))
        {
            Debug.LogError($"[LevelManager] LỖI: Không tìm thấy file level tại: {path}");
            return null;
        }

        List<string> lines = File.ReadAllLines(path).ToList();
        LevelData levelData = new LevelData();

        // 1. Đọc tất cả metadata trước
        foreach (var line in lines)
        {
            if (line.Contains("COLUMNS:")) levelData.Width = int.Parse(line.Split(':')[1].Trim());
            if (line.Contains("ROWS:")) levelData.Height = int.Parse(line.Split(':')[1].Trim());
            if (line.Contains("TIME:")) levelData.Time = int.Parse(line.Split(':')[1].Trim());
            if (line.Contains("GRAVITY:"))
            {
                if (System.Enum.TryParse(line.Split(':')[1].Trim().ToUpper(), out GravityType gravity))
                {
                    levelData.Gravity = gravity;
                }
            }
        }

        Debug.Log($"[LevelManager] Đã đọc metadata: Width={levelData.Width}, Height={levelData.Height}");

        // 2. Kiểm tra xem metadata có hợp lệ không
        if (levelData.Width <= 0 || levelData.Height <= 0)
        {
            Debug.LogError("[LevelManager] LỖI: Width hoặc Height không hợp lệ. Kiểm tra file .txt.");
            return null;
        }

        // 3. Tìm vị trí bắt đầu của lưới
        int gridStartIndex = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("Grid Layout"))
            {
                gridStartIndex = i + 1; // Dữ liệu bắt đầu từ dòng ngay sau "Grid Layout"
                break;
            }
        }

        if (gridStartIndex == -1)
        {
            Debug.LogError("[LevelManager] LỖI: Không tìm thấy dòng 'Grid Layout' trong file .txt.");
            return null;
        }

        Debug.Log($"[LevelManager] Tìm thấy Grid Layout tại dòng {gridStartIndex}. Bắt đầu đọc lưới.");

        // 4. Khởi tạo và đọc dữ liệu lưới
        levelData.Layout = new string[levelData.Height, levelData.Width];
        int currentRow = 0;
        for (int i = gridStartIndex; i < lines.Count && currentRow < levelData.Height; i++)
        {
            string line = lines[i];
            // Bỏ qua các dòng trống hoặc comment trong phần dữ liệu lưới
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

            string[] tiles = Regex.Split(line.Trim(), @"\s+");
            if (tiles.Length == levelData.Width)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    levelData.Layout[currentRow, x] = tiles[x];
                }
                currentRow++;
            }
        }

        if (currentRow != levelData.Height)
        {
            Debug.LogWarning($"[LevelManager] Cảnh báo: Số hàng đã đọc ({currentRow}) không khớp với Height đã khai báo ({levelData.Height}).");
        }

        Debug.Log("[LevelManager] Tải level thành công.");
        return levelData;
    }
}