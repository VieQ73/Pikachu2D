using UnityEngine;

public class LevelData
{
    public int Width;
    public int Height;
    public int[,] Layout; // Mảng 2 chiều chứa loại tile (0-35 là tile, ST, BB, etc.)

    public LevelData(int width, int height, int[,] layout)
    {
        Width = width;
        Height = height;
        Layout = layout;
    }
}