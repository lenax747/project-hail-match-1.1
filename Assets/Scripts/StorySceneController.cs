using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySceneController : MonoBehaviour
{
    public CanvasGroup storyTextGroup;
    public Button continueButton;
    public CanvasGroup playGlowGroup;

    public float textFadeDuration = 3f;
    public float glowDelay = 5f;
    public float glowFadeDuration = 1f;

    void Start()
    {
        storyTextGroup.alpha = 0f;
        continueButton.interactable = false;

        playGlowGroup.alpha = 0f;
        playGlowGroup.gameObject.SetActive(true);

        StartCoroutine(StoryRoutine());
    }

    IEnumerator StoryRoutine()
    {
        float t = 0f;

        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            storyTextGroup.alpha = t / textFadeDuration;
            yield return null;
        }

        storyTextGroup.alpha = 1f;

        yield return new WaitForSeconds(glowDelay);

        t = 0f;

        while (t < glowFadeDuration)
        {
            t += Time.deltaTime;
            playGlowGroup.alpha = t / glowFadeDuration;
            yield return null;
        }

        playGlowGroup.alpha = 1f;
        continueButton.interactable = true;
    }

    public void GoToLevel1()
    {
        SceneManager.LoadScene("Level1");
    }
}