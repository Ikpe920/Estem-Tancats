using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  RECEPTARI I BESTIARI
// =====================================================================
// Tot el que has descobert entre partides (es guarda al dispositiu):
//  - PLATS: els que has triat mai. Els altres surten com a silueta amb "???".
//  - CONJUNTS: els que has completat. Dels altres, es veuen els dos tipus de plat que cal combinar.
//  - CLIENTS: els clients i caps que has despatxat, amb quants n'has despatxat.
// S'obre des del menú principal (botó "Receptari").
public static class Codex
{
    // ---------- DESCOBRIMENTS ----------

    public static void DiscoverDish(Boon b) { if (b != null) PlayerPrefs.SetInt("seen_dish_" + BoonManager.DishKey(b), 1); }
    public static bool DishSeen(Boon b) { return b != null && PlayerPrefs.GetInt("seen_dish_" + BoonManager.DishKey(b), 0) == 1; }
    public static void DiscoverSet(SetId id) { PlayerPrefs.SetInt("seen_set_" + id, 1); }
    public static bool SetSeen(SetId id) { return PlayerPrefs.GetInt("seen_set_" + id, 0) == 1; }
    public static int Kills(string key) { return PlayerPrefs.GetInt("kills_" + key, 0); }
    public static void DiscoverEvo(EvoId id) { PlayerPrefs.SetInt("seen_evo_" + id, 1); }
    public static bool EvoSeen(EvoId id) { return PlayerPrefs.GetInt("seen_evo_" + id, 0) == 1; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Enemy.OnKilled -= OnKilled;
        Enemy.OnKilled += OnKilled;
    }

    static void OnKilled(Enemy e)
    {
        if (e == null || SceneManager.GetActiveScene().name != SceneNames.Game) return;
        string key = ClientKey(e);
        PlayerPrefs.SetInt("kills_" + key, Kills(key) + 1); // es desa amb el proper PlayerPrefs.Save()
    }

    // quin client és (per al Bestiari)
    public static string ClientKey(Enemy e)
    {
        if (e.isBoss)
        {
            if (e.GetComponent<BossDrunkBrain>() != null) return "Borratxo";
            if (e.GetComponent<BossChefBrain>() != null) return "XefRenegat";
            if (e.GetComponent<BossVipBrain>() != null) return "ClientVIP";
            if (e.GetComponent<BossInspectorBrain>() != null) return "Inspectora";
            if (e.GetComponent<BossTvBrain>() != null) return "CriticaTV";
            if (e.GetComponent<BossSommelierBrain>() != null) return "Sommelier";
            if (e.GetComponent<BossGelaterBrain>() != null) return "Gelater";
            return "Critic";
        }
        if (e.GetComponent<Golafre>() != null) return "Golafre";
        if (e.GetComponent<OllaPressio>() != null) return "Olla";
        if (e.GetComponent<SafataEscut>() != null) return "Safata";
        if (e.GetComponent<Cambrer>() != null) return "Cambrer";
        if (e.GetComponent<ClientNen>() != null) return "Nen";
        if (e.GetComponent<ClientIaia>() != null) return "Iaia";
        if (e.GetComponent<ClientTurista>() != null) return "Turista";
        if (e.GetComponent<ClientRepartidor>() != null) return "Repartidor";
        if (e.GetComponent<ClientInfluencer>() != null) return "Influencer";
        if (e.GetComponent<ClientMim>() != null) return "Mim";
        string n = e.gameObject.name;
        if (n.StartsWith("EnemyFast")) return "EnemyFast";
        if (n.StartsWith("EnemyShooter")) return "EnemyShooter";
        if (n.StartsWith("EnemyTank")) return "EnemyTank";
        return "Enemy";
    }

    // ---------- LA LLISTA DE CLIENTS ----------

    public class ClientInfo
    {
        public string key, name, desc, sprite;
        public int firstWave;
        public bool boss;
    }

