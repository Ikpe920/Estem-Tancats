using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  TRIA DE MODE (en prémer "Jugar" al menú principal)
// =====================================================================
// Dues cartes:
// - EL SERVEI: la partida de sempre (història, 40 onades, 8 sales i 8 caps).
// - MENÚ DEGUSTACIÓ: els 8 caps l'un darrere l'altre, contra rellotge (BossRush).
// Esc / B tornen al menú.
public class ModeSelectScreen : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    static readonly Color Panel = new Color(0.10f, 0.07f, 0.06f, 0.96f);
    static readonly Color NormalCol = new Color(0.22f, 0.45f, 0.28f);
    static readonly Color RushCol = new Color(0.50f, 0.14f, 0.22f);

    private MainMenu menu;
    private GameObject lastSelected;
    private RectTransform[] cards;
    private float born;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() { IsOpen = false; }

    public static void Open(MainMenu menu)
    {
        if (IsOpen) return;
        ModeSelectScreen s = new GameObject("TriaDeMode").AddComponent<ModeSelectScreen>();
        s.menu = menu;
        s.Build();
    }

    void Build()
    {
        IsOpen = true;
        born = Time.unscaledTime;
        lastSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        GameObject canvasGo = new GameObject("CanvasMode", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 590; // per sota de la tria de xef
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bg = new GameObject("Fons", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        RectTransform root = bg.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = Panel;

        TextMeshProUGUI title = EndScreenUI.CreateText(root, "Titol", "QUÈ SERVIM AVUI?", 80f, new Vector2(0.5f, 0.86f), new Vector2(1600f, 110f), UIStyle.Gold, true);
        UIStyle.Outline(title, 0.25f);
        title.raycastTarget = false;

        string normalRecord = RunStats.BestWave > 0
            ? "Rècord: " + RunStats.BestPoints + " punts · onada " + RunStats.BestWave : "Encara no l'has jugat";
        string rushRecord = BossRush.RecordLine(BossRush.Menu.Classic) + "\n<size=85%>5 menús per triar</size>";

        Button normal = Card(root, new Vector2(0.29f, 0.47f), NormalCol, "Walk/Enemy_Front_1",
            "EL SERVEI",
            "La partida de sempre: la història,\n40 onades, 8 sales i 8 caps.\nI si aguantes, hores extres.",
            normalRecord, () => { Close(false); if (menu != null) menu.PlayNormal(); });
        Button rush = Card(root, new Vector2(0.71f, 0.47f), RushCol, "Bosses/Sommelier",
            "MENÚ DEGUSTACIÓ",
            "Vuit plats forts: els 8 caps\nl'un darrere l'altre, sense clients.\nContra rellotge.",
            rushRecord, () => ShowMenus());
        cards = new[] { (RectTransform)normal.transform, (RectTransform)rush.transform };

        Button back = EndScreenUI.CreateButton(root, "Tornar", new Vector2(0.5f, 0.1f), new Vector2(300f, 72f), new Color(0.30f, 0.30f, 0.35f), () => Close(true));
        this.root = root;
        firstPage = new GameObject[] { normal.gameObject, rush.gameObject, back.gameObject };

        // navegació amb el comandament només entre aquests tres botons (no els del menú de sota)
        Link(normal, null, rush, null, back);
        Link(rush, normal, null, null, back);
        Link(back, null, null, normal, null);

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(normal.gameObject);
    }

    // ---------- ELS MENÚS DEGUSTACIÓ (segona pàgina) ----------
    private RectTransform root, menusPage;
    private GameObject[] firstPage;

    void ShowMenus()
    {
        foreach (GameObject g in firstPage) g.SetActive(false);
        menusPage = new GameObject("Menus", typeof(RectTransform)).GetComponent<RectTransform>();
        menusPage.SetParent(root, false);
        menusPage.anchorMin = Vector2.zero; menusPage.anchorMax = Vector2.one;
        menusPage.offsetMin = menusPage.offsetMax = Vector2.zero;

        BossRush.Menu[] menus = { BossRush.Menu.Classic, BossRush.Menu.Chef, BossRush.Menu.Surprise, BossRush.Menu.Infernal, BossRush.Menu.Ghost };
        Color[] cols = { RushCol, new Color(0.62f, 0.20f, 0.12f), new Color(0.42f, 0.22f, 0.50f), new Color(0.35f, 0.08f, 0.08f), new Color(0.20f, 0.42f, 0.38f) };
        Button[] buttons = new Button[menus.Length];
        for (int i = 0; i < menus.Length; i++)
        {
            BossRush.Menu m = menus[i];
            bool locked = m == BossRush.Menu.Ghost && !BossRush.GhostUnlocked;
            Button b = EndScreenUI.CreateButton(menusPage, "", new Vector2(0.5f, 0.72f - i * 0.12f), new Vector2(1240f, 112f),
                locked ? new Color(0.25f, 0.25f, 0.28f) : cols[i], () =>
                {
                    if (locked) { AudioManager.Play("ui_back"); return; }
                    Close(false);
                    if (menu != null) menu.PlayBossRush(m);
                });
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null)
            {
                t.enableAutoSizing = true; t.fontSizeMin = 16f; t.fontSizeMax = 30f;
                t.text = "<b><color=#FFCC4D>" + (locked ? "???" : BossRush.Name(m)) + "</color></b>   " + BossRush.Desc(m)
                       + "\n<size=80%><i>" + (locked ? "" : BossRush.RecordLine(m)) + "</i></size>";
            }
            buttons[i] = b;
        }
        Button back = EndScreenUI.CreateButton(menusPage, "Tornar", new Vector2(0.5f, 0.1f), new Vector2(300f, 72f), new Color(0.30f, 0.30f, 0.35f), HideMenus);
        for (int i = 0; i < buttons.Length; i++)
            Link(buttons[i], null, null, i > 0 ? buttons[i - 1] : null, i < buttons.Length - 1 ? buttons[i + 1] : back);
        Link(back, null, null, buttons[buttons.Length - 1], null);
        cards = new RectTransform[0];
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    void HideMenus()
    {
        AudioManager.Play("ui_back");
        if (menusPage != null) Destroy(menusPage.gameObject);
        menusPage = null;
        foreach (GameObject g in firstPage) g.SetActive(true);
        cards = new[] { (RectTransform)firstPage[0].transform, (RectTransform)firstPage[1].transform };
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstPage[1]);
    }

    static void Link(Button b, Button left, Button right, Button up, Button down)
    {
        Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
        n.selectOnLeft = left;
        n.selectOnRight = right;
        n.selectOnUp = up;
        n.selectOnDown = down;
        b.navigation = n;
    }

    Button Card(RectTransform root, Vector2 anchor, Color color, string sprite, string name, string desc, string record, UnityEngine.Events.UnityAction onClick)
    {
        Button b = EndScreenUI.CreateButton(root, "", anchor, new Vector2(680f, 560f), color, onClick);
        Transform t = b.transform;

        Sprite sp = Resources.Load<Sprite>(sprite);
        if (sp != null)
        {
            GameObject ic = new GameObject("Dibuix", typeof(RectTransform), typeof(Image));
            RectTransform irt = ic.GetComponent<RectTransform>();
            irt.SetParent(t, false);
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f);
            irt.sizeDelta = new Vector2(190f, 190f);
            irt.anchoredPosition = new Vector2(0f, -125f);
            Image img = ic.GetComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        TextMeshProUGUI n = Label(t, name, 54f, new Vector2(0f, 35f), new Vector2(640f, 70f), UIStyle.Gold, true);
        UIStyle.Outline(n, 0.25f);
        Label(t, desc, 28f, new Vector2(0f, -70f), new Vector2(640f, 130f), new Color(1f, 0.95f, 0.88f), false);
        Label(t, record, 24f, new Vector2(0f, -205f), new Vector2(640f, 36f), new Color(1f, 0.85f, 0.55f), false).fontStyle = FontStyles.Italic;
        return b;
    }

    static TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 pos, Vector2 box, Color color, bool bold)
    {
        TextMeshProUGUI tx = EndScreenUI.CreateText(parent, "Text", text, size, new Vector2(0.5f, 0.5f), box, color, bold);
        tx.rectTransform.anchoredPosition = pos;
        tx.alignment = TextAlignmentOptions.Center;
        tx.textWrappingMode = TextWrappingModes.Normal;
        tx.raycastTarget = false;
        return tx;
    }

    void Update()
    {
        // la carta triada es fa una mica més gran
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (cards != null)
            foreach (RectTransform c in cards)
            {
                if (c == null) continue;
                float target = sel == c.gameObject ? 1.05f : 1f;
                float s = Mathf.MoveTowards(c.localScale.x, target, Time.unscaledDeltaTime * 1.2f);
                c.localScale = Vector3.one * s;
            }

        if (Time.unscaledTime - born < 0.15f) return; // el mateix clic de "Jugar" no la tanca
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.buttonEast.wasPressedThisFrame);
        if (back && menusPage != null) HideMenus();
        else if (back) Close(true);
    }

    void Close(bool backToMenu)
    {
        if (backToMenu) AudioManager.Play("ui_back");
        IsOpen = false;
        if (backToMenu && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(lastSelected);
        Destroy(gameObject);
    }

    void OnDestroy() { IsOpen = false; }
}
