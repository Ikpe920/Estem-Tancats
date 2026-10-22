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
    private static Sprite circleSprite;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null || alreadyHit.Contains(enemy)) return;
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
    }

    void Explode(Enemy center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D h in hits)
        {
            Enemy e = h.GetComponent<Enemy>();
            if (e == null || e == center || alreadyHit.Contains(e)) continue;
            alreadyHit.Add(e);
            HitEnemy(e);
        }
        SpawnExplosionFx();
    }

    // cercle taronja que es veu un instant (es genera sol, no cal cap imatge)
    void SpawnExplosionFx()
    {
        if (circleSprite == null)
        {
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
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        GameObject fx = new GameObject("Explosio");
        fx.transform.position = transform.position;
        fx.transform.localScale = Vector3.one * explosionRadius * 2f;
        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = new Color(1f, 0.55f, 0.1f, 0.5f);
        sr.sortingOrder = 5;
        Destroy(fx, 0.15f);
    }
}