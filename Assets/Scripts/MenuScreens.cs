using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  MENÚ PRINCIPAL I HISTÒRIA: decoració que s'afegeix sola a les escenes
// =====================================================================
// No cal tocar les escenes: en carregar-se, es busca el seu canvas i s'hi afegeix la decoració
// al darrere dels botons i textos que ja hi ha.
public static class MenuScreens
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // si alguna cosa falla aquí, que no aturi els altres scripts que esperen l'escena (Unity els crida
    // un darrere l'altre i un error talla la resta: per exemple, el que crea les taules i les caixes)
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try { OnSceneLoadedSafe(scene, mode); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    static void OnSceneLoadedSafe(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.MainMenu) Attach<MainMenuDecor>(scene);
        else if (scene.name == SceneNames.Story) Attach<StoryDecor>(scene);
    }

    static void Attach<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas c = root.GetComponentInChildren<Canvas>(true);
            if (c != null) { c.gameObject.AddComponent<T>(); return; }
        }
    }

    // tovalles de quadres (vermell i blanc) per al fons, que es repeteixen
    public static Texture2D Gingham(Color a, Color b)
    {
        int n = 16;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        Color mid = Color.Lerp(a, b, 0.5f);
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool cx = x < n / 2, cy = y < n / 2;
                px[y * n + x] = cx && cy ? a : (!cx && !cy ? b : mid);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        return rt;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, Color color, bool bold)
    {
        RectTransform rt = Rect(parent, "Text", anchor, anchor, Vector2.zero, Vector2.zero);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = size * 0.55f;
        t.fontSizeMax = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        UIStyle.Outline(t);
        return t;
    }
}

// ---------- MENÚ PRINCIPAL ----------
// Fons de tovalles que llisca, el títol gran que es balanceja, una frase, una desfilada a baix
// (el xef fugint de tres clients) i el rècord i les propines.
public class MainMenuDecor : MonoBehaviour
{
    private RawImage cloth;
    private TextMeshProUGUI title, titleShadow;
    private RectTransform parade;
    private Image[] walkers;
    private Sprite[][] frames;
    private float paradeX;

    void Start()
    {
        Transform c = transform;

        // fons: tovalles de quadres que llisquen, i una capa fosca perquè es llegeixi tot
        RectTransform bg = MenuScreens.Rect(c, "Tovalles", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.SetAsFirstSibling();
        cloth = bg.gameObject.AddComponent<RawImage>();
        cloth.texture = MenuScreens.Gingham(new Color(0.62f, 0.14f, 0.16f), new Color(0.93f, 0.89f, 0.82f));
        cloth.uvRect = new Rect(0f, 0f, 1920f / 160f, 1080f / 160f);
        cloth.raycastTarget = false;
        RectTransform shade = MenuScreens.Rect(c, "Fosc", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        shade.SetSiblingIndex(1);
        Image sh = shade.gameObject.AddComponent<Image>();
        sh.color = new Color(0.08f, 0.04f, 0.06f, 0.72f);
        sh.raycastTarget = false;

        // el títol que ja hi ha: gran, daurat, amb ombra
        foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.text.Contains("ESTEM")) { title = t; break; }
        if (title != null)
        {
            title.color = UIStyle.Gold;
            title.fontStyle |= FontStyles.Bold;
            title.fontSize = Mathf.Max(title.fontSize, 150f);
            title.enableAutoSizing = false;
            UIStyle.Outline(title, 0.25f);
            titleShadow = Instantiate(title, title.transform.parent);
            titleShadow.name = "OmbraTitol";
            titleShadow.color = new Color(0f, 0f, 0f, 0.6f);
            titleShadow.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
            titleShadow.rectTransform.anchoredPosition = title.rectTransform.anchoredPosition + new Vector2(10f, -10f);

            RectTransform trt = title.rectTransform;
            MenuScreens.Text(c, "Un servei que no s'acaba mai", 40f, new Vector2(0.5f, 0.5f),
                trt.anchoredPosition + new Vector2(0f, -130f), new Vector2(1200f, 60f), new Color(1f, 0.92f, 0.8f), false)
                .fontStyle = FontStyles.Italic;
        }

        // els botons, una mica més grans
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            RectTransform rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 420f), Mathf.Max(rt.sizeDelta.y, 76f));
        }

        // rècord, onada, propines i reptes
        string stats = "Rècord: " + RunStats.BestPoints + " punts  ·  Millor onada: " + RunStats.BestWave
                     + "  ·  Propines: " + Progression.Tips + "  ·  Reptes: " + Progression.DoneCount() + "/" + Progression.Challenges.Length;
        MenuScreens.Text(c, stats, 28f, new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(1700f, 44f), new Color(0.95f, 0.88f, 0.72f), false);

        BuildParade(c);
    }

    // el xef triat fuig corrent i tres clients el persegueixen, d'esquerra a dreta
    void BuildParade(Transform c)
    {
        parade = MenuScreens.Rect(c, "Desfilada", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        parade.SetSiblingIndex(2); // per sota dels botons
        parade.anchoredPosition = new Vector2(0f, -330f);

        string chef = ChefRoster.All[Progression.IsChefUnlocked(ChefRoster.Selected) ? ChefRoster.Selected : 0].sprites;
        string[] names = { chef + "_Right", "Enemy_Front", "EnemyFast_Front", "EnemyTank_Front" };
        frames = new Sprite[names.Length][];
        walkers = new Image[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            frames[i] = new[] { Resources.Load<Sprite>("Walk/" + names[i] + "_1"), Resources.Load<Sprite>("Walk/" + names[i] + "_2") };
            RectTransform rt = MenuScreens.Rect(parade, names[i], new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rt.sizeDelta = new Vector2(150f, 150f);
            rt.anchoredPosition = new Vector2(i == 0 ? 0f : -150f - i * 150f, 0f);
            walkers[i] = rt.gameObject.AddComponent<Image>();
            walkers[i].sprite = frames[i][0];
            walkers[i].preserveAspect = true;
            walkers[i].raycastTarget = false;
            walkers[i].enabled = frames[i][0] != null;
        }
        paradeX = -200f;
    }

    void Update()
    {
        if (cloth != null)
        {
            Rect uv = cloth.uvRect;
            uv.x += Time.unscaledDeltaTime * 0.08f;
            uv.y += Time.unscaledDeltaTime * 0.05f;
            cloth.uvRect = uv;
        }

        if (title != null)
        {
            float t = Time.unscaledTime;
            title.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 2f);
            title.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.1f) * 0.02f);
            if (titleShadow != null)
            {
                titleShadow.transform.localRotation = title.transform.localRotation;
                titleShadow.transform.localScale = title.transform.localScale;
            }
        }

        if (parade != null)
        {
            float width = ((RectTransform)transform).rect.width;
            paradeX += Time.unscaledDeltaTime * 260f;
            if (paradeX > width + 800f) paradeX = -200f; // tornen a sortir per l'esquerra
            parade.anchoredPosition = new Vector2(paradeX, -330f);
            int f = Mathf.FloorToInt(Time.unscaledTime * 8f) % 2;
            for (int i = 0; i < walkers.Length; i++)
            {
                if (frames[i][f] != null) walkers[i].sprite = frames[i][f];
                walkers[i].rectTransform.anchoredPosition = new Vector2(walkers[i].rectTransform.anchoredPosition.x,
                    Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f + i)) * 10f); // boten en córrer
            }
        }
    }
}

