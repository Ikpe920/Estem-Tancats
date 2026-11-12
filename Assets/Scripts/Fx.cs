using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  EFECTES VISUALS (partícules)
// =====================================================================
// Un sistema de partícules propi i lleuger: cada partícula és un SpriteRenderer reaprofitat, i un sol
// component les mou totes. Es crea sol la primera vegada que es fa servir (un per escena).
//
//   Fx.Explosion(pos, radi)        flaix, ona, bola de foc, espurnes, fum i marca de cremat
//   Fx.FrostBurst(pos, radi)       esclat de gel: estelles, flocs i ona blava
//   Fx.Hit(pos, dir, crit)         espurnetes en un impacte (daurades i més als crítics)
//   Fx.Poof(pos)                   fum i engrunes quan un client mor
//   Fx.Shockwave(pos, radi, color) anell que s'expandeix, amb espurnes (romesco, cures...)
//   Fx.Flames(pos, radi)           flames de dibuix animat (6 fotogrames), estrelletes, fum i cremat a terra
//   Fx.FlameJet(pos, dir, abast, angle)  el raig del lanxa-flames
//   Fx.Sparkle(pos, color)         un lluentó (gel, monedes...)
//   Fx.Snowflake(pos)              un floc de neu que cau (clients frenats)
public class Fx : MonoBehaviour
{
    const int Max = 1200;

    struct P
    {
        public Vector2 pos, vel;
        public float drag, rise;           // fregament i pujada (en un joc vist de dalt, "pujar" és anar amunt a la pantalla)
        public float age, life;
        public float size0, size1;
        public Color c0, c1;
        public float rot, rotVel;
        public bool stretch;               // s'allarga en la direcció del moviment (espurnes)
        public bool flame;                 // flama de dibuix animat: fotogrames, creix i s'encongeix, balla
        public float frame0;               // el fotograma on comença (perquè no vagin totes igual)
    }

    static Fx instance;
    private readonly P[] ps = new P[Max];
    private readonly SpriteRenderer[] srs = new SpriteRenderer[Max];
    private readonly bool[] alive = new bool[Max];
    private int cursor;

    static Sprite soft, ring, spark, square, flake;

    static Fx I
    {
        get
        {
            if (instance == null) instance = new GameObject("Fx").AddComponent<Fx>();
            return instance;
        }
    }

    // ---------- EMISSIÓ ----------

    static void EmitFlame(Vector2 pos, Vector2 vel, float life, float size0, float size1, int order = 4, float drag = 0f, float rise = 0.6f)
    {
        Sprite[] fr = FlameFrames();
        if (fr == null) // sense els dibuixos: la flama difuminada d'abans
        {
            Emit(Soft(), pos, vel, life, size0 * 0.7f, 0.1f, new Color(1f, 0.85f, 0.3f, 0.9f), new Color(0.9f, 0.2f, 0.05f, 0f), order, drag, rise * 2f);
            return;
        }
        Emit(fr[0], pos, vel, life, size0, size1, Color.white, new Color(1f, 0.8f, 0.7f, 0f), order, drag, rise);
        Fx f = I;
        int i = (f.cursor + Max - 1) % Max; // la que s'acaba d'emetre
        P p = f.ps[i];
        p.flame = true;
        p.frame0 = Random.Range(0f, fr.Length);
        f.ps[i] = p;
        f.Apply(i);
    }

