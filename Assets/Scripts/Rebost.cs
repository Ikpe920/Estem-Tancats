using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  EL REBOST: millores permanents que es compren amb propines
// =====================================================================
// Com els "PowerUps" de Vampire Survivors: duren per sempre i s'apliquen a cada partida.
// PowerUps.Level("id") diu quin nivell en tens. Es compren a la pantalla del Rebost (menú principal).
public static class PowerUps
{
    public class Def
    {
        public string id, name, desc;
        public int maxLevel;
        public int[] costs; // preu de cada nivell
    }

    public static readonly Def[] All =
    {
        D("cor", "Davantal reforçat", "+1 cor màxim", 2, 300, 700),
        D("critic", "Ganivet esmolat", "+4% de crítics", 5, 100, 200, 300, 400, 500),
        D("cadencia", "Braç de cambrer", "Llances un 4% més ràpid", 5, 120, 240, 360, 480, 600),
        D("velocitat", "Sabates còmodes", "Camines un 4% més ràpid", 5, 80, 160, 240, 320, 400),
        D("dash", "Xancletes lleugeres", "El dash es recarrega un 6% abans", 3, 120, 240, 360),
        D("invul", "Pell de gallina", "+0,15 s d'invulnerabilitat quan et toquen", 2, 150, 300),
        D("propines", "Pot de propines", "+10% de propines en acabar la partida", 5, 150, 300, 450, 600, 750),
        D("canvis", "Segona opinió", "+1 canvi de cartes a cada oferta", 2, 250, 500),
        D("experiencia", "Llibre de receptes", "+10% d'experiència", 5, 150, 300, 450, 600, 750),
        D("imant", "Davantal amb butxaques", "Recolleixes les monedes un 15% més lluny", 3, 100, 200, 300),
        D("cartes", "Carta llarga", "+1 carta a cada oferta", 1, 900),
        D("esmorzar", "Esmorzar de forquilla", "Comences la partida amb un complement a l'atzar", 1, 500),
        D("compl", "Taula de complements", "+1 espai per a complements", 1, 1200),
        D("esp", "Taula d'especialitats", "+1 espai per a especialitats", 1, 1500),
    };

    static Def D(string id, string n, string d, int max, params int[] costs) { return new Def { id = id, name = n, desc = d, maxLevel = max, costs = costs }; }

    public static int Level(string id) { return PlayerPrefs.GetInt("powerup_" + id, 0); }

    public static int CostOf(Def d)
    {
        int lv = Level(d.id);
        return lv >= d.maxLevel ? 0 : d.costs[Mathf.Min(lv, d.costs.Length - 1)];
    }

    public static bool Buy(Def d)
    {
        int lv = Level(d.id);
        if (lv >= d.maxLevel) return false;
        if (!Progression.SpendTips(CostOf(d))) return false;
        PlayerPrefs.SetInt("powerup_" + d.id, lv + 1);
        PlayerPrefs.Save();
        return true;
    }

    // en començar la partida (després del xef triat)
    public static void ApplyToPlayer(PlayerMovement m, PlayerShooting s, PlayerHealth h)
    {
        if (h != null && Level("cor") > 0) h.SetMaxLives(Mathf.Min(PlayerHealth.MaxLivesCap, h.maxLives + Level("cor")));
        if (h != null) h.invulnerabilityTime += 0.15f * Level("invul");
        if (s != null)
        {
            s.critChance += 0.04f * Level("critic");
            s.fireRate *= 1f - 0.04f * Level("cadencia");
        }
        if (m != null)
        {
            m.speed *= 1f + 0.04f * Level("velocitat");
            m.dashCooldown *= 1f - 0.06f * Level("dash");
        }
    }
}

