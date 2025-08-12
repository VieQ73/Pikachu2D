using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip linkedSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip ohoSound;
    [SerializeField] private AudioClip shuffleSound;

    private bool _isBgmPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Đăng ký sự kiện để biết khi nào một scene mới được tải
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (DataManager.Instance != null)
        {
            ApplyLoadedVolumeSettings();
        }
    }

    // Hàm này được gọi mỗi khi một scene mới tải xong
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Khi vào lại MainMenu, dừng nhạc nền
        if (scene.name == "MainMenu")
        {
            StopBackgroundMusic();
        }
    }

    public void StartLevelBGM()
    {
        PlayBackgroundMusic();
    }

    public void ApplyLoadedVolumeSettings()
    {
        SetBGMVolume(DataManager.Instance.PlayerData.settings.bgmVolume, false); // false = không lưu lại
        SetSFXVolume(DataManager.Instance.PlayerData.settings.sfxVolume, false); // false = không lưu lại
    }

    public void SetBGMVolume(float volume, bool saveData = true)
    {
        if (bgmSource != null) bgmSource.volume = volume;
        if (saveData && DataManager.Instance != null) DataManager.Instance.SetBGMVolume(volume);
    }

    public void SetSFXVolume(float volume, bool saveData = true)
    {
        if (sfxSource != null) sfxSource.volume = volume;
        if (saveData && DataManager.Instance != null) DataManager.Instance.SetSFXVolume(volume);
    }

    // === Nhạc nền ===
    public void PlayBackgroundMusic()
    {
        // Chỉ phát nếu có đủ thông tin
        if (bgmSource != null && backgroundMusic != null)
        {
            // Dừng nhạc hiện tại (nếu có) để đảm bảo phát lại từ đầu
            bgmSource.Stop();
            bgmSource.mute = false;
            bgmSource.clip = backgroundMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // Hàm để tạm dừng/phát lại nhạc nền
    public void ToggleBGMMute(bool isMuted)
    {
        if (bgmSource != null)
        {
            bgmSource.mute = isMuted;
        }
    }
    public void PlayClickSound()
    {
        PlaySfx(clickSound);
    }

    public void PlayLinkedSound()
    {
        PlaySfx(linkedSound);
    }

    public void PlayErrorSound()
    {
        PlaySfx(errorSound);
    }

    public void PlayShuffleSound()
    {
        PlaySfx(shuffleSound);
    }

    public void PlayOhoSound()
    {
        PlaySfx(ohoSound);
    }
    internal void PlayWinSound()
    {
        PlaySfx(winSound);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public float GetBGMVolume() { return bgmSource != null ? bgmSource.volume : 0.8f; }
    public float GetSFXVolume() { return sfxSource != null ? sfxSource.volume : 1.0f; }
}