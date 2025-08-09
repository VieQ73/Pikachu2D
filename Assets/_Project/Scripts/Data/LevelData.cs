using UnityEngine;

public class LevelData
{
    public int Width;
    public int Height;
    public int Time;
    public int TileTypes;
    public GravityType Gravity;
    public string[,] Layout; // Dùng string để đọc ký hiệu "ST", "BB", "FZ"

    public LevelData() { }
}

public enum GravityType
{
    NONE,
    DOWN
}