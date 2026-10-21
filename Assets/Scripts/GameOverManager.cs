using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI resultText;
    public WaveSpawner spawner;

    void Start()
    {
        panel.SetActive(false);
    }

    public void ShowGameOver()
    {
        resultText.text = "Onada: " + spawner.currentWave +
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