// ---------- LA PANTALLA ----------
public class RebostScreen : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    static readonly Color Bg = new Color(0.10f, 0.07f, 0.06f, 0.97f);
    static readonly Color Row = new Color(0.20f, 0.14f, 0.11f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f);

    private TextMeshProUGUI tipsText;
    private readonly List<System.Action> refreshers = new List<System.Action>();
    private GameObject lastSelected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() { IsOpen = false; }

    public static void Open()
    {
        if (IsOpen) return;
        new GameObject("Rebost").AddComponent<RebostScreen>().Build();
    }

    void Build()
    {
        IsOpen = true;
        lastSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        GameObject canvasGo = new GameObject("CanvasRebost", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = Rect(canvasGo.transform, "Fons", Vector2.zero, Vector2.one);
        root.gameObject.AddComponent<Image>().color = Bg;
        EndScreenUI.CreateText(root, "Titol", "EL REBOST", 80f, new Vector2(0.5f, 0.92f), new Vector2(1200f, 100f), Gold, true).raycastTarget = false;
        EndScreenUI.CreateText(root, "Sub", "Millores per a totes les partides", 28f, new Vector2(0.5f, 0.86f), new Vector2(1200f, 40f), new Color(1f, 0.92f, 0.78f)).raycastTarget = false;
        tipsText = EndScreenUI.CreateText(root, "Propines", "", 34f, new Vector2(0.5f, 0.81f), new Vector2(800f, 48f), Gold, true);
        tipsText.raycastTarget = false;

        Button first = null;
        PowerUps.Def[] all = PowerUps.All;
        int rows = Mathf.CeilToInt(all.Length / 2f);
        for (int i = 0; i < all.Length; i++)
        {
            PowerUps.Def d = all[i];
            int col = i % 2, row = i / 2;
            float x0 = col == 0 ? 0.04f : 0.51f, x1 = col == 0 ? 0.49f : 0.96f;
            float y1 = 0.76f - row * (0.62f / rows), y0 = y1 - 0.62f / rows + 0.012f;
            RectTransform r = Rect(root, d.id, new Vector2(x0, y0), new Vector2(x1, y1));
            r.gameObject.AddComponent<Image>().color = Row;

            TextMeshProUGUI title = Text(r, "", 30f, new Vector2(0.02f, 0.5f), new Vector2(0.62f, 0.98f), Gold, true);
            TextMeshProUGUI desc = Text(r, d.desc, 22f, new Vector2(0.02f, 0.04f), new Vector2(0.62f, 0.5f), new Color(1f, 0.94f, 0.85f), false);
            desc.raycastTarget = false;

            Button buy = EndScreenUI.CreateButton(r, "", new Vector2(0.82f, 0.5f), new Vector2(230f, 56f), new Color(0.20f, 0.45f, 0.25f), null);
            buy.onClick.AddListener(() =>
            {
                if (PowerUps.Buy(d)) { AudioManager.Play("coin_big"); RefreshAll(); }
                else AudioManager.Play("ui_back");
            });
            TextMeshProUGUI buyText = buy.GetComponentInChildren<TextMeshProUGUI>();
            Image buyImg = buy.GetComponent<Image>();
            refreshers.Add(() =>
            {
                int lv = PowerUps.Level(d.id);
                // (la font no té quadrats ni estrelles: el nivell en text)
                title.text = d.name + "  <size=70%><color=#FFFFFFAA>nivell " + lv + "/" + d.maxLevel + "</color></size>";
                int cost = PowerUps.CostOf(d);
                bool max = lv >= d.maxLevel;
                if (buyText != null)
                {
                    buyText.enableAutoSizing = true; buyText.fontSizeMin = 16f; buyText.fontSizeMax = 28f;
                    buyText.text = max ? "COMPLET" : cost + " propines";
                }
                buyImg.color = max ? new Color(0.30f, 0.30f, 0.32f) : (Progression.Tips >= cost ? new Color(0.20f, 0.45f, 0.25f) : new Color(0.45f, 0.20f, 0.18f));
                buy.interactable = !max;
            });
            if (first == null) first = buy;
        }

        EndScreenUI.CreateButton(root, "Tornar", new Vector2(0.5f, 0.06f), new Vector2(300f, 70f), new Color(0.30f, 0.30f, 0.35f), Close);
        RefreshAll();
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    void RefreshAll()
    {
        tipsText.text = "Propines: " + Progression.Tips;
        foreach (System.Action a in refreshers) a();
    }

    TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 aMin, Vector2 aMax, Color color, bool bold)
    {
        TextMeshProUGUI t = EndScreenUI.CreateText(parent, "Text", text, size, new Vector2(0.5f, 0.5f), Vector2.zero, color, bold);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        t.alignment = TextAlignmentOptions.Left;
        t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = size * 0.6f;
        t.raycastTarget = false;
        return t;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    void Update()
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.buttonEast.wasPressedThisFrame);
        if (back) Close();
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
