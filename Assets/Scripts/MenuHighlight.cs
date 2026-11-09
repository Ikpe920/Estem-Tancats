using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Amb comandament, als menús es veu quin botó està seleccionat: un marc daurat que batega al seu voltant
// i una fletxa al costat. (Molts botons no canvien d'aspecte en seleccionar-los; només amb el ratolí a sobre.)
// El crea el GamepadInput. A les pantalles que es controlen soles (triar xef, triar plat...) no surt.
public class MenuHighlight : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);

    private RectTransform root;
    private readonly Image[] edges = new Image[4]; // dalt, baix, esquerra, dreta
    private Image arrow;
    private readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        GameObject canvasGo = new GameObject("MarcSeleccio", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500; // per sobre de tots els menús

        root = new GameObject("Marc", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvasGo.transform, false);
        root.anchorMin = root.anchorMax = Vector2.zero;
        root.pivot = Vector2.zero;

        for (int i = 0; i < 4; i++) edges[i] = Part("Vora", null);
        arrow = Part("Fletxa", SpriteFactory.Arrow());
        root.gameObject.SetActive(false);
    }

    Image Part(string name, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Gold;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return img;
    }

    void LateUpdate()
    {
        RectTransform target = Target();
        if (target == null)
        {
            if (root.gameObject.activeSelf) root.gameObject.SetActive(false);
            return;
        }
        if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);

        // el rectangle del botó, en píxels de pantalla (tant si el seu canvas és Overlay com si és de càmera)
        Canvas c = target.GetComponentInParent<Canvas>();
        Canvas top = c != null ? c.rootCanvas : null;
        Camera cam = top != null && top.renderMode != RenderMode.ScreenSpaceOverlay ? top.worldCamera : null;
        target.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        float k = Screen.height / 1080f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
        float pad = (6f + 3f * pulse) * k;
        float th = 4f * k;
        min -= Vector2.one * pad;
        max += Vector2.one * pad;
        Vector2 size = max - min;

        Place(edges[0], new Vector2(min.x + size.x / 2f, max.y), new Vector2(size.x + th, th));
        Place(edges[1], new Vector2(min.x + size.x / 2f, min.y), new Vector2(size.x + th, th));
        Place(edges[2], new Vector2(min.x, min.y + size.y / 2f), new Vector2(th, size.y + th));
        Place(edges[3], new Vector2(max.x, min.y + size.y / 2f), new Vector2(th, size.y + th));

        float a = 26f * k;
        Place(arrow, new Vector2(min.x - a * 0.9f - pulse * 6f * k, min.y + size.y / 2f), new Vector2(a, a));

        Color col = Color.Lerp(Gold, Color.white, pulse * 0.35f);
        foreach (Image e in edges) e.color = col;
        arrow.color = col;
    }

    static void Place(Image img, Vector2 center, Vector2 size)
    {
        RectTransform rt = img.rectTransform;
        rt.anchoredPosition = center;
        rt.sizeDelta = size;
    }

    // el botó que cal marcar (o cap)
    RectTransform Target()
    {
        if (!GamepadInput.UsingGamepad || GamepadInput.CustomScreens > 0) return null;
        EventSystem es = EventSystem.current;
        if (es == null) return null;
        GameObject sel = es.currentSelectedGameObject;
        if (sel == null || !sel.activeInHierarchy) return null;
        return sel.transform as RectTransform;
    }
}
