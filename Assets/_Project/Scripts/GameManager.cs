// File: _Project/Scripts/Managers/GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Manager References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UIManager uiManager;

    private Tile _selectedTile1;
    private Tile _selectedTile2;
    private LevelData _currentLevelData;
    private bool _isCheckingMatch = false;

    private int _currentScore;
    private float _currentTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        InputManager.OnTileSelected += HandleTileSelection;
    }

    private void OnDisable()
    {
        InputManager.OnTileSelected -= HandleTileSelection;
    }

    private void Start()
    {
        StartGame(1);
    }

    private void Update()
    {
        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            if (uiManager != null) uiManager.UpdateTime(_currentTime);
        }
    }

    void StartGame(int levelNumber)
    {
        if (levelManager == null || boardManager == null || uiManager == null)
        {
            Debug.LogError("MỘT TRONG CÁC MANAGER CHƯA ĐƯỢC GÁN TRONG INSPECTOR CỦA GAMEMANAGER!");
            return;
        }

        _currentLevelData = levelManager.LoadLevel(levelNumber);
        if (_currentLevelData == null)
        {
            Debug.LogError("[GameManager] Không thể tải LevelData. Dừng game.");
            return;
        }

        // DÒNG DEBUG QUAN TRỌNG
        if (_currentLevelData.Layout == null)
        {
            Debug.LogError("[GameManager] LỖI NGHIÊM TRỌNG: LevelData tồn tại nhưng Layout bên trong nó bị NULL!");
            return;
        }
        else
        {
            Debug.Log("[GameManager] LevelData và Layout đã sẵn sàng. Bắt đầu tạo bàn cờ.");
        }

        boardManager.GenerateBoard(_currentLevelData);

        _currentScore = 0;
        _currentTime = _currentLevelData.Time;
        uiManager.UpdateScore(_currentScore);
        uiManager.UpdateTime(_currentTime);
    }

    private async void HandleTileSelection(Tile tile)
    {
        if (_isCheckingMatch || tile == null || tile.IsFrozen) return;

        if (_selectedTile1 == null)
        {
            _selectedTile1 = tile;
            _selectedTile1.transform.DOScale(1.1f, 0.1f);
        }
        else if (_selectedTile2 == null)
        {
            if (tile == _selectedTile1) return;

            _selectedTile2 = tile;
            _selectedTile2.transform.DOScale(1.1f, 0.1f);

            _isCheckingMatch = true;
            await CheckForMatch();
            _isCheckingMatch = false;
        }
    }

    private async Task CheckForMatch()
    {
        if (_selectedTile1 == null || _selectedTile2 == null) return;

        bool isMatch = (_selectedTile1.TileType == _selectedTile2.TileType);

        if (isMatch && _selectedTile1.TileType > 0)
        {
            List<Vector2Int> path = Pathfinder.FindPath(_selectedTile1, _selectedTile2, boardManager._grid, _currentLevelData.Width, _currentLevelData.Height);
            if (path != null)
            {
                await boardManager.HandleMatch(_selectedTile1, _selectedTile2, path);
                _currentScore += 100;
                uiManager.UpdateScore(_currentScore);
            }
        }
        else if (isMatch && _selectedTile1.TileType == -1)
        {
            List<Vector2Int> bombPath = new List<Vector2Int> { _selectedTile1.GridPosition, _selectedTile2.GridPosition };
            await boardManager.HandleMatch(_selectedTile1, _selectedTile2, bombPath);
        }

        if (_selectedTile1 != null) _selectedTile1.transform.DOScale(1f, 0.1f);
        if (_selectedTile2 != null) _selectedTile2.transform.DOScale(1f, 0.1f);
        _selectedTile1 = null;
        _selectedTile2 = null;
    }
}