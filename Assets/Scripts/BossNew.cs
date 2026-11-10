using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  CAPS NOUS: EL SOMMELIER (onada 30, al Celler) i EL GELATER BOIG (onada 35, a la Cambra frigorífica)
// =====================================================================
// Fan servir el prefab d'un altre cap (com la Crítica de la tele); el WaveSpawner els hi posa aquest
// comportament. Caminen cap a tu com els altres caps.

// frena el jugador una estona (sense acumular-se)
public static class PlayerSlow
{
    static float until;

    public static void Apply(float fraction, float seconds, string text)
    {
        PlayerMovement pm = PlayerRef.Movement;
        if (pm == null || Time.time < until) return;
        until = Time.time + seconds;
        PlayerBuffs buffs = pm.GetComponent<PlayerBuffs>();
        if (buffs == null) buffs = pm.gameObject.AddComponent<PlayerBuffs>();
        buffs.SpeedBoost(-pm.speed * fraction, seconds);
        if (!string.IsNullOrEmpty(text))
            HazardFx.Popup((Vector2)pm.transform.position + Vector2.up * 1.2f, text, new Color(0.7f, 0.85f, 1f), 0.9f);
    }
}

// un tret de cap (el projectil dels clients)
static class BossShot
{
    public static void Fire(GameObject prefab, Vector2 from, Vector2 dir, float speed)
    {
        if (prefab == null) return;
        GameObject s = Object.Instantiate(prefab, from + dir * 0.8f, Quaternion.identity);
        Rigidbody2D rb = s.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = dir.normalized * speed;
    }
}

// ---------- EL SOMMELIER ----------
// - Llança ampolles de vi que cauen on ets (cercles a terra): si hi ets a dins, et fan mal,
//   i deixen un toll de vi que et frena.
// - "TAST!": un cercle de taps de suro que surten disparats.
// - A mitja vida, ho fa tot més de pressa.
public class BossSommelier : MonoBehaviour
{
    public GameObject projectilePrefab;

