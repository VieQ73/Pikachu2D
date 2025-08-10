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
    [SerializeField] private GameObject lineDrawerPrefab;

    [Header("Assets")]
    [SerializeField] private SpriteAtlas tileAtlas;

    public UILineDrawer uiLineDrawer;
    public Tile[,] _grid { get; private set; }
    private LevelData _currentLevelData;
    private LineDrawer _lineDrawer;

    private void Awake()
    {
        if (lineDrawerPrefab != null && boardContainer != null)
        {
            GameObject lineDrawerObj = Instantiate(lineDrawerPrefab, boardContainer);
            _lineDrawer = lineDrawerObj.GetComponent<LineDrawer>();
        }
    }

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
        int minRow = -1, maxRow = -1;
        for (int y = 0; y < _currentLevelData.Height; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                if (_currentLevelData.Layout[y, x] != "00")
                {
                    if (minRow == -1) minRow = y;
                    maxRow = y;
                    break;
                }
            }
        }
        if (minRow == -1) yield break;
        int visibleRows = maxRow - minRow + 1;

        yield return new WaitForEndOfFrame();

        // === Setup GridLayoutGroup ===
        GridLayoutGroup gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = _currentLevelData.Width;

        // Kích thước khả dụng (đã trừ padding hiện có)
        float availWidth = boardContainer.rect.width - gridLayout.padding.left - gridLayout.padding.right;
        float availHeight = boardContainer.rect.height - gridLayout.padding.top - gridLayout.padding.bottom;

        // Kích thước cell từ 2 hướng
        float cellWidthFromWidth = availWidth / _currentLevelData.Width;
        float cellHeightFromHeight = availHeight / visibleRows;

        // Tỉ lệ ô (40x50)
        float targetAspectRatio = 40f / 50f;

        // Chọn kích thước nhỏ nhất và giữ tỉ lệ
        float cellWidth = Mathf.Min(cellWidthFromWidth, cellHeightFromHeight * targetAspectRatio);
        float cellHeight = cellWidth / targetAspectRatio;

        // Gán cellSize
        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

        // Căn giữa theo chiều dọc
        float usedHeight = cellHeight * visibleRows;
        int extraVerticalSpace = Mathf.RoundToInt(availHeight - usedHeight);
        gridLayout.padding.top = extraVerticalSpace / 2;
        gridLayout.padding.bottom = extraVerticalSpace - gridLayout.padding.top;

        // === Sinh tile ===
        _grid = new Tile[_currentLevelData.Width, _currentLevelData.Height];
        for (int y = minRow; y <= maxRow; y++)
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
        List<Vector3> worldPath = new List<Vector3> { t1.transform.position };
        for (int i = 1; i < path.Count - 1; i++)
        {
            Tile pathTile = _grid[path[i].x, path[i].y];
            if (pathTile != null) worldPath.Add(pathTile.transform.position);
        }
        worldPath.Add(t2.transform.position);

        AudioManager.Instance.PlayLinkedSound();

        bool done = false;
        uiLineDrawer.DrawLine(worldPath, boardContainer, 0.3f, () => done = true);

        while (!done) await Task.Yield();

        RemoveTile(t1);
        RemoveTile(t2);
    }

    private void RemoveTile(Tile tile)
    {
        if (tile == null) return;
        Vector2Int pos = tile.GridPosition;
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
