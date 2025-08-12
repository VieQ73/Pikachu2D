using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanelPrefab;
    private GameObject _settingsPanelInstance;

    public void PlayGame()
    {
        // Tải màn chơi cao nhất mà người chơi đã mở khóa
        // int levelToLoad = DataManager.Instance.PlayerData.highestLevelUnlocked;
        // SceneManager.LoadScene("Game"); // Cần một cách để truyền level index

        // Tạm thời, luôn chơi màn 1
        SceneManager.LoadScene("Game");
    }

    public void OpenSettings()
    {
        // Tạo panel Cài đặt nếu chưa có
        if (_settingsPanelInstance == null)
        {
            // Tìm Canvas trong scene
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                _settingsPanelInstance = Instantiate(settingsPanelPrefab, canvas.transform);
            }
        }
        _settingsPanelInstance.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game!"); // Dòng này chỉ có tác dụng trong Editor
        Application.Quit();
    }
}