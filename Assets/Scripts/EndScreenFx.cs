using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  PANTALLES FINALS (Game Over i victòria)
// =====================================================================
// Fons tenyit amb ratlles que llisquen, el títol que cau de cop, el retrat del xef (caigut si has perdut,
// content si has guanyat), una targeta amb les estadístiques que surten una a una i compten cap amunt,
// "NOU RÈCORD!" si n'hi ha, i confeti si has guanyat. Els botons els afegeixen GameOver i Win.
public class EndScreenFx : MonoBehaviour
{
    class Row
    {
        public TextMeshProUGUI label, value;
        public float target;     // el número final (si en té)
        public string prefix, suffix, fixedText;
        public float at;         // quan surt
    }

    private bool win;
    private float t;
    private TextMeshProUGUI title, record;
    private Image portrait;
    private RawImage stripes;
    private readonly List<Row> rows = new List<Row>();
    private readonly List<RectTransform> confetti = new List<RectTransform>();
    private readonly List<Vector3> confettiVel = new List<Vector3>();
    private RectTransform root;
    private int ticked;

    public static void Build(Canvas canvas, bool win)
    {
        if (canvas == null) return;
        // els textos que ja té l'escena (el títol i el resultat vells) s'amaguen: els fem nous
        foreach (string n in new[] { "TitleText", "resultText" })
        {
            TextMeshProUGUI old = EndScreenUI.FindText(n);
            if (old != null) old.gameObject.SetActive(false);
        }
        EndScreenFx fx = canvas.gameObject.AddComponent<EndScreenFx>();
        fx.win = win;
        fx.Create((RectTransform)canvas.transform);
    }

