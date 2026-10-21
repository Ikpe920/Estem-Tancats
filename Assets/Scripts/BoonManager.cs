using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public enum BoonType
{
    Damage,          // +dany a cada tret
    FireRate,        // +disparos per segon
    MoveSpeed,       // +velocitat de moviment
    MaxLives,        // +1 cor màxim
    Heal,            // cura cor
    Invulnerability,  // +temps invulnerable
    DashCooldown,    // -recàrrega del dash
    Score,           // punts immediats
    ScoreBoost,      // +% de punts
    SlowEnemies      // els clients van més lents
}

[System.Serializable]
public class Boon
{
    public string name = "Punta de gelat";        // nom del boón
    public string description = "+1 de dany";      // què fa
    public BoonType type = BoonType.Damage;
    public float amount = 1f;                      // quantitat
    public Color color = new Color(0.95f, 0.75f, 0.45f); // color de la carta
    public Sprite icon;                            // opcional
    public int weight = 10;                        // probabilitat que aparegui
    public int maxStacks = 0;                      // 0 = sense limit
}

public class BoonManager : MonoBehaviour
{
    public Boon[] boons;              // la llibreria de boons
    public int optionsPerOffer = 3;   // quantes opcions s'ofereixen
    public int everyNWaves = 1;       // s'ofereix cada tantes onades

    public GameObject panel;          // el panell (si el deixes buit, es crea sol)
    public TextMeshProUGUI takenText; // opcional: llista de boons actius
    public ShopManager shop;           // els serveis (curar cor, +1 cor, canviar cartes)

    public PlayerShooting shooting;
    public PlayerMovement movement;
    public PlayerHealth health;

    private readonly Dictionary<Boon, int> taken = new Dictionary<Boon, int>();
    private bool picking;
    private Boon[] currentOffer;              // les cartes que s'estan oferint ara
    private Button[] serviceButtons;           // els tres serveis de sota
    private TextMeshProUGUI[] serviceLabels;
    private TextMeshProUGUI titleText;

    void Awake()
    {
        if (shooting == null) shooting = FindAnyObjectByType<PlayerShooting>();
        if (movement == null) movement = FindAnyObjectByType<PlayerMovement>();
        if (health == null) health = FindAnyObjectByType<PlayerHealth>();
        if (shop == null) shop = FindAnyObjectByType<ShopManager>();

        Enemy.speedMultiplier = 1f; // els boons de lentitud no acumulen entre partides
        if (panel != null) panel.SetActive(false);
        if (takenText == null) BuildTakenText();
    }

    void Reset()
    {
        boons = new Boon[]
        {
            MakeBoon("Punta de gelat", "+1 de dany a cada tret", BoonType.Damage, 1f, new Color(0.95f, 0.55f, 0.45f), 12),
            MakeBoon("Càmera ràpida", "Disposes un 25% més ràpid", BoonType.FireRate, 0.06f, new Color(0.95f, 0.85f, 0.45f), 12),
            MakeBoon("Sabates de roba", "Camines un 12% més ràpid", BoonType.MoveSpeed, 0.6f, new Color(0.55f, 0.85f, 0.95f), 12),
            MakeBoon("Coraç de cuir", "+1 cor màxim (i el deixes ple)", BoonType.MaxLives, 1f, new Color(0.95f, 0.45f, 0.6f), 5),
            MakeBoon("Aigua fresca", "Cures 1 cor", BoonType.Heal, 1f, new Color(0.5f, 0.9f, 0.55f), 8),
            MakeBoon("Placa de reforç", "+0,5 s d'invulnerabilitat", BoonType.Invulnerability, 0.5f, new Color(0.7f, 0.7f, 0.85f), 8),
            MakeBoon("Dash de cucut", "-0,25 s de recàrrega del dash", BoonType.DashCooldown, 0.25f, new Color(0.85f, 0.6f, 0.95f), 8),
            MakeBoon("Cope de sorts", "+75 punts de golpe", BoonType.Score, 75f, new Color(0.95f, 0.95f, 0.6f), 10),
            MakeBoon("Boa fortuna", "+25% de punts", BoonType.ScoreBoost, 0.25f, new Color(0.6f, 0.95f, 0.85f), 6),
            MakeBoon("Oli de malson", "Els clients van un 15% més lents", BoonType.SlowEnemies, 0.15f, new Color(0.45f, 0.55f, 0.45f), 7),
        };
    }

