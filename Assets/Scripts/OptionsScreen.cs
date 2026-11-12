using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  OPCIONS (des del menú principal)
// =====================================================================
// Volum general, música i efectes per separat, filtre de pel·lícula, vibració, pantalla completa,
// dificultat i tutorial. I dues pàgines més: Controls i Crèdits.
// Tot es guarda al dispositiu (PlayerPrefs). Esc / B tornen enrere.
public static class GameSettings
{
    const string MusicKey = "music_volume";
    const string SfxKey = "sfx_volume";
    const string MasterKey = "volume"; // el mateix que el menú de pausa

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(MusicKey, 1f); }
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(SfxKey, 1f); }
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(MasterKey, 1f); }
        set
        {
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, v);
            PlayerPrefs.Save();
            AudioListener.volume = v;
        }
    }
}

// ---------- ACCESSIBILITAT ----------
public static class Accessibility
{
    // sacseig de pantalla: 0 = normal, 1 = la meitat, 2 = cap
    public static int ShakeLevel
    {
        get { return PlayerPrefs.GetInt("acc_shake", 0); }
        set { PlayerPrefs.SetInt("acc_shake", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
    }
    public static float ShakeMul => ShakeLevel == 0 ? 1f : ShakeLevel == 1 ? 0.5f : 0f;

    // flaixos de pantalla (els focus de la Crítica, el flaix de la càmera...): si no, molt més suaus
    public static bool Flashes
    {
        get { return PlayerPrefs.GetInt("acc_flash", 1) == 1; }
        set { PlayerPrefs.SetInt("acc_flash", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // textos flotants (avisos, frases dels caps, números de mal) més grans
    public static bool BigText
    {
        get { return PlayerPrefs.GetInt("acc_bigtext", 0) == 1; }
        set { PlayerPrefs.SetInt("acc_bigtext", value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static float TextMul => BigText ? 1.35f : 1f;

    // els cercles d'avís dels atacs, més marcats
    public static bool StrongWarnings
    {
        get { return PlayerPrefs.GetInt("acc_warn", 0) == 1; }
        set { PlayerPrefs.SetInt("acc_warn", value ? 1 : 0); PlayerPrefs.Save(); }
    }
}

public class OptionsScreen : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    const float Step = 0.1f;
    static readonly Color Panel = new Color(0.10f, 0.07f, 0.06f, 0.96f);
    static readonly Color Btn = new Color(0.30f, 0.30f, 0.35f);
    static readonly Color BtnGold = new Color(0.55f, 0.40f, 0.15f);

    private RectTransform root;
    private GameObject mainPage, controlsPage, creditsPage, accessPage;
    private Button shakeButton, flashButton, bigTextButton, warnButton;
    private TextMeshProUGUI masterText, musicText, sfxText;
    private Button filmButton, vibrationButton, fullscreenButton, difficultyButton, tutorialButton, firstButton;
    private GameObject lastSelected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() { IsOpen = false; }

    public static void Open()
    {
        if (IsOpen) return;
        GameObject go = new GameObject("Opcions");
        go.AddComponent<OptionsScreen>().Build();
    }

    void Build()
    {
        IsOpen = true;
        lastSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        GameObject canvasGo = new GameObject("CanvasOpcions", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // fons fosc que tapa el menú (i els clics)
        GameObject bg = new GameObject("Fons", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        root = bg.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = Panel;

        mainPage = Page("Opcions");
        controlsPage = Page("Controls");
        creditsPage = Page("Credits");
        accessPage = Page("Accessibilitat");
        BuildMain(mainPage.transform);
        BuildControls(controlsPage.transform);
        BuildCredits(creditsPage.transform);
        BuildAccess(accessPage.transform);
        Show(mainPage);
        RefreshAll();
    }

    GameObject Page(string name)
    {
        GameObject p = new GameObject(name, typeof(RectTransform));
        p.transform.SetParent(root, false);
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return p;
    }

    void Show(GameObject page)
    {
        mainPage.SetActive(page == mainPage);
        controlsPage.SetActive(page == controlsPage);
        creditsPage.SetActive(page == creditsPage);
        accessPage.SetActive(page == accessPage);
        // perquè es pugui navegar amb el comandament
        Button first = page.GetComponentInChildren<Button>();
        if (page == mainPage && firstButton != null) first = firstButton;
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    // ---------- PÀGINA PRINCIPAL ----------

    void BuildMain(Transform p)
    {
        Title(p, "OPCIONS");

        float y = 0.76f, dy = 0.074f;
        masterText = VolumeRow(p, "Volum general", y, d => { GameSettings.MasterVolume = Round(GameSettings.MasterVolume + d); RefreshAll(); }, out firstButton); y -= dy;
        musicText = VolumeRow(p, "Música", y, d => { GameSettings.MusicVolume = Round(GameSettings.MusicVolume + d); RefreshAll(); }, out _); y -= dy;
        sfxText = VolumeRow(p, "Efectes de so", y, d => { GameSettings.SfxVolume = Round(GameSettings.SfxVolume + d); RefreshAll(); AudioManager.Play("coin"); }, out _); y -= dy;

        filmButton = ToggleRow(p, "Filtre de pel·lícula antiga", y, () => { FilmFilter.Enabled = !FilmFilter.Enabled; RefreshAll(); }); y -= dy;
        vibrationButton = ToggleRow(p, "Vibració del comandament", y, () =>
        {
            GamepadInput.Vibration = !GamepadInput.Vibration;
            if (GamepadInput.Vibration) GamepadInput.Rumble(0.4f, 0.4f, 0.2f);
            RefreshAll();
        }); y -= dy;
        fullscreenButton = ToggleRow(p, "Pantalla completa", y, () => { Screen.fullScreen = !Screen.fullScreen; Invoke(nameof(RefreshAll), 0.1f); }); y -= dy;
        difficultyButton = ToggleRow(p, "Dificultat", y, () =>
        {
            Progression.Difficulty = (Difficulty)(((int)Progression.Difficulty + 1) % 3);
            RefreshAll();
        }); y -= dy;
        tutorialButton = ToggleRow(p, "Tutorial", y, () => { TutorialHints.Done = !TutorialHints.Done; RefreshAll(); }); y -= dy;

        Vector2 size = new Vector2(300f, 72f);
        EndScreenUI.CreateButton(p, "Controls", new Vector2(0.23f, 0.1f), size, BtnGold, () => Show(controlsPage));
        EndScreenUI.CreateButton(p, "Accessibilitat", new Vector2(0.41f, 0.1f), size, BtnGold, () => Show(accessPage));
        EndScreenUI.CreateButton(p, "Crèdits", new Vector2(0.59f, 0.1f), size, BtnGold, () => Show(creditsPage));
        EndScreenUI.CreateButton(p, "Tornar", new Vector2(0.77f, 0.1f), size, new Color(0.20f, 0.45f, 0.25f), Close);
    }

    // ---------- ACCESSIBILITAT ----------

    void BuildAccess(Transform p)
    {
        Title(p, "ACCESSIBILITAT");
        float y = 0.7f, dy = 0.1f;
        shakeButton = ToggleRow(p, "Sacseig de pantalla", y, () => { Accessibility.ShakeLevel = (Accessibility.ShakeLevel + 1) % 3; RefreshAll(); }); y -= dy;
        flashButton = ToggleRow(p, "Flaixos de pantalla", y, () => { Accessibility.Flashes = !Accessibility.Flashes; RefreshAll(); }); y -= dy;
        bigTextButton = ToggleRow(p, "Textos flotants grans", y, () => { Accessibility.BigText = !Accessibility.BigText; RefreshAll(); }); y -= dy;
        warnButton = ToggleRow(p, "Avisos dels atacs més marcats", y, () => { Accessibility.StrongWarnings = !Accessibility.StrongWarnings; RefreshAll(); }); y -= dy;
        TextMeshProUGUI note = EndScreenUI.CreateText(p, "Nota", "El filtre de pel·lícula antiga (gra i ratlles) es treu a la pàgina principal d'Opcions.",
            26f, new Vector2(0.5f, y - 0.02f), new Vector2(1300f, 50f), new Color(1f, 0.85f, 0.6f));
        note.raycastTarget = false;
        EndScreenUI.CreateButton(p, "Tornar", new Vector2(0.5f, 0.1f), new Vector2(300f, 72f), new Color(0.20f, 0.45f, 0.25f), () => Show(mainPage));
    }

    TextMeshProUGUI VolumeRow(Transform p, string label, float y, System.Action<float> change, out Button minus)
    {
        Label(p, label, y);
        minus = EndScreenUI.CreateButton(p, "-", new Vector2(0.56f, y), new Vector2(70f, 60f), Btn, () => change(-Step));
        TextMeshProUGUI value = EndScreenUI.CreateText(p, "Valor", "", 36f, new Vector2(0.635f, y), new Vector2(170f, 60f), Color.white, true);
        value.raycastTarget = false;
        EndScreenUI.CreateButton(p, "+", new Vector2(0.71f, y), new Vector2(70f, 60f), Btn, () => change(Step));
        return value;
    }

    Button ToggleRow(Transform p, string label, float y, UnityEngine.Events.UnityAction onClick)
    {
        Label(p, label, y);
        return EndScreenUI.CreateButton(p, "", new Vector2(0.635f, y), new Vector2(330f, 60f), Btn, onClick);
    }

    void Label(Transform p, string text, float y)
    {
        TextMeshProUGUI t = EndScreenUI.CreateText(p, "Etiqueta", text, 36f, new Vector2(0.33f, y), new Vector2(620f, 60f), new Color(1f, 0.92f, 0.8f));
        t.alignment = TextAlignmentOptions.Right;
        t.raycastTarget = false;
    }

    void Title(Transform p, string text)
    {
        EndScreenUI.CreateText(p, "Titol", text, 84f, new Vector2(0.5f, 0.88f), new Vector2(1200f, 120f), UIStyle.Gold, true).raycastTarget = false;
    }

    static float Round(float v) { return Mathf.Clamp01(Mathf.Round(v * 10f) / 10f); }

    void RefreshAll()
    {
        if (masterText != null) masterText.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
        if (musicText != null) musicText.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
        if (sfxText != null) sfxText.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
        SetText(filmButton, FilmFilter.Enabled ? "Sí" : "No");
        SetText(vibrationButton, GamepadInput.UsingGamepad && !GamepadInput.CanRumble ? "No disponible" : (GamepadInput.Vibration ? "Sí" : "No"));
        SetText(fullscreenButton, Screen.fullScreen ? "Sí" : "No");
        SetText(difficultyButton, Progression.DifficultyName(Progression.Difficulty));
        SetText(tutorialButton, TutorialHints.Done ? "Ja vist (tornar-lo a veure)" : "Activat");
        SetText(shakeButton, Accessibility.ShakeLevel == 0 ? "Normal" : Accessibility.ShakeLevel == 1 ? "La meitat" : "Cap");
        SetText(flashButton, Accessibility.Flashes ? "Sí" : "Suaus");
        SetText(bigTextButton, Accessibility.BigText ? "Sí" : "No");
        SetText(warnButton, Accessibility.StrongWarnings ? "Sí" : "No");
    }

    static void SetText(Button b, string text)
    {
        if (b == null) return;
        TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return;
        t.enableAutoSizing = true;
        t.fontSizeMin = 18f;
        t.fontSizeMax = 32f;
        t.text = text;
    }

    // ---------- CONTROLS ----------

    void BuildControls(Transform p)
    {
        Title(p, "CONTROLS");
        string kb =
            "<b><color=#FFCC4D>TECLAT I RATOLÍ</color></b>\n\n" +
            "Moure-se:  <b>WASD</b> o fletxes\n" +
            "Apuntar:  <b>ratolí</b>\n" +
            "Llançar plats:  <b>clic esquerre</b> (mantenir)\n" +
            "Dash:  <b>Espai</b>\n" +
            "Habilitat del xef:  <b>Q</b> o <b>clic dret</b>\n" +
            "Bolcar una taula:  <b>E</b>\n" +
            "Veure els teus plats:  <b>Tab</b> (mantenir)\n" +
            "Pausa:  <b>Esc</b>";
        string pad =
            "<b><color=#FFCC4D>COMANDAMENT</color></b>\n\n" +
            "Moure-se:  <b>joystick esquerre</b>\n" +
            "Apuntar i llançar:  <b>joystick dret</b> (o <b>gallet dret</b>)\n" +
            "Dash:  <b>botó superior dret</b>\n" +
            "Habilitat del xef:  <b>botó superior esquerre</b>\n" +
            "Bolcar una taula:  <b>" + GamepadInput.West() + "</b>\n" +
            "Veure els teus plats:  <b>" + GamepadInput.SelectButton() + "</b> (mantenir)\n" +
            "Pausa:  <b>" + GamepadInput.StartButton() + "</b>";
        TextMeshProUGUI a = EndScreenUI.CreateText(p, "Teclat", kb, 34f, new Vector2(0.29f, 0.5f), new Vector2(780f, 560f), Color.white);
        TextMeshProUGUI b = EndScreenUI.CreateText(p, "Comandament", pad, 34f, new Vector2(0.71f, 0.5f), new Vector2(780f, 560f), Color.white);
        foreach (TextMeshProUGUI t in new[] { a, b })
        {
            t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.lineSpacing = 18f;
            t.raycastTarget = false;
        }
        EndScreenUI.CreateButton(p, "Tornar", new Vector2(0.5f, 0.1f), new Vector2(300f, 72f), new Color(0.20f, 0.45f, 0.25f), () => Show(mainPage));
    }

    // ---------- CRÈDITS ----------

    void BuildCredits(Transform p)
    {
        Title(p, "CRÈDITS");
        string text =
            "<size=120%><b><color=#FFCC4D>ESTEM TANCATS</color></b></size>\n" +
            "<i>Un servei que no s'acaba mai</i>\n\n" +
            "<b>Idea, disseny i direcció</b>\nIkpe920\n\n" +
            "<b>Programació, gràfics, música i sons</b>\nIkpe920 amb l'ajuda de Claude (Anthropic)\n\n" +
            "<b>Fet amb</b>\nUnity · TextMeshPro · Python (gràfics i música generats amb codi)\n\n" +
            "<i>Gràcies per jugar!</i>";
        TextMeshProUGUI t = EndScreenUI.CreateText(p, "Text", text, 36f, new Vector2(0.5f, 0.5f), new Vector2(1400f, 620f), Color.white);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = 10f;
        t.raycastTarget = false;
        EndScreenUI.CreateButton(p, "Tornar", new Vector2(0.5f, 0.1f), new Vector2(300f, 72f), new Color(0.20f, 0.45f, 0.25f), () => Show(mainPage));
    }

    // ---------- TANCAR ----------

    void Update()
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.buttonEast.wasPressedThisFrame);
        if (!back) return;
        if (!mainPage.activeSelf) { AudioManager.Play("ui_back"); Show(mainPage); }
        else Close();
    }

    void Close()
    {
        AudioManager.Play("ui_back");
        IsOpen = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(lastSelected);
        Destroy(gameObject);
    }

    void OnDestroy() { IsOpen = false; }
}
