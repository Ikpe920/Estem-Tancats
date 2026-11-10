using UnityEngine;
using UnityEngine.UI;
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
        Decorate();
    }

    // ---------- ASPECTE ----------
    // panells foscos darrere dels textos, contorn, requadres més amples (el text en sortia),
    // els punts que compten cap amunt i fan un salt, l'onada que fa un salt en canviar,
    // i una barra amb el temps que queda de ratxa
    private Image scorePanel, wavePanel, comboBarBg, comboBar;
    private TextMeshProUGUI actText; // la sala (i el rellotge de les hores extres), sota l'onada
    private float displayedScore;
    private float scorePop, wavePop;
    private int lastWaveShown = -1;

    void Decorate()
    {
        if (scoreText != null)
        {
            RectTransform rt = scoreText.rectTransform;
            rt.sizeDelta = new Vector2(380f, 56f);
            rt.anchoredPosition = new Vector2(-24f, -16f);
            UIStyle.Outline(scoreText);
            scorePanel = UIStyle.Panel(rt.parent, "PanellPunts", rt.anchorMin, rt.anchorMax, Vector2.zero, Vector2.zero);
            scorePanel.rectTransform.pivot = rt.pivot;
            scorePanel.rectTransform.anchoredPosition = new Vector2(-12f, -10f);
        }
        if (comboText != null)
        {
            RectTransform rt = comboText.rectTransform;
            rt.sizeDelta = new Vector2(380f, 44f);
            rt.anchoredPosition = new Vector2(-24f, -72f);
            comboText.enableAutoSizing = true;
            comboText.fontSizeMax = 32f;
            comboText.fontSizeMin = 18f;
            UIStyle.Outline(comboText);
            // barra del temps de ratxa, sota el text
            comboBarBg = MakeBar(rt.parent, new Color(0f, 0f, 0f, 0.55f));
            comboBar = MakeBar(rt.parent, Color.white);
            comboBar.type = Image.Type.Filled;
            comboBar.fillMethod = Image.FillMethod.Horizontal;
            comboBar.sprite = SpriteFactory.Square();
        }
        if (waveText != null)
        {
            // dues línies al mig de dalt: "ONADA 31" gran i, a sota, la sala (i l'hora) més petita;
            // amb una amplada màxima perquè no trepitgi mai els punts ni els cors
            RectTransform rt = waveText.rectTransform;
            rt.sizeDelta = new Vector2(520f, 48f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            waveText.enableAutoSizing = true;
            waveText.fontSizeMax = 40f;
            waveText.fontSizeMin = 24f;
            waveText.alignment = TextAlignmentOptions.Center;
            UIStyle.Outline(waveText);

            actText = Instantiate(waveText, rt.parent);
            actText.name = "SalaText";
            RectTransform art = actText.rectTransform;
            art.sizeDelta = new Vector2(560f, 30f);
            art.anchoredPosition = new Vector2(0f, -60f);
            actText.fontSizeMax = 24f;
            actText.fontSizeMin = 14f;
            actText.fontStyle = FontStyles.Bold;
            actText.color = UIStyle.Gold;
            actText.text = "";

            wavePanel = UIStyle.Panel(rt.parent, "PanellOnada", rt.anchorMin, rt.anchorMax, Vector2.zero, Vector2.zero);
            wavePanel.rectTransform.pivot = rt.pivot;
            wavePanel.rectTransform.anchoredPosition = new Vector2(0f, -6f);
        }
    }

    Image MakeBar(Transform parent, Color color)
    {
        GameObject go = new GameObject("BarraRatxa", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(240f, 8f);
        rt.anchoredPosition = new Vector2(-24f, -124f);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        ScoreManager sm = ScoreManager.Instance;

        // els punts compten cap amunt (i fan un salt quan en guanyes)
        if (sm != null && scoreText != null)
        {
            if (sm.totalScore > displayedScore + 0.5f) scorePop = 1f;
            displayedScore = Mathf.MoveTowards(displayedScore, sm.totalScore, Mathf.Max(30f, Mathf.Abs(sm.totalScore - displayedScore) * 6f) * dt);
            int show = Mathf.RoundToInt(displayedScore);
            if (show != shownScore) { shownScore = show; scoreText.text = "Punts: " + show; } // només quan canvia
            scorePop = Mathf.MoveTowards(scorePop, 0f, dt * 4f);
            scoreText.transform.localScale = Vector3.one * (1f + 0.15f * scorePop);
        }
        if (scorePanel != null && scoreText != null)
        {
            // amb ratxa, el panell s'allarga per tenir-la a dins
            bool combo = comboText != null && comboText.text.Length > 0;
            float w = Mathf.Max(scoreText.preferredWidth, combo ? comboText.preferredWidth : 0f) + 40f;
            scorePanel.rectTransform.sizeDelta = new Vector2(w, combo ? 132f : 68f);
        }

        // l'onada fa un salt (i brilla) quan canvia
        if (spawner != null && waveText != null)
        {
            if (spawner.currentWave != lastWaveShown) { if (lastWaveShown >= 0) wavePop = 1f; lastWaveShown = spawner.currentWave; }
            wavePop = Mathf.MoveTowards(wavePop, 0f, dt * 1.6f);
            waveText.transform.localScale = Vector3.one * (1f + 0.3f * wavePop);
            waveText.color = Color.Lerp(Color.white, UIStyle.Gold, wavePop);
            if (wavePanel != null)
            {
                float w = Mathf.Min(600f, Mathf.Max(waveText.preferredWidth, actText != null ? actText.preferredWidth : 0f) + 50f);
                bool twoLines = actText != null && actText.text.Length > 0;
                wavePanel.rectTransform.sizeDelta = new Vector2(w, twoLines ? 92f : 60f);
            }
        }

        // la barra de la ratxa: el temps que queda per despatxar el següent client
        if (comboBar != null && sm != null)
        {
            bool on = sm.Combo >= 2;
            if (comboBar.gameObject.activeSelf != on) { comboBar.gameObject.SetActive(on); comboBarBg.gameObject.SetActive(on); }
            if (on)
            {
                comboBar.fillAmount = Mathf.Clamp01(sm.ComboTimeLeft / Mathf.Max(0.01f, sm.comboWindow));
                comboBar.color = comboText != null ? comboText.color : UIStyle.Gold;
            }
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
            // (el text dels punts el porta LateUpdate: compta cap amunt)

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
            if (spawner.endless)
            {
                // hores extres: el rellotge al costat de la sala
                string clock = spawner.actManager != null
                    ? ActManager.Clock(spawner.actManager.Hour) + " " + ActManager.PartOfDay(spawner.actManager.Hour)
                    : "HORES EXTRES";
                act = act.Length > 0 ? act + "  ·  " + clock : clock;
            }
            if (spawner.currentWave != shownWave || act != shownAct)
            {
                shownWave = spawner.currentWave;
                shownAct = act;
                waveText.text = "ONADA " + shownWave;
                if (actText != null) actText.text = act;
                else if (act.Length > 0) waveText.text += " · " + act;
            }
        }
    }
}
