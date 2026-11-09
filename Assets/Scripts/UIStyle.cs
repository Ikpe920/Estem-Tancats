using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  ESTIL DE LA INTERFÍCIE
// =====================================================================
// Botons i panells en estil píxel (com les cartes dels plats), generats per codi i compartits per tot el joc.
//  - UIStyle.StyleButton(boto, color): marc fosc, cara del color, ratlla de llum a dalt i ombra a baix,
//    i una mica de moviment en passar-hi el ratolí o seleccionar-lo amb el comandament.
//  - UIStyle.Panel(...): un panell fosc amb vora, per posar darrere de textos.
//  - En carregar una escena, els botons que encara tenen l'aspecte per defecte de Unity es posen a to sols.
public static class UIStyle
{
    public static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);
    public static readonly Color Ink = new Color(0.12f, 0.08f, 0.09f, 1f);
    public static readonly Color Green = new Color(0.22f, 0.55f, 0.30f);
    public static readonly Color Grey = new Color(0.36f, 0.35f, 0.40f);

    static Sprite buttonSprite, panelSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // els botons de les escenes (Jugar, Sortir, Començar el joc...) encara són els de Unity: els posem a to
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (Button b in Resources.FindObjectsOfTypeAll<Button>()) // també els inactius
        {
            if (b == null || b.gameObject.scene != scene) continue;
            Image img = b.targetGraphic as Image;
            if (img == null || (img.sprite != null && img.sprite.name != "UISprite")) continue;
            TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
            string t = label != null ? label.text.ToLowerInvariant() : "";
            bool back = t.Contains("sortir") || t.Contains("menú") || t.Contains("enrere");
            StyleButton(b, back ? Grey : Green);
        }
    }

    // ---------- SPRITES (9 trossos: el marc no es deforma) ----------

    // botó: marc fosc de 2 píxels, cara blanca (es tenyeix amb el color de la Image), llum a dalt, ombra a baix
    public static Sprite ButtonSprite()
    {
        if (buttonSprite != null) return buttonSprite;
        int n = 16;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool corner = (x == 0 || x == n - 1) && (y == 0 || y == n - 1);
                Color32 c;
                if (corner) c = new Color32(0, 0, 0, 0);
                else if (x < 2 || x >= n - 2 || y < 2 || y >= n - 2) c = new Color32(40, 28, 30, 255);   // marc
                else if (y >= n - 4) c = new Color32(255, 255, 255, 255);                               // llum
                else if (y < 5) c = new Color32(170, 170, 170, 255);                                    // ombra
                else c = new Color32(225, 225, 225, 255);                                               // cara
                px[y * n + x] = c;
            }
        buttonSprite = Make(px, n, new Vector4(5, 5, 5, 5));
        return buttonSprite;
    }

    // panell: fons fosc mig transparent amb una vora clara d'1 píxel
    public static Sprite PanelSprite()
    {
        if (panelSprite != null) return panelSprite;
        int n = 12;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool corner = (x == 0 || x == n - 1) && (y == 0 || y == n - 1);
                bool border = x < 1 || x >= n - 1 || y < 1 || y >= n - 1;
                bool inner = x == 1 || x == n - 2 || y == 1 || y == n - 2;
                px[y * n + x] = corner ? new Color32(0, 0, 0, 0)
                              : border ? new Color32(20, 14, 16, 255)
                              : inner ? new Color32(255, 220, 140, 255)
                              : new Color32(36, 26, 30, 225);
            }
        panelSprite = Make(px, n, new Vector4(3, 3, 3, 3));
        return panelSprite;
    }

    static Sprite Make(Color32[] px, int n, Vector4 border)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    // ---------- BOTONS ----------

    public static void StyleButton(Button b, Color face)
    {
        if (b == null) return;
        Image img = b.targetGraphic as Image;
        if (img == null) img = b.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = ButtonSprite();
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.25f; // cada píxel del sprite fa 4 píxels: es veu "en píxel"
            img.color = face;
        }
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        cb.selectedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;

        TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle |= FontStyles.Bold;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(30, 20, 22, 255);
        }

        if (b.GetComponent<ButtonJuice>() == null) b.gameObject.AddComponent<ButtonJuice>();
    }

    // ---------- PANELLS I TEXTOS ----------

    // un panell fosc amb vora, com a fill de 'parent' (es posa al darrere dels altres fills)
    public static Image Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, bool behind = true)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        if (behind) go.transform.SetAsFirstSibling();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        Image img = go.GetComponent<Image>();
        img.sprite = PanelSprite();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.25f;
        img.raycastTarget = false;
        return img;
    }

    // contorn fosc per a textos que van sobre el joc (es llegeixen sobre qualsevol fons)
    public static void Outline(TextMeshProUGUI t, float width = 0.2f)
    {
        if (t == null) return;
        t.outlineWidth = width;
        t.outlineColor = new Color32(25, 16, 18, 255);
    }
}

// el botó creix una mica en passar-hi el ratolí o seleccionar-lo, i s'enfonsa en prémer-lo
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    private bool hover, selected, down;
    private Vector3 baseScale = Vector3.one;
    private bool hasBase;

    void OnEnable()
    {
        if (!hasBase) { baseScale = transform.localScale; hasBase = true; }
    }

    void Update()
    {
        float target = down ? 0.95f : (hover || selected) ? 1.06f : 1f;
        float s = Mathf.MoveTowards(transform.localScale.x / Mathf.Max(0.001f, baseScale.x), target, Time.unscaledDeltaTime * 1.6f);
        transform.localScale = baseScale * s;
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
    public void OnPointerDown(PointerEventData e) { down = true; }
    public void OnPointerUp(PointerEventData e) { down = false; }
}
