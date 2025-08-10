using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private AudioManager audioManager;

    private Tile _selectedTile1;
    private Tile _selectedTile2;
    private LevelData _currentLevelData;
    private bool _isCheckingMatch = false;
    private bool _isPaused = false;
    private bool _isGameOver = false;

    private int _currentScore;
    private float _currentTime;

    private Dictionary<PowerUpType, int> _powerUpInventory = new Dictionary<PowerUpType, int> { { PowerUpType.Hint, 3 }, { PowerUpType.Shuffle, 3 } };

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        Time.timeScale = 1f;
    }

    private void OnEnable() { InputManager.OnTileSelected += HandleTileSelection; }
    private void OnDisable() { InputManager.OnTileSelected -= HandleTileSelection; }

    private void Start() { StartGame(1); }

    private void Update()
    {
        if (_isPaused || _isGameOver) return;
        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            uiManager.UpdateTime(_currentTime, _currentLevelData.Time);
        }
        else
        {
            _currentTime = 0;
            uiManager.UpdateTime(0, _currentLevelData.Time);
            LoseGame();
        }
    }

    void StartGame(int levelNumber)
    {
        _isGameOver = false;
        _isPaused = false;
        Time.timeScale = 1f;

        _currentLevelData = levelManager.LoadLevel(levelNumber);
        if (_currentLevelData == null) return;

        boardManager.GenerateBoard(_currentLevelData);

        _currentScore = 0;
        _currentTime = _currentLevelData.Time;
        uiManager.UpdateScore(_currentScore);

        uiManager.UpdatePowerUpQuantity(PowerUpType.Hint, _powerUpInventory[PowerUpType.Hint]);
        uiManager.UpdatePowerUpQuantity(PowerUpType.Shuffle, _powerUpInventory[PowerUpType.Shuffle]);
    }

    private async void HandleTileSelection(Tile tile)
    {
        if (_isCheckingMatch || _isPaused || _isGameOver || tile == null || tile.TileType == 0) return;

        audioManager.PlayClickSound();

        if (_selectedTile1 == null)
        {
            _selectedTile1 = tile;
            _selectedTile1.SetSelected(true);
        }
        else if (_selectedTile2 == null)
        {
            if (tile == _selectedTile1)
            {
                _selectedTile1.SetSelected(false);
                _selectedTile1 = null;
                return;
            }

            _selectedTile2 = tile;
            _selectedTile2.SetSelected(true);

            _isCheckingMatch = true;
            await CheckForMatch();
            _isCheckingMatch = false;
        }
    }

    private async Task CheckForMatch()
    {
        if (_selectedTile1 == null || _selectedTile2 == null) return;

        bool isMatch = (_selectedTile1.TileType == _selectedTile2.TileType);
        List<Vector2Int> path = null;

        if (isMatch)
        {
            path = Pathfinder.FindPath(_selectedTile1, _selectedTile2, boardManager._grid, _currentLevelData.Width, _currentLevelData.Height);
        }

        if (path != null) // Nối thành công
        {
            await boardManager.HandleMatch(_selectedTile1, _selectedTile2, path);
            _currentScore += 100;
            uiManager.UpdateScore(_currentScore);
            _selectedTile1 = null;
            _selectedTile2 = null;
        }
        else // Nối thất bại
        {
            audioManager.PlayErrorSound();

            // Bỏ chọn ô 1, giữ lại ô 2 và gán nó thành ô 1 mới
            _selectedTile1.SetSelected(false);
            _selectedTile1 = _selectedTile2;
            _selectedTile2 = null;
            // Ô 1 mới (chính là ô 2 cũ) đã ở trạng thái được chọn, không cần làm gì thêm.
        }
    }

    private void LoseGame() { if (_isGameOver) return; _isGameOver = true; Time.timeScale = 0f; uiManager.ShowLosePanel(true); }
    public void PauseGame() { if (_isGameOver) return; _isPaused = true; Time.timeScale = 0f; uiManager.ShowPausePanel(true); }
    public void ResumeGame() { _isPaused = false; Time.timeScale = 1f; uiManager.ShowPausePanel(false); }
    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
}