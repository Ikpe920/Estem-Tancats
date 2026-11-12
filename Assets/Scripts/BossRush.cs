using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  MENÚ DEGUSTACIÓ (Boss Rush): els caps l'un darrere l'altre, contra rellotge, com plats d'un menú
// =====================================================================
// - Es tria al menú principal. No hi ha història ni clients normals: només caps (i els que facin sortir ells).
// - Comences amb 4 plats (dos cofres) i cada cap et deixa el seu cofre i un cor.
// - El rellotge només corre mentre es juga (no amb les presentacions ni triant plats).
// - Hi ha diversos menús, cadascun amb el seu rècord:
//     CLÀSSIC    els 8 caps en ordre
//     DEL XEF    els 8 caps, tots enfurismats
//     SORPRESA   els 8 caps en un ordre a l'atzar
//     INFERNAL   4 plats de dos caps alhora
//     FANTASMA   els 8 caps i, de postres, el Fantasma del Restaurant (cal haver-lo vençut a El Servei)
public static class BossRush
{
    public enum Menu { Classic, Chef, Surprise, Infernal, Ghost }
    public const int GhostIndex = 99; // el Fantasma no és a la llista de caps de l'escena

    public static bool Active;     // la partida actual és de caps seguits
    public static Menu Variant;    // quin menú
    public static float Elapsed;   // segons jugats
    public static int Beaten;      // plats (caps) vençuts en aquesta partida
    public static bool NewBest;    // aquesta partida ha batut el rècord

    static int[] order = { 0, 1, 2, 3, 4, 5, 6, 7 };

    // quants plats té el menú
    public static int Bosses => Variant == Menu.Infernal ? 4 : Variant == Menu.Ghost ? 9 : 8;
    public static bool Furious => Variant == Menu.Chef;
    public static bool GhostUnlocked => PlayerPrefs.GetInt("ghost_beaten", 0) == 1;

    public static string Name(Menu m)
    {
        switch (m)
        {
            case Menu.Chef: return "MENÚ DEL XEF";
            case Menu.Surprise: return "MENÚ SORPRESA";
            case Menu.Infernal: return "MENÚ INFERNAL";
            case Menu.Ghost: return "MENÚ FANTASMA";
        }
        return "MENÚ CLÀSSIC";
    }

    public static string Desc(Menu m)
    {
        switch (m)
        {
            case Menu.Chef: return "Els 8 caps, tots enfurismats. Més durs i més ràpids.";
            case Menu.Surprise: return "Els 8 caps en un ordre a l'atzar. Mai saps què et toca.";
            case Menu.Infernal: return "4 plats de dos caps alhora. Ni temps per respirar.";
            case Menu.Ghost: return GhostUnlocked ? "Els 8 caps i, de postres, el Fantasma del Restaurant."
                                                  : "Bloquejat: primer has de vèncer el Fantasma a El Servei.";
        }
        return "Els 8 caps en ordre, l'un darrere l'altre.";
    }

    static string Key(string what, Menu m) { return "bossrush_" + what + (m == Menu.Classic ? "" : "_" + m); }
    public static float BestTimeOf(Menu m) { return PlayerPrefs.GetFloat(Key("best_time", m), 0f); }
    public static int BestCountOf(Menu m) { return PlayerPrefs.GetInt(Key("best_count", m), 0); }
    public static float BestTime => BestTimeOf(Variant);
    public static int BestCount => BestCountOf(Variant);

    public static void Begin() { Begin(Variant); }

    public static void Begin(Menu m)
    {
        Active = true;
        Variant = m;
        Elapsed = 0f;
        Beaten = 0;
        NewBest = false;
        var list = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        if (m == Menu.Surprise)
            for (int i = list.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int t = list[i]; list[i] = list[j]; list[j] = t; }
        if (m == Menu.Ghost) list.Add(GhostIndex);
        order = list.ToArray();
    }

