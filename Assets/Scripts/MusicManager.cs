using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    private AudioSource audioSource;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        int musicOn = PlayerPrefs.GetInt("MusicOn", 1);
        audioSource.mute = musicOn == 0;
    }

    public void ToggleMusic()
    {
        audioSource.mute = !audioSource.mute;

        PlayerPrefs.SetInt("MusicOn", audioSource.mute ? 0 : 1);
        PlayerPrefs.Save();
    }

    public bool IsMusicOn()
    {
        return !audioSource.mute;
    }
}