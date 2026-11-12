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

    // la marca del 50% (on comença la 2a fase) i el punt feble
    private Image phaseMark, weakIcon;
    private TextMeshProUGUI weakText, phaseText;
    private bool phase2Shown;
    private float phasePulse;

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
        BuildExtras();
        if (healthBarName != null)
        {
            healthBarName.color = UIStyle.Gold;
            healthBarName.rectTransform.anchoredPosition += new Vector2(0f, -18f); // més avall: que no toqui el marc
            healthBarName.fontStyle |= FontStyles.Bold;
            UIStyle.Outline(healthBarName);
        }
    }

    void BuildExtras()
    {
        if (healthSlider.fillRect != null)
        {
            // la marca del 50%: una ratlla a mitja barra
            GameObject m = new GameObject("Marca2aFase", typeof(RectTransform), typeof(Image));
            RectTransform mrt = m.GetComponent<RectTransform>();
            mrt.SetParent(healthSlider.fillRect.parent, false);
            mrt.anchorMin = new Vector2(0.5f, -0.25f);
            mrt.anchorMax = new Vector2(0.5f, 1.25f);
            mrt.sizeDelta = new Vector2(6f, 0f);
            mrt.anchoredPosition = Vector2.zero;
            phaseMark = m.GetComponent<Image>();
            phaseMark.raycastTarget = false;

            phaseText = NewText(mrt, "2a FASE", 20f, new Vector2(0.5f, 0f), new Vector2(0f, -16f), new Vector2(160f, 26f), TextAlignmentOptions.Center);
        }
        if (barRt != null)
        {
            // el punt feble, a la dreta de la barra: la icona i el nom
            GameObject ic = new GameObject("PuntFeble", typeof(RectTransform), typeof(Image));
            RectTransform irt = ic.GetComponent<RectTransform>();
            irt.SetParent(barRt, false);
            irt.anchorMin = irt.anchorMax = new Vector2(1f, 0f);
            irt.pivot = new Vector2(1f, 1f);
            irt.sizeDelta = new Vector2(44f, 44f);
            irt.anchoredPosition = new Vector2(-4f, -14f);
            weakIcon = ic.GetComponent<Image>();
            weakIcon.preserveAspect = true;
            weakIcon.raycastTarget = false;
            weakText = NewText(irt, "", 22f, new Vector2(0f, 0.5f), new Vector2(-8f, 0f), new Vector2(320f, 40f), TextAlignmentOptions.Right);
            weakText.rectTransform.pivot = new Vector2(1f, 0.5f);
        }
    }

    TextMeshProUGUI NewText(RectTransform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        GameObject g = new GameObject("Text", typeof(RectTransform));
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        if (healthBarName != null) t.font = healthBarName.font;
        else if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = align;
        t.color = new Color(1f, 0.92f, 0.8f);
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        UIStyle.Outline(t);
        return t;
    }

    static string WeakIcon(DmgKind k)
    {
        switch (k)
        {
            case DmgKind.Fire: return "Fire";
            case DmgKind.Ice: return "Ice";
            case DmgKind.Explosion: return "Explosion";
            case DmgKind.Crit: return "Crit";
        }
        return null;
    }

    void LateUpdate()
    {
        UpdateExtras();
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

        // la 2a fase encara no ha començat
        phase2Shown = false;
        phasePulse = 0f;
        if (phaseMark != null) phaseMark.color = new Color(1f, 0.95f, 0.85f, 0.95f);
        if (phaseText != null) phaseText.alpha = 0f;

        // el punt feble
        string icon = WeakIcon(boss.weakness);
        Sprite sp = icon != null ? Resources.Load<Sprite>("Weak/" + icon) : null;
        if (weakIcon != null) { weakIcon.sprite = sp; weakIcon.enabled = sp != null; }
        if (weakText != null)
            weakText.text = sp != null ? "<color=#FF9A40>PUNT FEBLE:</color> " + DamageKind.Name(boss.weakness) : "";
    }

    void UpdateExtras()
    {
        if (currentBoss == null || phaseMark == null) return;
        if (!phase2Shown && currentBoss.CurrentHealth <= currentBoss.MaxHealth / 2)
        {
            // ha entrat a la 2a fase: la marca s'encén de vermell i ho diu
            phase2Shown = true;
            phasePulse = 1.2f;
            shake = 0.35f;
        }
        if (phase2Shown)
        {
            phasePulse = Mathf.MoveTowards(phasePulse, 0f, Time.unscaledDeltaTime);
            float k = Mathf.Abs(Mathf.Sin(phasePulse * 12f)) * Mathf.Clamp01(phasePulse);
            phaseMark.color = Color.Lerp(new Color(0.85f, 0.18f, 0.15f, 0.95f), Color.white, k);
            if (phaseText != null)
            {
                phaseText.alpha = 1f;
                phaseText.color = Color.Lerp(new Color(1f, 0.45f, 0.35f), Color.white, k);
                phaseText.rectTransform.localScale = Vector3.one * (1f + 0.4f * Mathf.Clamp01(phasePulse));
            }
        }
        if (weakIcon != null && weakIcon.enabled)
            weakIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 3f) * 6f);
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