    private Enemy enemy;
    private float nextBottles, nextRing;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        nextBottles = Time.time + 2f;
        nextRing = Time.time + 5f;
    }

    bool Angry => enemy != null && enemy.CurrentHealth <= enemy.MaxHealth / 2;

    void Update()
    {
        if (enemy == null || enemy.IsDead || PlayerHealth.IsDying) return;
        if (Time.time >= nextBottles)
        {
            nextBottles = Time.time + (Angry ? 2.2f : 3.2f);
            StartCoroutine(Bottles(Angry ? 4 : 3));
        }
        if (Time.time >= nextRing)
        {
            nextRing = Time.time + (Angry ? 4.5f : 7f);
            HazardFx.Popup((Vector2)transform.position + Vector2.up * 2.2f, "TAST!", new Color(0.8f, 0.3f, 0.4f), 1f);
            int n = Angry ? 16 : 12;
            float off = Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
            {
                float a = (off + i * 360f / n) * Mathf.Deg2Rad;
                BossShot.Fire(projectilePrefab, transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f);
            }
            AudioManager.Play("pickup", 0.6f, 0.7f);
            Squash.Kick(gameObject, 0.15f, -0.15f);
        }
    }

    IEnumerator Bottles(int n)
    {
        Transform player = PlayerRef.Transform;
        if (player == null) yield break;
        var spots = new List<Vector2> { player.position };
        for (int i = 1; i < n; i++) spots.Add((Vector2)player.position + Random.insideUnitCircle * 3.5f);

        var rings = new List<SpriteRenderer>();
        foreach (Vector2 p in spots) rings.Add(HazardFx.MakeCircle(p, 1.1f, new Color(0.6f, 0.1f, 0.25f, 0.15f), -2));
        for (float t = 0f; t < 0.9f; t += Time.deltaTime)
        {
            foreach (SpriteRenderer r in rings) if (r != null) r.color = new Color(0.6f, 0.1f, 0.25f, Mathf.Lerp(0.1f, 0.4f, t / 0.9f));
            yield return null;
        }
        foreach (SpriteRenderer r in rings) if (r != null) Destroy(r.gameObject);

        bool hit = false;
        foreach (Vector2 p in spots)
        {
            Fx.Hit(p, Vector2.down, false);
            Fx.Sparkles(p, new Color(0.7f, 0.15f, 0.3f), 6, 1f);
            if (player != null && Vector2.Distance(player.position, p) <= 1.1f) hit = true;
            StartCoroutine(Puddle(p));
        }
        AudioManager.Play("box_break", 0.7f, 1.2f);
        PlayerMovement pm = PlayerRef.Movement;
        if (hit && (pm == null || !pm.IsDashing) && PlayerRef.Health != null) PlayerRef.Health.TakeDamage();
    }

    // el toll de vi: et frena si el trepitges
    IEnumerator Puddle(Vector2 p)
    {
        SpriteRenderer sr = HazardFx.MakeCircle(p, 1.1f, new Color(0.45f, 0.05f, 0.18f, 0.45f), -3);
        for (float t = 0f; t < 3.5f; t += 0.1f)
        {
            Transform player = PlayerRef.Transform;
            if (player != null && Vector2.Distance(player.position, p) <= 1.1f) PlayerSlow.Apply(0.4f, 1.2f, "VI!");
            if (sr != null) sr.color = new Color(0.45f, 0.05f, 0.18f, 0.45f * (1f - t / 3.5f) + 0.05f);
            yield return new WaitForSeconds(0.1f);
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}

// ---------- EL GELATER BOIG ----------
// - Dispara ventalls de boles de gelat.
// - "GELADA!": cercles de gel a terra (un a sota teu) que es glacen al cap d'un segon:
//   si hi ets a dins, et fan mal i et frenen molt.
// - A mitja vida, de tant en tant fa girar una espiral de gelats.
public class BossGelater : MonoBehaviour
{
    public GameObject projectilePrefab;

    private Enemy enemy;
    private float nextFan, nextFrost, nextSpiral;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        nextFan = Time.time + 1.5f;
        nextFrost = Time.time + 4f;
        nextSpiral = Time.time + 6f;
    }

    bool Angry => enemy != null && enemy.CurrentHealth <= enemy.MaxHealth / 2;

    void Update()
    {
        if (enemy == null || enemy.IsDead || PlayerHealth.IsDying) return;
        Transform player = PlayerRef.Transform;
        if (player == null) return;

        if (Time.time >= nextFan)
        {
            nextFan = Time.time + (Angry ? 1.8f : 2.6f);
            Vector2 to = ((Vector2)player.position - (Vector2)transform.position).normalized;
            float baseA = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            int n = Angry ? 7 : 5;
            for (int i = 0; i < n; i++)
            {
                float a = (baseA + (i - (n - 1) / 2f) * 11f) * Mathf.Deg2Rad;
                BossShot.Fire(projectilePrefab, transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6f);
            }
        }
        if (Time.time >= nextFrost)
        {
            nextFrost = Time.time + (Angry ? 6f : 8f);
            StartCoroutine(Frost(Angry ? 6 : 4));
        }
        if (Angry && Time.time >= nextSpiral)
        {
            nextSpiral = Time.time + 9f;
            StartCoroutine(Spiral());
        }
    }

    IEnumerator Frost(int n)
    {
        Transform player = PlayerRef.Transform;
        if (player == null) yield break;
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 2.2f, "GELADA!", new Color(0.7f, 0.9f, 1f), 1.1f);
        var spots = new List<Vector2> { player.position };
        for (int i = 1; i < n; i++) spots.Add((Vector2)player.position + Random.insideUnitCircle * 4f);
        var rings = new List<SpriteRenderer>();
        foreach (Vector2 p in spots) rings.Add(HazardFx.MakeCircle(p, 1.4f, new Color(0.6f, 0.85f, 1f, 0.12f), -2));
        for (float t = 0f; t < 1.1f; t += Time.deltaTime)
        {
            foreach (SpriteRenderer r in rings) if (r != null) r.color = new Color(0.6f, 0.85f, 1f, Mathf.Lerp(0.1f, 0.4f, t / 1.1f));
            yield return null;
            if (enemy == null || enemy.IsDead) break;
        }
        foreach (SpriteRenderer r in rings) if (r != null) Destroy(r.gameObject);

        bool hit = false;
        foreach (Vector2 p in spots)
        {
            Fx.FrostBurst(p, 1.4f);
            if (player != null && Vector2.Distance(player.position, p) <= 1.4f) hit = true;
        }
        AudioManager.Play("crit", 0.6f, 1.4f);
        PlayerMovement pm = PlayerRef.Movement;
        if (hit && (pm == null || !pm.IsDashing))
        {
            if (PlayerRef.Health != null) PlayerRef.Health.TakeDamage();
            PlayerSlow.Apply(0.55f, 2f, "GLAÇAT!");
        }
    }

    IEnumerator Spiral()
    {
        float a = Random.Range(0f, 360f);
        for (float t = 0f; t < 2f; t += 0.12f)
        {
            if (enemy == null || enemy.IsDead) yield break;
            a += 27f;
            for (int k = 0; k < 2; k++)
            {
                float r = (a + k * 180f) * Mathf.Deg2Rad;
                BossShot.Fire(projectilePrefab, transform.position, new Vector2(Mathf.Cos(r), Mathf.Sin(r)), 5f);
            }
            yield return new WaitForSeconds(0.12f);
        }
    }
}