// ---------- HISTÒRIA ----------
// El fons animat (StoryBackdrop), un panell darrere del text, el text que s'escriu sol (un clic o una tecla
// l'acaben), i un rellotge "23:59".
public class StoryDecor : MonoBehaviour
{
    const float CharsPerSecond = 45f;
    private TextMeshProUGUI story;
    private TextMeshProUGUI clock;
    private float started;
    private bool finished;

    void Start()
    {
        Transform c = transform;
        // el fons animat: el restaurant de nit amb els clients que no marxen
        try { gameObject.AddComponent<StoryBackdrop>().Build(); }
        catch (System.Exception e) { Debug.LogException(e); }

        foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.text.Length > 60) { story = t; break; }

        if (story != null)
        {
            RectTransform srt = story.rectTransform;
            Image panel = UIStyle.Panel(srt.parent, "PanellHistoria", srt.anchorMin, srt.anchorMax, Vector2.zero, Vector2.zero);
            RectTransform prt = panel.rectTransform;
            prt.pivot = srt.pivot;
            prt.sizeDelta = srt.sizeDelta + new Vector2(80f, 60f);
            prt.anchoredPosition = srt.anchoredPosition;
            prt.SetSiblingIndex(srt.GetSiblingIndex()); // just darrere del text
            story.color = new Color(1f, 0.95f, 0.86f);
            UIStyle.Outline(story, 0.15f);
            story.maxVisibleCharacters = 0;
        }

        // el rellotge de dalt: són les 23:59 (els dos punts fan pampallugues)
        clock = MenuScreens.Text(c, "23:59", 110f, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(500f, 140f), new Color(1f, 0.35f, 0.3f), true);
        MenuScreens.Text(c, "EL RESTAURANT JA HA TANCAT", 34f, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(1000f, 50f), new Color(1f, 0.9f, 0.75f), true);

        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            RectTransform rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 330f), Mathf.Max(rt.sizeDelta.y, 70f));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - 40f, rt.anchoredPosition.y + 10f);
        }
        started = Time.unscaledTime;
    }

    void Update()
    {
        if (clock != null) clock.text = Mathf.FloorToInt(Time.unscaledTime * 1.5f) % 2 == 0 ? "23:59" : "23 59";
        if (story == null || finished) return;

        int total = story.textInfo != null && story.textInfo.characterCount > 0 ? story.textInfo.characterCount : story.text.Length;
        int shown = Mathf.FloorToInt((Time.unscaledTime - started) * CharsPerSecond);

        // un clic o una tecla l'acaben de cop (el botó de començar continua funcionant com sempre)
        bool skip = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                 || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                 || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.buttonEast.wasPressedThisFrame);
        if (skip) shown = total;

        story.maxVisibleCharacters = shown;
        if (shown >= total) { story.maxVisibleCharacters = 99999; finished = true; }
    }
}
