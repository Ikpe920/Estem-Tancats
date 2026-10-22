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
    SlowEnemies,      // els clients van més lents
    MultiShot,       // projectils extra en ventall
    Pierce,          // els projectils travessen clients
    Explosive,       // els projectils exploten
    SlowOnHit,       // els projectils frenen els clients
    DashDamage,      // el dash fa mal
    HealOnKills,     // cures un cor cada X clients
    MoreChoices,     // una carta més a triar
    None             // sense segon efecte
}

[System.Serializable]
public class Boon
{
    public string name = "Punta de gelat";        // nom del boón
    public string description = "+1 de dany";      // què fa
    public BoonType type = BoonType.Damage;
    public float amount = 1f;                      // quantitat
    public BoonType type2 = BoonType.None; // segon efecte (per als plats amb risc)
    public float amount2 = 0f;
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

    private int killsPerHeal = 0;
    private int killCounter = 0;

    private readonly Dictionary<Boon, int> taken = new Dictionary<Boon, int>();
    private bool picking;
    private Boon[] currentOffer;              // les cartes que s'estan oferint ara
    private Button healButton;                 // l'opció de curar (en lloc d'un plat)
    private TextMeshProUGUI healLabel;
    private TextMeshProUGUI titleText;
    public int healAmount = 1;     // cors que cura l'opció de curar
    public int maxHeartsCap = 5;   // màxim de cors possibles (3 inicials + 2 plats)

    void Awake()
    {
        if (shooting == null) shooting = FindAnyObjectByType<PlayerShooting>();
        if (movement == null) movement = FindAnyObjectByType<PlayerMovement>();
        if (health == null) health = FindAnyObjectByType<PlayerHealth>();
        if (shop == null) shop = FindAnyObjectByType<ShopManager>();

        Enemy.speedMultiplier = 1f; // els boons de lentitud no acumulen entre partides
        if (panel != null) panel.SetActive(false);
        if (takenText == null) BuildTakenText();
        Enemy.OnKilled += HandleKill;
        
    }

        void OnDestroy()
    {
        Enemy.OnKilled -= HandleKill;
    }

    void HandleKill(Enemy e)
    {
        if (killsPerHeal <= 0 || health == null) return;

        killCounter++;
        if (killCounter >= killsPerHeal)
        {
            killCounter = 0;
            health.Heal(1);
        }
    }

