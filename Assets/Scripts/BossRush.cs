using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  MENÚ DEGUSTACIÓ (Boss Rush): els 8 caps l'un darrere l'altre, contra rellotge, com 8 plats
// =====================================================================
// - Es tria al menú principal. No hi ha història ni clients normals: només caps (i els que facin sortir ells).
// - Comences amb 4 plats (dos cofres) i cada cap et deixa el seu cofre i un cor.
// - El rellotge només corre mentre es juga (no amb les presentacions ni triant plats).
// - Es guarda el millor temps i el màxim de caps vençuts.
public static class BossRush
{
    public const int Bosses = 8;
    const string BestTimeKey = "bossrush_best_time";
    const string BestCountKey = "bossrush_best_count";

    public static bool Active;     // la partida actual és de caps seguits
    public static float Elapsed;   // segons jugats
    public static int Beaten;      // caps vençuts en aquesta partida
    public static bool NewBest;    // aquesta partida ha batut el rècord

    public static float BestTime => PlayerPrefs.GetFloat(BestTimeKey, 0f); // 0 = encara no l'has acabat mai
    public static int BestCount => PlayerPrefs.GetInt(BestCountKey, 0);

    public static void Begin()
    {
        Active = true;
        Elapsed = 0f;
        Beaten = 0;
        NewBest = false;
    }

    // en acabar (guanyant o perdent): desa els rècords
    public static void Finish(bool completed)
    {
        NewBest = false;
        if (completed && (BestTime <= 0f || Elapsed < BestTime))
        {
            PlayerPrefs.SetFloat(BestTimeKey, Elapsed);
            NewBest = true;
        }
        if (Beaten > BestCount)
        {
            PlayerPrefs.SetInt(BestCountKey, Beaten);
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
        if (BestTime > 0f) return "Menú degustació: " + Clock(BestTime);
        if (BestCount > 0) return "Menú degustació: " + BestCount + "/" + Bosses;
        return null;
    }

    // per a les pantalles finals
    public static string ResultLines(bool completed)
    {
        string s = completed
            ? "Temps: " + Clock(Elapsed)
            : "Caps vençuts: " + Beaten + "/" + Bosses + "  ·  Temps: " + Clock(Elapsed);
        if (NewBest) s += "\n<color=#FFD24D>NOU RÈCORD!</color>";
        else if (BestTime > 0f) s += "\nMillor temps: " + Clock(BestTime);
        else if (BestCount > 0) s += "\nRècord: " + BestCount + "/" + Bosses + " caps";
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
