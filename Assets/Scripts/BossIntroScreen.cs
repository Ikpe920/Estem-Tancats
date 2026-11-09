using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  PRESENTACIÓ DELS CAPS (estil "versus")
// =====================================================================
// 1. la pantalla s'enfosqueix i entren dues franges negres de cinema
// 2. una franja diagonal del color del cap travessa la pantalla (amb ratlles que es mouen)
// 3. el retrat gran del cap entra de cop des de la dreta: flaix, cop de càmera i vibració
// 4. el nom cau de cop, amb l'etiqueta "CAP DE L'ACTE N · ONADA X"
// 5. la frase s'escriu lletra a lletra, i surt un consell per guanyar-lo
// 6. tot surt de la pantalla i comença el combat
// Es pot saltar amb A / Enter / Espai / clic. El joc està aturat mentre dura.
public class BossIntroScreen : MonoBehaviour
{
    const float TypeSpeed = 38f;   // lletres per segon de la frase
    const float Outro = 0.45f;     // durada de la sortida

    private RectTransform canvasRt;
    private Image dim, flash, barTop, barBottom, portrait, portraitShadow;
    private RectTransform band;
    private RawImage stripes;
    private TextMeshProUGUI tag, nameText, quote, hint, skip;
    private string fullQuote;
    private bool skipRequested;

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

    // la presentació sencera; acaba quan el combat ha de començar
    public static IEnumerator Play(BossInfo info, Sprite sprite, int act, int wave, string tagLine = null)
    {
        BossIntroScreen s = new GameObject("PresentacioCap").AddComponent<BossIntroScreen>();
        s.Build(info, sprite, act, wave, tagLine);
        yield return s.Run(ColorFor(info.type));
        Destroy(s.gameObject);
    }

    IEnumerator Run(Color color)
    {
        bool wasPaused = Time.timeScale == 0f;
        Time.timeScale = 0f;
        GamepadInput.CustomScreens++;
        AudioManager.Play("boss_intro");

        float typeStart = 0.85f;
        float end = Mathf.Max(3.6f, typeStart + fullQuote.Length / TypeSpeed + 1.4f);
        bool impact = false;
        float t = 0f;

        while (t < end)
        {
            t += Time.unscaledDeltaTime;
            if (t > 0.6f && SkipPressed()) t = end; // es pot saltar

            Animate(t, color);

            if (!impact && t >= 0.6f)
            {
                impact = true;
                CameraFX.Shake(0.25f, 0.25f);
                GamepadInput.Rumble(0.7f, 0.5f, 0.25f);
                AudioManager.Play("table", 1.2f, 0.7f); // un cop sord
            }

            // la frase, lletra a lletra
            int chars = Mathf.Clamp(Mathf.FloorToInt((t - typeStart) * TypeSpeed), 0, fullQuote.Length);
            quote.text = fullQuote.Substring(0, chars);
            skip.alpha = Mathf.Clamp01((t - 0.8f) * 3f) * 0.7f;
            yield return null;
        }

        // sortida: tot marxa de la pantalla
        quote.text = fullQuote;
        for (float o = 0f; o < Outro; o += Time.unscaledDeltaTime)
        {
            float k = EaseIn(o / Outro);
            band.anchoredPosition = new Vector2(-2600f * k, 0f);
            portrait.rectTransform.anchoredPosition = new Vector2(480f + 1500f * k, 10f);
            portraitShadow.rectTransform.anchoredPosition = portrait.rectTransform.anchoredPosition + new Vector2(14f, -14f);
            SetBars(1f - k);
            dim.color = new Color(0f, 0f, 0f, 0.6f * (1f - k));
            yield return null;
        }

        GamepadInput.CustomScreens = Mathf.Max(0, GamepadInput.CustomScreens - 1);
        if (!wasPaused) Time.timeScale = 1f;
    }