    void Reset()
    {
        boons = new Boon[]
        {
            // ---- plats que donen un buff ----
            MakeBoon("Salsa brava", "+1 de dany a cada tret", BoonType.Damage, 1f, new Color(0.95f, 0.45f, 0.35f), 12),
            MakeBoon("Cafè sol", "Disposes un 25% més ràpid", BoonType.FireRate, 0.06f, new Color(0.65f, 0.45f, 0.3f), 12),
            MakeBoon("Macarrons amb tomàquet", "Camines més ràpid", BoonType.MoveSpeed, 0.6f, new Color(0.95f, 0.6f, 0.4f), 12),
            MakeBoon("Escudella i carn d'olla", "+1 cor màxim (i el deixes ple)", BoonType.MaxLives, 1f, new Color(0.95f, 0.45f, 0.6f), 5),
            MakeBoon("Gaspatxo fresquet", "Cures 1 cor", BoonType.Heal, 1f, new Color(0.5f, 0.9f, 0.55f), 8),
            MakeBoon("Croquetes cruixents", "+0,5 s d'invulnerabilitat", BoonType.Invulnerability, 0.5f, new Color(0.9f, 0.75f, 0.45f), 8),
            MakeBoon("Xurros amb xocolata", "-0,25 s de recàrrega del dash", BoonType.DashCooldown, 0.25f, new Color(0.7f, 0.45f, 0.35f), 8),
            MakeBoon("Menú del dia", "+75 punts", BoonType.Score, 75f, new Color(0.95f, 0.95f, 0.6f), 10),
            MakeBoon("Safrà", "+25% de punts", BoonType.ScoreBoost, 0.25f, new Color(0.98f, 0.8f, 0.2f), 6),
            MakeBoon("Oli d'oliva verge", "Els clients van un 15% més lents", BoonType.SlowEnemies, 0.15f, new Color(0.65f, 0.75f, 0.3f), 7),

            // ---- plats que canvien com es juga ----
            MakeBoon("Paella mixta", "Cada tret en llença 2 més, en ventall", BoonType.MultiShot, 2f, new Color(0.95f, 0.75f, 0.25f), 5, 2),
            MakeBoon("Bunyols de bacallà", "Els teus projectils travessen 1 client", BoonType.Pierce, 1f, new Color(0.9f, 0.85f, 0.6f), 6, 3),
            MakeBoon("Calçots amb salvitxada", "Els teus projectils exploten i fan mal als clients de prop", BoonType.Explosive, 1.5f, new Color(0.95f, 0.4f, 0.2f), 5, 1),
            MakeBoon("Sorbet de llimona", "Els teus projectils frenen els clients durant 2 s", BoonType.SlowOnHit, 0.4f, new Color(0.7f, 0.95f, 0.9f), 6, 2),
            MakeBoon("Pollastre al curri", "El dash crema els clients que travessis", BoonType.DashDamage, 2f, new Color(0.95f, 0.65f, 0.15f), 5, 3),
            MakeBoon("Botifarra amb mongetes", "Cada 12 clients que despatxis, cures 1 cor", BoonType.HealOnKills, 12f, new Color(0.75f, 0.5f, 0.4f), 5, 2),
            MakeBoon("Menú de degustació", "A partir d'ara pots triar entre una carta més", BoonType.MoreChoices, 1f, new Color(0.85f, 0.7f, 0.9f), 3, 2),

            // ---- plats amb risc i recompensa ----
            MakeBoon("Xili amb carn", "+2 de dany, però els clients van un 15% més ràpids", BoonType.Damage, 2f, new Color(0.85f, 0.25f, 0.2f), 6, 0, BoonType.SlowEnemies, -0.15f),
            MakeBoon("Fondue de formatge", "Dispares molt més ràpid, però camines més lent", BoonType.FireRate, 0.1f, new Color(0.98f, 0.9f, 0.5f), 6, 0, BoonType.MoveSpeed, -0.8f),
        };
    }

    static Boon MakeBoon(string n, string d, BoonType t, float a, Color c, int w,
                         int maxStacks = 0, BoonType t2 = BoonType.None, float a2 = 0f)
    {
        Boon b = new Boon();
        b.name = n;
        b.description = d;
        b.type = t;
        b.amount = a;
        b.color = c;
        b.weight = w;
        b.maxStacks = maxStacks;
        b.type2 = t2;
        b.amount2 = a2;
        return b;
    }

    // clic dret sobre el component → "Regenerar plats": posa la llista nova sense tocar la resta
    [ContextMenu("Regenerar plats")]
    void RegenerateDishes()
    {
        Reset();
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
        BuildHealButton();
        RefreshHealButton();

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
        RefreshHealButton();
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
            if (b.type == BoonType.MaxLives && health != null && health.maxLives >= maxHeartsCap) continue;
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
        Debug.Log("Plats: " + boon.name + " (" + boon.description + ")");
    }

    void Apply(Boon boon)
    {
        ApplyEffect(boon.type, boon.amount);
        if (boon.type2 != BoonType.None) ApplyEffect(boon.type2, boon.amount2);
    }

