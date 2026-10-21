using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI healthText; // opcional: si el vols en text en lloc de cors
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;

    public PlayerHealth playerHealth;
    public WaveSpawner spawner;

    void Update()
    {
        if (playerHealth != null && healthText != null)
            healthText.text = "Cors: " + playerHealth.CurrentLives + " / " + playerHealth.maxLives;

        if (ScoreManager.Instance != null)
            scoreText.text = "Punts: " + ScoreManager.Instance.score;

        if (spawner != null)
        {
            string actName = "";
            if (spawner.actManager != null && spawner.actManager.Current != null)
                actName = " · " + spawner.actManager.Current.name;

            waveText.text = "Onada: " + spawner.currentWave + actName;
        }
    }
}