using UnityEngine;

// =====================================================================
//  SPRITES DE LES TAULES I LES CAIXES (generats per codi, estil píxel)
// =====================================================================
// Tots es dibuixen a 40 píxels per unitat i es fan un sol cop.
// La mida del sprite en unitats és la mida real de l'objecte (la mateixa que el col·lisionador).
public static class PropArt
{
    public const int PPU = 40;

    static Sprite tableUpright, tableFlipped, shadow;
    static readonly Sprite[] props = new Sprite[5];

    // ---------- PALETA ----------
    static readonly Color Outline = new Color(0.16f, 0.09f, 0.06f);
    static readonly Color Wood = new Color(0.58f, 0.37f, 0.20f);
    static readonly Color WoodDark = new Color(0.42f, 0.25f, 0.13f);
    static readonly Color WoodLight = new Color(0.70f, 0.48f, 0.27f);
    static readonly Color ClothRed = new Color(0.78f, 0.20f, 0.18f);
    static readonly Color ClothWhite = new Color(0.96f, 0.92f, 0.85f);
    static readonly Color Metal = new Color(0.55f, 0.56f, 0.60f);
    static readonly Color MetalDark = new Color(0.33f, 0.34f, 0.38f);

    // ---------- TAULES ----------

    // taula vista des de dalt: fusta amb estovalles de quadres i un gerro al mig (2,4 x 1,2)
    public static Sprite TableUpright()
    {
        if (tableUpright != null) return tableUpright;
        PixelCanvas c = new PixelCanvas(96, 48);

        c.RoundRect(0, 0, 96, 48, 6, Wood);
        Planks(c, 0, 0, 96, 48, true, 7);

        // estovalles amb quadres, una mica més petites que la taula
        for (int y = 6; y < 42; y++)
            for (int x = 10; x < 86; x++)
                c.Set(x, y, ((x - 10) / 6 + (y - 6) / 6) % 2 == 0 ? ClothRed : ClothWhite);
        c.Rect(10, 5, 76, 1, Shade(ClothRed, 0.7f)); // la vora de baix fa ombra

        // gerro amb una flor
        c.Circle(48, 24, 6, new Color(0.35f, 0.55f, 0.75f));
        c.Circle(48, 24, 3, new Color(0.95f, 0.85f, 0.30f));
        c.Set(46, 26, Color.white);

        c.OutlineAround(Outline);
        return tableUpright = c.ToSprite();
    }

    // taula bolcada: el tauler de cantell, fent de paret, i les potes enlaire (1,2 x 2,6)
    public static Sprite TableFlipped()
    {
        if (tableFlipped != null) return tableFlipped;
        PixelCanvas c = new PixelCanvas(48, 104);

        // les quatre potes (dues a cada banda, a dalt i a baix)
        for (int i = 0; i < 2; i++)
        {
            int ly = i == 0 ? 14 : 84;
            c.Rect(30, ly, 16, 6, WoodDark);
            c.Rect(42, ly - 1, 5, 8, Shade(WoodDark, 0.8f));
        }

        // el tauler, gruixut i amb llistons verticals
        c.RoundRect(2, 0, 28, 104, 4, Wood);
        Planks(c, 2, 0, 28, 104, false, 11);
        c.Rect(26, 2, 4, 100, WoodDark);   // el cantell fa ombra
        c.Rect(4, 2, 2, 100, WoodLight);    // la llum a l'altra banda

        // les estovalles han quedat penjant pel davant
        for (int y = 8; y < 96; y++)
            for (int x = 4; x < 12; x++)
                c.Set(x, y, ((x - 4) / 4 + (y - 8) / 4) % 2 == 0 ? ClothRed : ClothWhite);

        c.OutlineAround(Outline);
        return tableFlipped = c.ToSprite();
    }

    // ---------- CAIXES DE CADA ACTE ----------

    // 0 cadira · 1 bóta · 2 caixa de verdura · 3 test amb flors · 4 caixa de fusta
    public static Sprite Prop(int act, Vector2 size)
    {
        act = ((act % 5) + 5) % 5;
        if (props[act] != null) return props[act];

        int w = Mathf.Max(8, Mathf.RoundToInt(size.x * PPU));
        int h = Mathf.Max(8, Mathf.RoundToInt(size.y * PPU));
        PixelCanvas c = new PixelCanvas(w, h);

        switch (act)
        {
            case 0: Chair(c); break;
            case 1: Barrel(c); break;
            case 2: VeggieCrate(c); break;
            case 3: FlowerPot(c); break;
            default: Crate(c); break;
        }

        c.OutlineAround(Outline);
        return props[act] = c.ToSprite();
    }

