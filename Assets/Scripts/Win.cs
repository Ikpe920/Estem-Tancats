using UnityEngine;
using UnityEngine.SceneManagement;

// pantalla de victòria: has sobreviscut a totes les onades
public class Win : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // el títol, gran i a dalt
        EndScreenUI.CreateText(canvas.transform, "TitleText", "HAS SOBREVISCUT AL SERVEI!", 72f,
            new Vector2(0.5f, 0.68f), new Vector2(1500f, 110f), Color.white, true);

        // el resultat de la partida
        EndScreenUI.CreateText(canvas.transform, "Resultat",
            "Has completat les " + RunStats.wave + " onades!\nPunts: " + RunStats.points, 36f,
            new Vector2(0.5f, 0.50f), new Vector2(1100f, 160f), Color.white);

        // els botons per continuar
        EndScreenUI.CreateButton(canvas.transform, "Tornar a jugar", new Vector2(0.38f, 0.22f),
            new Vector2(320f, 72f), new Color(0.20f, 0.45f, 0.25f), Restart);
        EndScreenUI.CreateButton(canvas.transform, "Menú principal", new Vector2(0.62f, 0.22f),
            new Vector2(320f, 72f), new Color(0.30f, 0.30f, 0.35f), Menu);
    }

    public void Restart()
    {
        RunStats.Reset();
        SceneManager.LoadScene(SceneNames.Game);
    }

    public void Menu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
