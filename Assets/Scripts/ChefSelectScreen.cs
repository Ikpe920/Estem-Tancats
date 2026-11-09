using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Pantalla per triar el xef (surt en prémer Jugar al menú principal).
// Clica una carta (o fes servir les fletxes, 1-4 o el comandament) i prem Començar (o Enter).
public class ChefSelectScreen : MonoBehaviour
{
    static readonly Color Frame = new Color(0.16f, 0.11f, 0.13f, 1f);
    static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f, 1f);
    static readonly Color Ink = new Color(0.24f, 0.15f, 0.12f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);
    static readonly string[] Dirs = { "Down", "Left", "Up", "Right" }; // el xef triat dona voltes

    public static bool IsOpen { get; private set; }

    private Action onConfirm;
    private int selected;
    private readonly List<Card> cards = new List<Card>();
    private float nextTurn;
    private int turn;
    private bool done;

    class Card
    {
        public RectTransform root;
        public Image frame;
        public Image portrait;
        public Sprite[] sprites = new Sprite[4];
    }

    // obre la pantalla; quan el jugador confirma, es crida onConfirm
    public static void Open(Action confirm)
    {
        if (IsOpen) return;
        GameObject go = new GameObject("ChefSelectScreen");
        ChefSelectScreen s = go.AddComponent<ChefSelectScreen>();
        s.onConfirm = confirm;
    }

    void Awake()
    {
        IsOpen = true;
        selected = ChefRoster.Selected;
        Build();
        Select(selected);
        // que la tecla Enter no premi també el botó del menú que hi ha a sota
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void OnDestroy() { IsOpen = false; }

    void Update()
    {
        if (done) return;

        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Select(selected - 1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Select(selected + 1);
            if (kb.digit1Key.wasPressedThisFrame) Select(0);
            if (kb.digit2Key.wasPressedThisFrame) Select(1);
            if (kb.digit3Key.wasPressedThisFrame) Select(2);
            if (kb.digit4Key.wasPressedThisFrame) Select(3);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Confirm();
            if (kb.escapeKey.wasPressedThisFrame) Close();
        }
        if (pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame) Select(selected - 1);
            if (pad.dpad.right.wasPressedThisFrame) Select(selected + 1);
            if (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame) Confirm();
            if (pad.buttonEast.wasPressedThisFrame) Close();
        }

        // el xef triat dona voltes; la carta respira una mica
        if (Time.unscaledTime >= nextTurn)
        {
            nextTurn = Time.unscaledTime + 0.55f;
            turn = (turn + 1) % Dirs.Length;
            Card c = cards[selected];
            Sprite sp = c.sprites[turn] ?? c.sprites[0];
            if (sp != null) c.portrait.sprite = sp;
        }
        for (int i = 0; i < cards.Count; i++)
        {
            float target = i == selected ? 1.06f + 0.015f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
            Transform t = cards[i].root;
            t.localScale = Vector3.one * Mathf.Lerp(t.localScale.x, target, Time.unscaledDeltaTime * 12f);
        }
    }

    void Select(int i)
    {
        int n = ChefRoster.All.Length;
        selected = ((i % n) + n) % n;
        turn = 0;
        nextTurn = Time.unscaledTime + 0.55f;
        for (int k = 0; k < cards.Count; k++)
        {
            cards[k].frame.color = k == selected ? Gold : Frame;
            if (cards[k].sprites[0] != null) cards[k].portrait.sprite = cards[k].sprites[0];
        }
    }

    void Confirm()
    {
        if (done) return;
        done = true;
        ChefRoster.Selected = selected;
        Action a = onConfirm;
        Destroy(gameObject);
        a?.Invoke();
    }

    void Close()
    {
        done = true;
        Destroy(gameObject);
    }

    // ---------- LA INTERFÍCIE ----------

    void Build()
    {
        GameObject canvasGo = new GameObject("TriaXefCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // fons fosc que tapa el menú (i els seus clics)
        RectTransform bg = Rect(canvasGo.transform, "Fons", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.06f, 0.94f);

        EndScreenUI.CreateText(bg, "Titol", "TRIA EL TEU XEF", 84f, new Vector2(0.5f, 0.88f), new Vector2(1400f, 120f), Gold, true);

        int n = ChefRoster.All.Length;
        for (int i = 0; i < n; i++)
        {
            float x = (i + 0.5f) / n;
            cards.Add(BuildCard(bg, i, new Vector2(Mathf.Lerp(0.14f, 0.86f, n > 1 ? (float)i / (n - 1) : 0.5f), 0.5f)));
        }

        EndScreenUI.CreateButton(bg, "Enrere", new Vector2(0.38f, 0.1f), new Vector2(300f, 76f), new Color(0.30f, 0.30f, 0.35f), Close);
        EndScreenUI.CreateButton(bg, "Començar", new Vector2(0.62f, 0.1f), new Vector2(300f, 76f), new Color(0.20f, 0.45f, 0.25f), Confirm);
        TextMeshProUGUI help = EndScreenUI.CreateText(bg, "Ajuda", "← → o 1-4 per triar  ·  Enter per començar  ·  Esc per tornar",
            26f, new Vector2(0.5f, 0.19f), new Vector2(1400f, 50f), new Color(0.85f, 0.78f, 0.65f));
        help.raycastTarget = false;
    }

    Card BuildCard(Transform parent, int index, Vector2 anchor)
    {
        ChefRoster.Chef chef = ChefRoster.All[index];
        Card c = new Card();
        for (int d = 0; d < Dirs.Length; d++) c.sprites[d] = ChefRoster.LoadSprite(chef, Dirs[d]);

        // marc (daurat si està triada) i la carta de paper
        GameObject go = new GameObject("Carta " + chef.name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(380f, 560f);
        c.root = rt;
        c.frame = go.GetComponent<Image>();
        c.frame.color = Frame;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = c.frame;
        btn.transition = Selectable.Transition.None;
        int captured = index;
        btn.onClick.AddListener(() => { if (selected == captured) Confirm(); else Select(captured); });

        RectTransform face = Rect(rt, "Paper", Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Image faceImg = face.gameObject.AddComponent<Image>();
        faceImg.color = Paper;
        faceImg.raycastTarget = false;

        // franja de color del xef, a dalt, amb el retrat a sobre
        RectTransform band = Rect(face, "Franja", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -250f), Vector2.zero);
        Image bandImg = band.gameObject.AddComponent<Image>();
        bandImg.color = chef.color;
        bandImg.raycastTarget = false;

        RectTransform port = Rect(face, "Retrat", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        port.pivot = new Vector2(0.5f, 1f);
        port.sizeDelta = new Vector2(240f, 240f);
        port.anchoredPosition = new Vector2(0f, -10f);
        c.portrait = port.gameObject.AddComponent<Image>();
        c.portrait.sprite = c.sprites[0];
        c.portrait.preserveAspect = true;
        c.portrait.raycastTarget = false;

        // nom, punt fort i punt feble
        Label(face, chef.name, 40f, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(340f, 56f), Ink, true);
        Label(face, "<color=#2E8B3E>+</color> " + chef.strong, 26f, new Vector2(0.5f, 1f), new Vector2(0f, -375f), new Vector2(330f, 100f), Ink, false);
        Label(face, "<color=#B8352F>−</color> " + chef.weak, 26f, new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(330f, 100f), new Color(0.45f, 0.33f, 0.26f), false);

        // la tecla per triar-lo
        Label(face, (index + 1).ToString(), 28f, new Vector2(0f, 1f), new Vector2(28f, -26f), new Vector2(40f, 40f), Ink, true);
        return c;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        return rt;
    }

    static void Label(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, Color color, bool bold)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = size * 0.6f;
        t.fontSizeMax = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
    }
}
