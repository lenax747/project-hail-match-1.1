using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void GoToStory()
    {
        SceneManager.LoadScene("StoryScene");
    }
}