    public static List<ClientInfo> Clients()
    {
        var list = new List<ClientInfo>
        {
            new ClientInfo { key = "Enemy", name = "EL CLIENT", desc = "Camina cap a tu. Vol el compte... o el xef.", sprite = "Walk/Enemy_Front_1", firstWave = 1 },
            new ClientInfo { key = "EnemyFast", name = "EL CLIENT AFAMAT", desc = "Més ràpid i més fluix.", sprite = "Walk/EnemyFast_Front_1", firstWave = 1 },
            new ClientInfo { key = "EnemyShooter", name = "EL QUE LLANÇA PLATS", desc = "Es queda lluny i et llança plats.", sprite = "Walk/EnemyShooter_Front_1", firstWave = 1 },
            new ClientInfo { key = "EnemyTank", name = "EL CLIENT GROS", desc = "Lent, però n'aguanta molts.", sprite = "Walk/EnemyTank_Front_1", firstWave = 1 },
        };
        EnemyVariants.ForEachInfo((kind, name, hint, wave) =>
            list.Add(new ClientInfo { key = kind.ToString(), name = name, desc = hint + ".", sprite = "Walk/" + kind + "_Front_1", firstWave = wave }));
        list.Add(Boss("Critic", "EL CRÍTIC GASTRONÒMIC", BossType.Critic, 5));
        list.Add(Boss("Borratxo", "EL BORRATXO DE LA BARRA", BossType.Borratxo, 10));
        list.Add(Boss("XefRenegat", "EL XEF RENEGAT", BossType.Cuiner, 15));
        list.Add(Boss("ClientVIP", "EL CLIENT VIP", BossType.Pallas, 20));
        list.Add(Boss("Inspectora", "LA INSPECTORA", BossType.Inspector, 25));
        list.Add(Boss("Sommelier", "EL SOMMELIER", BossType.Sommelier, 30));
        list.Add(Boss("Gelater", "EL GELATER BOIG", BossType.Gelater, 35));
        list.Add(Boss("CriticaTV", "LA CRÍTICA DE LA TELE", BossType.TvCritic, 40));
        return list;
    }

    static ClientInfo Boss(string key, string name, BossType t, int wave)
    {
        return new ClientInfo { key = key, name = name, desc = BossIntroScreen.HintFor(t) + ".\n<color=#FF9A40>Punt feble: " + DamageKind.Name(DamageKind.WeaknessOf(t)).ToLower() + " (" + DamageKind.Source(DamageKind.WeaknessOf(t)) + ")</color>", sprite = "Walk/" + key + "_1", firstWave = wave, boss = true };
    }
}

