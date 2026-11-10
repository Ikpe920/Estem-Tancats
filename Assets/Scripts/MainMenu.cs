using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // primer es tria el xef, i després comença la història
    public void Play()
    {
        Time.timeScale = 1f;
        if (ChefSelectScreen.IsOpen) return;
        BossRush.Active = false; // la partida normal
        ChefSelectScreen.Open(() => Iris.LoadScene(SceneNames.Story));
    }

    // els 8 caps seguits, contra rellotge (sense història)
    public void PlayBossRush()
    {
        Time.timeScale = 1f;
        if (ChefSelectScreen.IsOpen) return;
        ChefSelectScreen.Open(() =>
        {
            RunStats.Reset();
            BossRush.Begin();
            Iris.LoadScene(SceneNames.Game);
        });
    }

    public void Quit()
    {
        Debug.Log("Sortir");
        Application.Quit();
    }
}