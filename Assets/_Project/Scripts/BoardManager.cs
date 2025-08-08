using UnityEngine;
using UnityEngine.U2D; // Cần dùng để truy cập SpriteAtlas

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private GameObject tilePrefab; 
    [SerializeField] private Transform boardPanel;   

    [Header("Assets")]
    [SerializeField] private SpriteAtlas tileAtlas;   

    private Tile[,] _grid; // Lưới logic để lưu trữ các tile

    // Test Function - Sẽ được gọi từ GameManager sau này
    public void GenerateBoard(LevelData levelData)
    {
        // 1. Dọn dẹp bàn cờ cũ (nếu có)
        foreach (Transform child in boardPanel)
        {
            Destroy(child.gameObject);
        }

        // 2. Khởi tạo lưới logic
        _grid = new Tile[levelData.Width, levelData.Height];
        // Thiết lập GridLayoutGroup theo kích thước level

        // 3. Sinh các tile mới
        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                int tileType = levelData.Layout[y, x];

                // Bỏ qua các ô trống (ký hiệu 00 trong GDD)
                if (tileType == 0)
                {
                    _grid[x, y] = null;
                    continue;
                }

                // Tạo đối tượng tile từ prefab
                GameObject tileObject = Instantiate(tilePrefab, boardPanel);
                Tile tileComponent = tileObject.GetComponent<Tile>();

                // Lấy sprite từ Atlas và gán vào Image
                Sprite tileSprite = tileAtlas.GetSprite(tileType.ToString());

                if (tileSprite != null)
                {
                    tileComponent.tileImage.sprite = tileSprite;
                }
                else
                {
                    Debug.LogError($"Sprite for type {tileType} not found in atlas!");
                }

                // Khởi tạo thông tin cho tile
                var gridPosition = new Vector2Int(x, y);
                tileComponent.Initialize(tileType, gridPosition);

                tileComponent.tileButton.onClick.AddListener(() =>
                {
                    InputManager.Instance.SelectTile(tileComponent);
                });

                // Lưu tile vào lưới logic
                _grid[x, y] = tileComponent;
            }
        }
    }
}