    static void Emit(Sprite sprite, Vector2 pos, Vector2 vel, float life, float size0, float size1, Color c0, Color c1,
                     int order = 6, float drag = 0f, float rise = 0f, bool stretch = false, float rotVel = 0f)
    {
        Fx f = I;
        int i = f.cursor;
        f.cursor = (f.cursor + 1) % Max; // si n'hi ha massa, es reaprofita la més vella
        if (f.srs[i] == null)
        {
            GameObject go = new GameObject("p");
            go.transform.SetParent(f.transform, false);
            f.srs[i] = go.AddComponent<SpriteRenderer>();
        }
        SpriteRenderer sr = f.srs[i];
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.color = c0;
        sr.enabled = true;
        f.ps[i] = new P
        {
            pos = pos, vel = vel, drag = drag, rise = rise, age = 0f, life = Mathf.Max(0.02f, life),
            size0 = size0, size1 = size1, c0 = c0, c1 = c1,
            rot = Random.Range(0f, 360f), rotVel = rotVel, stretch = stretch
        };
        f.alive[i] = true;
        f.Apply(i);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < Max; i++)
        {
            if (!alive[i]) continue;
            P p = ps[i];
            p.age += dt;
            if (p.age >= p.life) { alive[i] = false; srs[i].enabled = false; continue; }
            p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
            p.vel.y += p.rise * dt;
            p.pos += p.vel * dt;
            p.rot += p.rotVel * dt;
            ps[i] = p;
            Apply(i);
        }
    }

    void Apply(int i)
    {
        P p = ps[i];
        float k = p.age / p.life;
        Transform t = srs[i].transform;
        t.position = p.pos;
        float s = Mathf.Lerp(p.size0, p.size1, k);
        if (p.flame)
        {
            // creix de pressa, es manté i s'encongeix; balla una mica de costat; canvia de fotograma
            s = k < 0.2f ? Mathf.Lerp(p.size0 * 0.35f, p.size0, k / 0.2f) : Mathf.Lerp(p.size0, p.size1, (k - 0.2f) / 0.8f);
            Sprite[] fr = FlameFrames();
            if (fr != null) srs[i].sprite = fr[(int)(p.frame0 + p.age * 14f) % fr.Length];
            t.position = p.pos + Vector2.up * s * 0.35f; // el peu de la flama a terra
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p.age * 9f + p.frame0) * 6f);
            t.localScale = new Vector3(s * (1f + 0.08f * Mathf.Sin(p.age * 17f + p.frame0)), s, 1f);
            srs[i].color = Color.Lerp(p.c0, p.c1, Mathf.Clamp01((k - 0.65f) / 0.35f));
            return;
        }
        if (p.stretch && p.vel.sqrMagnitude > 0.01f)
        {
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(s * Mathf.Clamp(p.vel.magnitude * 0.12f, 1f, 4f), s * 0.45f, 1f);
        }
        else
        {
            t.rotation = Quaternion.Euler(0f, 0f, p.rot);
            t.localScale = new Vector3(s, s, 1f);
        }
        srs[i].color = Color.Lerp(p.c0, p.c1, k);
    }

    // ---------- EFECTES ----------

    public static void Explosion(Vector2 pos, float radius, float strength = 1f)
    {
        radius = Mathf.Max(0.4f, radius);
        // flaix i ona expansiva
        Emit(Soft(), pos, Vector2.zero, 0.12f, radius * 2.2f, radius * 2.7f, new Color(1f, 1f, 0.85f, 0.9f), new Color(1f, 0.7f, 0.2f, 0f), 7);
        Emit(Ring(), pos, Vector2.zero, 0.28f, radius * 0.4f, radius * 2.4f, new Color(1f, 0.9f, 0.5f, 0.9f), new Color(1f, 0.5f, 0.1f, 0f), 7);
        // bola de foc
        int n = Mathf.RoundToInt(6 * strength);
        for (int i = 0; i < n; i++)
        {
            Vector2 o = Random.insideUnitCircle * radius * 0.5f;
            Emit(Soft(), pos + o, o * 2f, Random.Range(0.25f, 0.45f), radius * 0.5f, radius * 1.1f,
                 new Color(1f, Random.Range(0.55f, 0.8f), 0.2f, 0.95f), new Color(0.6f, 0.15f, 0.05f, 0f), 6, 3f, 1f);
        }
        // espurnes
        for (int i = 0; i < Mathf.RoundToInt(14 * strength); i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(5f, 12f) * Mathf.Sqrt(radius);
            Emit(Spark(), pos, d, Random.Range(0.25f, 0.5f), 0.22f, 0.05f, new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.35f, 0.1f, 0.6f), 8, 4f, 0f, true);
        }
        // fum que puja a poc a poc
        for (int i = 0; i < Mathf.RoundToInt(5 * strength); i++)
            Emit(Soft(), pos + Random.insideUnitCircle * radius * 0.6f, Random.insideUnitCircle * 0.6f, Random.Range(0.8f, 1.3f),
                 radius * 0.6f, radius * 1.4f, new Color(0.35f, 0.32f, 0.3f, 0.45f), new Color(0.2f, 0.2f, 0.2f, 0f), 5, 1f, 1.2f);
        // unes quantes flames de dibuix animat que queden cremant
        for (int i = 0; i < Mathf.RoundToInt(4 * strength); i++)
        {
            float sz = Mathf.Clamp(radius * 0.5f, 0.35f, 1.2f) * Random.Range(0.8f, 1.2f);
            EmitFlame(pos + Random.insideUnitCircle * radius * 0.6f, Random.insideUnitCircle * 0.5f, Random.Range(0.5f, 0.8f), sz, sz * 0.2f, 6);
        }
        // marca de cremat a terra
        Emit(ScorchSprite(), pos, Vector2.zero, 4f, radius * 1.9f, radius * 2f, new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0f), -3);
    }

    public static void FrostBurst(Vector2 pos, float radius)
    {
        radius = Mathf.Max(0.4f, radius);
        Emit(Soft(), pos, Vector2.zero, 0.15f, radius * 2f, radius * 2.4f, new Color(0.85f, 0.97f, 1f, 0.8f), new Color(0.5f, 0.85f, 1f, 0f), 7);
        Emit(Ring(), pos, Vector2.zero, 0.3f, radius * 0.4f, radius * 2.3f, new Color(0.8f, 0.95f, 1f, 0.9f), new Color(0.4f, 0.8f, 1f, 0f), 7);
        for (int i = 0; i < 14; i++) // estelles de gel
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(4f, 9f) * Mathf.Sqrt(radius);
            Emit(Spark(), pos, d, Random.Range(0.3f, 0.55f), 0.28f, 0.08f, new Color(0.9f, 1f, 1f, 1f), new Color(0.5f, 0.85f, 1f, 0.5f), 8, 5f, 0f, true);
        }
        for (int i = 0; i < 10; i++) // flocs que queden surant
            Emit(Flake(), pos + Random.insideUnitCircle * radius, Random.insideUnitCircle * 0.8f, Random.Range(0.6f, 1.1f), 0.22f, 0.05f,
                 new Color(1f, 1f, 1f, 0.95f), new Color(0.7f, 0.9f, 1f, 0f), 8, 2f, -0.3f, false, Random.Range(-180f, 180f));
    }

    public static void Hit(Vector2 pos, Vector2 dir, bool crit)
    {
        Vector2 back = dir.sqrMagnitude > 0.01f ? -dir.normalized : Random.insideUnitCircle.normalized;
        int n = crit ? 9 : 4;
        Color c0 = crit ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 1f, 0.95f);
        for (int i = 0; i < n; i++)
        {
            Vector2 d = (Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f)) * back) * Random.Range(3f, crit ? 9f : 6f);
            Emit(Spark(), pos, d, Random.Range(0.12f, crit ? 0.32f : 0.22f), crit ? 0.2f : 0.14f, 0.04f, c0, new Color(c0.r, c0.g * 0.6f, 0.2f, 0f), 9, 6f, 0f, true);
        }
        if (crit) Emit(Soft(), pos, Vector2.zero, 0.12f, 0.6f, 1.1f, new Color(1f, 0.95f, 0.6f, 0.9f), new Color(1f, 0.8f, 0.3f, 0f), 9);
    }

    public static void Poof(Vector2 pos)
    {
        for (int i = 0; i < 8; i++) // fum
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(1f, 2.5f);
            Emit(Soft(), pos + d * 0.1f, d, Random.Range(0.35f, 0.6f), 0.5f, 1.1f, new Color(0.95f, 0.93f, 0.9f, 0.75f), new Color(0.7f, 0.7f, 0.7f, 0f), 6, 4f, 0.6f);
        }
        Color[] crumbs = { new Color(0.85f, 0.6f, 0.3f), new Color(0.9f, 0.3f, 0.25f), new Color(0.5f, 0.75f, 0.3f), new Color(1f, 0.9f, 0.6f) };
        for (int i = 0; i < 7; i++) // engrunes de menjar
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            Color c = crumbs[Random.Range(0, crumbs.Length)];
            Emit(Square(), pos, d, Random.Range(0.35f, 0.6f), 0.16f, 0.1f, c, new Color(c.r, c.g, c.b, 0f), 7, 5f, 0f, false, Random.Range(-400f, 400f));
        }
    }

    public static void Shockwave(Vector2 pos, float radius, Color color)
    {
        Emit(Ring(), pos, Vector2.zero, 0.35f, radius * 0.3f, radius * 2.2f, new Color(color.r, color.g, color.b, 0.95f), new Color(color.r, color.g, color.b, 0f), 7);
        Emit(Soft(), pos, Vector2.zero, 0.25f, radius * 1.4f, radius * 2.1f, new Color(color.r, color.g, color.b, 0.35f), new Color(color.r, color.g, color.b, 0f), 6);
        for (int i = 0; i < 10; i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(4f, 8f) * Mathf.Sqrt(radius);
            Emit(Spark(), pos, d, Random.Range(0.25f, 0.4f), 0.18f, 0.05f, Color.Lerp(color, Color.white, 0.5f), new Color(color.r, color.g, color.b, 0f), 8, 4f, 0f, true);
        }
    }

    // flames d'una zona de foc: s'han de cridar sovint (la zona ho fa a cada fotograma)
    // flames de dibuix animat (Resources/Fx/Flame_0..5), estrelletes, fum amb contorn i cremat a terra
    public static void Flames(Vector2 pos, float radius, float amount = 1f)
    {
        float size = Mathf.Clamp(radius * 0.9f, 0.3f, 1.3f);
        if (Random.value < 0.9f * amount)
        {
            Vector2 o = Random.insideUnitCircle * radius * 0.8f;
            float sz = size * Random.Range(0.7f, 1.1f);
            EmitFlame(pos + o, new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.3f, 0.7f)), Random.Range(0.45f, 0.75f), sz, sz * 0.25f);
        }
        if (Random.value < 0.22f * amount)
            Emit(EmberSprite(), pos + Random.insideUnitCircle * radius * 0.7f, new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(2f, 3.5f)), Random.Range(0.4f, 0.8f),
                 0.2f, 0.05f, Color.white, new Color(1f, 0.6f, 0.3f, 0f), 5, 1f, 0f, false, Random.Range(-360f, 360f));
        if (Random.value < 0.05f * amount)
            Emit(SmokeSprite(), pos + Random.insideUnitCircle * radius * 0.5f + Vector2.up * (0.4f + size * 0.6f), new Vector2(Random.Range(-0.2f, 0.2f), 0.7f),
                 Random.Range(0.9f, 1.4f), 0.35f, 0.9f, new Color(0.55f, 0.5f, 0.48f, 0.75f), new Color(0.35f, 0.33f, 0.32f, 0f), 5, 0.5f, 0.5f, false, Random.Range(-30f, 30f));
        Scorch(pos, radius, amount);
    }

    // el cremat que queda a terra on hi ha hagut foc (poques vegades, i ben espaiat)
    static float nextScorch;
    static Vector2 lastScorch = new Vector2(9999f, 9999f);
    public static void Scorch(Vector2 pos, float radius, float amount = 1f)
    {
        if (Time.time < nextScorch || Random.value > 0.25f * amount) return;
        if (Vector2.Distance(pos, lastScorch) < Mathf.Max(0.5f, radius) && Time.time < nextScorch + 1f) return;
        nextScorch = Time.time + 0.2f;
        lastScorch = pos;
        float sz = Mathf.Clamp(radius * 2.2f, 0.6f, 3f) * Random.Range(0.85f, 1.15f);
        Emit(ScorchSprite(), pos, Vector2.zero, Random.Range(5f, 7f), sz, sz * 1.05f, new Color(1f, 1f, 1f, 0.85f), new Color(1f, 1f, 1f, 0f), -3, 0f, 0f, false, 0f);
    }

    // el raig del lanxa-flames: flames que surten disparades en un con i es fan grans
    public static void FlameJet(Vector2 from, Vector2 dir, float range, float halfAngle, float amount = 1f)
    {
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();
        int n = Random.value < amount ? (Random.value < amount * 0.5f ? 2 : 1) : 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-halfAngle, halfAngle) * 0.8f) * dir;
            float life = Random.Range(0.3f, 0.42f);
            float speed = range / life * Random.Range(0.75f, 1f);
            EmitFlame(from + dir * 0.5f, d * speed, life, Random.Range(0.25f, 0.35f), Mathf.Clamp(range * 0.28f, 0.5f, 1.3f), 6, 1.5f, 0.3f);
        }
        if (Random.value < 0.15f * amount)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-halfAngle, halfAngle)) * dir;
            Emit(EmberSprite(), from + dir * 0.5f, d * range * 3f, Random.Range(0.3f, 0.5f), 0.18f, 0.05f, Color.white, new Color(1f, 0.6f, 0.3f, 0f), 7, 2f, 0f, false, 300f);
        }
        if (Random.value < 0.06f * amount) Scorch(from + dir * range * Random.Range(0.5f, 0.95f), range * 0.25f, 1f);
    }

    // ---------- ELS DIBUIXOS DEL FOC (Tools/SpriteGen/fire.py) ----------
    static Sprite[] flameFrames;
    static bool flamesLoaded;
    static Sprite ember, smokeSp, scorchSp;

    static Sprite[] FlameFrames()
    {
        if (flamesLoaded) return flameFrames;
        flamesLoaded = true;
        var list = new List<Sprite>();
        for (int i = 0; i < 8; i++)
        {
            Sprite sp = Resources.Load<Sprite>("Fx/Flame_" + i);
            if (sp == null) break;
            list.Add(sp);
        }
        flameFrames = list.Count > 0 ? list.ToArray() : null;
        return flameFrames;
    }

    static Sprite EmberSprite()
    {
        if (ember == null) ember = Resources.Load<Sprite>("Fx/Ember");
        return ember != null ? ember : Spark();
    }

    static Sprite SmokeSprite()
    {
        if (smokeSp == null) smokeSp = Resources.Load<Sprite>("Fx/Smoke");
        return smokeSp != null ? smokeSp : Soft();
    }

    static Sprite ScorchSprite()
    {
        if (scorchSp == null) scorchSp = Resources.Load<Sprite>("Fx/Scorch");
        return scorchSp != null ? scorchSp : Soft();
    }

    public static void Sparkle(Vector2 pos, Color color)
    {
        Emit(Flake(), pos, Vector2.zero, Random.Range(0.35f, 0.6f), 0.05f, 0.28f, new Color(color.r, color.g, color.b, 1f), new Color(1f, 1f, 1f, 0f), 7, 0f, 0f, false, 90f);
    }

    public static void Snowflake(Vector2 pos)
    {
        Emit(Flake(), pos, new Vector2(Random.Range(-0.4f, 0.4f), -0.6f), Random.Range(0.5f, 0.8f), 0.16f, 0.06f,
             new Color(0.9f, 0.98f, 1f, 0.95f), new Color(0.6f, 0.85f, 1f, 0f), 8, 0f, 0f, false, Random.Range(-120f, 120f));
    }

    // una onada petita en un bassal
    public static void Ripple(Vector2 pos, float size, Color color)
    {
        Emit(Ring(), pos, Vector2.zero, 0.8f, size * 0.2f, size, new Color(color.r, color.g, color.b, 0.6f), new Color(color.r, color.g, color.b, 0f), -1);
    }

    // pols als peus (el dash)
    public static void Dust(Vector2 pos)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 d = Random.insideUnitCircle * 1.5f;
            Emit(Soft(), pos + Vector2.down * 0.4f + d * 0.1f, d, Random.Range(0.3f, 0.5f), 0.3f, 0.7f,
                 new Color(0.85f, 0.8f, 0.72f, 0.6f), new Color(0.8f, 0.75f, 0.7f, 0f), 2, 4f, 0.3f);
        }
    }

    // un grapat de lluentons (premis, cures)
    public static void Sparkles(Vector2 pos, Color color, int n = 6, float spread = 0.8f)
    {
        for (int i = 0; i < n; i++)
            Emit(Flake(), pos + Random.insideUnitCircle * spread, new Vector2(0f, Random.Range(0.5f, 1.5f)), Random.Range(0.4f, 0.7f), 0.08f, 0.26f,
                 new Color(color.r, color.g, color.b, 1f), new Color(1f, 1f, 1f, 0f), 9, 1f, 0f, false, 120f);
    }

    // ---------- SPRITES DEL TERRA ----------

    static Sprite frostGround, oilPuddle;

    // terra glaçat: disc blau clar amb la vora més marcada i esquerdes (1 unitat de diàmetre)
    public static Sprite FrostGround()
    {
        if (frostGround != null) return frostGround;
        int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[n * n];
        System.Random r = new System.Random(5);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                d += (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 0.12f; // vora irregular
                if (d > 1f) { px[y * n + x] = new Color32(0, 0, 0, 0); continue; }
                float edge = Mathf.Clamp01((d - 0.8f) / 0.2f);
                byte a = (byte)(Mathf.Lerp(120f, 230f, edge));
                px[y * n + x] = new Color32((byte)Mathf.Lerp(200, 235, edge), (byte)Mathf.Lerp(235, 250, edge), 255, a);
            }
        // esquerdes: ratlles blanques des del centre
        for (int k = 0; k < 7; k++)
        {
            float ang = k * Mathf.PI * 2f / 7f + (float)r.NextDouble() * 0.5f;
            float len = 18f + (float)r.NextDouble() * 10f;
            for (float t = 4f; t < len; t += 0.5f)
            {
                int x = Mathf.RoundToInt(n / 2f + Mathf.Cos(ang) * t + Mathf.Sin(t * 0.7f) * 1.2f);
                int y = Mathf.RoundToInt(n / 2f + Mathf.Sin(ang) * t);
                if (x >= 0 && y >= 0 && x < n && y < n && px[y * n + x].a > 0) px[y * n + x] = new Color32(255, 255, 255, 240);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        frostGround = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return frostGround;
    }

    // bassal d'oli: forma irregular (uns quants cercles junts), daurat fosc, amb reflexos (1 unitat)
    public static Sprite OilPuddle()
    {
        if (oilPuddle != null) return oilPuddle;
        int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[n * n];
        Vector3[] blobs = { new Vector3(32, 32, 20), new Vector3(20, 38, 12), new Vector3(44, 26, 13), new Vector3(40, 44, 10), new Vector3(22, 22, 10), new Vector3(50, 38, 7) };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float inside = 0f;
                foreach (Vector3 b in blobs)
                {
                    float dd = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(b.x, b.y)) / b.z;
                    inside = Mathf.Max(inside, 1f - dd);
                }
                if (inside <= 0f) { px[y * n + x] = new Color32(0, 0, 0, 0); continue; }
                bool rim = inside < 0.12f;
                Color c = rim ? new Color(0.45f, 0.3f, 0.08f, 0.85f) : Color.Lerp(new Color(0.75f, 0.55f, 0.15f, 0.7f), new Color(0.95f, 0.8f, 0.35f, 0.75f), inside);
                px[y * n + x] = c;
            }
        // reflexos blancs
        foreach (Vector2Int h in new[] { new Vector2Int(26, 40), new Vector2Int(27, 40), new Vector2Int(28, 41), new Vector2Int(38, 32), new Vector2Int(39, 32), new Vector2Int(18, 30) })
            px[h.y * n + h.x] = new Color32(255, 250, 220, 230);
        tex.SetPixels32(px);
        tex.Apply();
        oilPuddle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return oilPuddle;
    }

    // ---------- SPRITES ----------

    static Sprite Soft()
    {
        if (soft != null) return soft;
        soft = Radial(32, (d) => { float a = Mathf.Clamp01(1f - d); return a * a; }, FilterMode.Bilinear);
        return soft;
    }

    static Sprite Ring()
    {
        if (ring != null) return ring;
        ring = Radial(64, (d) => Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f), FilterMode.Bilinear);
        return ring;
    }

    static Sprite Square()
    {
        if (square != null) return square;
        square = Pixels(4, (x, y) => true);
        return square;
    }

    // espurna: un rombe estret (s'allarga segons la velocitat)
    static Sprite Spark()
    {
        if (spark != null) return spark;
        spark = Pixels(8, (x, y) => Mathf.Abs(x - 3.5f) / 4f + Mathf.Abs(y - 3.5f) / 2f <= 1f);
        return spark;
    }

    // floc / lluentó: una creu petita
    static Sprite Flake()
    {
        if (flake != null) return flake;
        flake = Pixels(7, (x, y) => x == 3 || y == 3 || (Mathf.Abs(x - 3) == Mathf.Abs(y - 3) && Mathf.Abs(x - 3) <= 1));
        return flake;
    }

    static Sprite Radial(int n, System.Func<float, float> alpha, FilterMode filter)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = filter;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(alpha(d) * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n); // 1 unitat de diàmetre
    }

    static Sprite Pixels(int n, System.Func<int, int, bool> on)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = on(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
