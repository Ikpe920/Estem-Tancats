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

        // el cartell de pel·lícula muda del final
        Intertitle.ShowAsync(BossRush.Active ? "I el xef es va acabar el menú.\nFins a l'últim plat." : Intertitle.WinText(), true, 2.4f);

        // els botons per continuar
        EndScreenUI.CreateButton(canvas.transform, "Tornar a jugar", new Vector2(0.38f, 0.22f),
            new Vector2(320f, 72f), new Color(0.20f, 0.45f, 0.25f), Restart);
        EndScreenUI.CreateButton(canvas.transform, "Menú principal", new Vector2(0.62f, 0.22f),
            new Vector2(320f, 72f), new Color(0.30f, 0.30f, 0.35f), Menu);
    }

    public void Restart()
    {
        RunStats.Reset();
        if (BossRush.Active) BossRush.Begin(); // torna a començar els caps seguits
        Iris.LoadScene(SceneNames.Game);
    }

    public void Menu()
    {
        Iris.LoadScene(SceneNames.MainMenu);
    }
}
