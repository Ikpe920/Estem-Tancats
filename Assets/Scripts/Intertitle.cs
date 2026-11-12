using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  CARTELLS DE PEL·LÍCULA MUDA ("intertítols")
// =====================================================================
// Pantalla negra amb un marc decorat i el text en blanc trencat, com a les pel·lícules dels anys 30.
// Surten en començar la partida, en canviar de sala, a les hores extres, en morir i en guanyar.
//   yield return Intertitle.Show("Mentrestant, a la cuina...");   // espera que acabi
//   Intertitle.ShowAsync("...", fin: true);                        // no espera (pantalles finals)
// No toca el temps del joc (qui el crida decideix si l'atura). Es pot saltar amb A / Enter / Espai / clic.
public class Intertitle : MonoBehaviour
{
    const float FadeIn = 0.25f;
    const float FadeOut = 0.3f;

    static readonly Color Cream = new Color(0.93f, 0.89f, 0.80f);

    private CanvasGroup group;
    private RectTransform content;
    private Image flicker;
    private float hold;
    private Vector2 jitter;
    private float nextJitter;

    // ---------- ELS TEXTOS ----------

    public static string Opening()
    {
        return "El restaurant ja havia tancat...\nperò ningú no va avisar els clients.";
    }

    public static string ActIntro(int act, string actName, bool overtime, bool firstOvertime, int hour = 0)
    {
        if (firstOvertime) return "Quan tothom es pensava que ja s'havia acabat...\nel xef va haver de fer hores extres.";
        if (overtime)
        {
            string room = "a " + (actName ?? "").ToLowerInvariant();
            if (hour < 5) return HourWords(hour) + " de la matinada,\n" + room + "...";
            if (hour < 8) return "Va sortir el sol... i encara no havien marxat.\n" + room + "...";
            if (hour < 12) return "A l'hora d'esmorzar,\n" + room + "...";
            if (hour < 16) return "A l'hora de dinar,\n" + room + "...";
            if (hour < 20) return "A mitja tarda,\n" + room + "...";
            return "Es va tornar a fer fosc...\n" + room + ".";
        }
        switch (act)
        {
            case 0: return "Mentrestant, a la sala...";
            case 1: return "Mentrestant, a la barra...";
            case 2: return "Mentrestant, a la cuina...";
            case 3: return "Una mica més tard, a la terrassa...";
            case 4: return "I quan ja no quedava on amagar-se...\na la teulada!";
            case 5: return "Però encara quedava baixar...\nal celler.";
            case 6: return "Més avall i més fred...\na la cambra frigorífica.";
            case 7: return "I per acabar, en directe...\ndes del plató!";
        }
        return "Mentrestant...";
    }

    // "A la una", "A les dues"... (de 0 a 4 de la matinada)
    static string HourWords(int h)
    {
        switch (h)
        {
            case 1: return "A la una";
            case 2: return "A les dues";
            case 3: return "A les tres";
            case 4: return "A les quatre";
        }
        return "A les dotze";
    }

    public static string GameOverText(bool endless)
    {
        return endless ? "Les hores extres es van acabar...\nper al xef." : "I així, el xef va acabar\nal menú del dia.";
    }

    public static string WinText()
    {
        return "I per fi, el restaurant va poder tancar.\nAlmenys fins demà...";
    }

    // ---------- MOSTRAR ----------

    public static IEnumerator Show(string text, bool fin = false, float hold = 2.4f)
    {
        Intertitle it = Create(text, fin, hold);
        yield return it.Run();
        if (it != null) Destroy(it.gameObject);
    }

    public static void ShowAsync(string text, bool fin = false, float hold = 2.4f)
    {
        Intertitle it = Create(text, fin, hold);
        it.StartCoroutine(it.RunAndDie());
    }

    IEnumerator RunAndDie()
    {
        yield return Run();
        Destroy(gameObject);
    }

