using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  CARTELLS D'ACTE I D'ONADA COMPLETADA
// =====================================================================
//  - Acte nou: una franja fosca que entra de costat amb "ACTE 2" entre dues ratlles, el nom de l'acte
//    que cau de cop i la seva frase.
//  - Onada completada: un segell que cau inclinat, "ONADA 7 SUPERADA!" (o "SERVEI PERFECTE!" si no t'han
//    tocat), i "Prepara't per als plats" a sota.
// L'ActManager els fa servir en lloc del panell de l'escena.
public class ActBanner : MonoBehaviour
{
    static bool lastFlawless;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        ScoreManager.OnWaveEnded -= Remember;
        ScoreManager.OnWaveEnded += Remember;
    }

    static void Remember(bool flawless) { lastFlawless = flawless; }

    private RectTransform canvasRt;

    static ActBanner Create(int order)
    {
        ActBanner b = new GameObject("Cartell").AddComponent<ActBanner>();
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(b.transform, false);
        Canvas c = canvasGo.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        CanvasScaler s = canvasGo.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight = 0.5f;
        b.canvasRt = canvasGo.GetComponent<RectTransform>();
        return b;
    }

    // ---------- ACTE NOU ----------

    public static IEnumerator ActTitle(int actNumber, string name, string subtitle, float hold, Color tint)
    {
        return ActTitle("—  ACTE " + actNumber + "  —", name, subtitle, hold, tint);
    }

    // el mateix cartell amb una etiqueta qualsevol a dalt (per exemple, "HORES EXTRES · 03:00")
    public static IEnumerator ActTitle(string header, string name, string subtitle, float hold, Color tint)
    {
        ActBanner b = Create(240);
        RectTransform root = b.canvasRt;

        Image dim = Box(root, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
        RectTransform band = Box(root, new Color(0.08f, 0.05f, 0.07f, 0.92f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f)).rectTransform;
        band.sizeDelta = new Vector2(0f, 300f);
        Color edge = Color.Lerp(UIStyle.Gold, tint, 0.3f);
        Image top = Box(band, edge, new Vector2(0f, 1f), Vector2.one); top.rectTransform.sizeDelta = new Vector2(0f, 6f);
        Image bot = Box(band, edge, Vector2.zero, new Vector2(1f, 0f)); bot.rectTransform.sizeDelta = new Vector2(0f, 6f);

        TextMeshProUGUI act = MenuScreens.Text(band, header, 38f, new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(900f, 56f), UIStyle.Gold, true);
        TextMeshProUGUI title = MenuScreens.Text(band, name, 110f, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1600f, 130f), Color.white, true);
        TextMeshProUGUI sub = MenuScreens.Text(band, subtitle, 36f, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(1500f, 56f), new Color(1f, 0.92f, 0.8f), false);
        sub.fontStyle = FontStyles.Italic;
        UIStyle.Outline(title, 0.25f);

        float total = 0.5f + hold + 0.45f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime) // el joc pot estar aturat
        {
            float kin = Ease(Mathf.Clamp01(t / 0.4f));
            float kout = Mathf.Clamp01((t - (total - 0.45f)) / 0.45f);
            band.anchoredPosition = new Vector2(-2200f * (1f - kin) + 2200f * kout * kout, 0f);
            dim.color = new Color(0f, 0f, 0f, 0.45f * (kin - kout));
            act.alpha = Mathf.Clamp01((t - 0.25f) / 0.25f);
            float kt = Mathf.Clamp01((t - 0.35f) / 0.22f);
            title.alpha = kt;
            title.transform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, Ease(kt));
            sub.alpha = Mathf.Clamp01((t - 0.7f) / 0.4f);
            yield return null;
        }
        Destroy(b.gameObject);
    }

    // ---------- ONADA COMPLETADA ----------

    public static IEnumerator RoundComplete(int wave, float hold)
    {
        bool perfect = lastFlawless;
        ActBanner b = Create(230);
        RectTransform root = b.canvasRt;

        Color col = perfect ? new Color(0.45f, 0.9f, 0.5f) : UIStyle.Gold;
        RectTransform stamp = MenuScreens.Rect(root, "Segell", new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, Vector2.zero);
        stamp.sizeDelta = new Vector2(820f, 170f);
        Image frame = stamp.gameObject.AddComponent<Image>();
        frame.sprite = UIStyle.PanelSprite();
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 0.2f;
        frame.color = Color.white;
        frame.raycastTarget = false;
        Image inner = Box(stamp, new Color(col.r, col.g, col.b, 0.18f), Vector2.zero, Vector2.one);
        inner.rectTransform.offsetMin = new Vector2(14f, 14f);
        inner.rectTransform.offsetMax = new Vector2(-14f, -14f);

        TextMeshProUGUI main = MenuScreens.Text(stamp, perfect ? "SERVEI PERFECTE!" : BossRush.Active ? "PLAT " + wave + " SERVIT!" : "ONADA " + wave + " SUPERADA!", 70f,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(780f, 90f), col, true);
        TextMeshProUGUI small = MenuScreens.Text(stamp, perfect ? (BossRush.Active ? "Plat " : "Onada ") + wave + " sense cap cop" : "Bona feina, xef", 30f,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(700f, 44f), new Color(1f, 0.95f, 0.85f), false);
        TextMeshProUGUI next = MenuScreens.Text(root, "Prepara't per als plats...", 30f, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(900f, 50f), new Color(1f, 0.9f, 0.75f), false);
        next.rectTransform.anchorMin = next.rectTransform.anchorMax = new Vector2(0.5f, 0.47f);

        float total = 0.3f + hold + 0.35f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            // el segell cau gran i inclinat, i queda picat
            float k = Mathf.Clamp01(t / 0.22f);
            float kout = Mathf.Clamp01((t - (total - 0.35f)) / 0.35f);
            stamp.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, k * k) * (1f - 0.15f * kout);
            stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-18f, -5f, k));
            float a = Mathf.Clamp01(t / 0.12f) * (1f - kout);
            frame.color = new Color(1f, 1f, 1f, a);
            inner.color = new Color(col.r, col.g, col.b, 0.18f * a);
            main.alpha = a;
            small.alpha = a * Mathf.Clamp01((t - 0.3f) / 0.3f);
            next.alpha = (1f - kout) * Mathf.Clamp01((t - 0.6f) / 0.4f);
            yield return null;
        }
        Destroy(b.gameObject);
    }

    // ---------- AJUDES ----------

    static Image Box(Transform parent, Color color, Vector2 aMin, Vector2 aMax)
    {
        RectTransform rt = MenuScreens.Rect(parent, "Caixa", aMin, aMax, Vector2.zero, Vector2.zero);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static float Ease(float x) { return 1f - (1f - x) * (1f - x) * (1f - x); }
}
