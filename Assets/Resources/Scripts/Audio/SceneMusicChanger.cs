using UnityEngine;

public class SceneMusicChanger : MonoBehaviour
{
    [Header("Music here")]
    public AudioClip newMusic;
    private AudioClip previousMusic;

    void Start()
    {
        if (PersistentAudio.current != null)
        {
            if (PersistentAudio.current.audioSource != null)
            {
                previousMusic = PersistentAudio.current.audioSource.clip;
            }
            PersistentAudio.current.ChangeMusic(newMusic);
        }
    }

    void OnDestroy()
    {
        if (PersistentAudio.current != null && previousMusic != null)
        {
            PersistentAudio.current.ChangeMusic(previousMusic);
        }
    }
}