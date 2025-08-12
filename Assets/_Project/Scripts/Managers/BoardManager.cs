// File: _Project/Scripts/Managers/BoardManager.cs
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private RectTransform boardContainer;

    [Header("Assets")]
    [SerializeField] private SpriteAtlas tileAtlas;

    public UILineDrawer uiLineDrawer;
    public Tile[,] _grid { get; private set; }
    private LevelData _currentLevelData;
    private int _minRow;
    private int _maxRow;

    public int GenerateBoard(LevelData levelData)
    {
        _currentLevelData = levelData;
        int pairCount = 0; // Biến đếm

        // Tạo một Dictionary để đếm số lượng mỗi loại tile
        Dictionary<int, int> tileTypeCounts = new Dictionary<int, int>();

        // Lặp qua layout để đếm trước
        for (int y = 0; y < _currentLevelData.Height; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                string tileCode = _currentLevelData.Layout[y, x];
                int tileType = ParseTileType(tileCode);
                if (tileType > 0) // Chỉ đếm tile thường
                {
                    if (tileTypeCounts.ContainsKey(tileType))
                        tileTypeCounts[tileType]++;
                    else
                        tileTypeCounts[tileType] = 1;
                }
            }
        }

        // Tính tổng số cặp (mỗi 2 tile cùng loại là 1 cặp)
        foreach (var count in tileTypeCounts.Values)
        {
            pairCount += count / 2;
        }

        Debug.Log($"[BoardManager] Màn chơi có {pairCount} cặp tile.");

        // Bắt đầu coroutine để sinh bàn cờ vật lý
        StartCoroutine(GenerateBoardRoutine());

        return pairCount; // Trả về số cặp
    }

    public int GetRemainingTileCount()
    {
        int count = 0;
        foreach (var tile in _grid)
        {
            if (tile != null && tile.TileType != 0) count++;
        }
        return count;
    }

    private IEnumerator GenerateBoardRoutine()
    {
        // Xoá tile cũ
        foreach (Transform child in boardContainer)
        {
            if (child.GetComponent<Tile>() != null) Destroy(child.gameObject);
        }

        // Tìm hàng có tile
        _minRow = -1;
        _maxRow = -1;
        for (int y = 0; y < _currentLevelData.Height; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                if (_currentLevelData.Layout[y, x] != "00")
                {
                    if (_minRow == -1) _minRow = y;
                    _maxRow = y;
                    break;
                }
            }
        }
        if (_minRow == -1) yield break;
        int visibleRows = _maxRow - _minRow + 1;

        yield return new WaitForEndOfFrame();

        // === Setup GridLayoutGroup ===
        GridLayoutGroup gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = _currentLevelData.Width;

        float availWidth = boardContainer.rect.width - gridLayout.padding.left - gridLayout.padding.right;
        float availHeight = boardContainer.rect.height - gridLayout.padding.top - gridLayout.padding.bottom;

        float cellWidthFromWidth = availWidth / _currentLevelData.Width;
        float cellHeightFromHeight = availHeight / visibleRows;

        float targetAspectRatio = 40f / 50f;

        float cellWidth = Mathf.Min(cellWidthFromWidth, cellHeightFromHeight * targetAspectRatio);
        float cellHeight = cellWidth / targetAspectRatio;

        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

        float usedHeight = cellHeight * visibleRows;
        int extraVerticalSpace = Mathf.RoundToInt(availHeight - usedHeight);
        gridLayout.padding.top = extraVerticalSpace / 2;
        gridLayout.padding.bottom = extraVerticalSpace - gridLayout.padding.top;

        float usedWidth = cellWidth * _currentLevelData.Width;
        int extraHorizontalSpace = Mathf.RoundToInt(availWidth - usedWidth);
        gridLayout.padding.left = extraHorizontalSpace / 2;
        gridLayout.padding.right = extraHorizontalSpace - gridLayout.padding.left;


        // === Sinh tile ===
        _grid = new Tile[_currentLevelData.Width, _currentLevelData.Height];
        for (int y = _minRow; y <= _maxRow; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                GameObject tileObject = Instantiate(tilePrefab, boardContainer);
                Tile tileComponent = tileObject.GetComponent<Tile>();
                string tileCode = _currentLevelData.Layout[y, x];
                int tileType = ParseTileType(tileCode);

                tileComponent.Initialize(tileType, new Vector2Int(x, y));

                if (tileType > 0)
                {
                    Sprite pokeSprite = tileAtlas.GetSprite(tileType.ToString());
                    if (pokeSprite != null)
                        tileComponent.SetSprite(pokeSprite);
                    else
                        Debug.LogError($"Không tìm thấy sprite tên '{tileType}' trong Tile_Atlas!");
                }

                tileComponent.AddClickListener(() => {
                    InputManager.Instance.SelectTile(tileComponent);
                });

                _grid[x, y] = tileComponent;
            }
        }
    }

    public async Task HandleMatch(Tile t1, Tile t2, List<Vector2Int> path)
    {
        if (path == null || path.Count < 2 || t1 == null || t2 == null) return;

        List<Vector2> localPath = new List<Vector2>();
        foreach (var gridPoint in path)
        {
            localPath.Add(GetLocalPositionForGridPoint(gridPoint));
        }

        AudioManager.Instance.PlayLinkedSound();

        bool done = false;
        uiLineDrawer.DrawLine(localPath, 0.5f, () => done = true);

        while (!done) await Task.Yield();

        Task t1RemoveTask = RemoveTile(t1);
        Task t2RemoveTask = RemoveTile(t2);

        await Task.WhenAll(t1RemoveTask, t2RemoveTask);
    }

    private Vector2 GetLocalPositionForGridPoint(Vector2Int gridPoint)
    {
        GridLayoutGroup gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
        Vector2 cellSize = gridLayout.cellSize;
        Vector2 spacing = gridLayout.spacing;

        // Kích thước đầy đủ của container
        float containerWidth = boardContainer.rect.width;
        float containerHeight = boardContainer.rect.height;

        // Pivot của container (thường là 0.5, 0.5)
        Vector2 pivot = boardContainer.pivot;

        // Tọa độ của góc trên bên trái của toàn bộ lưới (khu vực padding + cells)
        float startX = -containerWidth * pivot.x + gridLayout.padding.left;
        float startY = containerHeight * (1 - pivot.y) - gridLayout.padding.top;

        // Tọa độ của góc trên bên trái của Ô (cell) tương ứng
        // Tọa độ y trong lưới được hiển thị là y - _minRow
        float cellCornerX = startX + gridPoint.x * (cellSize.x + spacing.x);
        float cellCornerY = startY - (gridPoint.y - _minRow) * (cellSize.y + spacing.y);

        // Tọa độ TÂM của ô
        float centerX = cellCornerX + cellSize.x / 2f;
        float centerY = cellCornerY - cellSize.y ;

        return new Vector2(centerX, centerY);
    }


    private Task RemoveTile(Tile tile)
    {
        if (tile == null) return Task.CompletedTask;
        Vector2Int pos = tile.GridPosition;
        if (pos.x < 0 || pos.x >= _currentLevelData.Width || pos.y < 0 || pos.y >= _currentLevelData.Height) return Task.CompletedTask;
        if (_grid[pos.x, pos.y] == null) return Task.CompletedTask;

        // Đánh dấu ô này là trống trong lưới logic NGAY LẬP TỨC
        _grid[pos.x, pos.y] = null;

        // Trả về Task của hiệu ứng để GameManager có thể chờ
        return tile.ShowAsEmptyAndReturnTask();
    }

    private int ParseTileType(string code)
    {
        if (code == "ST" || code == "00") return 0;
        if (code == "BB") return -1;
        if (int.TryParse(code, out int type)) return type;
        return 0;
    }

    public bool AreMovesAvailable()
    {
        // Gọi hàm FindHint, nếu nó không trả về null thì vẫn còn nước đi
        var hint = Pathfinder.FindHint(_grid, _currentLevelData.Width, _currentLevelData.Height);
        return hint.Item1 != null;
    }

    public void ShuffleBoard()
    {
        AudioManager.Instance.PlayShuffleSound();

        // 1. Lấy ra danh sách các tile object và dữ liệu của chúng
        List<Tile> remainingTiles = new List<Tile>();
        for (int y = _minRow; y <= _maxRow; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                if (_grid[x, y] != null && _grid[x, y].TileType > 0)
                {
                    remainingTiles.Add(_grid[x, y]);
                }
            }
        }

        // Lấy ra chỉ dữ liệu (Sprite và Type) để xáo trộn
        var tileDataList = remainingTiles.Select(t => new { Sprite = t.GetSprite(), Type = t.TileType }).ToList();

        // Xáo trộn danh sách dữ liệu
        System.Random rng = new System.Random();
        var shuffledDataList = tileDataList.OrderBy(a => rng.Next()).ToList();

        // 2. Gán lại dữ liệu đã xáo trộn vào các tile object hiện có
        for (int i = 0; i < remainingTiles.Count; i++)
        {
            Tile tileObject = remainingTiles[i];
            var newTileData = shuffledDataList[i];

            // Hoán đổi dữ liệu mà không thay đổi vị trí vật lý hay parent
            tileObject.Initialize(newTileData.Type, tileObject.GridPosition);
            tileObject.SetSprite(newTileData.Sprite);

            tileObject.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.3f);
        }

        // Đảm bảo có nước đi sau khi xáo trộn
        if (!AreMovesAvailable())
        {
            Debug.Log("Xáo trộn ra thế bí, đang thử lại...");
            ShuffleBoard();
        }
    }
}