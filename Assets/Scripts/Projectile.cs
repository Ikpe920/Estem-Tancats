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

    private readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    void Start()
    {
        Destroy(gameObject, lifetime);

        Rigidbody2D prb = GetComponent<Rigidbody2D>();
        if (prb != null) prb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
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
            e.Flash(Color.white, 0.12f);
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
        sr.color = new Color(1f, 0.55f, 0.1f, 0.5f);
        sr.sortingOrder = 5;
        Destroy(fx, 0.15f);
    }
}