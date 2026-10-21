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

    public PlayerShooting shooting;
    public PlayerMovement movement;
    public PlayerHealth health;

    private readonly Dictionary<Boon, int> taken = new Dictionary<Boon, int>();
    private bool picking;

    void Awake()
    {
        if (shooting == null) shooting = FindAnyObjectByType<PlayerShooting>();
        if (movement == null) movement = FindAnyObjectByType<PlayerMovement>();
        if (health == null) health = FindAnyObjectByType<PlayerHealth>();

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

        BuildCards(picked);

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

    Boon[] PickBoons(int howMany)
    {
        List<Boon> pool = new List<Boon>();
        foreach (Boon b in boons)
        {
            if (b == null) continue;
            int stacks = taken.ContainsKey(b) ? taken[b] : 0;
            if (b.maxStacks > 0 && stacks >= b.maxStacks) continue; // ja el té al màxim
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
            if (child.name.StartsWith("Carta")) Destroy(child);
        }

        picking = false;

        // les cartes es dimensionen segons la pantalla (el Canvas treballa en píxels)
        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float w = Mathf.Clamp(sw * 0.17f, 150f, 320f);
        float h = Mathf.Clamp(sh * 0.42f, 190f, 460f);
        float gap = w * 0.07f;
        float iconSize = w * 0.22f;
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
            rt.anchoredPosition = new Vector2(-totalW / 2f + w / 2f + index * (w + gap), 0f);

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
