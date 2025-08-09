using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Sự kiện được phát đi mỗi khi một tile được click
    public static event Action<Tile> OnTileSelected;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Phương thức này sẽ được gọi bởi Button của Tile
    public void SelectTile(Tile tile)
    {
        // Phát sự kiện đi cho bất kỳ ai đang lắng nghe (ở đây là GameManager)
        OnTileSelected?.Invoke(tile);
        Debug.Log($"InputManager: Đã chọn tiled tại {tile.GridPosition}");
    }
}