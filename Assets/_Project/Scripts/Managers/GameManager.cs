using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

// Định nghĩa các trạng thái của game
public enum GameState
{
    Playing,
    Paused,
    GameOver,
    Settings,
    Win
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Manager References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject settingsPanelPrefab;

    private AudioManager audioManager;
    private Tile _selectedTile1;
    private Tile _selectedTile2;
    private LevelData _currentLevelData;
    private GameObject _settingsPanelInstance;
    private int _remainingPairs;

    // Biến trạng thái chính của game
    public GameState CurrentState { get; private set; }

    private bool _isCheckingMatch = false;
    private int _currentScore;
    private float _currentTime;

    private Dictionary<PowerUpType, int> _powerUpInventory = new Dictionary<PowerUpType, int>
    {
        { PowerUpType.Hint, 3 },
        { PowerUpType.Shuffle, 3 }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable() { InputManager.OnTileSelected += HandleTileSelection; }
    private void OnDisable() { InputManager.OnTileSelected -= HandleTileSelection; }

    private void Start()
    {
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogError("KHÔNG TÌM THẤY AUDIOMANAGER TRONG SCENE!");
        }

        StartGame(2);
    }

    private void Update()
    {
        // Chỉ đếm ngược thời gian khi game ở trạng thái 'Playing' // setting
        if (CurrentState == GameState.Paused || CurrentState == GameState.GameOver || CurrentState == GameState.Win)
        {
            return;
        }

        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            uiManager.UpdateTime(_currentTime, _currentLevelData.Time);
        }
        else
        {
            _currentTime = 0;
            uiManager.UpdateTime(0, _currentLevelData.Time);
            // Chuyển sang trạng thái thua
            ChangeState(GameState.GameOver);
        }
    }

    // Hàm quản lý việc chuyển đổi trạng thái
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                uiManager.ShowPausePanel(false);
                if (_settingsPanelInstance != null) _settingsPanelInstance.SetActive(false);
                audioManager.UnPauseBackgroundMusic();
                Time.timeScale = 1f; // Đảm bảo game chạy
                break;
            case GameState.Paused:
                Time.timeScale = 0f; // Dừng hoàn toàn game
                uiManager.ShowPausePanel(true);
                audioManager.PauseBackgroundMusic();
                break;
            case GameState.Settings:
                // Không dừng Time.timeScale
                if (_settingsPanelInstance == null)
                {
                    _settingsPanelInstance = Instantiate(settingsPanelPrefab, uiManager.transform);
                    // Cấu hình để panel hiển thị trên cùng
                    Canvas panelCanvas = _settingsPanelInstance.GetComponent<Canvas>();
                    if (panelCanvas != null)
                    {
                        panelCanvas.overrideSorting = true;
                        panelCanvas.sortingOrder = 200;
                    }
                    _settingsPanelInstance.transform.SetAsLastSibling();
                }
                _settingsPanelInstance.SetActive(true);
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                uiManager.ShowLosePanel(true);
                audioManager.PauseBackgroundMusic();
                audioManager.PlayOhoSound();
                break;
            case GameState.Win:
                Time.timeScale = 0f;
                audioManager.PlayWinSound();
                uiManager.ShowWinPanel(true);
                audioManager.PauseBackgroundMusic();
                break;
        }
    }

    void StartGame(int levelNumber)
    {
        _currentLevelData = levelManager.LoadLevel(levelNumber);
        if (_currentLevelData == null) return;

        boardManager.GenerateBoard(_currentLevelData);

        _currentScore = 0;
        _currentTime = _currentLevelData.Time;
        uiManager.UpdateScore(_currentScore);

        // Yêu cầu AudioManager bắt đầu phát nhạc cho màn chơi
        audioManager.PlayBackgroundMusic();

        uiManager.UpdatePowerUpQuantity(PowerUpType.Hint, _powerUpInventory[PowerUpType.Hint]);
        uiManager.UpdatePowerUpQuantity(PowerUpType.Shuffle, _powerUpInventory[PowerUpType.Shuffle]);

        ChangeState(GameState.Playing);
    }

    private async void HandleTileSelection(Tile tile)
    {
        // Chỉ cho phép chọn tile khi đang ở trạng thái Playing
        if (CurrentState != GameState.Playing || _isCheckingMatch || tile == null || tile.TileType == 0) return;

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
            path = Pathfinder.FindPath(
                _selectedTile1,
                _selectedTile2,
                boardManager._grid,
                _currentLevelData.Width,
                _currentLevelData.Height
            );
        }

        if (path != null) // Nối thành công
        {
            await boardManager.HandleMatch(_selectedTile1, _selectedTile2, path);
            _currentScore += 100;
            uiManager.UpdateScore(_currentScore);
            _remainingPairs--;

            // Điều kiện thắng: Không còn tile nào
            if (boardManager.GetRemainingTileCount() == 0)
            {
                ChangeState(GameState.Win);
            }
            // Nếu chưa thắng, kiểm tra hết nước đi → shuffle
            else if (!boardManager.AreMovesAvailable())
            {
                Debug.Log("Hết nước đi! Tự động xáo trộn...");
                await Task.Delay(500);
                boardManager.ShuffleBoard();

                // Shuffle đến khi có ít nhất 1 nước đi
                int safetyCount = 0;
                while (!boardManager.AreMovesAvailable() && safetyCount < 5)
                {
                    boardManager.ShuffleBoard();
                    safetyCount++;
                }
            }
        }
        else // Nối thất bại
        {
            audioManager.PlayErrorSound();
            _selectedTile1.SetSelected(false);
            _selectedTile1 = _selectedTile2;
            _selectedTile2 = null;
            return;
        }

        // Reset lựa chọn
        _selectedTile1 = null;
        _selectedTile2 = null;
    }

    // === Các hàm public được gọi từ UI ===

    public void OnHintButtonClicked()
    {
        if (CurrentState != GameState.Playing) return; // Chỉ hoạt động khi đang chơi
        if (_powerUpInventory[PowerUpType.Hint] > 0)
        {
            var hintPair = Pathfinder.FindHint(boardManager._grid, _currentLevelData.Width, _currentLevelData.Height);
            if (hintPair.Item1 != null && hintPair.Item2 != null)
            {
                hintPair.Item1.DoHintEffect();
                hintPair.Item2.DoHintEffect();
                _powerUpInventory[PowerUpType.Hint]--;
                uiManager.UpdatePowerUpQuantity(PowerUpType.Hint, _powerUpInventory[PowerUpType.Hint]);
            }
        }
    }

    public void OnShuffleButtonClicked()
    {
        if (CurrentState != GameState.Playing) return; // Chỉ hoạt động khi đang chơi
        if (_powerUpInventory[PowerUpType.Shuffle] > 0)
        {
            boardManager.ShuffleBoard();
            _powerUpInventory[PowerUpType.Shuffle]--;
            uiManager.UpdatePowerUpQuantity(PowerUpType.Shuffle, _powerUpInventory[PowerUpType.Shuffle]);
        }
    }

    public void OnSettingsButtonClicked()
    {
        // Nếu panel chưa được tạo, hãy tạo nó
        if (_settingsPanelInstance == null)
        {
            _settingsPanelInstance = Instantiate(settingsPanelPrefab, uiManager.transform);
            // Áp dụng các thay đổi để nó hiển thị đúng
            _settingsPanelInstance.transform.SetAsLastSibling();
            Canvas panelCanvas = _settingsPanelInstance.GetComponent<Canvas>();
            if (panelCanvas != null)
            {
                panelCanvas.overrideSorting = true;
                panelCanvas.sortingOrder = 100;
            }
        }

        // Bật/tắt panel
        bool isActive = _settingsPanelInstance.activeSelf;
        _settingsPanelInstance.SetActive(!isActive);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }

    public void CloseSettingsPanel()
    {
        if (CurrentState == GameState.Settings)
        {
            ChangeState(GameState.Playing);
        }
    }

    public void RestartGame()
    {
        // Dừng BGM trước khi tải lại scene
        audioManager.StopBackgroundMusic();
        // Tải lại scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void NextLevel()
    {
        // Dừng BGM trước khi chuyển màn
        audioManager.StopBackgroundMusic();
        // Tạm thời, chỉ chơi lại màn 
        Debug.Log("Chuyển sang màn tiếp theo... (chưa triển khai)");
        RestartGame();
    }
}