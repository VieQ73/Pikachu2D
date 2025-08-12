// File: _Project/Scripts/Managers/DataManager.cs
using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData PlayerData { get; private set; }
    private string _savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        LoadData();
    }

    public void LoadData()
    {
        if (File.Exists(_savePath))
        {
            try
            {
                string json = File.ReadAllText(_savePath);
                PlayerData = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("Tải dữ liệu thành công!");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Lỗi khi tải dữ liệu, tạo dữ liệu mới. Lỗi: " + e.Message);
                CreateNewData();
            }
        }
        else
        {
            Debug.Log("Không tìm thấy file lưu, tạo dữ liệu mới.");
            CreateNewData();
        }
    }

    public void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(PlayerData, true); // true để format đẹp
            File.WriteAllText(_savePath, json);
            Debug.Log("Lưu dữ liệu thành công tại: " + _savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi khi lưu dữ liệu: " + e.Message);
        }
    }

    private void CreateNewData()
    {
        PlayerData = new PlayerData();
    }

    // Hàm tiện ích để lấy và lưu cài đặt
    public void SetBGMVolume(float volume)
    {
        PlayerData.settings.bgmVolume = volume;
        SaveData();
    }

    public void SetSFXVolume(float volume)
    {
        PlayerData.settings.sfxVolume = volume;
        SaveData();
    }
}