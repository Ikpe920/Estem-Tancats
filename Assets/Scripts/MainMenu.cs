using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.Story);
    }

    public void Quit()
    {
        Debug.Log("Sortir");
        Application.Quit();
    }
}