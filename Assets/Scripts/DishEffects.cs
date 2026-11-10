using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  EFECTES DELS PLATS NOUS (el BoonManager els afegeix al jugador)
// =====================================================================
//   Olives farcides    -> OrbitingOlives: olives que giren al teu voltant i fan mal
//   Romesco            -> RomescoWave: quan et toquen, una ona empeny i fa mal als clients de prop
//   Panellets          -> PanelletsRing: en fer el dash, llences panellets en cercle
//   Coca de recapte    -> KillBurst: els clients de vegades esclaten en morir
// (Les Mandonguilles, que fan rebotar els trets, són a Projectile.)

// ---------- OLIVES FARCIDES ----------
public class OrbitingOlives : MonoBehaviour
{
    public int count = 0;            // olives que giren
    public float radius = 1.6f;
    public float degreesPerSecond = 200f;
    public int damage = 1;

    private readonly List<Transform> olives = new List<Transform>();
    private readonly Dictionary<Enemy, float> nextHit = new Dictionary<Enemy, float>();
    private static readonly List<Enemy> buffer = new List<Enemy>();
    private float angle;

    public void AddOlives(int n)
    {
        count += n;
        while (olives.Count < count)
        {
            GameObject go = new GameObject("Oliva");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PropArt.Olive();
            sr.sortingOrder = 4;
            olives.Add(go.transform);
        }
    }

    void Update()
    {
        if (olives.Count == 0) return;
        bool evo = Evo.Has(EvoId.OlivesAnxova); // plat estrella: triple de mal, més de pressa i més lluny
        angle += degreesPerSecond * (evo ? 1.5f : 1f) * Time.deltaTime;
        int dmg = SetBonuses.Has(SetId.SpicyOlives) ? damage * 2 : damage; // Aperitiu picant
        if (evo) dmg *= 3;
        float r = (radius + (evo ? 0.6f : 0f)) * DishArea.Mul;

        for (int i = 0; i < olives.Count; i++)
        {
            float a = (angle + i * 360f / olives.Count) * Mathf.Deg2Rad;
            Vector3 world = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
            olives[i].position = world;
            olives[i].rotation = Quaternion.identity;

            // cada client pot rebre una olivada cada 0,5 s
            // (sobre una còpia: un client que mor pot fer-ne sortir d'altres, com el Golafre)
            buffer.Clear();
            buffer.AddRange(Enemy.All);
            foreach (Enemy e in buffer)
            {
                if (e == null || e.IsDead) continue;
                if (((Vector2)e.transform.position - (Vector2)world).sqrMagnitude > 0.55f * 0.55f) continue;
                if (nextHit.TryGetValue(e, out float t) && Time.time < t) continue;
                nextHit[e] = Time.time + 0.5f;
                e.TakeDamage(dmg);
            }
        }

        // que la llista no creixi amb clients que ja no hi són
        if (nextHit.Count > 64) nextHit.Clear();
    }
}

// ---------- ROMESCO ----------
public class RomescoWave : MonoBehaviour
{
    public float radius = 2.5f;
    public int damage = 2;

    private PlayerHealth health;
    private int lastLives = -1;

    void Awake() { health = GetComponent<PlayerHealth>(); }

    void OnEnable()
    {
        if (health == null) health = GetComponent<PlayerHealth>();
        if (health != null) { health.OnLivesChanged += Changed; lastLives = health.CurrentLives; }
    }

    void OnDisable() { if (health != null) health.OnLivesChanged -= Changed; }

    private float nextAuto;

    // Romesco de Tarragona (plat estrella): l'ona surt sola cada 5 s
    void Update()
    {
        if (!Evo.Has(EvoId.RomescoTarragona) || PlayerHealth.IsDying || Time.timeScale == 0f) return;
        if (Time.time < nextAuto) return;
        nextAuto = Time.time + 5f;
        Wave();
    }

    void Changed(int lives)
    {
        bool hit = lastLives >= 0 && lives < lastLives;
        lastLives = lives;
        if (!hit || lives <= 0) return;
        Wave();
    }

    void Wave()
    {
        float r = (SetBonuses.Has(SetId.Revenge) ? radius * 2f : radius) * DishArea.Mul; // Salsa de revenja
        Vector2 pos = transform.position;
        Fx.Shockwave(pos, r * 0.5f, new Color(0.95f, 0.45f, 0.2f));
        AudioManager.Play("explosion", 0.6f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, r))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            e.TakeDamage(damage);
            e.Knockback((Vector2)e.transform.position - pos, 10f, 0.2f);
        }
    }
}

// ---------- PANELLETS ----------
public class PanelletsRing : MonoBehaviour
{
    public int count = 0; // panellets per dash

    private PlayerMovement movement;
    private PlayerShooting shooting;
    private bool wasDashing;
    private float nextAuto;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        shooting = GetComponent<PlayerShooting>();
    }

    void Update()
    {
        if (movement == null || shooting == null || count <= 0) return;
        bool dashing = movement.IsDashing;
        if (dashing && !wasDashing) Throw();
        wasDashing = dashing;
        // Castanyada eterna (plat estrella): també sense dash, cada 2,5 s
        if (Evo.Has(EvoId.CastanyadaEterna) && Time.timeScale > 0f && Time.time >= nextAuto) { nextAuto = Time.time + 2.5f; Throw(); }
    }

    void Throw()
    {
        if (shooting.projectilePrefabs == null || shooting.projectilePrefabs.Length == 0) return;
        int n = count + (SetBonuses.Has(SetId.Castanyada) ? 4 : 0); // Castanyada
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < n; i++)
        {
            float a = offset + i * 360f / n;
            Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            GameObject prefab = shooting.projectilePrefabs[Random.Range(0, shooting.projectilePrefabs.Length)];
            Projectile p = Projectile.Get(prefab, (Vector2)transform.position + dir * 0.6f, Quaternion.Euler(0f, 0f, a));
            if (p == null) continue;
            p.damage += shooting.damage - 1;
            p.Launch(dir * shooting.projectileSpeed * 0.8f);
        }
    }
}

// ---------- COCA DE RECAPTE ----------
public class KillBurst : MonoBehaviour
{
    public float chance = 0.3f;
    public float radius = 1.4f;

    private PlayerShooting shooting;
    private static int depth; // perquè una cadena d'esclats no sigui infinita

    void Awake() { shooting = GetComponent<PlayerShooting>(); }
    void OnEnable() { Enemy.OnKilled += Killed; }
    void OnDisable() { Enemy.OnKilled -= Killed; }

    void Killed(Enemy dead)
    {
        if (dead == null || depth >= 3) return;
        bool evo = Evo.Has(EvoId.CocaSantJoan); // plat estrella: tots esclaten, i més fort
        bool always = SetBonuses.Has(SetId.Revetlla) || evo; // Revetlla
        if (!always && Random.value > chance) return;

        float radius = this.radius * DishArea.Mul * (evo ? 1.5f : 1f);
        Vector2 pos = dead.transform.position;
        int dmg = shooting != null ? Mathf.Max(1, shooting.damage) : 1;
        Fx.Explosion(pos, radius, 0.6f);
        AudioManager.Play("explosion", 0.35f);

        depth++;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e == dead || e.IsDead) continue;
            e.TakeDamage(dmg);
        }
        depth--;
    }
}
