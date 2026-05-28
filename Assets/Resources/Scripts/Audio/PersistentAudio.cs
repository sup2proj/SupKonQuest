using UnityEngine;
using UnityEngine.Audio;

public class PersistentAudio : MonoBehaviour
{
    public static PersistentAudio current;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Audio Source")]
    public AudioSource audioSource;

    /// <summary>
    /// Initialise l'instance persistante audio (singleton) et empêche la destruction lors du chargement de scènes.
    /// </summary>
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

    /// <summary>
    /// Initialise la source audio si nécessaire et applique les volumes sauvegardés depuis PlayerPrefs.
    /// </summary>
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

    /// <summary>
    /// Applique les valeurs de volume au mixer audio en utilisant une échelle logarithmique adaptée.
    /// </summary>
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

    /// <summary>
    /// Change la musique jouée par l'AudioSource si elle est différente de la musique courante.
    /// </summary>
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