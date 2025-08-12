using UnityEngine;
using UnityEngine.UI;
using TMPro; // Dùng TextMeshPro

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("In-Game HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TextMeshProUGUI timeValueText;

    [Header("Power-up Buttons")]
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintQuantityText;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private TextMeshProUGUI shuffleQuantityText;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // Tắt hết các panel khi bắt đầu
        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = $"Điểm: {newScore}";
    }

    public void UpdateTime(float currentTime, float maxTime)
    {
        if (maxTime > 0)
        {
            timeSlider.value = currentTime / maxTime;
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timeValueText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    public void UpdatePowerUpQuantity(PowerUpType type, int quantity)
    {
        switch (type)
        {
            case PowerUpType.Hint:
                hintQuantityText.text = quantity.ToString();
                hintButton.interactable = quantity > 0;
                break;
            case PowerUpType.Shuffle:
                shuffleQuantityText.text = quantity.ToString();
                shuffleButton.interactable = quantity > 0;
                break;
        }
    }

    public bool IsPausePanelActive()
    {
        return pausePanel != null && pausePanel.activeInHierarchy;
    }

    // Các hàm để bật/tắt panel
    public void ShowPausePanel(bool show)
    {
        pausePanel.SetActive(show);
    }

    public void ShowWinPanel(bool show)
    {
        winPanel.SetActive(show);
    }

    public void ShowLosePanel(bool show)
    {
        losePanel.SetActive(show);
    }
}

public enum PowerUpType { Hint, Shuffle, Hammer, FreezeClock }