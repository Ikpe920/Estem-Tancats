using System.Collections;
using UnityEngine;

// ---------- LA BARRA: tolls relliscosos ----------
public class SlipZone : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null) pm.slipUntil = Time.time + 0.15f;
    }
}

public class SlipperyHazard : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = HazardFx.RandomPoint(3f);
            SpriteRenderer sr = HazardFx.MakeCircle(pos, Random.Range(2f, 3f), new Color(0.45f, 0.75f, 1f, 0.35f), -3);
            sr.gameObject.name = "Bassa";
            sr.transform.SetParent(transform, true);

            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            sr.gameObject.AddComponent<SlipZone>();
        }
    }
}

// ---------- LA BARRA: ampolles que exploten si les dispares ----------
public class Bottle : MonoBehaviour
{
    public float blastRadius = 2.4f;
    public int enemyDamage = 6;

    private bool exploded;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded) return;
        if (other.GetComponent<Projectile>() == null) return;

        exploded = true;
        Explode();
    }

    void Explode()
    {
        foreach (Collider2D h in Physics2D.OverlapCircleAll(transform.position, blastRadius))
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage();
                continue;
            }

            Enemy e = h.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(enemyDamage);
        }

        SpriteRenderer fx = HazardFx.MakeCircle(transform.position, blastRadius, new Color(1f, 0.6f, 0.15f, 0.7f), 5);
        Destroy(fx.gameObject, 0.25f);
        HazardFx.Shake(0.22f, 0.2f);

        Destroy(gameObject);
    }
}

public class BottleHazard : MonoBehaviour
{
    public int maxBottles = 3;

    IEnumerator Start()
    {
        for (int i = 0; i < maxBottles; i++) SpawnBottle();

        while (true)
        {
            yield return new WaitForSeconds(6f);
            if (transform.childCount < maxBottles) SpawnBottle();
        }
    }

    void SpawnBottle()
    {
        Vector2 pos = HazardFx.RandomPoint(2.5f);

        GameObject root = new GameObject("Ampolla");
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.7f, 1.1f);

        Bottle bottle = root.AddComponent<Bottle>();

        // el got de l'ampolla
        GameObject body = new GameObject("Cos");
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.3f, 0.7f, 1f);
        SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
        bsr.sprite = HazardFx.Square();
        bsr.color = new Color(0.25f, 0.65f, 0.35f, 1f);
        bsr.sortingOrder = -1;

        // el radi de l'explosió, molt fluix, perquè se sàpiga on fa mal
        SpriteRenderer radius = HazardFx.MakeCircle(pos, bottle.blastRadius, new Color(1f, 0.3f, 0.1f, 0.07f), -3);
        radius.transform.SetParent(root.transform, true);
    }
}

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

public class FireHazard : MonoBehaviour
{
    public int maxFires = 3;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            if (transform.childCount < maxFires)
            {
                SpriteRenderer sr = HazardFx.MakeCircle(HazardFx.RandomPoint(2.5f), 1.6f, new Color(1f, 0.2f, 0.1f, 0.12f), -3);
                sr.gameObject.name = "Foc";
                sr.transform.SetParent(transform, true);
                sr.gameObject.AddComponent<FireZone>();
            }

            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }
    }
}

// ---------- LA TERRASSA: vent ----------
public class WindStreak : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 1.5f;

    void Start()
    {
        Destroy(gameObject, life);
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }
}

public class WindHazard : MonoBehaviour
{
    public float force = 3.2f;       // velocitat extra que t'empeny
    public float warningTime = 1.5f; // només es veuen les línies de vent
    public float gustTime = 3.5f;    // la ràfega empeny de debò

    private PlayerMovement movement;
    private Camera cam;
    private float nextStreak;

    private static readonly Vector2[] Directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

    IEnumerator Start()
    {
        movement = FindAnyObjectByType<PlayerMovement>();
        cam = Camera.main;

        yield return new WaitForSeconds(4f);

        while (true)
        {
            Vector2 dir = Directions[Random.Range(0, Directions.Length)];

            // 1. avís
            float t = 0f;
            while (t < warningTime)
            {
                t += Time.deltaTime;
                Streaks(dir, 0.12f);
                yield return null;
            }

            // 2. ràfega
            t = 0f;
            while (t < gustTime)
            {
                t += Time.deltaTime;
                float ramp = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((gustTime - t) / 0.5f);
                if (movement != null) movement.externalPush = dir * force * ramp;
                Streaks(dir, 0.04f);
                yield return null;
            }

            if (movement != null) movement.externalPush = Vector2.zero;

            yield return new WaitForSeconds(Random.Range(5f, 9f));
        }
    }

