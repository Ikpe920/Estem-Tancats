using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI healthText; // opcional: si el vols en text en lloc de cors
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;

    public TextMeshProUGUI comboText;  // opcional: si el deixes buit, es crea sol sota els punts

    public PlayerHealth playerHealth;
    public WaveSpawner spawner;

    void Start()
    {
        if (comboText == null && scoreText != null)
        {
            comboText = Instantiate(scoreText, scoreText.transform.parent);
            comboText.name = "ComboText";
            comboText.rectTransform.anchoredPosition = scoreText.rectTransform.anchoredPosition
                - new Vector2(0f, scoreText.fontSize * 1.25f);
            comboText.text = "";
        }
    }

    void Update()
    {
        if (playerHealth != null && healthText != null)
            healthText.text = "Cors: " + playerHealth.CurrentLives + " / " + playerHealth.maxLives;

        if (ScoreManager.Instance != null)
        {
            ScoreManager sm = ScoreManager.Instance;
            scoreText.text = "Punts: " + sm.totalScore;

            if (comboText != null)
            {
                if (sm.Combo >= 2)
                {
                    // el color passa a vermell quan la ratxa està a punt de caure
                    Color c = Color.Lerp(new Color(1f, 0.35f, 0.3f), new Color(1f, 0.85f, 0.35f), Mathf.Clamp01(sm.ComboTimeLeft / 1.5f));
                    comboText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">Ratxa " + sm.Combo
                        + " · ×" + sm.ComboMultiplier.ToString("0.00").Replace('.', ',') + "</color>";
                }
                else comboText.text = "";
            }
        }

        if (spawner != null)
        {
            string actName = "";
            if (spawner.actManager != null && spawner.actManager.Current != null)
                actName = " · " + spawner.actManager.Current.name;

            waveText.text = "Onada: " + spawner.currentWave + actName;
        }
    }
}