using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// botons i textos compartits per les pantalles de final de partida (Game Over i victòria)
public static class EndScreenUI
{
    // crea un boto ancorat a la pantalla
    public static Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 size, Color color, UnityAction onClick)
    {
        GameObject go = new GameObject("Boto " + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>(); // el fons del boto
        img.color = color;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        btn.onClick.AddListener(() => AudioManager.Play("ui_click")); // so de clic a tots els botons
        if (onClick != null) btn.onClick.AddListener(onClick);

        // el text del boto, estirat per tot el requadre
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);

        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = Mathf.RoundToInt(Mathf.Clamp(size.y * 0.38f, 16f, 34f));
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        UIStyle.StyleButton(btn, color); // aspecte en píxel, com la resta del joc
        return btn;
    }

    // crea un text ancorat a la pantalla
    public static TextMeshProUGUI CreateText(Transform parent, string objName, string text, float fontSize, Vector2 anchor, Vector2 box, Color color, bool bold = false)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    // troba un text de l'escena pel nom del GameObject
    // el canvas de l'escena oberta (no els que duren entre escenes, com el del filtre de pel·lícula o
    // el del cercle de les transicions, que no reben clics)
    public static Canvas SceneCanvas()
    {
        Canvas found = null;
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Canvas c = root.GetComponentInChildren<Canvas>(true);
            if (c == null) continue;
            if (c.GetComponent<GraphicRaycaster>() != null) return c;
            if (found == null) found = c;
        }
        // perquè els botons funcionin, el canvas ha de poder rebre clics
        if (found != null && found.GetComponent<GraphicRaycaster>() == null) found.gameObject.AddComponent<GraphicRaycaster>();
        return found;
    }

    public static TextMeshProUGUI FindText(string name)
    {
        foreach (TextMeshProUGUI t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>())
        {
            if (t.gameObject.name == name) return t;
        }
        return null;
    }
}
