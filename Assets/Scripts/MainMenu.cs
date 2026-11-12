using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // "Jugar" obre la tria de mode: El Servei (la partida normal) o el Menú Degustació (els 8 caps)
    public void Play()
    {
        Time.timeScale = 1f;
        if (ChefSelectScreen.IsOpen || ModeSelectScreen.IsOpen) return;
        ModeSelectScreen.Open(this);
    }

    // El Servei: primer es tria el xef, i després comença la història
    public void PlayNormal()
    {
        Time.timeScale = 1f;
        if (ChefSelectScreen.IsOpen) return;
        BossRush.Active = false;
        ChefSelectScreen.Open(() => Iris.LoadScene(SceneNames.Story));
    }

    // el Menú Degustació: els 8 caps seguits, contra rellotge (sense història)
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