    void ApplyEffect(BoonType type, float amount)
    {
        switch (type)
        {
            case BoonType.Damage:
                if (shooting != null) shooting.damage = Mathf.Max(1, shooting.damage + Mathf.RoundToInt(amount));
                break;

            case BoonType.FireRate:
                if (shooting != null) shooting.fireRate = Mathf.Clamp(shooting.fireRate - amount, 0.06f, 1f);
                break;

            case BoonType.MoveSpeed:
                if (movement != null) movement.speed = Mathf.Max(2f, movement.speed + amount);
                break;

            case BoonType.MaxLives:
                if (health != null)
                {
                    int add = Mathf.Min(Mathf.RoundToInt(amount), maxHeartsCap - health.maxLives);
                    if (add > 0)
                    {
                        health.maxLives += add;
                        health.Heal(add);
                    }
                }
                break;

            case BoonType.Heal:
                if (health != null) health.Heal(Mathf.RoundToInt(amount));
                break;

            case BoonType.Invulnerability:
                if (health != null) health.invulnerabilityTime += amount;
                break;

            case BoonType.DashCooldown:
                if (movement != null) movement.dashCooldown = Mathf.Max(0.15f, movement.dashCooldown - amount);
                break;

            case BoonType.Score:
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(Mathf.RoundToInt(amount));
                break;

            case BoonType.ScoreBoost:
                if (ScoreManager.Instance != null) ScoreManager.Instance.scoreMultiplier += amount;
                break;

            case BoonType.SlowEnemies:
                Enemy.speedMultiplier *= (1f - amount); // amb amount negatiu, els clients van més ràpid
                break;

            case BoonType.MultiShot:
                if (shooting != null) shooting.extraShots += Mathf.RoundToInt(amount);
                break;

            case BoonType.Pierce:
                if (shooting != null) shooting.pierce += Mathf.RoundToInt(amount);
                break;

            case BoonType.Explosive:
                if (shooting != null) shooting.explosionRadius += amount;
                break;

            case BoonType.SlowOnHit:
                if (shooting != null)
                {
                    shooting.slowFactor = Mathf.Max(0.3f, Mathf.Min(shooting.slowFactor, 1f - amount));
                    shooting.slowDuration = 2f;
                }
                break;

            case BoonType.DashDamage:
                if (movement != null)
                {
                    DashDamage dd = movement.GetComponent<DashDamage>();
                    if (dd == null) dd = movement.gameObject.AddComponent<DashDamage>();
                    dd.damage += Mathf.RoundToInt(amount);
                }
                break;

            case BoonType.HealOnKills:
                killsPerHeal = killsPerHeal == 0 ? Mathf.RoundToInt(amount) : Mathf.Max(5, killsPerHeal - 3);
                break;

            case BoonType.MoreChoices:
                optionsPerOffer = Mathf.Min(5, optionsPerOffer + Mathf.RoundToInt(amount));
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

    // ---------- EL TÍTOL ("Tria un plat") ----------

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

    // ---------- L'OPCIÓ DE CURAR (en lloc d'agafar un plat) ----------

    void BuildHealButton()
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;
        if (healButton != null) return;

        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float w = Mathf.Clamp(sw * 0.34f, 280f, 560f);
        float h = Mathf.Clamp(sh * 0.08f, 46f, 80f);
        float y = -sh * 0.30f; // a sota de les cartes

        GameObject go = new GameObject("BotoCurar", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panel.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, y);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.16f, 0.24f, 0.18f, 0.98f);

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
        btn.onClick.AddListener(ChooseHeal);

        TextMeshProUGUI label = CreateText(go.transform, "Text", "", Mathf.RoundToInt(Mathf.Clamp(h * 0.32f, 13f, 24f)),
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(w - 14f, h), Color.white);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = Vector2.zero;
        label.raycastTarget = false;

        healButton = btn;
        healLabel = label;
    }

    void RefreshHealButton()
    {
        if (healButton == null || healLabel == null) return;

        bool full = health == null || health.CurrentLives >= health.maxLives;

        healButton.interactable = !full;
        healLabel.text = full
            ? "Cors plens"
            : "Curar " + healAmount + (healAmount > 1 ? " cors" : " cor") + " en lloc d'un plat (H)";
        healLabel.color = full ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white;
    }

    void ChooseHeal()
    {
        if (picking) return;
        if (health == null || health.CurrentLives >= health.maxLives) return;

        picking = true; // això tanca la pantalla, i no s'agafa cap plat
        health.Heal(healAmount);
        Debug.Log("Has triat curar-te en lloc d'agafar un plat");
    }

    // el llistat de plats actius (sota els cors, a dalt a l'esquerra)
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

}
