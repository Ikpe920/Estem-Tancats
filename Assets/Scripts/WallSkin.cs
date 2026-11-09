using UnityEngine;

// =====================================================================
//  PARETS DECORADES (textures generades per codi, estil píxel)
// =====================================================================

public static class WallSkin
{
    private static readonly Sprite[] cache = new Sprite[5];

    public static Sprite Get(int act)
    {
        act = ((act % 5) + 5) % 5;
        if (cache[act] != null) return cache[act];

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = Pixel(act, x, y);

        tex.SetPixels32(px);
        tex.Apply();

        cache[act] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        return cache[act];
    }

    static Color32 Pixel(int act, int x, int y)
    {
        switch (act)
        {
            case 0: // la sala: llistons de fusta
            {
                int plank = x / 8;
                if (x % 8 == 0) return C(0.22f, 0.13f, 0.07f);
                float shade = 0.85f + 0.15f * (Hash(plank, 3) % 100) / 100f;
                if ((y + plank * 7) % 13 == 0) shade *= 0.85f;
                return C(0.55f * shade, 0.36f * shade, 0.20f * shade);
            }
            case 1: // la barra: prestatge amb ampolles
            {
                if (y < 3) return C(0.45f, 0.30f, 0.17f);
                if (y == 3) return C(0.28f, 0.18f, 0.10f);

                int bx = x % 8;
                bool body = y >= 4 && y < 20 && bx >= 2 && bx <= 5;
                bool neck = y >= 20 && y < 27 && bx >= 3 && bx <= 4;
                if (body || neck)
                {
                    int b = Hash(x / 8, 1) % 4;
                    if (b == 0) return C(0.25f, 0.60f, 0.30f);
                    if (b == 1) return C(0.70f, 0.35f, 0.15f);
                    if (b == 2) return C(0.30f, 0.45f, 0.75f);
                    return C(0.75f, 0.65f, 0.25f);
                }
                return C(0.20f, 0.13f, 0.10f);
            }
            case 2: // la cuina: rajoles
            {
                if (x % 16 == 0 || y % 16 == 0) return C(0.55f, 0.60f, 0.62f);
                float s = 0.86f + 0.10f * (Hash(x / 16, y / 16) % 100) / 100f;
                if (x % 16 == 1 || y % 16 == 1) s += 0.05f;
                return C(0.82f * s, 0.90f * s, 0.92f * s);
            }
            case 3: // la terrassa: bardissa
            {
                int h = Hash(x, y) % 100;
                if (h < 12) return C(0.30f, 0.62f, 0.28f);
                if (h < 26) return C(0.09f, 0.26f, 0.11f);
                return C(0.16f, 0.40f, 0.18f);
            }
            default: // la teulada: teules
            {
                int row = y / 8;
                int shift = (x + (row % 2) * 8);
                if (y % 8 == 0 || shift % 16 == 0) return C(0.12f, 0.14f, 0.22f);
                float s = 0.80f + 0.20f * (y % 8) / 8f;
                float v = 0.88f + 0.12f * (Hash(shift / 16, row) % 100) / 100f;
                return C(0.33f * s * v, 0.38f * s * v, 0.52f * s * v);
            }
        }
    }

    static Color32 C(float r, float g, float b)
    {
        return new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);
    }

    static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7fffffff;
        }
    }
}