    // els caps del plat 'plate' (1, 2, 3...): índexs a la llista de caps (GhostIndex = el Fantasma)
    public static int[] BossesForPlate(int plate)
    {
        int p = Mathf.Max(0, plate - 1);
        if (Variant == Menu.Infernal) return new[] { order[(p * 2) % 8], order[(p * 2 + 1) % 8] };
        return new[] { order[p % order.Length] };
    }

    // en acabar (guanyant o perdent): desa els rècords
    public static void Finish(bool completed)
    {
        NewBest = false;
        if (completed)
        {
            Achievements.Unlock("rush_" + Variant);
            if (Variant == Menu.Classic && Elapsed < 600f) Achievements.Unlock("rush_fast");
        }
        if (completed && (BestTime <= 0f || Elapsed < BestTime))
        {
            PlayerPrefs.SetFloat(Key("best_time", Variant), Elapsed);
            NewBest = true;
        }
        if (Beaten > BestCount)
        {
            PlayerPrefs.SetInt(Key("best_count", Variant), Beaten);
            if (!completed) NewBest = true;
        }
        PlayerPrefs.Save();
    }

    public static string Clock(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    // per al menú principal
    public static string MenuLine()
    {
        float t = BestTimeOf(Menu.Classic);
        if (t > 0f) return "Menú degustació: " + Clock(t);
        int c = BestCountOf(Menu.Classic);
        if (c > 0) return "Menú degustació: " + c + "/8";
        return null;
    }

    public static string RecordLine(Menu m)
    {
        float t = BestTimeOf(m);
        if (t > 0f) return "Millor temps: " + Clock(t);
        int c = BestCountOf(m);
        int total = m == Menu.Infernal ? 4 : m == Menu.Ghost ? 9 : 8;
        return c > 0 ? "Rècord: " + c + " de " + total + " plats" : "Encara no l'has tastat";
    }

    // per a les pantalles finals
    public static string ResultLines(bool completed)
    {
        string s = Name(Variant) + "\n" + (completed
            ? "Temps: " + Clock(Elapsed)
            : "Plats vençuts: " + Beaten + "/" + Bosses + "  ·  Temps: " + Clock(Elapsed));
        if (NewBest) s += "\n<color=#FFD24D>NOU RÈCORD!</color>";
        else if (BestTime > 0f) s += "\nMillor temps: " + Clock(BestTime);
        else if (BestCount > 0) s += "\nRècord: " + BestCount + "/" + Bosses + " plats";
        return s;
    }
}

// el rellotge a la pantalla (dalt a la dreta, sota els punts)
public class BossRushHud : MonoBehaviour
{
    private TextMeshProUGUI clock, count;

    public static void Create()
    {
        if (FindAnyObjectByType<BossRushHud>() != null) return;
        GameObject go = new GameObject("RellotgeCapsSeguits", typeof(Canvas), typeof(CanvasScaler));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 40;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        BossRushHud h = go.AddComponent<BossRushHud>();
        h.clock = h.Label(go.transform, 40f, new Vector2(-24f, -140f), UIStyle.Gold);
        h.count = h.Label(go.transform, 24f, new Vector2(-24f, -188f), new Color(1f, 0.92f, 0.8f));
    }

    TextMeshProUGUI Label(Transform parent, float size, Vector2 pos, Color color)
    {
        GameObject g = new GameObject("Text", typeof(RectTransform));
        g.transform.SetParent(parent, false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(360f, 50f);
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Right;
        t.color = color;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        UIStyle.Outline(t);
        return t;
    }

    void Update()
    {
        if (!BossRush.Active) { Destroy(gameObject); return; }
        if (!PlayerHealth.IsDying) BossRush.Elapsed += Time.deltaTime; // amb el joc pausat no avança
        clock.text = BossRush.Clock(BossRush.Elapsed);
        count.text = "PLAT " + Mathf.Min(BossRush.Beaten + 1, BossRush.Bosses) + " DE " + BossRush.Bosses;
    }
}
