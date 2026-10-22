using UnityEngine;

// Petits sprites generats per codi per als avisos dels caps (cercles i línies)
public static class BossFx
{
    private static Sprite circle;
    private static Sprite square;

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

    // quadrat blanc d'1x1 unitats (per fer línies d'avís)
    public static Sprite Square()
    {
        if (square != null) return square;

        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return square;
    }

    // cercle d'avís al terra (el diàmetre és 2 * radius)
    public static GameObject Telegraph(Vector2 pos, float radius, Color color, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = color;
        sr.sortingOrder = -2;
        return go;
    }

    // línia d'avís que surt d'un punt i va en una direcció
    public static SpriteRenderer Line(Vector2 from, Vector2 dir, float length, float width, Color color, string name)
    {
        GameObject go = new GameObject(name);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.position = from + dir * (length * 0.5f);
        go.transform.localScale = new Vector3(length, width, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square();
        sr.color = color;
        sr.sortingOrder = -2;
        return sr;
    }

    // fum curt (teletransports, canvis de fase)
    public static void Puff(Vector2 pos, float radius, Color color)
    {
        GameObject go = Telegraph(pos, radius, color, "BossFum");
        Object.Destroy(go, 0.25f);
    }
}