    static Boon MakeBoon(string n, string d, BoonType t, float a, Color c, int w)
    {
        Boon b = new Boon();
        b.name = n;
        b.description = d;
        b.type = t;
        b.amount = a;
        b.color = c;
        b.weight = w;
        return b;
    }

    // ---------- OFERIR BOONS ----------

    public bool ShouldOffer(int wave)
    {
        if (boons == null || boons.Length == 0) return false;
        if (everyNWaves <= 0) return false;
        return wave % everyNWaves == 0;
    }

    public IEnumerator OfferBoons()
    {
        Boon[] picked = PickBoons(optionsPerOffer);
        if (picked == null || picked.Length == 0) yield break;

        currentOffer = picked;

        BuildCards(picked);
        BuildTitle();
        BuildServices();
        RefreshServices();

        panel.SetActive(true);
        bool wasPaused = Time.timeScale == 0f;
        Time.timeScale = 0f; // el joc s'atura per triar

        while (!picking) // espera que el jugador trii un boó
        {
            // es pot triar amb el ratolí o amb les tecles 1, 2 i 3
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame && picked.Length > 0) Choose(picked[0]);
                else if (kb.digit2Key.wasPressedThisFrame && picked.Length > 1) Choose(picked[1]);
                else if (kb.digit3Key.wasPressedThisFrame && picked.Length > 2) Choose(picked[2]);
            }
            yield return null;
        }

        panel.SetActive(false);
        if (!wasPaused) Time.timeScale = 1f;
    }

    // ---------- REROLL (servei "Canviar les cartes") ----------

    public bool CanReroll()
    {
        if (boons == null || boons.Length == 0) return false;
        foreach (Boon b in boons)
        {
            if (b == null) continue;
            int stacks = taken.ContainsKey(b) ? taken[b] : 0;
            if (b.maxStacks > 0 && stacks >= b.maxStacks) continue; // ja el té al màxim
            return true;
        }
        return false;
    }

    public bool RerollCards()
    {
        // intentem que les cartes noves no siguin les mateixes que ja s'estan oferint
        List<Boon> exclou = currentOffer != null ? new List<Boon>(currentOffer) : null;
        Boon[] picked = PickBoons(optionsPerOffer, exclou);
        if (picked == null || picked.Length == 0) picked = PickBoons(optionsPerOffer);
        if (picked == null || picked.Length == 0) return false;

        currentOffer = picked;
        BuildCards(picked);
        RefreshServices();
        return true;
    }

    Boon[] PickBoons(int howMany, List<Boon> exclou = null)
    {
        List<Boon> pool = new List<Boon>();
        foreach (Boon b in boons)
        {
            if (b == null) continue;
            int stacks = taken.ContainsKey(b) ? taken[b] : 0;
            if (b.maxStacks > 0 && stacks >= b.maxStacks) continue; // ja el té al màxim
            if (exclou != null && exclou.Contains(b)) continue; // no repetim la mateixa carta
            pool.Add(b);
        }
        if (pool.Count == 0) return null;

        List<Boon> result = new List<Boon>();
        while (result.Count < howMany && pool.Count > 0)
        {
            int total = 0;
            foreach (Boon b in pool) total += Mathf.Max(1, b.weight);

            int roll = Random.Range(0, total);
            int chosen = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(1, pool[i].weight);
                if (roll < 0) { chosen = i; break; }
                chosen = i;
            }

            result.Add(pool[chosen]);
            pool.RemoveAt(chosen);
        }
        return result.ToArray();
    }

    void Choose(Boon boon)
    {
        if (picking) return;
        picking = true;

        taken[boon] = (taken.ContainsKey(boon) ? taken[boon] : 0) + 1;
        Apply(boon);
        RefreshTakenText();
        Debug.Log("Boón: " + boon.name + " (" + boon.description + ")");
    }

    void Apply(Boon boon)
    {
        switch (boon.type)
        {
            case BoonType.Damage:
                if (shooting != null) shooting.damage += Mathf.RoundToInt(boon.amount);
                break;

            case BoonType.FireRate:
                if (shooting != null) shooting.fireRate = Mathf.Max(0.06f, shooting.fireRate - boon.amount);
                break;

            case BoonType.MoveSpeed:
                if (movement != null) movement.speed += boon.amount;
                break;

            case BoonType.MaxLives:
                if (health != null)
                {
                    health.maxLives += Mathf.RoundToInt(boon.amount);
                    health.Heal(Mathf.RoundToInt(boon.amount));
                }
                break;

            case BoonType.Heal:
                if (health != null) health.Heal(Mathf.RoundToInt(boon.amount));
                break;

            case BoonType.Invulnerability:
                if (health != null) health.invulnerabilityTime += boon.amount;
                break;

            case BoonType.DashCooldown:
                if (movement != null) movement.dashCooldown = Mathf.Max(0.15f, movement.dashCooldown - boon.amount);
                break;

            case BoonType.Score:
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(Mathf.RoundToInt(boon.amount));
                break;

            case BoonType.ScoreBoost:
                if (ScoreManager.Instance != null) ScoreManager.Instance.scoreMultiplier += boon.amount;
                break;

            case BoonType.SlowEnemies:
                Enemy.speedMultiplier *= (1f - boon.amount);
                break;
        }
    }

    void RefreshTakenText()
    {
        if (takenText == null) return;
        string s = "Boons: ";
        bool first = true;
        foreach (Boon b in taken.Keys)
        {
            if (!first) s += " · ";
            s += b.name + (taken[b] > 1 ? " x" + taken[b] : "");
            first = false;
        }
        takenText.text = s;
    }

    // ---------- LA INTERFÍCIE (es crea sola si no la cableges) ----------

    void BuildCards(Boon[] picked)
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;

        // esborra les cartes de l'oferta anterior
        for (int i = panel.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = panel.transform.GetChild(i).gameObject;
            if (child.name.StartsWith("Carta"))
            {
                child.SetActive(false); // així no es veu un parpelleig d'un fotograma
                Destroy(child);
            }
        }

        picking = false;

        // les cartes es dimensionen segons la pantalla (el Canvas treballa en píxels)
        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float w = Mathf.Clamp(sw * 0.17f, 150f, 320f);
        float h = Mathf.Clamp(sh * 0.42f, 190f, 460f);
        float gap = w * 0.07f;
        float iconSize = w * 0.22f;
        float cardY = sh * 0.10f; // les cartes van una mica amunt, els serveis queden a sota
        int titleSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.125f, 16f, 34f));
        int descSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.085f, 12f, 24f));

        float totalW = picked.Length * w + (picked.Length - 1) * gap;

        for (int i = 0; i < picked.Length; i++)
        {
            Boon b = picked[i];
            int index = i;

            GameObject card = new GameObject("Carta" + index, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(panel.transform, false);

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-totalW / 2f + w / 2f + index * (w + gap), cardY);

            Image bg = card.GetComponent<Image>();
            bg.color = new Color(0.13f, 0.12f, 0.15f, 0.98f);

            Button btn = card.GetComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => Choose(b));

            // el requadre de color (icono)
            GameObject icon = new GameObject("Icona", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(card.transform, false);
            RectTransform irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 1f);
            irt.anchorMax = new Vector2(0.5f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.sizeDelta = new Vector2(iconSize, iconSize);
            irt.anchoredPosition = new Vector2(0f, -iconSize * 0.5f - 12f);
            Image iimg = icon.GetComponent<Image>();
            iimg.color = b.icon != null ? Color.white : b.color;

            if (b.icon != null) iimg.sprite = b.icon;

            // el nom, sota l'icono
            CreateText(card.transform, "Nom", b.name, titleSize, TextAlignmentOptions.Center,
                new Vector2(0f, -(iconSize + 24f)), new Vector2(w - 24f, titleSize * 1.8f), Color.white);

            // la descripcio, sota el nom
            CreateText(card.transform, "Descripcio", b.description, descSize, TextAlignmentOptions.Top,
                new Vector2(0f, -(iconSize + 24f + titleSize * 1.9f)), new Vector2(w - 28f, descSize * 4.5f), new Color(0.85f, 0.85f, 0.85f));
        }
    }

    void BuildPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject panelGo = new GameObject("BoonsPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        panelGo.transform.SetAsLastSibling();

        RectTransform rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panelGo.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        panel = panelGo;
    }

    // ---------- EL TÍTOL ("Tria un boó") ----------

    void BuildTitle()
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;
        if (titleText != null) return;

        float sh = Mathf.Max(240f, Screen.height);
        int size = Mathf.RoundToInt(Mathf.Clamp(sh * 0.05f, 22f, 56f));

        titleText = CreateText(panel.transform, "Titol", "Tria un boó", size, TextAlignmentOptions.Center,
            new Vector2(0f, -30f), new Vector2(sh * 0.9f, size * 1.6f), Color.white);
    }

    // ---------- ELS SERVEIS (la botiga, a sota de les cartes) ----------

    void BuildServices()
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;
        if (serviceButtons != null && serviceButtons.Length == 3) return;

        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float w = Mathf.Clamp(sw * 0.19f, 180f, 320f);
        float h = Mathf.Clamp(sh * 0.07f, 42f, 72f);
        float gap = w * 0.06f;
        float y = -sh * 0.30f; // la fila de serveis queda a sota de les cartes
        float totalW = 3 * w + 2 * gap;

        serviceButtons = new Button[3];
        serviceLabels = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            int index = i;

            GameObject go = new GameObject("Servei" + index, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-totalW / 2f + w / 2f + index * (w + gap), y);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.15f, 0.18f, 0.98f);

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => BuyService(index));

            // el text del servei, centrat dins del botó
            TextMeshProUGUI label = CreateText(go.transform, "Text", "", Mathf.RoundToInt(Mathf.Clamp(h * 0.32f, 13f, 24f)),
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(w - 14f, h), Color.white);
            RectTransform lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = Vector2.zero;
            label.raycastTarget = false;

            serviceButtons[i] = btn;
            serviceLabels[i] = label;
        }
    }

    void RefreshServices()
    {
        if (serviceButtons == null || serviceLabels == null) return;

        int pts = ScoreManager.Instance != null ? ScoreManager.Instance.score : 0;

        for (int i = 0; i < serviceButtons.Length; i++)
        {
            int cost = ServiceCost(i);
            bool usable = ServiceUsable(i);
            bool canPay = pts >= cost;

            serviceButtons[i].interactable = usable && canPay;
            serviceLabels[i].text = ServiceName(i) + " (" + cost + ")";
            serviceLabels[i].color = (usable && canPay) ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
        }
    }

    public void BuyService(int index)
    {
        if (shop == null) shop = FindAnyObjectByType<ShopManager>();
        if (shop == null) return;

        bool bought = false;
        if (index == 0) bought = shop.TryBuyHeal();
        else if (index == 1) bought = shop.TryBuyMaxLife();
        else if (index == 2) bought = shop.TryBuyReroll(this);

        if (bought) RefreshServices();
    }

    int ServiceCost(int index)
    {
        if (shop != null)
        {
            if (index == 0) return shop.healCost;
            if (index == 1) return shop.maxLifeCost;
            return shop.rerollCost;
        }
        if (index == 0) return 20;
        if (index == 1) return 60;
        return 30;
    }

    string ServiceName(int index)
    {
        if (index == 0) return "Curar un cor";
        if (index == 1) return "+1 cor màxim";
        return "Canviar les cartes";
    }

    bool ServiceUsable(int index)
    {
        if (index == 0) // curar un cor
        {
            if (health == null) return false;
            return health.CurrentLives < health.maxLives;
        }
        if (index == 1) // +1 cor màxim
        {
            if (health == null || shop == null) return false;
            return health.maxLives < shop.maxLifeCap;
        }
        return CanReroll(); // canviar les cartes
    }

    // el llistat de boons actius (sota els cors, a dalt a l'esquerra)
    void BuildTakenText()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("BoonsActius", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(Mathf.Max(240f, Screen.width * 0.32f), Mathf.Max(90f, Screen.height * 0.2f));
        rt.anchoredPosition = new Vector2(12f, -60f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = "";
        tmp.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.022f, 14f, 26f));
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(1f, 0.88f, 0.55f);
        tmp.textWrappingMode = TextWrappingModes.Normal;

        takenText = tmp;
    }

    TextMeshProUGUI CreateText(Transform parent, string objName, string text, float size, TextAlignmentOptions align, Vector2 pos, Vector2 box, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    // ----- per provar els boons sense arribar al final de l'onada -----
    [ContextMenu("Oferir boons")]
    void DebugOfferBoons()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(OfferBoons());
    }
}
