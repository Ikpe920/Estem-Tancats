using UnityEngine;
using UnityEngine.SceneManagement;

// pantalla de victòria: has sobreviscut a totes les onades
public class Win : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;

        Canvas canvas = EndScreenUI.SceneCanvas(); // el de l'escena, no el del filtre ni el del cercle
        if (canvas == null) return;

        // títol, retrat del xef, estadístiques que compten cap amunt i confeti
        EndScreenFx.Build(canvas, true);

        // els botons per continuar
        EndScreenUI.CreateButton(canvas.transform, "Tornar a jugar", new Vector2(0.38f, 0.22f),
            new Vector2(320f, 72f), new Color(0.20f, 0.45f, 0.25f), Restart);
        EndScreenUI.CreateButton(canvas.transform, "Menú principal", new Vector2(0.62f, 0.22f),
            new Vector2(320f, 72f), new Color(0.30f, 0.30f, 0.35f), Menu);
    }

    public void Restart()
    {
        RunStats.Reset();
        Iris.LoadScene(SceneNames.Game);
    }

    public void Menu()
    {
        Iris.LoadScene(SceneNames.MainMenu);
    }
}