    void Streaks(Vector2 dir, float interval)
    {
        if (cam == null || Time.time < nextStreak) return;
        nextStreak = Time.time + interval;

        float hh = cam.orthographicSize;
        float hw = hh * cam.aspect;
        bool horizontal = Mathf.Abs(dir.x) > 0.5f;
        float reach = horizontal ? hw : hh;
        float span = horizontal ? hh : hw;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2 pos = (Vector2)cam.transform.position - dir * (reach + 1f) + perp * Random.Range(-span, span);

        GameObject go = new GameObject("Vent");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        go.transform.localScale = new Vector3(Random.Range(1f, 2.2f), 0.05f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = new Color(1f, 1f, 1f, 0.3f);
        sr.sortingOrder = 50;

        WindStreak s = go.AddComponent<WindStreak>();
        s.velocity = dir * 22f;
        s.life = (reach * 2f + 3f) / 22f;
    }

    void OnDestroy()
    {
        if (movement != null) movement.externalPush = Vector2.zero;
    }
}

// ---------- LA TEULADA: llamps ----------
public class LightningHazard : MonoBehaviour
{
    public float radius = 1.5f;
    public float warningTime = 1.1f;
    public int enemyDamage = 6;

    private Transform player;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            StartCoroutine(Strike(PickTarget()));

            // a les últimes onades cauen més sovint
            int wave = WaveSpawner.Instance != null ? WaveSpawner.Instance.currentWave : 21;
            float k = Mathf.InverseLerp(21f, 25f, wave);
            yield return new WaitForSeconds(Mathf.Lerp(3.2f, 1.8f, k));
        }
    }

    // dos de cada tres llamps van cap a on ets; l'altre cau a l'atzar
    Vector2 PickTarget()
    {
        if (player == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        if (player != null && Random.value < 0.66f) return player.position;
        return HazardFx.RandomPoint(1.5f);
    }

    IEnumerator Strike(Vector2 pos)
    {
        // avís: cercle groc amb un cercle interior que creix
        SpriteRenderer warn = HazardFx.MakeCircle(pos, radius, new Color(1f, 0.9f, 0.3f, 0.12f), -3);
        warn.transform.SetParent(transform, true);
        SpriteRenderer fill = HazardFx.MakeCircle(pos, radius, new Color(1f, 0.95f, 0.5f, 0.35f), -2);
        fill.transform.SetParent(transform, true);
        fill.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < warningTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / warningTime);
            fill.transform.localScale = Vector3.one * radius * 2f * k;
            warn.color = new Color(1f, 0.9f, 0.3f, Mathf.Lerp(0.12f, 0.4f, k));
            yield return null;
        }

        Destroy(warn.gameObject);
        Destroy(fill.gameObject);

        // el llamp: flaix al terra i un raig que cau del cel
        SpriteRenderer flash = HazardFx.MakeCircle(pos, radius, new Color(1f, 1f, 1f, 0.9f), 20);
        Destroy(flash.gameObject, 0.12f);

        GameObject beam = new GameObject("Raig");
        beam.transform.position = pos + Vector2.up * 7f;
        beam.transform.localScale = new Vector3(0.6f, 14f, 1f);
        SpriteRenderer bsr = beam.AddComponent<SpriteRenderer>();
        bsr.sprite = HazardFx.Square();
        bsr.color = new Color(1f, 1f, 0.85f, 0.95f);
        bsr.sortingOrder = 21;
        Destroy(beam, 0.12f);

        HazardFx.Shake(0.2f, 0.22f);

        DarknessHazard.Flash(); // a la Teulada, el llamp il·lumina un instant

        foreach (Collider2D h in Physics2D.OverlapCircleAll(pos, radius))
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage();
                continue;
            }

            Enemy e = h.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(enemyDamage);
        }
    }
}