    static void Chair(PixelCanvas c)
    {
        int w = c.w, h = c.h;
        // seient de fusta amb un coixí vermell, i el respatller a dalt
        c.RoundRect(2, 1, w - 4, h - 9, 3, Wood);
        Planks(c, 2, 1, w - 4, h - 9, true, 6);
        c.RoundRect(7, 5, w - 14, h - 18, 3, new Color(0.72f, 0.22f, 0.20f));
        c.Rect(9, h - 15, w - 18, 2, new Color(0.85f, 0.35f, 0.30f)); // llum del coixí
        c.RoundRect(0, h - 9, w, 8, 2, WoodDark);
        c.Rect(2, h - 4, w - 4, 2, Wood);
    }

    static void Barrel(PixelCanvas c)
    {
        int w = c.w, h = c.h;
        float cx = w / 2f, cy = h / 2f, r = Mathf.Min(w, h) / 2f - 1f;
        // tapa de llistons dins d'un cèrcol de ferro
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (d > r) continue;
                Color col;
                if (d > r - 3f) col = d > r - 1.5f ? MetalDark : Metal;
                else if (d > r - 5f) col = WoodDark;
                else
                {
                    int stave = x / 6;
                    col = x % 6 == 0 ? WoodDark : Shade(Wood, 0.9f + 0.1f * (Hash(stave, 5) % 3));
                }
                c.Set(x, y, col);
            }
        // tap i reflex
        c.Circle(Mathf.RoundToInt(cx + r * 0.35f), Mathf.RoundToInt(cy + r * 0.3f), 2, MetalDark);
        c.Rect(Mathf.RoundToInt(cx - r * 0.5f), Mathf.RoundToInt(cy + r - 2.5f), Mathf.RoundToInt(r), 1, new Color(0.8f, 0.8f, 0.85f));
    }

    static void VeggieCrate(PixelCanvas c)
    {
        int w = c.w, h = c.h;
        // caixa de fusta oberta
        c.Rect(0, 0, w, h, WoodDark);
        c.Rect(3, 3, w - 6, h - 6, Shade(WoodDark, 0.6f));
        // verdura a dins: enciams, tomàquets i pastanagues
        Color[] veg = { new Color(0.40f, 0.72f, 0.28f), new Color(0.85f, 0.22f, 0.18f), new Color(0.95f, 0.55f, 0.15f), new Color(0.30f, 0.60f, 0.22f) };

        for (int y = 7; y < h - 4; y += 7)
            for (int x = 7; x < w - 4; x += 7)
            {
                int k = Hash(x * 3 + y, 11) % veg.Length;
                int ox = Hash(x, y) % 3 - 1, oy = Hash(y, x) % 3 - 1;
                c.Circle(x + ox, y + oy, 4, veg[k]);
                c.Set(x + ox - 1, y + oy + 2, Shade(veg[k], 1.3f));

            }
        // les vores de la caixa, per sobre de la verdura
        c.Rect(0, 0, w, 3, Wood);
        c.Rect(0, h - 3, w, 3, Wood);
        c.Rect(0, 0, 3, h, Wood);
        c.Rect(w - 3, 0, 3, h, Wood);
        c.Rect(0, h - 1, w, 1, WoodLight);
    }

    static void FlowerPot(PixelCanvas c)
    {
        int w = c.w, h = c.h;
        int cx = w / 2, cy = h / 2;
        int r = Mathf.Min(w, h) / 2 - 1;
        Color clay = new Color(0.80f, 0.42f, 0.25f);
        // test de terrissa amb terra
        c.Circle(cx, cy, r, clay);
        c.Circle(cx, cy, r - 3, Shade(clay, 0.75f));
        c.Circle(cx, cy, r - 4, new Color(0.30f, 0.20f, 0.12f));
        // fulles al voltant del centre
        Color leaf = new Color(0.30f, 0.62f, 0.25f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f + 0.3f;
            int lx = cx + Mathf.RoundToInt(Mathf.Cos(a) * r * 0.42f);
            int ly = cy + Mathf.RoundToInt(Mathf.Sin(a) * r * 0.42f);
            c.Circle(lx, ly, Mathf.Max(2, r / 4), i % 2 == 0 ? leaf : Shade(leaf, 1.2f));
        }
        // flors
        Color[] flowers = { new Color(0.95f, 0.45f, 0.65f), new Color(0.98f, 0.85f, 0.30f), Color.white };
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            int fx = cx + Mathf.RoundToInt(Mathf.Cos(a) * r * 0.25f);
            int fy = cy + Mathf.RoundToInt(Mathf.Sin(a) * r * 0.25f);
            c.Circle(fx, fy, 2, flowers[i]);
            c.Set(fx, fy, new Color(0.95f, 0.75f, 0.2f));
        }
        c.Rect(cx - r / 2, cy + r - 2, r, 1, Shade(clay, 1.25f)); // reflex a la vora
    }

    static void Crate(PixelCanvas c)
    {
        int w = c.w, h = c.h;
        Color wood = new Color(0.60f, 0.50f, 0.38f);
        // llistons horitzontals
        c.Rect(0, 0, w, h, wood);
        Planks(c, 0, 0, w, h, true, 8, wood);
        // marc i creu de reforç
        Color frame = Shade(wood, 0.55f);
        c.Rect(0, 0, w, 4, frame);
        c.Rect(0, h - 4, w, 4, frame);
        c.Rect(0, 0, 4, h, frame);
        c.Rect(w - 4, 0, 4, h, frame);
        c.Line(3, 3, w - 4, h - 4, 3, frame);
        c.Line(3, h - 4, w - 4, 3, 3, frame);
        // claus a les cantonades
        foreach (Vector2Int p in new[] { new Vector2Int(2, 2), new Vector2Int(w - 3, 2), new Vector2Int(2, h - 3), new Vector2Int(w - 3, h - 3) })
            c.Set(p.x, p.y, Metal);
        c.Rect(1, h - 1, w - 2, 1, Shade(wood, 1.2f));
    }

    // ---------- PREMIS QUE SURTEN DE LES CAIXES ----------

    static Sprite coffee, pepper, coin, heart;

    // tassa de cafè amb plat i fum (0,8 x 0,8)
    public static Sprite Coffee()
    {
        if (coffee != null) return coffee;
        PixelCanvas c = new PixelCanvas(32, 32);
        Color china = new Color(0.97f, 0.95f, 0.92f);
        Color chinaShade = new Color(0.78f, 0.76f, 0.74f);

        // plat
        c.Ellipse(16, 6, 13, 4, chinaShade);
        c.Ellipse(16, 7, 12, 3, china);
        // nansa
        c.Ring(24, 15, 4, 2, china);
        // tassa
        c.RoundRect(7, 6, 16, 15, 3, china);
        c.Rect(7, 6, 16, 3, chinaShade);
        c.Rect(19, 9, 3, 10, chinaShade);
        // cafè a dalt
        c.Ellipse(15, 20, 7, 2, new Color(0.38f, 0.22f, 0.11f));
        c.Set(12, 21, new Color(0.62f, 0.42f, 0.25f));
        c.OutlineAround(Outline);

        // fum (sense contorn)
        Color steam = new Color(1f, 1f, 1f, 0.75f);
        for (int i = 0; i < 2; i++)
        {
            int sx = 12 + i * 6;
            for (int y = 24; y < 31; y++)
                c.Set(sx + ((y / 2) % 2 == 0 ? 0 : 1), y, steam);
        }
        return coffee = c.ToSprite();
    }

    // bitxo vermell amb la cua verda (0,8 x 0,8)
    public static Sprite Pepper()
    {
        if (pepper != null) return pepper;
        PixelCanvas c = new PixelCanvas(32, 32);
        Color red = new Color(0.86f, 0.15f, 0.12f);
        Color redDark = new Color(0.62f, 0.08f, 0.08f);

        // el cos: cercles cada cop més petits seguint una corba, de dalt a l'esquerra cap a baix a la dreta
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            float x = Mathf.Lerp(10f, 25f, t);
            float y = Mathf.Lerp(22f, 4f, t) + Mathf.Sin(t * Mathf.PI) * 3f;
            int r = Mathf.RoundToInt(Mathf.Lerp(6f, 1.5f, t));
            c.Circle(Mathf.RoundToInt(x), Mathf.RoundToInt(y), r, red);
        }
        // ombra i reflex
        for (int i = 0; i <= 14; i++)
        {
            float t = i / 20f;
            c.Set(Mathf.RoundToInt(Mathf.Lerp(12f, 25f, t)) + 2, Mathf.RoundToInt(Mathf.Lerp(20f, 4f, t) + Mathf.Sin(t * Mathf.PI) * 3f) - 3, redDark);
            c.Set(Mathf.RoundToInt(Mathf.Lerp(8f, 22f, t)), Mathf.RoundToInt(Mathf.Lerp(23f, 7f, t) + Mathf.Sin(t * Mathf.PI) * 3f) + 1, new Color(1f, 0.55f, 0.5f));
        }
        // la cua i el capoll verds
        Color green = new Color(0.25f, 0.62f, 0.22f);
        c.Ellipse(9, 25, 5, 2, green);
        c.Line(8, 26, 5, 30, 2, green);
        c.OutlineAround(Outline);
        return pepper = c.ToSprite();
    }

    // moneda daurada amb una estrella (0,8 x 0,8)
    public static Sprite Coin()
    {
        if (coin != null) return coin;
        PixelCanvas c = new PixelCanvas(32, 32);
        Color gold = new Color(1f, 0.80f, 0.20f);
        Color goldDark = new Color(0.80f, 0.55f, 0.10f);
        Color goldLight = new Color(1f, 0.95f, 0.60f);

        c.Circle(16, 16, 13, goldDark);
        c.Circle(16, 17, 12, gold);
        c.Ring(16, 17, 9, 1, goldDark);
        // estrella de cinc puntes al mig
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (InStar(x + 0.5f - 16f, y + 0.5f - 17f, 6.5f, 2.8f)) c.Set(x, y, goldLight);
        // reflex
        c.Line(8, 22, 11, 26, 2, goldLight);
        c.OutlineAround(Outline);
        return coin = c.ToSprite();
    }

    // cor vermell (per si el HUD no té sprite de cor)
    public static Sprite Heart()
    {
        if (heart != null) return heart;
        PixelCanvas c = new PixelCanvas(32, 32);
        Color red = new Color(0.90f, 0.18f, 0.25f);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float u = (x + 0.5f - 16f) / 13f, v = (y + 0.5f - 15f) / 13f;
                float a = u * u + v * v - 1f;
                if (a * a * a - u * u * v * v * v <= 0f) c.Set(x, y, red); // la corba del cor
            }
        c.Circle(10, 20, 2, new Color(1f, 0.6f, 0.65f));
        c.OutlineAround(Outline);
        return heart = c.ToSprite();
    }

    static bool InStar(float x, float y, float outer, float inner)
    {
        float ang = Mathf.Atan2(y, x) + Mathf.PI / 2f; // una punta cap amunt
        float seg = Mathf.PI * 2f / 5f;
        float a = Mathf.Repeat(ang, seg) - seg / 2f;    // -seg/2 .. seg/2
        float k = Mathf.Abs(a) / (seg / 2f);            // 0 a la punta, 1 entre puntes
        float r = Mathf.Lerp(outer, inner, k);
        return x * x + y * y <= r * r;
    }

    // ---------- OMBRA ----------

    // ombra suau (el·lipse fosca) per posar a sota de taules i caixes
    public static Sprite Shadow()
    {
        if (shadow != null) return shadow;
        int w = 64, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f);
                float dy = (y + 0.5f - h / 2f) / (h / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(1f - d) * 255f * 0.9f);
                px[y * w + x] = new Color32(0, 0, 0, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return shadow = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w); // 1 x 0,5 unitats
    }

    // ---------- ACCESSORIS DELS CLIENTS ESPECIALS ----------

    static Sprite bigTray;

    // safata gran de metall, vista de cantell (l'Escut): fa de paret pel davant
    public static Sprite BigTray()
    {
        if (bigTray != null) return bigTray;
        PixelCanvas c = new PixelCanvas(10, 40);
        c.RoundRect(0, 0, 10, 40, 4, MetalDark);
        c.RoundRect(2, 2, 6, 36, 3, Metal);
        c.Rect(3, 4, 2, 30, new Color(0.85f, 0.86f, 0.9f)); // reflex
        c.OutlineAround(Outline);
        return bigTray = c.ToSprite();
    }

    static Sprite crown, olive;

    // oliva verda farcida de pebrot (Olives farcides)
    public static Sprite Olive()
    {
        if (olive != null) return olive;
        PixelCanvas c = new PixelCanvas(14, 12);
        c.Ellipse(7, 6, 5, 4, new Color(0.45f, 0.58f, 0.2f));
        c.Ellipse(6, 7, 3, 2, new Color(0.62f, 0.75f, 0.32f));
        c.Ellipse(10, 6, 1, 1, new Color(0.85f, 0.2f, 0.18f));
        c.OutlineAround(Outline);
        return olive = c.ToSprite();
    }

    // corona daurada amb pedres (el client VIP)
    public static Sprite Crown()
    {
        if (crown != null) return crown;
        PixelCanvas c = new PixelCanvas(22, 14);
        Color gold = new Color(1f, 0.80f, 0.20f);
        Color goldDark = new Color(0.80f, 0.55f, 0.10f);
        c.Rect(1, 0, 20, 6, gold);
        c.Rect(1, 0, 20, 2, goldDark);
        for (int i = 0; i < 3; i++)          // les tres puntes
        {
            int cx = 3 + i * 8;
            for (int k = 0; k < 7; k++)
            {
                int half = Mathf.Max(0, 3 - k / 2);
                c.Rect(cx - half, 6 + k, half * 2 + 1, 1, gold);
            }
        }
        c.Rect(2, 12, 3, 2, gold); c.Rect(10, 12, 3, 2, gold); c.Rect(18, 12, 3, 2, gold); // boletes a les puntes
        c.Set(6, 3, new Color(0.9f, 0.15f, 0.2f)); c.Set(11, 3, new Color(0.2f, 0.5f, 0.95f)); c.Set(16, 3, new Color(0.9f, 0.15f, 0.2f));
        c.OutlineAround(Outline);
        return crown = c.ToSprite();
    }

    static Sprite glow;

    // llum suau blanca i rodona (1 x 1 unitat) per tenyir del color que calgui
    public static Sprite Glow()
    {
        if (glow != null) return glow;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return glow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ---------- AJUDES ----------

    // llistons de fusta amb veta (horitzontals o verticals)
    static void Planks(PixelCanvas c, int x0, int y0, int w, int h, bool horizontal, int plank, Color? baseColor = null)
    {
        Color b = baseColor ?? Wood;
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                if (!c.IsSet(x, y)) continue;
                int along = horizontal ? x : y;
                int across = horizontal ? y - y0 : x - x0;
                int idx = across / plank;
                Color col = Shade(b, 0.88f + 0.06f * (Hash(idx, 9) % 4));
                if (across % plank == 0) col = Shade(b, 0.65f);                       // junta entre llistons
                else if ((along + idx * 13) % 17 == 0) col = Shade(col, 0.85f);      // veta
                c.Set(x, y, col);
            }
    }

    static Color Shade(Color c, float k)
    {
        return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
    }

    static int Hash(int a, int b)
    {
        unchecked
        {
            int h = a * 73856093 ^ b * 19349663;
            h ^= h >> 13;
            return (h & 0x7fffffff);
        }
    }

    // un llenç de píxels senzill per dibuixar
    class PixelCanvas
    {
        public readonly int w, h;
        readonly Color32[] px;

        public PixelCanvas(int w, int h)
        {
            this.w = w;
            this.h = h;
            px = new Color32[w * h];
        }

        public bool IsSet(int x, int y)
        {
            return x >= 0 && y >= 0 && x < w && y < h && px[y * w + x].a > 0;
        }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }

        public void Rect(int x0, int y0, int rw, int rh, Color c)
        {
            for (int y = y0; y < y0 + rh; y++)
                for (int x = x0; x < x0 + rw; x++)
                    Set(x, y, c);
        }

        public void RoundRect(int x0, int y0, int rw, int rh, int r, Color c)
        {
            for (int y = y0; y < y0 + rh; y++)
                for (int x = x0; x < x0 + rw; x++)
                {
                    int dx = Mathf.Max(x0 + r - x, 0, x - (x0 + rw - 1 - r));
                    int dy = Mathf.Max(y0 + r - y, 0, y - (y0 + rh - 1 - r));
                    if (dx * dx + dy * dy <= r * r) Set(x, y, c);
                }
        }

        public void Circle(int cx, int cy, int r, Color c)
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + r * 0.8f) Set(x, y, c); // +r: sense piquets d'un píxel a les vores
        }

        public void Ellipse(int cx, int cy, int rx, int ry, Color c)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float dx = (x - cx) / (rx + 0.5f), dy = (y - cy) / (ry + 0.5f);
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Ring(int cx, int cy, int r, int thickness, Color c)
        {
            for (int y = cy - r - 1; y <= cy + r + 1; y++)
                for (int x = cx - r - 1; x <= cx + r + 1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r + 0.5f && d >= r - thickness + 0.5f) Set(x, y, c);
                }
        }

        public void Line(int x0, int y0, int x1, int y1, int thickness, Color c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Rect(x - thickness / 2, y - thickness / 2, thickness, thickness, c);
            }
        }

        // contorn d'un píxel als píxels pintats que toquen un de buit (o la vora)
        public void OutlineAround(Color c)
        {
            bool[] edge = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x].a == 0) continue;
                    if (!IsSet(x - 1, y) || !IsSet(x + 1, y) || !IsSet(x, y - 1) || !IsSet(x, y + 1))
                        edge[y * w + x] = true;
                }
            for (int i = 0; i < edge.Length; i++)
                if (edge[i]) px[i] = c;
        }

        public Sprite ToSprite()
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
        }
    }
}
