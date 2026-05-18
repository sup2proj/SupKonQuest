using UnityEngine;
using UnityEngine.Audio;

public class PersistentAudio : MonoBehaviour
{
    public static PersistentAudio current;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Audio Source")]
    public AudioSource audioSource;

    void Awake()
    {
        if (current == null)
        {
            current = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        ApplyVolumes(savedMusicVolume, savedSfxVolume);
    }

    void ApplyVolumes(float musicValue, float sfxValue)
    {
        if (audioMixer != null)
        {
            if (musicValue <= 0)
            {
                audioMixer.SetFloat("MusicVolume", -80f);
            }
            else
            {
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicValue) * 80f);
            }

            if (sfxValue <= 0)
            {
                audioMixer.SetFloat("SFXVolume", -80f);
            }
            else
            {
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxValue) * 80f);
            }
        }
    }

    public void ChangeMusic(AudioClip newMusic)
    {
        if (audioSource != null && newMusic != null && audioSource.clip != newMusic)
        {
            audioSource.Stop();
            audioSource.clip = newMusic;
            audioSource.Play();
        }
    }
}