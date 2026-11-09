using UnityEngine;

// sprites blancs generats per codi (cercle i quadrat d'1 unitat), creats una sola vegada
public static class SpriteFactory
{
    private static Sprite circle;
    private static Sprite square;
    private static Sprite arrow;

    // cercle blanc d'1 unitat de diàmetre
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

    // quadrat blanc d'1x1 unitats
    public static Sprite Square()
    {
        if (square != null) return square;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return square;
    }

    // triangle blanc que apunta a la dreta (per a les fletxes de la vora de la pantalla)
    public static Sprite Arrow()
    {
        if (arrow != null) return arrow;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // la punta a la dreta, la base a l'esquerra
                float half = (1f - (x + 0.5f) / size) * size * 0.5f;
                bool inside = Mathf.Abs(y + 0.5f - size / 2f) <= half;
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        arrow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return arrow;
    }
}