    void Create(RectTransform canvasRt)
    {
        root = MenuScreens.Rect(canvasRt, "PantallaFinal", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.SetSiblingIndex(1); // per sobre del fons de l'escena, per sota dels botons

        Color tint = win ? new Color(0.25f, 0.17f, 0.05f, 0.78f) : new Color(0.22f, 0.04f, 0.06f, 0.8f);
        Image dim = root.gameObject.AddComponent<Image>();
        dim.color = tint;
        dim.raycastTarget = false;

        RectTransform st = MenuScreens.Rect(root, "Ratlles", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        stripes = st.gameObject.AddComponent<RawImage>();
        stripes.texture = Stripes();
        stripes.uvRect = new Rect(0f, 0f, 1920f / 120f, 1080f / 120f);
        stripes.color = new Color(1f, 1f, 1f, 0.05f);
        stripes.raycastTarget = false;

        // títol i subtítol
        bool rush = BossRush.Active;
        string titleText = rush ? (win ? "HAS VENÇUT TOTS ELS CAPS!" : "FI DELS CAPS SEGUITS")
                                : win ? "HAS SOBREVISCUT AL SERVEI!" : (RunStats.endless ? "FI DEL MODE INFINIT" : "FI DEL JOC");
        Color titleCol = win ? UIStyle.Gold : new Color(1f, 0.42f, 0.38f);
        title = MenuScreens.Text(root, titleText, 100f, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(1700f, 130f), titleCol, true);
        UIStyle.Outline(title, 0.25f);
        string sub = rush ? (win ? "Els " + BossRush.Bosses + " caps, l'un darrere l'altre, en " + BossRush.Clock(BossRush.Elapsed) + "."
                                 : "Has caigut contra el cap " + Mathf.Min(BossRush.Beaten + 1, BossRush.Bosses) + " de " + BossRush.Bosses + ".")
                   : win ? "Has completat les " + RunStats.wave + " onades. El restaurant ja pot tancar."
                         : "T'han fet fora a l'onada " + RunStats.wave + ". Els clients guanyen... per ara.";
        MenuScreens.Text(root, sub, 34f, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(1500f, 50f), new Color(1f, 0.92f, 0.82f), false).fontStyle = FontStyles.Italic;

        // el retrat del xef
        ChefRoster.Chef chef = ChefRoster.All[Progression.IsChefUnlocked(ChefRoster.Selected) ? ChefRoster.Selected : 0];
        RectTransform prt = MenuScreens.Rect(root, "Xef", new Vector2(0.22f, 0.48f), new Vector2(0.22f, 0.48f), Vector2.zero, Vector2.zero);
        prt.sizeDelta = new Vector2(340f, 340f);
        portrait = prt.gameObject.AddComponent<Image>();
        portrait.sprite = ChefRoster.LoadSprite(chef, "Down");
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portrait.enabled = portrait.sprite != null;
        if (!win)
        {
            prt.localRotation = Quaternion.Euler(0f, 0f, 78f); // caigut
            portrait.color = new Color(0.85f, 0.7f, 0.7f);
        }

        // la targeta amb les estadístiques
        Image card = UIStyle.Panel(root, "Targeta", new Vector2(0.6f, 0.48f), new Vector2(0.6f, 0.48f), Vector2.zero, Vector2.zero, false);
        RectTransform crt = card.rectTransform;
        crt.sizeDelta = new Vector2(860f, 400f);
        float y = 140f;
        if (rush)
        {
            AddRow(crt, "Caps vençuts", BossRush.Beaten, "", " / " + BossRush.Bosses, null, ref y, 0.5f);
            AddRow(crt, "Temps", 0, "", "", BossRush.Clock(BossRush.Elapsed), ref y, 0.75f);
            AddRow(crt, "Rècord", 0, "", "", BossRush.BestTime > 0f ? BossRush.Clock(BossRush.BestTime) : BossRush.BestCount + " / " + BossRush.Bosses + " caps", ref y, 1.0f);
        }
        else
        {
            AddRow(crt, "Onada", RunStats.wave, "", "", null, ref y, 0.5f);
            AddRow(crt, "Punts", RunStats.points, "", "", null, ref y, 0.75f);
            AddRow(crt, "Millor ratxa", RunStats.maxCombo, "", " clients", null, ref y, 1.0f);
        }
        AddRow(crt, "Propines", Progression.LastRunTips, "+", "  (en tens " + Progression.Tips + ")", null, ref y, 1.25f);
        AddRow(crt, "Xef", 0, "", "", chef.name + "  ·  " + Progression.DifficultyName(Progression.Difficulty), ref y, 1.5f);

        if (rush ? BossRush.NewBest : RunStats.newRecord)
        {
            record = MenuScreens.Text(crt, "NOU RÈCORD!", 46f, new Vector2(1f, 1f), new Vector2(-40f, 10f), new Vector2(420f, 70f), UIStyle.Gold, true);
            record.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            record.alpha = 0f;
        }

        if (win) for (int i = 0; i < 70; i++) SpawnConfetti(true);
    }

    void AddRow(RectTransform card, string label, float value, string prefix, string suffix, string fixedText, ref float y, float at)
    {
        Row r = new Row { target = value, prefix = prefix, suffix = suffix, fixedText = fixedText, at = at };
        r.label = MenuScreens.Text(card, label, 36f, new Vector2(0f, 0.5f), new Vector2(230f, y), new Vector2(380f, 56f), new Color(1f, 0.85f, 0.6f), false);
        r.label.alignment = TextAlignmentOptions.Left;
        r.value = MenuScreens.Text(card, fixedText ?? "", 40f, new Vector2(1f, 0.5f), new Vector2(-240f, y), new Vector2(420f, 56f), Color.white, true);
        r.value.alignment = TextAlignmentOptions.Right;
        r.label.alpha = r.value.alpha = 0f;
        rows.Add(r);
        y -= 70f;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;

        // el títol cau de cop
        float kt = Mathf.Clamp01(t / 0.3f);
        title.transform.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, 1f - (1f - kt) * (1f - kt));
        title.alpha = kt;

        Rect uv = stripes.uvRect;
        uv.x += Time.unscaledDeltaTime * 0.15f;
        stripes.uvRect = uv;

        // el xef: content i saltant si has guanyat; caigut i quiet si has perdut
        if (win && portrait != null)
            portrait.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 4f)) * 24f);

        // les files surten una a una; els números compten cap amunt
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            float k = Mathf.Clamp01((t - r.at) / 0.25f);
            r.label.alpha = r.value.alpha = k;
            r.label.rectTransform.localScale = r.value.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, k);
            if (k > 0f && i >= ticked) { ticked = i + 1; AudioManager.Play("ui_click"); }
            if (r.fixedText == null)
            {
                float c = Mathf.Clamp01((t - r.at) / 0.6f);
                r.value.text = r.prefix + Mathf.RoundToInt(r.target * c * (2f - c)) + r.suffix;
            }
        }

        if (record != null)
        {
            float k = Mathf.Clamp01((t - 2f) / 0.2f);
            record.alpha = k;
            record.transform.localScale = Vector3.one * (Mathf.Lerp(2f, 1f, k) + (k >= 1f ? Mathf.Sin(t * 6f) * 0.05f : 0f));
        }

        // confeti que cau (i torna a sortir per dalt)
        for (int i = 0; i < confetti.Count; i++)
        {
            RectTransform c = confetti[i];
            Vector3 v = confettiVel[i];
            c.anchoredPosition += new Vector2(v.x + Mathf.Sin(t * 2f + i) * 30f, v.y) * Time.unscaledDeltaTime;
            c.localRotation = Quaternion.Euler(0f, 0f, c.localEulerAngles.z + v.z * Time.unscaledDeltaTime);
            if (c.anchoredPosition.y < -40f) c.anchoredPosition = new Vector2(Random.Range(0f, 1920f), 1120f);
        }
    }

    void SpawnConfetti(bool anywhere)
    {
        RectTransform c = MenuScreens.Rect(root, "Confeti", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        c.sizeDelta = new Vector2(Random.Range(10f, 18f), Random.Range(16f, 26f));
        c.anchoredPosition = new Vector2(Random.Range(0f, 1920f), anywhere ? Random.Range(0f, 1100f) : 1120f);
        Image img = c.gameObject.AddComponent<Image>();
        Color[] cols = { UIStyle.Gold, new Color(0.9f, 0.3f, 0.3f), new Color(0.4f, 0.8f, 0.45f), new Color(0.4f, 0.6f, 1f), Color.white };
        img.color = cols[Random.Range(0, cols.Length)];
        img.raycastTarget = false;
        confetti.Add(c);
        confettiVel.Add(new Vector3(Random.Range(-40f, 40f), Random.Range(-260f, -140f), Random.Range(-360f, 360f)));
    }

    static Texture2D stripeTex;
    static Texture2D Stripes()
    {
        if (stripeTex != null) return stripeTex;
        int n = 32;
        stripeTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        stripeTex.wrapMode = TextureWrapMode.Repeat;
        stripeTex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = ((x + y) % n) < n / 2 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        stripeTex.SetPixels32(px);
        stripeTex.Apply();
        return stripeTex;
    }
}
