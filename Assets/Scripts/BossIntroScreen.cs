using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  PRESENTACIÓ DELS CAPS (cartell de cinema dels anys 30)
// =====================================================================
// 1. el compte enrere de les pel·lícules antigues: 3, 2, 1 (amb l'agulla que gira)
// 2. el cartell art déco cau des de dalt i s'atura de cop: cop de càmera, flaix i vibració
// 3. "ESTEM TANCATS PRESENTA", el cap dins d'un medalló amb raigs de sol al darrere,
//    el seu nom, el títol de la seva "pel·lícula", la frase lletra a lletra i un consell
// 4. el cartell puja com un fotograma que avança i comença el combat
// Tot tremola una mica, com la pel·lícula al projector. Es pot saltar amb A / Enter / Espai / clic.
// El joc està aturat mentre dura.
public class BossIntroScreen : MonoBehaviour
{
    const float TypeSpeed = 38f;   // lletres per segon de la frase
    const float Outro = 0.4f;      // durada de la sortida
    const float CountTime = 0.32f; // cada número del compte enrere

    static readonly Color Gold = new Color(0.92f, 0.74f, 0.36f);
    static readonly Color Paper = new Color(0.17f, 0.11f, 0.08f);

    private RectTransform canvasRt, card, cameo, rays, leader;
    private Image dim, flash, flicker, sweep;
    private TextMeshProUGUI presents, nameText, filmPre, filmTitle, quote, hint, footer, skip, countText;
    private string fullQuote;
    private Vector2 jitter;
    private float nextJitter;

    // el color i el consell de cada cap
    public static Color ColorFor(BossType t)
    {
        switch (t)
        {
            case BossType.Critic: return new Color(0.25f, 0.32f, 0.62f);
            case BossType.Borratxo: return new Color(0.55f, 0.32f, 0.16f);
            case BossType.Cuiner: return new Color(0.70f, 0.14f, 0.18f);
            case BossType.Pallas: return new Color(0.42f, 0.22f, 0.55f);
            case BossType.Inspector: return new Color(0.16f, 0.45f, 0.36f);
        }
        return new Color(0.5f, 0.2f, 0.2f);
    }

    public static string HintFor(BossType t)
    {
        switch (t)
        {
            case BossType.Critic: return "Compte amb el cop de safata quan s'acosta i amb les ràfegues de plats";
            case BossType.Borratxo: return "Carrega en línia recta: aparta't i ataca'l quan quedi estabornit";
            case BossType.Cuiner: return "Fa sortir clients i llença ganivets en ventall: no et quedis quiet";
            case BossType.Pallas: return "Es teletransporta i deixa bombes al terra: mira on trepitges";
            case BossType.Inspector: return "Dispara en espiral i s'enfada quan està tocada";
        }
        return "";
    }

    // el títol de la "pel·lícula" de cada cap
    public static string FilmTitleFor(BossType t)
    {
        switch (t)
        {
            case BossType.Critic: return "LA NOTA FINAL";
            case BossType.Borratxo: return "L'ÚLTIMA RONDA";
            case BossType.Cuiner: return "CUINA SALVATGE";
            case BossType.Pallas: return "RESERVAT PER A TU";
            case BossType.Inspector: return "TANCAT PER INSPECCIÓ";
        }
        return "EL SERVEI DE LA NIT";
    }

    // la presentació sencera; acaba quan el combat ha de començar
    public static IEnumerator Play(BossInfo info, Sprite sprite, int act, int wave, string tagLine = null)
    {
        BossIntroScreen s = new GameObject("PresentacioCap").AddComponent<BossIntroScreen>();
        s.Build(info, sprite, act, wave, tagLine);
        yield return s.Run();
        Destroy(s.gameObject);
    }

