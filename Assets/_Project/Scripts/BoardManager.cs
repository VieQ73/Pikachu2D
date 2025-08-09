using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform boardPanel;
    [SerializeField] private GameObject lineDrawerPrefab;

    [Header("Assets")]
    [SerializeField] private SpriteAtlas tileAtlas;

    public Tile[,] _grid { get; private set; }
    private LineDrawer _lineDrawer;
    private LevelData _currentLevelData;

    private void Awake()
    {
        GameObject lineDrawerObj = Instantiate(lineDrawerPrefab, boardPanel);
        _lineDrawer = lineDrawerObj.GetComponent<LineDrawer>();
    }

    public void GenerateBoard(LevelData levelData)
    {
        _currentLevelData = levelData;

        foreach (Transform child in boardPanel)
        {
            if (child.GetComponent<Tile>() != null) Destroy(child.gameObject);
        }

        _grid = new Tile[_currentLevelData.Width, _currentLevelData.Height];

        for (int y = 0; y < _currentLevelData.Height; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                string tileCode = _currentLevelData.Layout[y, x];
                bool isFrozen = false;

                if (tileCode.EndsWith("FZ"))
                {
                    isFrozen = true;
                    tileCode = tileCode.Replace("FZ", "");
                }

                if (tileCode == "00" || tileCode == "ST")
                {
                    _grid[x, y] = null;
                    continue;
                }

                int tileType = ParseTileType(tileCode);

                GameObject tileObject = Instantiate(tilePrefab, boardPanel);
                Tile tileComponent = tileObject.GetComponent<Tile>();

                Sprite tileSprite = tileAtlas.GetSprite(tileType.ToString());
                if (tileSprite != null)
                {
                    tileComponent.GetTileImage().sprite = tileSprite;
                }
                else
                {
                    Debug.LogError($"Sprite cho loại {tileType} không tìm thấy trong atlas! Mã: {tileCode}");
                }

                var gridPosition = new Vector2Int(x, y);
                tileComponent.Initialize(tileType, gridPosition);
                tileComponent.SetFrozen(isFrozen);

                // SỬA LỖI: Gọi phương thức public thay vì truy cập trực tiếp
                tileComponent.AddClickListener(() =>
                {
                    InputManager.Instance.SelectTile(tileComponent);
                });

                _grid[x, y] = tileComponent;
            }
        }
    }

    private int ParseTileType(string code)
    {
        if (code == "BB") return -1;
        return int.Parse(code);
    }

    public async Task HandleMatch(Tile t1, Tile t2, List<Vector2Int> path)
    {
        List<Vector2> localPathPoints = new List<Vector2>();
        foreach (var gridPoint in path)
        {
            // Chuyển đổi từ tọa độ grid logic sang tọa độ local của tile tương ứng trên canvas
            if (IsValidPosition(gridPoint) && _grid[gridPoint.x, gridPoint.y] != null)
            {
                localPathPoints.Add(_grid[gridPoint.x, gridPoint.y].transform.localPosition);
            }
            else
            {
                // Xử lý các điểm đi vòng ngoài (ô trống)
                // logic này cần được làm phức tạp hơn, tạm thời chỉ nối các điểm có tile
            }
        }

        // Đảm bảo điểm bắt đầu và kết thúc luôn đúng
        localPathPoints.Insert(0, t1.transform.localPosition);
        localPathPoints.Add(t2.transform.localPosition);

        _lineDrawer.DrawLine(localPathPoints);

        await Task.Delay(200);

        if (t1.TileType == -1)
        {
            ExplodeBomb(t1);
            ExplodeBomb(t2);
        }
        else
        {
            RemoveTile(t1);
            RemoveTile(t2);
        }

        if (_currentLevelData.Gravity == GravityType.DOWN)
        {
            await ApplyGravity();
        }
    }

    private void ExplodeBomb(Tile bombTile)
    {
        Vector2Int bombPos = bombTile.GridPosition;
        RemoveTile(bombTile);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            Vector2Int targetPos = bombPos + dir;
            if (IsValidPosition(targetPos))
            {
                Tile targetTile = _grid[targetPos.x, targetPos.y];
                if (targetTile != null && targetTile.TileType > 0)
                {
                    RemoveTile(targetTile);
                }
            }
        }
    }

    private void RemoveTile(Tile tile)
    {
        if (tile == null) return;
        Vector2Int pos = tile.GridPosition;
        if (!IsValidPosition(pos) || _grid[pos.x, pos.y] == null) return;

        _grid[pos.x, pos.y] = null;

        tile.transform.DOScale(0, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(tile.gameObject);
        });

        CheckForThaw(pos);
    }

    private void CheckForThaw(Vector2Int removedPos)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            Vector2Int neighborPos = removedPos + dir;
            if (IsValidPosition(neighborPos))
            {
                Tile neighbor = _grid[neighborPos.x, neighborPos.y];
                if (neighbor != null && neighbor.IsFrozen)
                {
                    neighbor.Thaw();
                }
            }
        }
    }

    private async Task ApplyGravity()
    {
        List<Task> allTasks = new List<Task>();
        for (int x = 0; x < _currentLevelData.Width; x++)
        {
            for (int y = 0, emptyCount = 0; y < _currentLevelData.Height; y++)
            {
                if (_grid[x, y] == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    Tile tileToMove = _grid[x, y];
                    Vector2Int newPos = new Vector2Int(x, y - emptyCount);

                    _grid[x, y] = null;
                    _grid[newPos.x, newPos.y] = tileToMove;
                    tileToMove.SetGridPosition(newPos);

                    Tween moveTween = tileToMove.transform.DOLocalMove(tileToMove.transform.localPosition + Vector3.down * 125f * emptyCount, 0.3f).SetEase(Ease.OutQuad);
                    allTasks.Add(moveTween.AsyncWaitForCompletion());
                }
            }
        }

        if (allTasks.Count > 0)
        {
            await Task.WhenAll(allTasks);
        }
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < _currentLevelData.Width && pos.y >= 0 && pos.y < _currentLevelData.Height;
    }
}