// ---------- LA PANTALLA ----------
public class CodexScreen : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    static readonly Color Bg = new Color(0.10f, 0.07f, 0.06f, 0.97f);
    static readonly Color CellCol = new Color(0.22f, 0.16f, 0.12f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f);

    private RectTransform root, content;
    private GameObject lastSelected;
    private Image detailIcon;
    private TextMeshProUGUI detailName, detailText, countText;
    private readonly Button[] tabs = new Button[3];
    private int tab;
    private Boon[] dishes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() { IsOpen = false; }

    public static void Open()
    {
        if (IsOpen) return;
        new GameObject("Receptari").AddComponent<CodexScreen>().Build();
    }

    void Build()
    {
        IsOpen = true;
        dishes = BoonManager.DefaultDishes();
        lastSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        GameObject canvasGo = new GameObject("CanvasReceptari", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = Rect(canvasGo.transform, "Fons", Vector2.zero, Vector2.one);
        root.gameObject.AddComponent<Image>().color = Bg;

        EndScreenUI.CreateText(root, "Titol", "RECEPTARI I BESTIARI", 76f, new Vector2(0.5f, 0.92f), new Vector2(1400f, 100f), Gold, true).raycastTarget = false;
        countText = EndScreenUI.CreateText(root, "Compte", "", 28f, new Vector2(0.5f, 0.855f), new Vector2(1400f, 44f), new Color(1f, 0.92f, 0.78f));
        countText.raycastTarget = false;

        string[] names = { "PLATS", "CONJUNTS", "CLIENTS" };
        for (int i = 0; i < 3; i++)
        {
            int captured = i;
            tabs[i] = EndScreenUI.CreateButton(root, names[i], new Vector2(0.32f + i * 0.18f, 0.785f), new Vector2(300f, 64f), CellCol, () => ShowTab(captured));
        }

        // a l'esquerra la graella, a la dreta el detall
        content = Rect(root, "Contingut", new Vector2(0.04f, 0.13f), new Vector2(0.64f, 0.73f));
        RectTransform detail = Rect(root, "Detall", new Vector2(0.66f, 0.13f), new Vector2(0.96f, 0.73f));
        detail.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.11f, 0.09f);
        detailIcon = Rect(detail, "Icona", new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.95f)).gameObject.AddComponent<Image>();
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;
        detailName = EndScreenUI.CreateText(detail, "Nom", "", 40f, new Vector2(0.5f, 0.42f), new Vector2(520f, 60f), Gold, true);
        detailText = EndScreenUI.CreateText(detail, "Text", "", 28f, new Vector2(0.5f, 0.2f), new Vector2(520f, 220f), Color.white);
        detailText.textWrappingMode = TextWrappingModes.Normal;
        detailName.raycastTarget = detailText.raycastTarget = false;

        EndScreenUI.CreateButton(root, "Tornar", new Vector2(0.5f, 0.06f), new Vector2(300f, 70f), new Color(0.20f, 0.45f, 0.25f), Close);

        RefreshCount();
        ShowTab(0);
    }

    void RefreshCount()
    {
        int d = 0; foreach (Boon b in dishes) if (Codex.DishSeen(b)) d++;
        int ev = 0; foreach (Evo.Def e in Evo.All) if (Codex.EvoSeen(e.id)) ev++;
        int s = 0; foreach (SetBonus set in SetBonuses.All) if (Codex.SetSeen(set.id)) s++;
        List<Codex.ClientInfo> clients = Codex.Clients();
        int c = 0; foreach (Codex.ClientInfo ci in clients) if (Codex.Kills(ci.key) > 0) c++;
        countText.text = "Plats " + d + "/" + dishes.Length + "   ·   Estrella " + ev + "/" + Evo.All.Length + "   ·   Conjunts " + s + "/" + SetBonuses.All.Length + "   ·   Clients " + c + "/" + clients.Count;
    }

    void ShowTab(int t)
    {
        tab = t;
        for (int i = 0; i < 3; i++)
            if (tabs[i] != null) tabs[i].GetComponent<Image>().color = i == t ? new Color(0.55f, 0.40f, 0.15f) : CellCol;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        Button first = null;
        if (t == 0) first = BuildDishes();
        else if (t == 1) first = BuildSets();
        else first = BuildClients();
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    // ---------- PLATS ----------

    Button BuildDishes()
    {
        Button first = null;
        int cols = 10;
        for (int i = 0; i < dishes.Length; i++)
        {
            Boon b = dishes[i];
            bool seen = Codex.DishSeen(b);
            Sprite icon = BoonManager.DishIcon(b);
            Button cell = Cell(i, cols, Mathf.CeilToInt((dishes.Length + Evo.All.Length) / (float)cols), icon, seen, () =>
                ShowDetail(icon, seen, seen ? b.name : "???", seen ? b.description + "\n\n<color=#FFCC4D>" + TypeName(b.type) + "</color>" : "Encara no l'has tastat."));
            if (first == null) first = cell;
        }
        // els plats estrella, a continuació (amb el fons daurat)
        int total = dishes.Length + Evo.All.Length;
        for (int k = 0; k < Evo.All.Length; k++)
        {
            Evo.Def d = Evo.All[k];
            bool seen = Codex.EvoSeen(d.id);
            Boon baseDish = DishOfType(d.baseType), partner = DishOfType(d.partner);
            Sprite icon = BoonManager.DishIcon(baseDish);
            string recipe = (baseDish != null ? baseDish.name : "?") + " al màxim + " + (partner != null ? partner.name : "?");
            Button cell = Cell(dishes.Length + k, cols, Mathf.CeilToInt(total / (float)cols), icon, seen, () =>
                ShowDetail(icon, seen, seen ? "ESTRELLA: " + d.name : "ESTRELLA: ???", (seen ? d.desc + "\n\n" : "Un plat estrella secret.\n\n") + "<color=#FFCC4D>" + recipe + "</color>"));
            cell.GetComponent<Image>().color = new Color(0.45f, 0.32f, 0.10f);
        }

        if (dishes.Length > 0)
        {
            Boon b0 = dishes[0];
            bool s0 = Codex.DishSeen(b0);
            ShowDetail(BoonManager.DishIcon(b0), s0, s0 ? b0.name : "???", s0 ? b0.description : "Encara no l'has tastat.");
        }
        return first;
    }

    // ---------- CONJUNTS ----------

    Button BuildSets()
    {
        Button first = null;
        SetBonus[] sets = SetBonuses.All;
        int cols = 2;
        float rowH = 1f / Mathf.Max(1, Mathf.CeilToInt(sets.Length / (float)cols));
        for (int i = 0; i < sets.Length; i++)
        {
            SetBonus set = sets[i];
            bool seen = Codex.SetSeen(set.id);
            int col = i % cols, row = i / cols;
            RectTransform r = Rect(content, "Conjunt", new Vector2(col * 0.5f + 0.005f, 1f - (row + 1) * rowH + 0.004f), new Vector2((col + 1) * 0.5f - 0.005f, 1f - row * rowH - 0.004f));
            Image img = r.gameObject.AddComponent<Image>();
            img.color = seen ? new Color(0.30f, 0.20f, 0.12f) : CellCol;
            Button btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            string hint = "Combina un plat de <b>" + TypeName(set.a) + "</b> i un de <b>" + TypeName(set.b) + "</b>";
            Sprite icon = IconOfType(set.a);
            btn.onClick.AddListener(() => ShowDetail(icon, true, seen ? set.name : "???", seen ? set.effect + "\n\n" + hint : hint + " per descobrir-lo."));
            r.gameObject.AddComponent<CodexHover>().onFocus = () => btn.onClick.Invoke();
            TextMeshProUGUI tx = EndScreenUI.CreateText(r, "Text", seen ? "<color=#FFCC4D><b>" + set.name + "</b></color>  " + set.effect : "<color=#888888>???</color>", 22f, new Vector2(0.5f, 0.5f), Vector2.zero, Color.white);
            RectTransform trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(12f, 2f); trt.offsetMax = new Vector2(-12f, -2f);
            tx.alignment = TextAlignmentOptions.Left;
            tx.raycastTarget = false;
            if (first == null) first = btn;
        }
        if (sets.Length > 0) ShowDetail(IconOfType(sets[0].a), true, Codex.SetSeen(sets[0].id) ? sets[0].name : "???", "Tria un conjunt per veure què cal combinar.");
        return first;
    }

    // ---------- CLIENTS ----------

    Button BuildClients()
    {
        Button first = null;
        List<Codex.ClientInfo> clients = Codex.Clients();
        int cols = 7;
        for (int i = 0; i < clients.Count; i++)
        {
            Codex.ClientInfo c = clients[i];
            int kills = Codex.Kills(c.key);
            bool seen = kills > 0;
            Sprite sp = Resources.Load<Sprite>(c.sprite);
            string text = seen
                ? c.desc + "\n\n<color=#FFCC4D>Despatxats: " + kills + "</color>" + (c.boss ? (BossBrain.BestTime(c.key) > 0f ? "\n<color=#FFCC4D>Millor temps: " + BossRush.Clock(BossBrain.BestTime(c.key)) + "</color>" : "") : "\nSurt a partir de l'onada " + c.firstWave)
                : (c.boss ? "Un cap que encara no has vençut." : "Encara no n'has despatxat cap.") + "\nSurt a partir de l'onada " + c.firstWave + ".";
            Button cell = Cell(i, cols, Mathf.CeilToInt(clients.Count / (float)cols), sp, seen, () => ShowDetail(sp, seen, seen ? c.name : "???", text));
            if (c.boss) cell.GetComponent<Image>().color = new Color(0.32f, 0.14f, 0.14f);
            if (first == null) first = cell;
        }
        if (clients.Count > 0)
        {
            Codex.ClientInfo c0 = clients[0];
            bool s0 = Codex.Kills(c0.key) > 0;
            ShowDetail(Resources.Load<Sprite>(c0.sprite), s0, s0 ? c0.name : "???", s0 ? c0.desc : "Encara no n'has despatxat cap.");
        }
        return first;
    }

    // ---------- PECES ----------

    Button Cell(int i, int cols, int rows, Sprite icon, bool seen, UnityEngine.Events.UnityAction onShow)
    {
        int col = i % cols, row = i / cols;
        float w = 1f / cols;
        float h = 1f / Mathf.Max(rows, 1);
        RectTransform r = Rect(content, "Casella", new Vector2(col * w + 0.004f, 1f - (row + 1) * h + 0.006f), new Vector2((col + 1) * w - 0.004f, 1f - row * h - 0.006f));
        Image bg = r.gameObject.AddComponent<Image>();
        bg.color = CellCol;
        Button btn = r.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.4f, 1.3f, 1.1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;
        btn.onClick.AddListener(onShow);
        r.gameObject.AddComponent<CodexHover>().onFocus = onShow;

        Image ic = Rect(r, "Icona", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f)).gameObject.AddComponent<Image>();
        ic.sprite = icon;
        ic.preserveAspect = true;
        ic.raycastTarget = false;
        ic.color = seen ? Color.white : new Color(0f, 0f, 0f, 0.85f); // silueta
        ic.enabled = icon != null;
        return btn;
    }

    void ShowDetail(Sprite icon, bool seen, string name, string text)
    {
        detailIcon.sprite = icon;
        detailIcon.enabled = icon != null;
        detailIcon.color = seen ? Color.white : new Color(0f, 0f, 0f, 0.85f);
        detailName.text = name;
        detailText.text = text;
    }

    Boon DishOfType(BoonType t)
    {
        foreach (Boon b in dishes) if (b.type == t) return b;
        return null;
    }

    Sprite IconOfType(BoonType t)
    {
        foreach (Boon b in dishes) if (b.type == t) return BoonManager.DishIcon(b);
        return null;
    }

    static string TypeName(BoonType t)
    {
        switch (t)
        {
            case BoonType.Damage: return "dany";
            case BoonType.FireRate: return "velocitat de tret";
            case BoonType.MoveSpeed: return "velocitat";
            case BoonType.MaxLives: return "cors";
            case BoonType.Heal: return "curar";
            case BoonType.Invulnerability: return "invulnerabilitat";
            case BoonType.DashCooldown: return "dash";
            case BoonType.Score: case BoonType.ScoreBoost: return "punts";
            case BoonType.SlowEnemies: return "clients lents";
            case BoonType.MultiShot: return "ventall";
            case BoonType.Pierce: return "travessar";
            case BoonType.Explosive: return "explosions";
            case BoonType.SlowOnHit: return "gel";
            case BoonType.DashDamage: return "dash que crema";
            case BoonType.HealOnKills: return "curar despatxant";
            case BoonType.MoreChoices: return "més cartes";
            case BoonType.ProjectileSpeed: return "trets ràpids";
            case BoonType.Crit: return "crítics";
            case BoonType.FlawlessHeal: return "servei perfecte";
            case BoonType.Ricochet: return "rebots";
            case BoonType.Orbit: return "olives";
            case BoonType.HitWave: return "ona";
            case BoonType.DashRing: return "panellets";
            case BoonType.KillBurst: return "esclats";
            case BoonType.CritDamage: return "crítics forts";
            case BoonType.Regen: return "regeneració";
            case BoonType.Dodge: return "esquivar";
            case BoonType.Area: return "àrea";
            case BoonType.ProjectileSize: return "projectils grans";
            case BoonType.Knockback: return "empenta";
            case BoonType.Rerolls: return "canvis de cartes";
            case BoonType.Magnet: return "imant";
            case BoonType.BackKnives: return "ganivets";
            case BoonType.Soup: return "tolls";
            case BoonType.PieRain: return "pluja de pastissos";
            case BoonType.Cava: return "bombolles";
            case BoonType.Flame: return "flames";
            case BoonType.Bombs: return "bombes";
            case BoonType.Whip: return "fuet";
            case BoonType.Tortilla: return "truites";
        }
        return t.ToString();
    }

    static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    void Update()
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.buttonEast.wasPressedThisFrame);
        if (back) { Close(); return; }
        // canviar de pestanya: Q / E o els botons superiors del comandament
        bool prev = (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.leftShoulder.wasPressedThisFrame);
        bool next = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.rightShoulder.wasPressedThisFrame);
        if (prev) { AudioManager.Play("ui_click"); ShowTab((tab + 2) % 3); }
        if (next) { AudioManager.Play("ui_click"); ShowTab((tab + 1) % 3); }
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

// quan una casella es marca (ratolí o comandament), en mostra el detall
public class CodexHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public UnityEngine.Events.UnityAction onFocus;
    public void OnPointerEnter(PointerEventData e) { onFocus?.Invoke(); }
    public void OnSelect(BaseEventData e) { onFocus?.Invoke(); }
}
