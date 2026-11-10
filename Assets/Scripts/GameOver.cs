using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// pantalla de derrota: mostra l'onada i els punts i deixa tornar a jugar
public class GameOver : MonoBehaviour
{
    public TextMeshProUGUI resultText; // l'onada i els punts

    void Start()
    {
        Time.timeScale = 1f;

        if (resultText == null) resultText = EndScreenUI.FindText("resultText");

        TextMeshProUGUI title = EndScreenUI.FindText("TitleText");
        if (title != null) title.text = RunStats.endless ? "FI DEL MODE INFINIT" : "FI DEL JOC";

        if (resultText != null)
            resultText.text = (RunStats.endless ? "Mode infinit: has arribat a l'onada " : "Has arribat a l'onada ") + RunStats.wave
                + "\nPunts: " + RunStats.points + RunStats.ExtraLines();

        Canvas canvas = EndScreenUI.SceneCanvas(); // el de l'escena, no el del filtre ni el del cercle
        if (canvas == null) return;

        // la pantalla nova: títol, retrat del xef, estadístiques que compten cap amunt (amaga els textos vells)
        EndScreenFx.Build(canvas, false);

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
