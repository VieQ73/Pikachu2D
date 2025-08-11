// File: _Project/Scripts/Managers/BoardManager.cs
using UnityEngine;
using UnityEngine.U2D;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections.Generic;

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

    public void GenerateBoard(LevelData levelData)
    {
        _currentLevelData = levelData;
        StartCoroutine(GenerateBoardRoutine());
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

        RemoveTile(t1);
        RemoveTile(t2);
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


    private void RemoveTile(Tile tile)
    {
        if (tile == null) return;
        Vector2Int pos = tile.GridPosition;
        if (pos.x < 0 || pos.x >= _currentLevelData.Width || pos.y < 0 || pos.y >= _currentLevelData.Height) return;
        if (_grid[pos.x, pos.y] == null) return;

        tile.ShowAsEmpty();
        _grid[pos.x, pos.y] = null;
    }

    private int ParseTileType(string code)
    {
        if (code == "ST" || code == "00") return 0;
        if (code == "BB") return -1;
        if (int.TryParse(code, out int type)) return type;
        return 0;
    }
}