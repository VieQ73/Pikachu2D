using UnityEngine;
using UnityEngine.UI;

// Yêu cầu phải có 2 component này khi gắn script Tile
[RequireComponent(typeof(Image), typeof(Button))]
public class Tile : MonoBehaviour
{
    public int TileType { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    // Tham chiếu đến các component của chính nó
    public Image tileImage;
    public Button tileButton;

    private void Awake()
    {
        // Tự động lấy component khi được tạo ra
        tileImage = GetComponent<Image>();
        tileButton = GetComponent<Button>();
    }

    public void Initialize(int type, Vector2Int position)
    {
        TileType = type;
        GridPosition = position;
        gameObject.name = $"Tile_{position.x}_{position.y} (Type: {type})";
    }
}