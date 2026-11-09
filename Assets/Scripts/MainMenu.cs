using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // primer es tria el xef, i després comença la història
    public void Play()
    {
        Time.timeScale = 1f;
        if (ChefSelectScreen.IsOpen) return;
        ChefSelectScreen.Open(() => SceneManager.LoadScene(SceneNames.Story));
    }

    public void Quit()
    {
        Debug.Log("Sortir");
        Application.Quit();
    }
}