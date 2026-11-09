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

    // els últims valors mostrats: el text només es refà quan canvien (abans es reconstruïa cada fotograma)
    private int shownLives = -1, shownMaxLives = -1, shownScore = -1, shownCombo = -1, shownWave = -1;
    private float shownMultiplier = -1f;
    private string shownAct;

    void Update()
    {
        if (playerHealth != null && healthText != null
            && (playerHealth.CurrentLives != shownLives || playerHealth.maxLives != shownMaxLives))
        {
            shownLives = playerHealth.CurrentLives;
            shownMaxLives = playerHealth.maxLives;
            healthText.text = "Cors: " + shownLives + " / " + shownMaxLives;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager sm = ScoreManager.Instance;
            if (scoreText != null && sm.totalScore != shownScore)
            {
                shownScore = sm.totalScore;
                scoreText.text = "Punts: " + shownScore;
            }

            if (comboText != null)
            {
                int combo = sm.Combo >= 2 ? sm.Combo : 0;
                float mul = sm.ComboMultiplier;
                if (combo != shownCombo || !Mathf.Approximately(mul, shownMultiplier))
                {
                    shownCombo = combo;
                    shownMultiplier = mul;
                    comboText.text = combo > 0 ? "Ratxa " + combo + " · ×" + mul.ToString("0.00").Replace('.', ',') : "";
                }

                // el color passa a vermell quan la ratxa està a punt de caure (només el color, sense refer el text)
                if (combo > 0)
                    comboText.color = Color.Lerp(new Color(1f, 0.35f, 0.3f), new Color(1f, 0.85f, 0.35f), Mathf.Clamp01(sm.ComboTimeLeft / 1.5f));
            }
        }

        if (spawner != null && waveText != null)
        {
            string act = spawner.actManager != null && spawner.actManager.Current != null ? spawner.actManager.Current.name : "";
            if (spawner.endless) act = act.Length > 0 ? act + " · INFINIT" : "INFINIT";
            if (spawner.currentWave != shownWave || act != shownAct)
            {
                shownWave = spawner.currentWave;
                shownAct = act;
                waveText.text = "Onada: " + shownWave + (act.Length > 0 ? " · " + act : "");
            }
        }
    }
}
