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
            float k = age / warningTime;
            sr.color = new Color(1f, 0.2f, 0.1f, Mathf.Lerp(0.12f, 0.5f, k));
            return;
        }

        // 2. crema: es mou, rebota a les parets i fa mal
        float burnAge = age - warningTime;
        float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, transform.position.x);
        float fade = Mathf.Clamp01((burnTime - burnAge) / 1f); // l'últim segon s'apaga
        sr.color = new Color(1f, 0.55f * flicker, 0.1f, 0.75f * fade);

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
            if (e != null) e.TakeDamage(1);
        }
    }
}