    IEnumerator Run()
    {
        bool wasPaused = Time.timeScale == 0f;
        Time.timeScale = 0f;
        GamepadInput.CustomScreens++;

        // ---- 1. el compte enrere
        bool skipped = false;
        int shown = 0;
        for (float c = 0f; c < CountTime * 3f; c += Time.unscaledDeltaTime)
        {
            if (c > 0.15f && SkipPressed()) { skipped = true; break; }
            int n = 3 - Mathf.Min(2, Mathf.FloorToInt(c / CountTime));
            if (n != shown) { shown = n; AudioManager.Play("ui_click", 1f, 0.7f + (3 - n) * 0.15f); }
            countText.text = n.ToString();
            sweep.fillAmount = Mathf.Repeat(c / CountTime, 1f);
            dim.color = new Color(0f, 0f, 0f, 0.7f * Mathf.Clamp01(c / 0.15f));
            Jitter(leader, Vector2.zero);
            yield return null;
        }
        leader.gameObject.SetActive(false);

        // ---- 2 i 3. el cartell
        AudioManager.Play("boss_intro");
        float typeStart = 0.95f;
        float end = Mathf.Max(3.8f, typeStart + fullQuote.Length / TypeSpeed + 1.4f);
        bool impact = false;
        float t = skipped ? end : 0f;

        while (t < end)
        {
            t += Time.unscaledDeltaTime;
            if (t > 0.35f && SkipPressed()) t = end; // es pot saltar

            Animate(t);
            if (!impact && t >= 0.3f)
            {
                impact = true;
                CameraFX.Shake(0.25f, 0.25f);
                GamepadInput.Rumble(0.7f, 0.5f, 0.25f);
                AudioManager.Play("table", 1.2f, 0.7f); // un cop sord
            }

            int chars = Mathf.Clamp(Mathf.FloorToInt((t - typeStart) * TypeSpeed), 0, fullQuote.Length);
            quote.text = fullQuote.Substring(0, chars);
            skip.alpha = Mathf.Clamp01((t - 0.5f) * 3f) * 0.7f;
            yield return null;
        }
        Animate(end);
        quote.text = fullQuote;

        // ---- 4. el cartell puja com un fotograma que avança
        for (float o = 0f; o < Outro; o += Time.unscaledDeltaTime)
        {
            float k = EaseIn(o / Outro);
            card.anchoredPosition = new Vector2(0f, 1300f * k);
            dim.color = new Color(0f, 0f, 0f, 0.7f * (1f - k));
            yield return null;
        }

        GamepadInput.CustomScreens = Mathf.Max(0, GamepadInput.CustomScreens - 1);
        if (!wasPaused) Time.timeScale = 1f;
    }

