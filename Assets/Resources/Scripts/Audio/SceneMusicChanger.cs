using UnityEngine;

public class SceneMusicChanger : MonoBehaviour
{
    [Header("Music here")]
    public AudioClip newMusic;
    private AudioClip previousMusic;

    /// <summary>
    /// Sauvegarde la musique précédente et demande au PersistentAudio de jouer la nouvelle musique.
    /// </summary>
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

    /// <summary>
    /// Lors de la destruction, restaure la musique précédente si elle existe.
    /// </summary>
    void OnDestroy()
    {
        if (PersistentAudio.current != null && previousMusic != null)
        {
            PersistentAudio.current.ChangeMusic(previousMusic);
        }
    }
}