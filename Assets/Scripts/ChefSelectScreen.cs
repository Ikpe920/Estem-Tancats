using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Pantalla per triar el xef (surt en prémer Jugar al menú principal).
//  - Clica una carta (o fletxes / 1-4 / comandament) i prem Començar (o Enter).
//  - Els xefs bloquejats es compren amb propines: tria'l i prem Començar per comprar-lo.
//  - A sota es tria la dificultat (Tab o LB/RB), i el botó Reptes mostra la llista de reptes.
public class ChefSelectScreen : MonoBehaviour
{
    static readonly Color Frame = new Color(0.16f, 0.11f, 0.13f, 1f);
    static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f, 1f);
    static readonly Color Ink = new Color(0.24f, 0.15f, 0.12f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);
    static readonly Color Locked = new Color(0.35f, 0.32f, 0.36f, 1f);
    static readonly string[] Dirs = { "Down", "Left", "Up", "Right" }; // el xef triat dona voltes

    public static bool IsOpen { get; private set; }

    private Action onConfirm;
    private int selected;
    private readonly List<Card> cards = new List<Card>();
    private float nextTurn;
    private int turn;
    private bool done;

    private TextMeshProUGUI tipsText;
    private TextMeshProUGUI messageText;
    private float messageUntil;
    private TextMeshProUGUI startLabel;
    private readonly Image[] diffButtons = new Image[3];
    private TextMeshProUGUI diffHint;
    private GameObject challengesPanel;

    class Card
    {
        public RectTransform root;
        public Image frame;
        public Image portrait;
        public GameObject lockBand;
        public TextMeshProUGUI lockText;
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
        RefreshAll();
        // que la tecla Enter no premi també el botó del menú que hi ha a sota
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void OnDestroy() { IsOpen = false; }

    void Update()
    {
        if (done) return;

        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;

        // amb la llista de reptes oberta, qualsevol tecla de tornar la tanca
        if (challengesPanel != null && challengesPanel.activeSelf)
        {
            if ((kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame)) ||
                (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)))
                ShowChallenges(false);
            return;
        }

        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Select(selected - 1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Select(selected + 1);
            if (kb.digit1Key.wasPressedThisFrame) Select(0);
            if (kb.digit2Key.wasPressedThisFrame) Select(1);
            if (kb.digit3Key.wasPressedThisFrame) Select(2);
            if (kb.digit4Key.wasPressedThisFrame) Select(3);
            if (kb.tabKey.wasPressedThisFrame) SetDifficulty(((int)Progression.Difficulty + 1) % 3);
            if (kb.rKey.wasPressedThisFrame) ShowChallenges(true);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Confirm();
            if (kb.escapeKey.wasPressedThisFrame) Close();
        }
        if (pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame) Select(selected - 1);
            if (pad.dpad.right.wasPressedThisFrame) Select(selected + 1);
            if (pad.leftShoulder.wasPressedThisFrame) SetDifficulty(Mathf.Max(0, (int)Progression.Difficulty - 1));
            if (pad.rightShoulder.wasPressedThisFrame) SetDifficulty(Mathf.Min(2, (int)Progression.Difficulty + 1));
            if (pad.buttonNorth.wasPressedThisFrame) ShowChallenges(true);
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

        if (messageText != null && Time.unscaledTime > messageUntil) messageText.text = "";
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
        RefreshStartLabel();
    }

    // Començar: si el xef està bloquejat, primer es compra
    void Confirm()
    {
        if (done) return;

        if (!Progression.IsChefUnlocked(selected))
        {
            int price = Progression.ChefPrice(selected);
            if (Progression.BuyChef(selected))
            {
                Say("Has desbloquejat " + ChefRoster.All[selected].name + "!", new Color(0.55f, 1f, 0.6f));
                RefreshAll();
            }
            else Say("Et falten " + (price - Progression.Tips) + " propines", new Color(1f, 0.55f, 0.5f));
            return;
        }

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

    void SetDifficulty(int d)
    {
        Progression.Difficulty = (Difficulty)Mathf.Clamp(d, 0, 2);
        RefreshDifficulty();
    }

    void Say(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
        messageUntil = Time.unscaledTime + 2.5f;
    }

    // ---------- REFRESCAR ----------

    void RefreshAll()
    {
        tipsText.text = "Propines: <b>" + Progression.Tips + "</b>   ·   Reptes: " + Progression.DoneCount() + "/" + Progression.Challenges.Length;
        for (int i = 0; i < cards.Count; i++)
        {
            bool unlocked = Progression.IsChefUnlocked(i);
            cards[i].lockBand.SetActive(!unlocked);
            cards[i].portrait.color = unlocked ? Color.white : new Color(0.25f, 0.22f, 0.25f, 1f); // silueta fosca
            if (!unlocked) cards[i].lockText.text = "BLOQUEJAT · " + Progression.ChefPrice(i) + " propines";
        }
        RefreshStartLabel();
        RefreshDifficulty();
    }

    void RefreshStartLabel()
    {
        if (startLabel == null) return;
        startLabel.text = Progression.IsChefUnlocked(selected) ? "Començar" : "Comprar (" + Progression.ChefPrice(selected) + ")";
    }

    void RefreshDifficulty()
    {
        int d = (int)Progression.Difficulty;
        Color[] on = { new Color(0.3f, 0.6f, 0.35f), new Color(0.75f, 0.6f, 0.2f), new Color(0.75f, 0.25f, 0.22f) };
        for (int i = 0; i < 3; i++)
            if (diffButtons[i] != null) diffButtons[i].color = i == d ? on[i] : new Color(0.28f, 0.26f, 0.3f);
        if (diffHint != null) diffHint.text = Progression.DifficultyHint(Progression.Difficulty);
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

        EndScreenUI.CreateText(bg, "Titol", "TRIA EL TEU XEF", 76f, new Vector2(0.5f, 0.92f), new Vector2(1400f, 110f), Gold, true);
        tipsText = EndScreenUI.CreateText(bg, "Propines", "", 32f, new Vector2(0.5f, 0.845f), new Vector2(1400f, 50f), new Color(1f, 0.92f, 0.7f));
        tipsText.raycastTarget = false;

        int n = ChefRoster.All.Length;
        for (int i = 0; i < n; i++)
            cards.Add(BuildCard(bg, i, new Vector2(Mathf.Lerp(0.14f, 0.86f, n > 1 ? (float)i / (n - 1) : 0.5f), 0.53f)));

        // dificultat
        string[] names = { "Fàcil", "Normal", "Difícil" };
        for (int d = 0; d < 3; d++)
        {
            int captured = d;
            Button b = EndScreenUI.CreateButton(bg, names[d], new Vector2(0.4f + d * 0.1f, 0.215f), new Vector2(180f, 54f),
                new Color(0.28f, 0.26f, 0.3f), () => SetDifficulty(captured));
            diffButtons[d] = b.GetComponent<Image>();
        }
        TextMeshProUGUI dl = EndScreenUI.CreateText(bg, "Dificultat", "Dificultat:", 28f, new Vector2(0.29f, 0.215f), new Vector2(240f, 54f), new Color(0.85f, 0.78f, 0.65f));
        dl.raycastTarget = false;
        diffHint = EndScreenUI.CreateText(bg, "PistaDificultat", "", 24f, new Vector2(0.5f, 0.165f), new Vector2(1200f, 40f), new Color(0.85f, 0.78f, 0.65f));
        diffHint.raycastTarget = false;

        EndScreenUI.CreateButton(bg, "Enrere", new Vector2(0.32f, 0.085f), new Vector2(260f, 70f), new Color(0.30f, 0.30f, 0.35f), Close);
        EndScreenUI.CreateButton(bg, "Reptes", new Vector2(0.5f, 0.085f), new Vector2(260f, 70f), new Color(0.25f, 0.35f, 0.50f), () => ShowChallenges(true));
        Button start = EndScreenUI.CreateButton(bg, "Començar", new Vector2(0.68f, 0.085f), new Vector2(260f, 70f), new Color(0.20f, 0.45f, 0.25f), Confirm);
        startLabel = start.GetComponentInChildren<TextMeshProUGUI>();

        messageText = EndScreenUI.CreateText(bg, "Missatge", "", 30f, new Vector2(0.5f, 0.27f), new Vector2(1200f, 50f), Color.white, true);
        messageText.raycastTarget = false;
        TextMeshProUGUI help = EndScreenUI.CreateText(bg, "Ajuda", "← → triar  ·  Tab dificultat  ·  R reptes  ·  Enter començar  ·  Esc tornar",
            22f, new Vector2(0.5f, 0.025f), new Vector2(1500f, 36f), new Color(0.7f, 0.64f, 0.55f));
        help.raycastTarget = false;

        BuildChallenges(canvasGo.transform);
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
        rt.sizeDelta = new Vector2(380f, 520f);
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
        RectTransform band = Rect(face, "Franja", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -230f), Vector2.zero);
        Image bandImg = band.gameObject.AddComponent<Image>();
        bandImg.color = chef.color;
        bandImg.raycastTarget = false;

        RectTransform port = Rect(face, "Retrat", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        port.pivot = new Vector2(0.5f, 1f);
        port.sizeDelta = new Vector2(220f, 220f);
        port.anchoredPosition = new Vector2(0f, -8f);
        c.portrait = port.gameObject.AddComponent<Image>();
        c.portrait.sprite = c.sprites[0];
        c.portrait.preserveAspect = true;
        c.portrait.raycastTarget = false;

        // nom, punt fort i punt feble
        Label(face, chef.name, 38f, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(340f, 52f), Ink, true);
        Label(face, "<color=#2E8B3E>+</color> " + chef.strong, 25f, new Vector2(0.5f, 1f), new Vector2(0f, -345f), new Vector2(330f, 90f), Ink, false);
        Label(face, "<color=#B8352F>−</color> " + chef.weak, 25f, new Vector2(0.5f, 1f), new Vector2(0f, -435f), new Vector2(330f, 90f), new Color(0.45f, 0.33f, 0.26f), false);

        // la tecla per triar-lo
        Label(face, (index + 1).ToString(), 28f, new Vector2(0f, 1f), new Vector2(28f, -26f), new Vector2(40f, 40f), Ink, true);

        // franja de "bloquejat" amb el preu, a sobre del retrat
        RectTransform lockRt = Rect(face, "Bloquejat", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(0f, -95f));
        Image lockImg = lockRt.gameObject.AddComponent<Image>();
        lockImg.color = new Color(Locked.r, Locked.g, Locked.b, 0.92f);
        lockImg.raycastTarget = false;
        c.lockBand = lockRt.gameObject;
        GameObject lt = new GameObject("Text", typeof(RectTransform));
        lt.transform.SetParent(lockRt, false);
        RectTransform ltr = lt.GetComponent<RectTransform>();
        ltr.anchorMin = Vector2.zero; ltr.anchorMax = Vector2.one; ltr.offsetMin = ltr.offsetMax = Vector2.zero;
        c.lockText = lt.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) c.lockText.font = TMP_Settings.defaultFontAsset;
        c.lockText.fontSize = 26f;
        c.lockText.fontStyle = FontStyles.Bold;
        c.lockText.alignment = TextAlignmentOptions.Center;
        c.lockText.color = Gold;
        c.lockText.raycastTarget = false;
        return c;
    }

    // ---------- LA LLISTA DE REPTES ----------

    void BuildChallenges(Transform canvas)
    {
        RectTransform panel = Rect(canvas, "Reptes", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.03f, 0.05f, 0.97f);
        challengesPanel = panel.gameObject;

        EndScreenUI.CreateText(panel, "Titol", "REPTES", 72f, new Vector2(0.5f, 0.91f), new Vector2(1000f, 100f), Gold, true);

        float y = 0.79f;
        foreach (Progression.Challenge ch in Progression.Challenges)
        {
            RectTransform row = Rect(panel, "Repte " + ch.id, new Vector2(0.18f, y), new Vector2(0.82f, y), new Vector2(0f, -30f), new Vector2(0f, 30f));
            Image rowBg = row.gameObject.AddComponent<Image>();
            rowBg.raycastTarget = false;
            TextMeshProUGUI t = Label(row, "", 26f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 56f), Color.white, false);
            t.alignment = TextAlignmentOptions.Left;
            t.enableAutoSizing = false;
            row.gameObject.AddComponent<ChallengeRow>().Init(ch, rowBg, t);
            y -= 0.066f;
        }

        EndScreenUI.CreateButton(panel, "Tornar", new Vector2(0.5f, 0.08f), new Vector2(260f, 70f), new Color(0.30f, 0.30f, 0.35f), () => ShowChallenges(false));
        challengesPanel.SetActive(false);
    }

    void ShowChallenges(bool show)
    {
        if (challengesPanel == null) return;
        challengesPanel.SetActive(show);
        if (show) foreach (ChallengeRow r in challengesPanel.GetComponentsInChildren<ChallengeRow>()) r.Refresh();
    }

    // ---------- AJUDES ----------

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

    static TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, Color color, bool bold)
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
        return t;
    }
}

// una fila de la llista de reptes: nom, què cal fer, el progrés i la recompensa
public class ChallengeRow : MonoBehaviour
{
    private Progression.Challenge challenge;
    private Image bg;
    private TextMeshProUGUI text;

    public void Init(Progression.Challenge c, Image background, TextMeshProUGUI label)
    {
        challenge = c;
        bg = background;
        text = label;
        Refresh();
    }

    public void Refresh()
    {
        if (challenge == null) return;
        bool done = Progression.IsDone(challenge);
        bg.color = done ? new Color(0.18f, 0.36f, 0.22f, 0.9f) : new Color(0.2f, 0.17f, 0.2f, 0.9f);
        string progress = "";
        if (!done && challenge.goal > 0 && challenge.progress != null)
            progress = "  <color=#C9B98F>(" + Mathf.Min(challenge.progress(), challenge.goal) + "/" + challenge.goal + ")</color>";
        string state = done ? "<color=#8FE39B><b>FET</b></color>" : "<color=#FFD24D>+" + challenge.reward + "</color>";
        text.text = "   " + state + "   <b>" + challenge.name + "</b> — " + challenge.desc + progress;
    }
}
