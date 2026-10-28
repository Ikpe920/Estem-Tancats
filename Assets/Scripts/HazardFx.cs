using UnityEngine;

public static class HazardFx
{
    private static Sprite circle;
    private static Sprite square;

    public static Sprite Circle()
    {
        if (circle != null) return circle;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f);
                px[y * size + x] = p.magnitude <= size / 2f
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circle;
    }

    public static Sprite Square()
    {
        if (square != null) return square;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return square;
    }

    // cercle de radi 'radius' (en unitats del món)
    public static SpriteRenderer MakeCircle(Vector2 pos, float radius, Color color, int order = -3)
    {
        GameObject go = new GameObject("Cercle");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // un punt a l'atzar dins de la sala
    public static Vector2 RandomPoint(float margin)
    {
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * margin;
        return HazardManager.ArenaCenter +
               new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
    }

    public static void Shake(float duration, float magnitude)
    {
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(duration, magnitude);
    }

        // text que puja i s'esvaeix (per als drops)
    public static void Popup(Vector2 pos, string text, Color color)
    {
        GameObject go = new GameObject("Avis");
        go.transform.position = pos;

        TMPro.TextMeshPro tmp = go.AddComponent<TMPro.TextMeshPro>();
        if (TMPro.TMP_Settings.defaultFontAsset != null) tmp.font = TMPro.TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = 4f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 60;
        tmp.rectTransform.sizeDelta = new Vector2(8f, 1f);
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        go.AddComponent<PopupText>();
    }
}