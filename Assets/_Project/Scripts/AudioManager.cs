// File: _Project/Scripts/Managers/AudioManager.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip linkedSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip winSound;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = GetComponent<AudioSource>();
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

    public void PlayWinSound()
    {
        PlaySfx(winSound);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            // PlayOneShot cho phép phát nhiều âm thanh chồng lên nhau,
            // phù hợp cho hiệu ứng âm thanh.
            sfxSource.PlayOneShot(clip);
        }
    }
}