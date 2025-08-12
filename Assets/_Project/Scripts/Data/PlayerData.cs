// File: _Project/Scripts/Data/PlayerData.cs
using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public PlayerSettings settings;
    public int highestLevelUnlocked;

    // Dictionary không được serialize mặc định, chúng ta sẽ xử lý sau
    // public Dictionary<int, int> levelStars;

    // Constructor cho người chơi mới
    public PlayerData()
    {
        settings = new PlayerSettings();
        highestLevelUnlocked = 1;
        // levelStars = new Dictionary<int, int>();
    }
}

[System.Serializable]
public class PlayerSettings
{
    public float bgmVolume = 0.8f;
    public float sfxVolume = 1.0f;
}