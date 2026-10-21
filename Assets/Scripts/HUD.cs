using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;

    public PlayerHealth playerHealth;
    public WaveSpawner spawner;

    void Update()
    {
        if (playerHealth != null)
            healthText.text = "Vida: " + playerHealth.CurrentHealth;

        if (ScoreManager.Instance != null)
            scoreText.text = "Punts: " + ScoreManager.Instance.score;

        if (spawner != null)
            waveText.text = "Onada: " + spawner.currentWave;
    }
}