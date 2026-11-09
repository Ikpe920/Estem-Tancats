using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 2f;
    public int damage = 1;
    public float spinSpeed = 360f;

    // efectes dels plats (els posa PlayerShooting en disparar)
    [HideInInspector] public int pierce = 0;              // clients extra que travessa
    [HideInInspector] public float explosionRadius = 0f;  // 0 = no explota
    [HideInInspector] public float slowFactor = 1f;       // 1 = no frena
    [HideInInspector] public float slowDuration = 0f;
    [HideInInspector] public bool isCrit = false;        // tret crític: més gran i daurat

    private readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    void Start()
    {
        Destroy(gameObject, lifetime);

        Rigidbody2D prb = GetComponent<Rigidbody2D>();
        if (prb != null) prb.interpolation = RigidbodyInterpolation2D.Interpolate;

        ApplyDishLook();
    }

    // l'aspecte del projectil canvia segons els plats que has triat
    void ApplyDishLook()
    {
        if (isCrit)
        {
            transform.localScale *= 1.35f;
            foreach (SpriteRenderer csr in GetComponentsInChildren<SpriteRenderer>())
                csr.color = Color.Lerp(csr.color, new Color(1f, 0.92f, 0.4f), 0.55f);
        }

        bool fire = explosionRadius > 0f;   // Calçots: foc
        bool ice = slowDuration > 0f;       // Sorbet: tint blau
        bool trail = pierce > 0;            // Bunyols: estela

        Color tint = Color.white;
        int n = 0;
        Color sum = Color.black;
        if (fire) { sum += new Color(1f, 0.5f, 0.15f); n++; }
        if (ice) { sum += new Color(0.5f, 0.85f, 1f); n++; }
        if (trail) { sum += new Color(1f, 0.92f, 0.65f); n++; }
        if (n == 0) return;
        tint = sum / n;
        tint.a = 1f;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.color = Color.Lerp(sr.color, tint, fire || ice ? 0.6f : 0.3f);

        // estela: llarga amb Bunyols, curta amb foc o gel
        if (trail || fire || ice)
        {
            TrailRenderer tr = gameObject.AddComponent<TrailRenderer>();
            tr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            tr.time = trail ? 0.3f : 0.15f;
            tr.startWidth = trail ? 0.22f : 0.16f;
            tr.endWidth = 0f;
            tr.startColor = new Color(tint.r, tint.g, tint.b, 0.8f);
            tr.endColor = new Color(tint.r, tint.g, tint.b, 0f);
            tr.sortingOrder = 4;
        }

        // el foc batega una mica
        if (fire) { flicker = true; baseScale = transform.localScale; }
    }

    private bool flicker;
    private Vector3 baseScale = Vector3.one;

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        if (flicker)
        {
            float k = 1f + 0.12f * Mathf.Sin(Time.time * 40f);
            transform.localScale = baseScale * k;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<ArenaWalls>() != null) { Destroy(gameObject); return; }
        Breakable prop = other.GetComponent<Breakable>();
        if (prop != null)
        {
            prop.Hit(damage);
            if (pierce > 0) pierce--;
            else Destroy(gameObject);
            return;
        }
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;
        if (alreadyHit.Contains(enemy)) return;
        alreadyHit.Add(enemy);

        HitEnemy(enemy);

        if (explosionRadius > 0f) Explode(enemy);

        if (pierce > 0) pierce--;
        else Destroy(gameObject);
    }

    void HitEnemy(Enemy e)
    {
        e.TakeDamage(damage);
        if (slowDuration > 0f) e.Slow(slowFactor, slowDuration);
        if (e != null)
        {
            e.Flash(isCrit ? new Color(1f, 0.9f, 0.3f) : Color.white, isCrit ? 0.2f : 0.12f);
        }
    }

    void Explode(Enemy center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D h in hits)
        {
            Enemy e = h.GetComponent<Enemy>();
            if (e == null) e = h.GetComponentInParent<Enemy>(); // igual que a l'impacte directe
            if (e == null || e == center || alreadyHit.Contains(e)) continue;
            alreadyHit.Add(e);
            HitEnemy(e);
        }
        SpawnExplosionFx();
    }

    // cercle taronja que es veu un instant (es genera sol, no cal cap imatge)
    void SpawnExplosionFx()
    {
        GameObject fx = new GameObject("Explosio");
        fx.transform.position = transform.position;
        fx.transform.localScale = Vector3.one * explosionRadius * 2f;
        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.color = slowDuration > 0f ? new Color(0.75f, 0.55f, 0.35f, 0.5f) : new Color(1f, 0.55f, 0.1f, 0.5f);
        sr.sortingOrder = 5;
        Destroy(fx, 0.15f);
    }
}