    void Animate(float t, Color color)
    {
        dim.color = new Color(0f, 0f, 0f, 0.6f * Mathf.Clamp01(t / 0.25f));
        SetBars(EaseOut(Mathf.Clamp01(t / 0.3f)));

        // la franja diagonal entra des de la dreta
        float kb = EaseOut(Mathf.Clamp01((t - 0.15f) / 0.3f));
        band.anchoredPosition = new Vector2(2600f * (1f - kb), 0f);
        band.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, kb), 1f);
        Rect uv = stripes.uvRect;
        uv.x = -t * 0.6f; // les ratlles es mouen
        stripes.uvRect = uv;

        // el retrat entra de cop, amb una mica de rebot, i després respira
        float kp = EaseOutBack(Mathf.Clamp01((t - 0.3f) / 0.3f));
        float bob = t > 0.6f ? Mathf.Sin((t - 0.6f) * 3f) * 8f : 0f;
        portrait.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1500f, 480f, kp), 10f + bob);
        portraitShadow.rectTransform.anchoredPosition = portrait.rectTransform.anchoredPosition + new Vector2(14f, -14f);

        // flaix blanc en arribar
        float f = t - 0.6f;
        flash.color = new Color(1f, 1f, 1f, f >= 0f && f < 0.25f ? 0.75f * (1f - f / 0.25f) : 0f);

        // el nom cau de cop (gran -> normal)
        float kn = Mathf.Clamp01((t - 0.5f) / 0.22f);
        nameText.alpha = kn;
        nameText.transform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, EaseOut(kn));
        tag.alpha = Mathf.Clamp01((t - 0.6f) / 0.2f);
        hint.alpha = Mathf.Clamp01((t - 1.4f) / 0.3f);
    }

    void SetBars(float k)
    {
        float h = 130f;
        barTop.rectTransform.anchoredPosition = new Vector2(0f, h * (1f - k));
        barBottom.rectTransform.anchoredPosition = new Vector2(0f, -h * (1f - k));
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

        Color color = ColorFor(info.type);

        dim = Box(canvasRt, "Fosc", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // la franja diagonal, amb les ratlles i dues vores clares
        band = new GameObject("Franja", typeof(RectTransform)).GetComponent<RectTransform>();
        band.SetParent(canvasRt, false);
        band.anchorMin = band.anchorMax = new Vector2(0.5f, 0.5f);
        band.sizeDelta = new Vector2(2700f, 340f);
        band.localRotation = Quaternion.Euler(0f, 0f, -6f);
        Box(band, "Color", color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject sg = new GameObject("Ratlles", typeof(RectTransform), typeof(RawImage));
        sg.transform.SetParent(band, false);
        RectTransform srt = sg.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = srt.offsetMax = Vector2.zero;
        stripes = sg.GetComponent<RawImage>();
        stripes.texture = StripeTexture();
        stripes.uvRect = new Rect(0f, 0f, 2700f / 96f, 340f / 96f);
        stripes.color = new Color(1f, 1f, 1f, 0.12f);
        stripes.raycastTarget = false;
        Color edge = Color.Lerp(color, Color.white, 0.55f);
        Box(band, "VoraDalt", edge, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -10f), Vector2.zero);
        Box(band, "VoraBaix", edge, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 10f));

        // el retrat (amb ombra)
        portraitShadow = Box(canvasRt, "Ombra", new Color(0f, 0f, 0f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        portrait = Box(canvasRt, "Retrat", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        foreach (Image img in new[] { portraitShadow, portrait })
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(560f, 560f);
            img.enabled = sprite != null;
        }

        // textos (a l'esquerra, a sobre de la franja)
        string tagText = tagLine ?? "— CAP DE L'ACTE " + act + " · ONADA " + wave + " —"; // (la font no té estrelles)
        tag = Text(canvasRt, tagText, 30f, new Vector2(-330f, 125f), new Vector2(1000f, 50f), new Color(1f, 0.85f, 0.4f), true);
        nameText = Text(canvasRt, info.name, 96f, new Vector2(-330f, 40f), new Vector2(1150f, 130f), Color.white, true);
        nameText.outlineWidth = 0.22f;
        nameText.outlineColor = new Color32(25, 15, 20, 255);
        fullQuote = "«" + info.subtitle + "»";
        quote = Text(canvasRt, "", 36f, new Vector2(-330f, -50f), new Vector2(1100f, 60f), new Color(1f, 0.95f, 0.88f), false);
        quote.fontStyle = FontStyles.Italic;
        hint = Text(canvasRt, HintFor(info.type), 27f, new Vector2(-330f, -118f), new Vector2(1100f, 50f), new Color(1f, 0.85f, 0.45f), false);
        foreach (TextMeshProUGUI t in new[] { tag, nameText, hint }) t.alpha = 0f;

        // franges de cinema i el flaix (per sobre de tot)
        barTop = Box(canvasRt, "FranjaDalt", Color.black, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -130f), Vector2.zero);
        barBottom = Box(canvasRt, "FranjaBaix", Color.black, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 130f));
        barTop.rectTransform.pivot = new Vector2(0.5f, 1f);
        barBottom.rectTransform.pivot = new Vector2(0.5f, 0f);
        SetBars(0f);

        string skipKey = GamepadInput.UsingGamepad ? GamepadInput.South() : "Enter";
        skip = Text(canvasRt, skipKey + " · Saltar", 24f, Vector2.zero, new Vector2(300f, 40f), new Color(1f, 1f, 1f, 0.7f), false);
        RectTransform skrt = skip.rectTransform;
        skrt.anchorMin = skrt.anchorMax = new Vector2(1f, 0f);
        skrt.anchoredPosition = new Vector2(-170f, 60f);
        skip.alpha = 0f;

        flash = Box(canvasRt, "Flaix", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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

    // ratlles diagonals blanques (es repeteixen; la franja les fa lliscar)
    static Texture2D stripeTex;
    static Texture2D StripeTexture()
    {
        if (stripeTex != null) return stripeTex;
        int n = 32;
        stripeTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        stripeTex.wrapMode = TextureWrapMode.Repeat;
        stripeTex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = ((x + y) % n) < n / 3 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        stripeTex.SetPixels32(px);
        stripeTex.Apply();
        return stripeTex;
    }
}
