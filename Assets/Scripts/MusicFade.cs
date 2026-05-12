using UnityEngine;

public class MusicFade : MonoBehaviour
{
    public AudioSource audioSource;
    public float targetVolume = 0.4f;
    public float fadeSpeed = 0.2f;

    void Start()
    {
        audioSource.volume = 0;
        audioSource.Play();
    }

    void Update()
    {
        if (audioSource.volume < targetVolume)
        {
            audioSource.volume += fadeSpeed * Time.deltaTime;
        }
    }
}