using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleText;   // si el deixes buit, busca l'objecte "TitleText" dins del panell
    public TextMeshProUGUI resultText;
    public WaveSpawner spawner;

    public string gameOverTitle = "GAME OVER";
    public string victoryTitle = "HAS SOBREVISCUT AL SERVEI!";

    private bool ended;

    void Start()
    {
        if (titleText == null && panel != null)
        {
            Transform t = panel.transform.Find("TitleText");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }

        panel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (ended) return;
        ended = true;

        if (titleText != null) titleText.text = gameOverTitle;
        resultText.text = "Has arribat a l'onada " + spawner.currentWave +
                          "\nPunts: " + ScoreManager.Instance.totalScore;
        panel.SetActive(true);
        Time.timeScale = 0f; // pausa el joc
    }

    public void ShowVictory()
    {
        if (ended) return;
        ended = true;

        if (titleText != null) titleText.text = victoryTitle;
        resultText.text = "Has completat les " + spawner.finalWave + " onades!" +
                          "\nPunts: " + ScoreManager.Instance.totalScore;
        panel.SetActive(true);
        Time.timeScale = 0f; // pausa el joc
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}