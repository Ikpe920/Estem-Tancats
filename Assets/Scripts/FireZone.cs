using UnityEngine;

// ---------- LA CUINA: zones de foc ----------
public class FireZone : MonoBehaviour
{
    public float warningTime = 1.4f;
    public float burnTime = 6f;
    public float driftSpeed = 0.9f;

    private SpriteRenderer sr;
    private float born;
    private Vector2 drift;
    private float nextTick;
    private int tick;
    private bool lit;

    // el resplendor del foc a terra: cercle suau (més viu al centre)
    static Sprite glow;
    static Sprite FireGlow()
    {
        if (glow != null) return glow;
        int n = 48;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Sqrt(a) * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return glow;
    }

    float Radius { get { return transform.localScale.x * 0.5f; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        born = Time.time;

        float a = Random.value * Mathf.PI * 2f;
        drift = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * driftSpeed;
    }

    void Update()
    {
        float age = Time.time - born;

        // 1. avís: cercle vermell que es va omplint
        if (age < warningTime)
        {
            // avís: el cercle vermell batega cada cop més de pressa, i ja surt algun fum
            float k = age / warningTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(age * Mathf.Lerp(6f, 22f, k));
            sr.color = new Color(1f, 0.2f, 0.1f, Mathf.Lerp(0.1f, 0.45f, k) * (0.7f + 0.3f * pulse));
            if (Random.value < k * 0.3f) Fx.Flames(transform.position, Radius * 0.6f, 0.3f);
            return;
        }
        if (!lit) { lit = true; sr.sprite = FireGlow(); Fx.Explosion(transform.position, Radius * 0.6f, 0.4f); }

        // 2. crema: es mou, rebota a les parets i fa mal
        float burnAge = age - warningTime;
        float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, transform.position.x);
        float fade = Mathf.Clamp01((burnTime - burnAge) / 1f); // l'últim segon s'apaga
        sr.color = new Color(1f, 0.5f * flicker, 0.1f, 0.6f * fade);
        Fx.Flames(transform.position, Radius, fade);          // flames, brases i fum
        if (Random.value < 0.5f * fade) Fx.Flames(transform.position, Radius, fade);

        Vector3 p = transform.position + (Vector3)(drift * Time.deltaTime);
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * Radius;
        Vector2 c = HazardManager.ArenaCenter;
        if (Mathf.Abs(p.x - c.x) > half.x) { drift.x = -drift.x; p.x = Mathf.Clamp(p.x, c.x - half.x, c.x + half.x); }
        if (Mathf.Abs(p.y - c.y) > half.y) { drift.y = -drift.y; p.y = Mathf.Clamp(p.y, c.y - half.y, c.y + half.y); }
        transform.position = p;

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + 0.3f;
            tick++;
            Burn(tick % 3 == 0); // els clients es cremen més a poc a poc que tu
        }

        if (burnAge >= burnTime) Destroy(gameObject);
    }

    void Burn(bool hurtEnemies)
    {
        foreach (Collider2D h in Physics2D.OverlapCircleAll(transform.position, Radius * 0.9f))
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage();
                continue;
            }

            if (!hurtEnemies) continue;
            Enemy e = h.GetComponent<Enemy>();
            if (e != null) DamageKind.Hit(e, 1, DmgKind.Fire);
        }
    }
}
