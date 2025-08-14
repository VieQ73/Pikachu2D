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
        int pairCount = 0;

        Dictionary<int, int> tileTypeCounts = new Dictionary<int, int>();

        for (int y = 0; y < _currentLevelData.Height; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                string tileCode = _currentLevelData.Layout[y, x];
                int tileType = ParseTileType(tileCode);
                if (tileType > 0)
                {
                    if (tileTypeCounts.ContainsKey(tileType))
                        tileTypeCounts[tileType]++;
                    else
                        tileTypeCounts[tileType] = 1;
                }
            }
        }

        foreach (var count in tileTypeCounts.Values)
        {
            pairCount += count / 2;
        }

        Debug.Log($"[BoardManager] Màn chơi có {pairCount} cặp tile.");

        StartCoroutine(GenerateBoardRoutine());

        return pairCount;
    }

    public int GetRemainingTileCount()
    {
        int count = 0;
        if (_grid == null) return 0;
        foreach (var tile in _grid)
        {
            if (tile != null && tile.TileType > 0) count++; // Chỉ đếm tile thường, không đếm đá (-2) hoặc bom (-1) nếu không muốn
        }
        return count;
    }

    private IEnumerator GenerateBoardRoutine()
    {
        // Xóa cũ
        foreach (Transform child in boardContainer)
        {
            if (child.GetComponent<UILineDrawer>() == null)
            {
                Destroy(child.gameObject);
            }
        }

        // Tính _minRow, _maxRow
        _minRow = -1; _maxRow = -1;
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

        // Cấu hình GridLayoutGroup
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

        _grid = new Tile[_currentLevelData.Width, _currentLevelData.Height];
        for (int y = _minRow; y <= _maxRow; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                GameObject tileObject = Instantiate(tilePrefab, boardContainer);
                Tile tileComponent = tileObject.GetComponent<Tile>();
                string tileCode = _currentLevelData.Layout[y, x];
                bool isFrozen = false;
                if (tileCode.EndsWith("FZ"))
                {
                    isFrozen = true;
                    tileCode = tileCode.Replace("FZ", "");
                }

                int tileType = ParseTileType(tileCode);
                Sprite sprite = GetSpriteForType(tileType);

                // Gọi Initialize với đầy đủ thông tin
                tileComponent.Initialize(tileType, new Vector2Int(x, y), sprite);
                tileComponent.SetFrozen(isFrozen);

                // Gán sự kiện click và lưu vào _grid
                if (tileType > 0 || tileType == -1) // Pokemon hoặc Bom
                {
                    tileComponent.AddClickListener(() => { InputManager.Instance.SelectTile(tileComponent); });
                    _grid[x, y] = tileComponent;
                }
                else if (tileType == -2) // Đá
                {
                    // Đá vẫn cần được lưu vào _grid để Pathfinder nhận diện là vật cản
                    _grid[x, y] = tileComponent;
                }
                else // Ô trống
                {
                    _grid[x, y] = null;
                }
            }
        }
    }

    private Sprite GetSpriteForType(int tileType)
    {
        if (tileType > 0) return tileAtlas.GetSprite(tileType.ToString());
        if (tileType == -1) return tileAtlas.GetSprite("BB");
        if (tileType == -2) return tileAtlas.GetSprite("ST");
        return null;
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

        if (t1.TileType == -1) // Là Bom
        {
            await Task.WhenAll(ExplodeBomb(t1), ExplodeBomb(t2));
            BalanceBoardAfterExplosion();
        }
        else // Là tile thường
        {
            await Task.WhenAll(RemoveTile(t1), RemoveTile(t2));
        }
    }


    private async Task ExplodeBomb(Tile bombTile)
    {
        if (bombTile == null) return;

        bombTile.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f);
        await Task.Delay(150);

        Vector2Int bombPos = bombTile.GridPosition;

        // Dùng HashSet để tránh xử lý một tile nhiều lần (khi 2 quả bom nổ gần nhau)
        HashSet<Tile> tilesToRemove = new HashSet<Tile>();

        // Luôn xóa chính quả bom
        tilesToRemove.Add(bombTile);

        // Tác động lên 4 ô xung quanh
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            Vector2Int targetPos = bombPos + dir;
            if (IsValidPosition(targetPos))
            {
                Tile targetTile = _grid[targetPos.x, targetPos.y];
                if (targetTile == null) continue;

                // Trường hợp 1: Tile bị đóng băng -> Ưu tiên cao nhất
                if (targetTile.IsFrozen)
                {
                    // Chỉ làm tan băng. KHÔNG thêm vào danh sách xóa.
                    targetTile.Thaw();
                }
                // Trường hợp 2: Nếu không bị đóng băng, thêm vào danh sách để xóa
                else
                {
                    tilesToRemove.Add(targetTile);
                }
            }
        }

        List<Task> explosionTasks = tilesToRemove.Select(tile => RemoveTile(tile)).ToList();
        await Task.WhenAll(explosionTasks);
    }

    private async void BalanceBoardAfterExplosion()
    {
        // Thêm độ trễ nhỏ để người chơi thấy các ô trống sau vụ nổ
        await Task.Delay(300);

        // 1. Đếm và tìm tile lẻ
        Dictionary<int, int> typeCounts = new Dictionary<int, int>();
        for (int y = 0; y < _currentLevelData.Height; y++)
        {
            for (int x = 0; x < _currentLevelData.Width; x++)
            {
                // Duyệt qua lưới vật lý (GridLayout) thay vì lưới logic (_grid)
                if (x + y * _currentLevelData.Width < boardContainer.childCount)
                {
                    Tile tile = boardContainer.GetChild(x + y * _currentLevelData.Width).GetComponent<Tile>();
                    if (tile != null && tile.TileType > 0)
                    {
                        if (typeCounts.ContainsKey(tile.TileType)) typeCounts[tile.TileType]++;
                        else typeCounts[tile.TileType] = 1;
                    }
                }
            }
        }
        List<int> oddTypes = typeCounts.Where(p => p.Value % 2 != 0).Select(p => p.Key).ToList();

        if (oddTypes.Count == 0) return;
        Debug.Log($"[BoardManager] Phát hiện {oddTypes.Count} tile lẻ. Bắt đầu cân bằng.");

        // 2. Tìm các ô trống vật lý
        List<Tile> emptyTiles = new List<Tile>();
        foreach (Transform child in boardContainer)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null && tile.TileType == 0)
            {
                emptyTiles.Add(tile);
            }
        }

        // 3. Bổ sung tile mới vào ô trống một cách từ từ
        System.Random rng = new System.Random();
        emptyTiles = emptyTiles.OrderBy(t => rng.Next()).ToList();

        int emptyIndex = 0;
        foreach (int typeToCreate in oddTypes)
        {
            if (emptyIndex >= emptyTiles.Count) break;

            Tile targetEmptyTile = emptyTiles[emptyIndex];
            Sprite sprite = GetSpriteForType(typeToCreate);
            if (sprite != null)
            {
                // Gọi hàm "hồi sinh" có hiệu ứng
                targetEmptyTile.SpawnAsNewTile(typeToCreate, sprite);

                // Gán lại listener và cập nhật lưới logic
                targetEmptyTile.AddClickListener(() => { InputManager.Instance.SelectTile(targetEmptyTile); });
                _grid[targetEmptyTile.GridPosition.x, targetEmptyTile.GridPosition.y] = targetEmptyTile;

                emptyIndex++;
                await Task.Delay(150);
            }
        }
    }

    private Vector2 GetLocalPositionForGridPoint(Vector2Int gridPoint)
    {
        GridLayoutGroup gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
        Vector2 cellSize = gridLayout.cellSize;
        Vector2 spacing = gridLayout.spacing;

        float containerWidth = boardContainer.rect.width;
        float containerHeight = boardContainer.rect.height;
        Vector2 pivot = boardContainer.pivot;

        float startX = -containerWidth * pivot.x + gridLayout.padding.left;
        float startY = containerHeight * (1 - pivot.y) - gridLayout.padding.top;

        float cellCornerX = startX + gridPoint.x * (cellSize.x + spacing.x);
        float cellCornerY = startY - (gridPoint.y - _minRow) * (cellSize.y + spacing.y);

        float centerX = cellCornerX + cellSize.x / 2f;
        float centerY = cellCornerY - cellSize.y;

        return new Vector2(centerX, centerY);
    }

    private Task RemoveTile(Tile tile)
    {
        if (tile == null) return Task.CompletedTask;
        Vector2Int pos = tile.GridPosition;
        if (!IsValidPosition(pos) || _grid[pos.x, pos.y] == null) return Task.CompletedTask;

        _grid[pos.x, pos.y] = null;

        CheckForThaw(pos);

        return tile.ShowAsEmptyAndReturnTask();
    }

    private void CheckForThaw(Vector2Int removedPos)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            Vector2Int neighborPos = removedPos + dir;
            if (IsValidPosition(neighborPos) && _grid[neighborPos.x, neighborPos.y] != null)
            {
                _grid[neighborPos.x, neighborPos.y].Thaw();
            }
        }
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < _currentLevelData.Width && pos.y >= 0 && pos.y < _currentLevelData.Height;
    }

    private int ParseTileType(string code)
    {
        if (code == "ST") return -2; // Đá
        if (code == "BB") return -1; // Bom
        if (code == "00") return 0; // Ô trống
        if (int.TryParse(code, out int type)) return type; // Pokemon
        return 0;
    }

    public bool AreMovesAvailable()
    {
        var hint = Pathfinder.FindHint(_grid, _currentLevelData.Width, _currentLevelData.Height);
        return hint.Item1 != null;
    }

    public void ShuffleBoard()
    {
        AudioManager.Instance.PlayShuffleSound();

        // Vòng lặp để đảm bảo tìm được cách xáo trộn hợp lệ
        int attempts = 0;
        const int maxAttempts = 5; // Giới hạn số lần thử để tránh treo game

        while (attempts < maxAttempts)
        {
            List<Tile> remainingTiles = new List<Tile>();
            for (int y = 0; y < _currentLevelData.Height; y++)
            {
                for (int x = 0; x < _currentLevelData.Width; x++)
                {
                    if (_grid[x, y] != null && _grid[x, y].TileType > 0)
                    {
                        remainingTiles.Add(_grid[x, y]);
                    }
                }
            }

            if (remainingTiles.Count < 2) return;

            var tileDataList = remainingTiles.Select(t => new { Sprite = t.GetSprite(), Type = t.TileType }).ToList();
            System.Random rng = new System.Random();
            var shuffledDataList = tileDataList.OrderBy(a => rng.Next()).ToList();

            // Gán lại dữ liệu đã xáo trộn
            for (int i = 0; i < remainingTiles.Count; i++)
            {
                Tile tileObject = remainingTiles[i];
                var newTileData = shuffledDataList[i];

                // SỬA LỖI: Gọi Initialize với đầy đủ Sprite
                tileObject.Initialize(newTileData.Type, tileObject.GridPosition, newTileData.Sprite);
                tileObject.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.3f);
            }

            // Kiểm tra xem cách xáo trộn mới có nước đi không
            if (AreMovesAvailable())
            {
                Debug.Log($"Xáo trộn thành công sau {attempts + 1} lần thử.");
                return; // Thoát khỏi hàm nếu đã hợp lệ
            }

            attempts++;
        }

        Debug.LogError("Không thể tìm thấy cách xáo trộn có nước đi sau " + maxAttempts + " lần thử.");
        // Có thể hiện thông báo cho người chơi ở đây
    }
}