    IEnumerator Run()
    {
        GamepadInput.CustomScreens++;
        float total = FadeIn + hold + FadeOut;
        float t = 0f;
        bool leaving = false;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            if (!leaving && t > FadeIn + 0.3f && t < FadeIn + hold && SkipPressed())
            {
                t = FadeIn + hold; // salta directament a la sortida
                leaving = true;
            }
            float a = t < FadeIn ? t / FadeIn : t > FadeIn + hold ? 1f - (t - FadeIn - hold) / FadeOut : 1f;
            group.alpha = Mathf.Clamp01(a);

            // tremola una mica i parpelleja, com al projector
            if (Time.unscaledTime > nextJitter)
            {
                nextJitter = Time.unscaledTime + 1f / 12f;
                jitter = new Vector2(Random.Range(-2f, 2f), Random.Range(-3f, 3f));
                flicker.color = new Color(1f, 1f, 1f, Random.Range(0f, 0.035f));
            }
            content.anchoredPosition = jitter;
            yield return null;
        }
        GamepadInput.CustomScreens = Mathf.Max(0, GamepadInput.CustomScreens - 1);
    }

    static bool SkipPressed()
    {
        Keyboard kb = Keyboard.current;
        Gamepad pad = GamepadInput.ActivePad;
        Mouse ms = Mouse.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
        if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
        if (ms != null && ms.leftButton.wasPressedThisFrame) return true;
        return false;
    }

    // ---------- LA PEÇA ----------

    static Intertitle Create(string text, bool fin, float hold)
    {
        GameObject go = new GameObject("CartellMut", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Intertitle it = go.AddComponent<Intertitle>();
        it.hold = hold;
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // per sobre de tot menys el filtre de pel·lícula i el cercle de les transicions
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        it.group = go.GetComponent<CanvasGroup>();
        it.group.alpha = 0f;
        it.group.blocksRaycasts = true; // mentre hi és, no es pot clicar el que hi ha a sota

        RectTransform root = (RectTransform)go.transform;
        Image bg = Box(root, "Negre", new Color(0.03f, 0.025f, 0.02f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.raycastTarget = true;

        it.content = new GameObject("Contingut", typeof(RectTransform)).GetComponent<RectTransform>();
        it.content.SetParent(root, false);
        it.content.anchorMin = Vector2.zero; it.content.anchorMax = Vector2.one;
        it.content.offsetMin = it.content.offsetMax = Vector2.zero;

        // el marc: doble línia, amb diamants i floritures a les cantonades
        Frame(it.content, 90f, 4f);
        Frame(it.content, 106f, 2f);
        foreach (Vector2 c in new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) })
        {
            Vector2 inward = new Vector2(c.x < 0.5f ? 1f : -1f, c.y < 0.5f ? 1f : -1f);
            Image d = Box(it.content, "Diamant", Cream, c, c, Vector2.zero, Vector2.zero);
            d.rectTransform.sizeDelta = new Vector2(26f, 26f);
            d.rectTransform.anchoredPosition = inward * 98f;
            d.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image dd = Box(it.content, "DiamantDins", new Color(0.03f, 0.025f, 0.02f), c, c, Vector2.zero, Vector2.zero);
            dd.rectTransform.sizeDelta = new Vector2(10f, 10f);
            dd.rectTransform.anchoredPosition = inward * 98f;
            dd.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            // dues floritures (línies curtes) que surten del diamant
            Image h = Box(it.content, "Floritura", Cream, c, c, Vector2.zero, Vector2.zero);
            h.rectTransform.sizeDelta = new Vector2(70f, 2f);
            h.rectTransform.anchoredPosition = inward * 98f + new Vector2(inward.x * 55f, inward.y * 16f);
            Image v = Box(it.content, "Floritura", Cream, c, c, Vector2.zero, Vector2.zero);
            v.rectTransform.sizeDelta = new Vector2(2f, 70f);
            v.rectTransform.anchoredPosition = inward * 98f + new Vector2(inward.x * 16f, inward.y * 55f);
        }
        // un adorn a dalt i a baix del text
        foreach (float y in new[] { 0.72f, 0.28f })
        {
            Image line = Box(it.content, "Adorn", Cream, new Vector2(0.5f, y), new Vector2(0.5f, y), Vector2.zero, Vector2.zero);
            line.rectTransform.sizeDelta = new Vector2(420f, 2f);
            Image dot = Box(it.content, "AdornMig", Cream, new Vector2(0.5f, y), new Vector2(0.5f, y), Vector2.zero, Vector2.zero);
            dot.rectTransform.sizeDelta = new Vector2(14f, 14f);
            dot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // el text
        TextMeshProUGUI tx = Text(it.content, text, 62f, new Vector2(0f, fin ? 70f : 0f), new Vector2(1500f, 300f), Cream);
        tx.fontStyle = FontStyles.Italic;
        if (fin)
        {
            TextMeshProUGUI end = Text(it.content, "FI", 150f, new Vector2(0f, -120f), new Vector2(600f, 180f), Cream);
            end.fontStyle = FontStyles.Bold;
            end.characterSpacing = 30f;
        }

        // el parpelleig (un vel blanc que va i ve)
        it.flicker = Box(root, "Parpelleig", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return it;
    }

    static void Frame(RectTransform parent, float inset, float w)
    {
        Box(parent, "MarcDalt", Cream, new Vector2(0f, 1f), Vector2.one, new Vector2(inset, -inset - w), new Vector2(-inset, -inset));
        Box(parent, "MarcBaix", Cream, Vector2.zero, new Vector2(1f, 0f), new Vector2(inset, inset), new Vector2(-inset, inset + w));
        Box(parent, "MarcEsq", Cream, Vector2.zero, new Vector2(0f, 1f), new Vector2(inset, inset), new Vector2(inset + w, -inset));
        Box(parent, "MarcDreta", Cream, new Vector2(1f, 0f), Vector2.one, new Vector2(-inset - w, inset), new Vector2(-inset, -inset));
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

    static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 pos, Vector2 box, Color color)
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
        t.fontSizeMin = size * 0.5f;
        t.fontSizeMax = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }
}