    void Animate(float t)
    {
        dim.color = new Color(0f, 0f, 0f, 0.7f);

        // el cartell cau des de dalt i s'atura amb un petit rebot
        float kc = EaseOutBack(Mathf.Clamp01(t / 0.3f));
        Jitter(card, new Vector2(0f, Mathf.Lerp(1300f, 0f, kc)));

        // flaix en aturar-se, i el parpelleig del projector
        float f = t - 0.3f;
        flash.color = new Color(1f, 0.95f, 0.85f, f >= 0f && f < 0.22f ? 0.6f * (1f - f / 0.22f) : 0f);

        // el medalló apareix d'una revolada i els raigs giren a poc a poc
        float km = EaseOutBack(Mathf.Clamp01((t - 0.25f) / 0.3f));
        cameo.localScale = Vector3.one * km;
        rays.localRotation = Quaternion.Euler(0f, 0f, -t * 8f);
        rays.localScale = Vector3.one * Mathf.Clamp01((t - 0.3f) / 0.4f);

        presents.alpha = Mathf.Clamp01((t - 0.2f) / 0.25f);
        // el nom cau de cop (gran -> normal)
        float kn = Mathf.Clamp01((t - 0.45f) / 0.22f);
        nameText.alpha = kn;
        nameText.transform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, EaseOut(kn));
        filmPre.alpha = Mathf.Clamp01((t - 0.7f) / 0.2f);
        filmTitle.alpha = Mathf.Clamp01((t - 0.75f) / 0.25f);
        filmTitle.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, EaseOut(Mathf.Clamp01((t - 0.75f) / 0.25f)));
        hint.alpha = Mathf.Clamp01((t - 1.5f) / 0.3f);
        footer.alpha = Mathf.Clamp01((t - 0.6f) / 0.3f) * 0.85f;
    }

    // tot tremola una mica, com la pel·lícula al projector (12 vegades per segon)
    void Jitter(RectTransform rt, Vector2 basePos)
    {
        if (Time.unscaledTime > nextJitter)
        {
            nextJitter = Time.unscaledTime + 1f / 12f;
            jitter = new Vector2(Random.Range(-2f, 2f), Random.Range(-3f, 3f));
            flicker.color = new Color(0f, 0f, 0f, Random.Range(0f, 0.07f));
        }
        rt.anchoredPosition = basePos + jitter;
    }

    bool SkipPressed()
    {
        Keyboard kb = Keyboard.current;
        Gamepad pad = GamepadInput.ActivePad;
        Mouse ms = Mouse.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
        if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
        if (ms != null && ms.leftButton.wasPressedThisFrame) return true;
        return false;
    }

    static float EaseOut(float x) { return 1f - (1f - x) * (1f - x) * (1f - x); }
    static float EaseIn(float x) { return x * x * x; }
    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float k = x - 1f;
        return 1f + c3 * k * k * k + c1 * k * k;
    }

    // ---------- LA INTERFÍCIE ----------

    void Build(BossInfo info, Sprite sprite, int act, int wave, string tagLine)
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRt = canvasGo.GetComponent<RectTransform>();

        Color accent = ColorFor(info.type);
        bool furious = info.name != null && info.name.Contains("ENFURISMA"); // fent hores extres
        Color frame = furious ? new Color(0.95f, 0.35f, 0.25f) : Gold;

        dim = Box(canvasRt, "Fosc", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ---- el compte enrere (la "cua" de les pel·lícules)
        leader = new GameObject("CompteEnrere", typeof(RectTransform)).GetComponent<RectTransform>();
        leader.SetParent(canvasRt, false);
        leader.anchorMin = leader.anchorMax = new Vector2(0.5f, 0.5f);
        leader.sizeDelta = new Vector2(1920f, 1080f);
        Color leaderInk = new Color(0.85f, 0.78f, 0.62f);
        Box(leader, "Fons", new Color(0.30f, 0.24f, 0.18f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image ring1 = Centered(leader, "Anell1", leaderInk, Vector2.zero, new Vector2(620f, 620f)); ring1.sprite = Circle();
        Image ring1b = Centered(leader, "Anell1b", new Color(0.30f, 0.24f, 0.18f), Vector2.zero, new Vector2(600f, 600f)); ring1b.sprite = Circle();
        sweep = Centered(leader, "Agulla", new Color(0.18f, 0.14f, 0.10f), Vector2.zero, new Vector2(600f, 600f));
        sweep.sprite = Circle();
        sweep.type = Image.Type.Filled;
        sweep.fillMethod = Image.FillMethod.Radial360;
        sweep.fillOrigin = (int)Image.Origin360.Top;
        sweep.fillClockwise = true;
        // la creu del mig, per sobre de l'agulla
        Centered(leader, "Ratlla1", leaderInk, Vector2.zero, new Vector2(1920f, 4f));
        Centered(leader, "Ratlla2", leaderInk, Vector2.zero, new Vector2(4f, 1080f));
        countText = Text(leader, "3", 300f, Vector2.zero, new Vector2(500f, 360f), new Color(0.12f, 0.09f, 0.07f), true);
        countText.alignment = TextAlignmentOptions.Center;
        countText.outlineWidth = 0f;

        // ---- el cartell
        card = new GameObject("Cartell", typeof(RectTransform)).GetComponent<RectTransform>();
        card.SetParent(canvasRt, false);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(1240f, 900f);
        card.anchoredPosition = new Vector2(0f, 1300f);
        Image cardBg = Box(card, "Paper", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        cardBg.raycastTarget = false;

        // els raigs de sol (retallats dins del cartell)
        RectTransform clip = Box(card, "Retall", Color.clear, Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f)).rectTransform;
        clip.gameObject.AddComponent<RectMask2D>();
        rays = new GameObject("Raigs", typeof(RectTransform)).GetComponent<RectTransform>();
        rays.SetParent(clip, false);
        rays.anchorMin = rays.anchorMax = new Vector2(0.5f, 0.5f);
        rays.anchoredPosition = new Vector2(0f, 150f);
        for (int i = 0; i < 18; i++)
        {
            Image r = Centered(rays, "Raig", new Color(accent.r, accent.g, accent.b, 0.35f), Vector2.zero, new Vector2(46f, 1100f));
            r.rectTransform.pivot = new Vector2(0.5f, 0f);
            r.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 20f);
        }

        // el marc art déco: doble línia daurada i diamants a les cantonades
        Frame(card, frame, 0f, 8f);
        Frame(card, frame, 24f, 3f);
        foreach (Vector2 corner in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
        {
            Image d = Centered(card, "Diamant", frame, new Vector2(corner.x * (620f - 25f), corner.y * (450f - 25f)), new Vector2(30f, 30f));
            d.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image d2 = Centered(card, "DiamantDins", Paper, new Vector2(corner.x * (620f - 25f), corner.y * (450f - 25f)), new Vector2(14f, 14f));
            d2.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        // un parell de franges decoratives a dalt i a baix
        foreach (float y in new[] { 300f, -360f })
        {
            Centered(card, "Franja", frame, new Vector2(-330f, y), new Vector2(260f, 3f));
            Centered(card, "Franja", frame, new Vector2(330f, y), new Vector2(260f, 3f));
        }

        // "ESTEM TANCATS PRESENTA"
        presents = Text(card, "ESTEM TANCATS  PRESENTA", 34f, new Vector2(0f, 385f), new Vector2(1000f, 50f), frame, true);
        presents.alignment = TextAlignmentOptions.Center;
        presents.characterSpacing = 12f;

        // el medalló amb el cap
        cameo = new GameObject("Medallo", typeof(RectTransform)).GetComponent<RectTransform>();
        cameo.SetParent(card, false);
        cameo.anchorMin = cameo.anchorMax = new Vector2(0.5f, 0.5f);
        cameo.anchoredPosition = new Vector2(0f, 150f);
        Centered(cameo, "Vora", frame, Vector2.zero, new Vector2(330f, 330f)).sprite = Circle();
        Centered(cameo, "Vora2", Paper, Vector2.zero, new Vector2(316f, 316f)).sprite = Circle();
        Centered(cameo, "Fons", Color.Lerp(accent, Paper, 0.35f), Vector2.zero, new Vector2(300f, 300f)).sprite = Circle();
        Image portrait = Centered(cameo, "Retrat", Color.white, new Vector2(0f, -6f), new Vector2(330f, 330f));
        portrait.sprite = sprite;
        portrait.preserveAspect = true;
        portrait.enabled = sprite != null;

        // el nom i la pel·lícula
        nameText = Text(card, info.name, 84f, new Vector2(0f, -70f), new Vector2(1120f, 110f), Color.white, true);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.outlineWidth = 0.22f;
        nameText.outlineColor = new Color32(25, 15, 20, 255);
        filmPre = Text(card, "a la pel·lícula", 28f, new Vector2(0f, -132f), new Vector2(600f, 40f), new Color(1f, 0.92f, 0.8f), false);
        filmPre.alignment = TextAlignmentOptions.Center;
        filmPre.fontStyle = FontStyles.Italic;
        string film = FilmTitleFor(info.type) + (furious ? " II: LA REVENJA" : "");
        filmTitle = Text(card, "«" + film + "»", 58f, new Vector2(0f, -190f), new Vector2(1100f, 76f), frame, true);
        filmTitle.alignment = TextAlignmentOptions.Center;
        filmTitle.characterSpacing = 4f;

        fullQuote = info.subtitle ?? "";
        quote = Text(card, "", 30f, new Vector2(0f, -262f), new Vector2(1100f, 48f), new Color(1f, 0.95f, 0.88f), false);
        quote.alignment = TextAlignmentOptions.Center;
        quote.fontStyle = FontStyles.Italic;
        hint = Text(card, HintFor(info.type), 24f, new Vector2(0f, -310f), new Vector2(1100f, 40f), new Color(1f, 0.85f, 0.45f), false);
        hint.alignment = TextAlignmentOptions.Center;
        string tagText = tagLine ?? "— CAP DE L'ACTE " + act + " · ONADA " + wave + " —";
        footer = Text(card, tagText, 22f, new Vector2(0f, -400f), new Vector2(900f, 34f), frame, true);
        footer.alignment = TextAlignmentOptions.Center;
        footer.characterSpacing = 6f;
        foreach (TextMeshProUGUI tx in new[] { presents, nameText, filmPre, filmTitle, hint, footer }) tx.alpha = 0f;
        cameo.localScale = Vector3.zero;

        // per sobre de tot: el parpelleig del projector, el flaix i el botó de saltar
        flicker = Box(canvasRt, "Parpelleig", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        flash = Box(canvasRt, "Flaix", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        string skipKey = GamepadInput.UsingGamepad ? GamepadInput.South() : "Enter";
        skip = Text(canvasRt, skipKey + " · Saltar", 24f, Vector2.zero, new Vector2(300f, 40f), new Color(1f, 1f, 1f, 0.7f), false);
        RectTransform skrt = skip.rectTransform;
        skrt.anchorMin = skrt.anchorMax = new Vector2(1f, 0f);
        skrt.anchoredPosition = new Vector2(-170f, 60f);
        skip.alpha = 0f;
    }

    // un marc de línies (inset des de la vora, gruix)
    static void Frame(RectTransform parent, Color c, float inset, float w)
    {
        Box(parent, "MarcDalt", c, new Vector2(0f, 1f), Vector2.one, new Vector2(inset, -inset - w), new Vector2(-inset, -inset));
        Box(parent, "MarcBaix", c, Vector2.zero, new Vector2(1f, 0f), new Vector2(inset, inset), new Vector2(-inset, inset + w));
        Box(parent, "MarcEsq", c, Vector2.zero, new Vector2(0f, 1f), new Vector2(inset, inset), new Vector2(inset + w, -inset));
        Box(parent, "MarcDreta", c, new Vector2(1f, 0f), Vector2.one, new Vector2(-inset - w, inset), new Vector2(-inset, -inset));
    }

    static Image Centered(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        Image img = Box(parent, name, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        img.rectTransform.sizeDelta = size;
        img.rectTransform.anchoredPosition = pos;
        return img;
    }

    static Sprite circle;
    static Sprite Circle()
    {
        if (circle != null) return circle;
        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                byte a = (byte)(Mathf.Clamp01(r - d) * 255);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }

    static Image Box(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 pos, Vector2 box, Color color, bool bold)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = size * 0.55f;
        t.fontSizeMax = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Left;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }
}
