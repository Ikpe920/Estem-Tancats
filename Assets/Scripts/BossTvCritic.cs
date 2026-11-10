using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  LA CRÍTICA DE LA TELE (cap de les hores extres, cada 15 onades)
// =====================================================================
// - Es mou de costat, a mitja distància, amb el micròfon.
// - FOCUS: marca uns cercles a terra (un a sota teu) i al cap d'un segon s'encenen:
//   si hi ets a dins, et fa mal i t'enlluerna (el dash ho esquiva).
// - APLAUDIMENTS: el públic s'aixeca... i entren més clients.
// - Quan li queda menys de la meitat de la vida, ho fa tot més de pressa i amb més focus.
public class BossTvCritic : MonoBehaviour
{
    public WaveSpawner spawner;

    const float SpotRadius = 1.6f;
    const float SpotWarn = 1.0f;
    const float KeepMin = 5f, KeepMax = 8f;

    private Enemy enemy;
    private Rigidbody2D rb;
    private float nextSpots, nextApplause;
    private float strafe = 1f;
    private bool angry;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        if (enemy != null) enemy.externalMovement = true;
        nextSpots = Time.time + 2f;
        nextApplause = Time.time + 6f;
    }

    void FixedUpdate()
    {
        if (enemy == null || rb == null || enemy.IsStunned || enemy.IsDead) return;
        Transform player = PlayerRef.Transform;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }
        Vector2 to = (Vector2)player.position - rb.position;
        float dist = to.magnitude;
        Vector2 d = to / Mathf.Max(0.01f, dist);
        Vector2 move = dist < KeepMin ? -d : dist > KeepMax ? d : new Vector2(-d.y, d.x) * strafe;
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2.5f;
        if (Mathf.Abs(rb.position.x - c.x) > half.x || Mathf.Abs(rb.position.y - c.y) > half.y) move += (c - rb.position).normalized * 1.5f;
        rb.linearVelocity = move.normalized * enemy.MoveSpeed;
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead || PlayerHealth.IsDying) return;
        if (!angry && enemy.CurrentHealth <= enemy.MaxHealth / 2)
        {
            angry = true;
            HazardFx.Popup((Vector2)transform.position + Vector2.up * 2.2f, "PUBLICITAT? NO! MÉS EMOCIÓ!", new Color(1f, 0.5f, 0.5f), 1.6f);
            Squash.Hit(gameObject, 0.15f);
        }
        if (Time.time >= nextSpots)
        {
            nextSpots = Time.time + (angry ? 2.6f : 3.6f);
            if (Random.value < 0.35f) strafe = -strafe;
            StartCoroutine(Spotlights(angry ? 4 : 3));
        }
        if (Time.time >= nextApplause)
        {
            nextApplause = Time.time + (angry ? 7f : 10f);
            Applause(angry ? 4 : 3);
        }
    }

    // els focus: avisen, i s'encenen
    IEnumerator Spotlights(int n)
    {
        Transform player = PlayerRef.Transform;
        if (player == null) yield break;

        var spots = new List<Vector2> { player.position }; // un sempre a sota teu
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2f;
        for (int i = 1; i < n; i++)
        {
            Vector2 p = (Vector2)player.position + Random.insideUnitCircle * 5f;
            p.x = Mathf.Clamp(p.x, c.x - half.x, c.x + half.x);
            p.y = Mathf.Clamp(p.y, c.y - half.y, c.y + half.y);
            spots.Add(p);
        }

        HazardFx.Popup((Vector2)transform.position + Vector2.up * 2f, "LLUMS!", new Color(1f, 0.95f, 0.6f), SpotWarn);
        var rings = new List<SpriteRenderer>();
        foreach (Vector2 p in spots) rings.Add(HazardFx.MakeCircle(p, SpotRadius, new Color(1f, 0.95f, 0.5f, 0.12f), -2));

        for (float t = 0f; t < SpotWarn; t += Time.deltaTime)
        {
            float k = t / SpotWarn;
            foreach (SpriteRenderer r in rings)
                if (r != null) r.color = new Color(1f, 0.95f, 0.5f, Mathf.Lerp(0.1f, 0.35f, k) * (0.8f + 0.2f * Mathf.Sin(t * 30f)));
            yield return null;
            if (enemy == null || enemy.IsDead) { Clear(rings); yield break; }
        }

        // s'encenen
        AudioManager.Play("crit", 0.7f, 0.8f);
        bool hit = false;
        foreach (Vector2 p in spots)
        {
            Fx.Shockwave(p, SpotRadius * 0.6f, new Color(1f, 0.97f, 0.7f));
            Fx.Sparkles(p, new Color(1f, 0.95f, 0.6f), 5, SpotRadius);
            if (!hit && player != null && Vector2.Distance(player.position, p) <= SpotRadius) hit = true;
        }
        foreach (SpriteRenderer r in rings) if (r != null) r.color = new Color(1f, 1f, 0.8f, 0.7f);

        PlayerMovement pm = PlayerRef.Movement;
        if (hit && (pm == null || !pm.IsDashing))
        {
            PlayerHealth ph = PlayerRef.Health;
            if (ph != null) ph.TakeDamage();
            ScreenFlash.Blind(0.7f);
        }

        yield return new WaitForSeconds(0.25f);
        Clear(rings);
    }

    static void Clear(List<SpriteRenderer> rings)
    {
        foreach (SpriteRenderer r in rings) if (r != null) Destroy(r.gameObject);
    }

    // el públic aplaudeix... i baixa a la pista
    void Applause(int count)
    {
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 2.2f, "APLAUDIMENTS!", new Color(1f, 0.8f, 0.3f), 1.4f);
        AudioManager.Play("wave_clear", 0.8f, 1.1f);
        Squash.Kick(gameObject, -0.15f, 0.2f);
        if (spawner != null) spawner.SpawnMinions(transform.position, count);
    }
}
