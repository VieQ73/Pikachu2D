using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private BoardManager boardManager;

    private Tile _selectedTile1;
    private Tile _selectedTile2;

    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện khi script được kích hoạt
        InputManager.OnTileSelected += HandleTileSelection;
    }

    private void OnDisable()
    {
        // Hủy đăng ký để tránh lỗi
        InputManager.OnTileSelected -= HandleTileSelection;
    }

    // Hàm khởi động màn chơi (tạm thời)
    void StartGame()
    {
        // Hard-code level 1 để test
        int[,] layout = new int[,]
        {
        // Dòng dưới cùng (y=11) -> Dòng trên cùng (y=0)
        // Cấu trúc layout này cần đảo ngược so với file txt
        // vì mảng 2D trong C# là [row, col] ~ [y, x]
        // và y=0 là hàng trên cùng
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,5,5,6,6,0,0,0}, // Dòng y=5
        {0,4,4,0,0,0,0,0}, // Dòng y=4
        {0,3,0,0,0,0,3,0}, // Dòng y=3
        {0,1,1,0,2,2,0,0}, // Dòng y=2
        {0,0,0,0,0,0,0,0}, // Dòng y=1
        {0,0,0,0,0,0,0,0}  // Dòng y=0
        };
        // Đảo ngược layout theo trục y để khớp với cách duyệt của BoardManager
        int[,] finalLayout = new int[12, 8];
        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                finalLayout[y, x] = layout[11 - y, x];
            }
        }

        LevelData testLevel = new LevelData(8, 12, finalLayout);
        boardManager.GenerateBoard(testLevel);
    }

    // GỌI HÀM NÀY ĐỂ TEST TRONG UNITY
    private void Start()
    {
        StartGame();
    }

    private void HandleTileSelection(Tile tile)
    {
        if (_selectedTile1 == null)
        {
            _selectedTile1 = tile;
            // TODO: Thêm hiệu ứng cho tile được chọn (phóng to, đổi màu,...)
            _selectedTile1.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        }
        else if (_selectedTile2 == null)
        {
            // Không cho phép chọn lại chính tile đó
            if (tile == _selectedTile1) return;

            _selectedTile2 = tile;
            _selectedTile2.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            // Có đủ 2 tile, tiến hành kiểm tra
            CheckForMatch();
        }
    }

    private void CheckForMatch()
    {
        // Gọi hàm kiểm tra từ Pathfinder
        if (Pathfinder.AreTilesMatchable(_selectedTile1, _selectedTile2))
        {
            Debug.Log("MATCH FOUND! (Logic xóa sẽ ở Cột mốc 2)");
            // TODO: Yêu cầu BoardManager xóa 2 tile này
        }
        else
        {
            Debug.Log("NO MATCH.");
        }

        // Reset lại lựa chọn sau khi kiểm tra
        _selectedTile1.transform.localScale = Vector3.one;
        _selectedTile2.transform.localScale = Vector3.one;
        _selectedTile1 = null;
        _selectedTile2 = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ GameManager tồn tại qua các scene
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        switch (newState)
        {
            case GameState.AppStart:
                // Logic khi khởi động app
                break;
            case GameState.MainMenu:
                // Logic khi ở menu chính
                break;
            case GameState.LevelLoading:
                // Logic khi tải level
                break;
            case GameState.Playing:
                // Logic khi đang chơi
                break;
            case GameState.Pause:
                // Logic khi tạm dừng
                break;
            case GameState.Win:
                // Logic khi thắng
                break;
            case GameState.Lose:
                // Logic khi thua
                break;
        }
    }
}

// Enum định nghĩa các trạng thái của game
public enum GameState
{
    AppStart,
    MainMenu,
    LevelLoading,
    Playing,
    Pause,
    Win,
    Lose
}