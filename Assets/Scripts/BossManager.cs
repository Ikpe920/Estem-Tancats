using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossManager : MonoBehaviour
{
    [Header("Introducció")]
    public GameObject introPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI subtitleText;
    public float introDuration = 3f;

    [Header("Barra de vida")]
    public GameObject healthBarRoot;
    public Slider healthSlider;
    public TextMeshProUGUI healthBarName;

    private Enemy currentBoss;
    private int bossMaxHealth;

    void Start()
    {
        introPanel.SetActive(false);
        healthBarRoot.SetActive(false);
        StyleBar();
    }

    // ---------- ASPECTE DE LA BARRA ----------
    // del color del cap, amb una franja blanca que mostra el mal que acabes de fer (es va encongint),
    // un panell al darrere, el nom amb contorn, i un tremolor a cada cop
    private Image fillImg, trailImg;
    private RectTransform barRt;
    private Vector2 barBasePos;
    private float trailValue = 1f, trailWait, shake;
    private float lastValue = 1f;

    void StyleBar()
    {
        if (healthSlider == null) return;
        barRt = healthBarRoot.GetComponent<RectTransform>();
        if (barRt != null) barBasePos = barRt.anchoredPosition;
        if (healthSlider.fillRect != null)
        {
            fillImg = healthSlider.fillRect.GetComponent<Image>();
            // la franja blanca: una còpia del farciment, just al darrere
            GameObject trail = new GameObject("Rastre", typeof(RectTransform), typeof(Image));
            RectTransform trt = trail.GetComponent<RectTransform>();
            trt.SetParent(healthSlider.fillRect.parent, false);
            trt.SetSiblingIndex(healthSlider.fillRect.GetSiblingIndex());
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = healthSlider.fillRect.offsetMin;
            trt.offsetMax = healthSlider.fillRect.offsetMax;
            trailImg = trail.GetComponent<Image>();
            trailImg.color = new Color(1f, 0.95f, 0.85f, 0.9f);
            trailImg.raycastTarget = false;
        }
        Transform bg = healthSlider.transform.Find("Background");
        if (bg != null && bg.GetComponent<Image>() != null) bg.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.1f, 0.9f);
        if (barRt != null)
        {
            Image panel = UIStyle.Panel(barRt, "Marc", Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f));
            panel.transform.SetAsFirstSibling();
        }
        if (healthBarName != null)
        {
            healthBarName.color = UIStyle.Gold;
            healthBarName.fontStyle |= FontStyles.Bold;
            UIStyle.Outline(healthBarName);
        }
    }

    void LateUpdate()
    {
        if (healthSlider == null || currentBoss == null) return;
        float v = healthSlider.value;
        if (v < lastValue - 0.0001f) { trailWait = 0.35f; shake = 0.15f; } // cop: la franja espera i després s'encongeix
        if (v > trailValue) trailValue = v;                                 // si es cura, la franja l'acompanya
        lastValue = v;

        trailWait -= Time.deltaTime;
        if (trailWait <= 0f) trailValue = Mathf.MoveTowards(trailValue, v, Time.deltaTime * 0.6f);
        if (trailImg != null)
        {
            RectTransform trt = trailImg.rectTransform;
            trt.anchorMax = new Vector2(trailValue, 1f);
        }

        if (barRt != null)
        {
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime);
            barRt.anchoredPosition = barBasePos + (shake > 0f ? Random.insideUnitCircle * 5f : Vector2.zero);
        }
    }

    public IEnumerator PlayIntro(string bossName, string subtitle)
    {
        nameText.text = bossName;
        subtitleText.text = subtitle;

        CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = introPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        introPanel.SetActive(true);
        AudioManager.Play("boss_intro");
        bool wasPaused = Time.timeScale == 0f; // si ja estava pausat, no el reprenem en acabar
        Time.timeScale = 0f; // pausa el joc

        float fade = 0.5f;
        float t = 0f;
        while (t < introDuration)
        {
            t += Time.unscaledDeltaTime; // el temps normal està aturat

            float alpha = 1f;
            if (t < fade) alpha = t / fade;
            else if (t > introDuration - fade) alpha = (introDuration - t) / fade;
            cg.alpha = alpha;

            // el nom comença gran i es redueix
            float scale = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(t / fade));
            nameText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        introPanel.SetActive(false);
        if (!wasPaused) Time.timeScale = 1f;
    }

    public void TrackBoss(Enemy boss, string bossName) { TrackBoss(boss, bossName, new Color(0.85f, 0.2f, 0.2f)); }

    public void TrackBoss(Enemy boss, string bossName, Color color)
    {
        if (boss == null) return;
        currentBoss = boss;
        bossMaxHealth = boss.maxHealth;
        healthBarName.text = bossName;
        healthSlider.value = 1f;
        healthBarRoot.SetActive(true);
        trailValue = lastValue = 1f;
        // el farciment, del color del cap (una mica més clar perquè es vegi bé sobre el fons fosc)
        if (fillImg != null) fillImg.color = Color.Lerp(color, Color.white, 0.25f);
    }

    void Update()
    {
        if (currentBoss == null)
        {
            if (healthBarRoot.activeSelf) healthBarRoot.SetActive(false);
            return;
        }

        healthSlider.value = (float)currentBoss.CurrentHealth / bossMaxHealth;
    }
}