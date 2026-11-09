using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 2f;
    public int damage = 1;
    public float spinSpeed = 360f;

    // efectes dels plats (els posa PlayerShooting en disparar)
    [HideInInspector] public int pierce = 0;              // clients extra que travessa
    [HideInInspector] public int bounces = 0;             // rebots que li queden a les parets (Mandonguilles)
    [HideInInspector] public float explosionRadius = 0f;  // 0 = no explota
    [HideInInspector] public float slowFactor = 1f;       // 1 = no frena
    [HideInInspector] public float slowDuration = 0f;
    [HideInInspector] public bool isCrit = false;        // tret crític: més gran i daurat

    private readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    // ---------- RESERVA DE PROJECTILS ----------
    // Es dispara molt (i més amb el ventall), així que els projectils no es destrueixen:
    // es desactiven i es tornen a fer servir al tret següent.
    static readonly Dictionary<GameObject, Stack<Projectile>> pools = new Dictionary<GameObject, Stack<Projectile>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools() { pools.Clear(); }

    private GameObject sourcePrefab;  // de quin prefab ve (null = creat a mà, es destrueix com abans)
    private bool launched;            // ja s'ha preparat amb Launch (si no, Start fa el que feia abans)
    private bool alive;
    private float dieAt;
    private Rigidbody2D rb;
    private TrailRenderer trailRenderer;

    // l'estat del prefab, per deixar-lo net cada cop que es reaprofita
    private bool defaultsSaved;
    private int defaultDamage;
    private Vector3 defaultScale;
    private SpriteRenderer[] renderers;
    private Color[] defaultColors;

    // agafa un projectil de la reserva (o en crea un) a la posició donada, a punt per configurar
    public static Projectile Get(GameObject prefab, Vector2 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        if (!pools.TryGetValue(prefab, out Stack<Projectile> pool))
        {
            pool = new Stack<Projectile>();
            pools[prefab] = pool;
        }

        Projectile p = null;
        while (pool.Count > 0 && p == null) p = pool.Pop(); // els de l'escena anterior ja no existeixen

        if (p == null)
        {
            GameObject go = Instantiate(prefab, position, rotation);
            p = go.GetComponent<Projectile>();
            if (p == null) { Destroy(go); return null; } // prefab mal configurat
            p.sourcePrefab = prefab;
        }
        else
        {
            p.transform.SetPositionAndRotation(position, rotation);
            p.gameObject.SetActive(true);
            if (p.rb != null) { p.rb.position = position; p.rb.rotation = rotation.eulerAngles.z; }
        }

        p.ResetState();
        return p;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        SaveDefaults();
    }

    void SaveDefaults()
    {
        if (defaultsSaved) return;
        defaultsSaved = true;
        defaultDamage = damage;
        defaultScale = transform.localScale;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        defaultColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) defaultColors[i] = renderers[i].color;
    }

    // deixa el projectil com el prefab: sense plats, sense crític, sense estela
    void ResetState()
    {
        SaveDefaults();
        damage = defaultDamage;
        pierce = 0;
        bounces = 0;
        explosionRadius = 0f;
        slowFactor = 1f;
        slowDuration = 0f;
        isCrit = false;
        flicker = false;
        alreadyHit.Clear();
        transform.localScale = defaultScale;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = defaultColors[i];
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = false;
            trailRenderer.enabled = false;
        }
        launched = false;
        alive = true;
    }

    // es crida quan ja s'han posat el dany i els efectes: aplica l'aspecte i el llança
    public void Launch(Vector2 velocity)
    {
        launched = true;
        alive = true;
        dieAt = Time.time + lifetime;
        ApplyDishLook();
        if (trailRenderer != null && trailRenderer.enabled) trailRenderer.Clear(); // que no surti una ratlla des de l'última posició
        if (rb != null) rb.linearVelocity = velocity;
    }

    void Start()
    {
        // projectil creat amb Instantiate sense passar per Launch: com sempre
        if (!launched)
        {
            launched = true;
            alive = true;
            dieAt = Time.time + lifetime;
            ApplyDishLook();
        }
    }

    // en lloc de Destroy: torna a la reserva (o es destrueix si no en ve de cap)
    void Despawn()
    {
        if (!alive) return;
        alive = false;

        if (sourcePrefab == null) { Destroy(gameObject); return; }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        if (pools.TryGetValue(sourcePrefab, out Stack<Projectile> pool)) pool.Push(this);
        else Destroy(gameObject);
    }

    // l'aspecte del projectil canvia segons els plats que has triat
    void ApplyDishLook()
    {
        if (isCrit)
        {
            transform.localScale *= 1.35f;
            foreach (SpriteRenderer csr in renderers)
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

        foreach (SpriteRenderer sr in renderers)
            sr.color = Color.Lerp(sr.color, tint, fire || ice ? 0.6f : 0.3f);

        // estela: llarga amb Bunyols, curta amb foc o gel
        if (trail || fire || ice)
        {
            // l'estela es crea un sol cop per projectil i es reaprofita
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                Material mat = TrailMaterial();
                if (mat != null) trailRenderer.sharedMaterial = mat;
            }
            TrailRenderer tr = trailRenderer;
            tr.enabled = true;
            tr.emitting = true;
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

    // un sol material per a totes les esteles (abans se'n creava un de nou a cada tret i no s'esborrava mai)
    private static Material trailMaterial;

    static Material TrailMaterial()
    {
        if (trailMaterial != null) return trailMaterial;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return null; // al build pot no existir: millor sense material que petar
        trailMaterial = new Material(shader);
        return trailMaterial;
    }

    private bool flicker;
    private Vector3 baseScale = Vector3.one;

    void Update()
    {
        if (alive && Time.time >= dieAt) { Despawn(); return; }

        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        if (flicker)
        {
            float k = 1f + 0.12f * Mathf.Sin(Time.time * 40f);
            transform.localScale = baseScale * k;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alive) return; // ja ha tocat alguna cosa en aquest mateix pas de física
        if (other.GetComponentInParent<ArenaWalls>() != null)
        {
            if (bounces > 0 && rb != null) { Bounce(); return; }
            Despawn();
            return;
        }
        Breakable prop = other.GetComponent<Breakable>();
        if (prop != null)
        {
            prop.Hit(damage);
            if (pierce > 0) pierce--;
            else Despawn();
            return;
        }
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;
        if (alreadyHit.Contains(enemy)) return;

        // el de la safata para els trets que li arriben pel davant
        SafataEscut shield = enemy.GetComponent<SafataEscut>();
        if (shield != null && rb != null && shield.TryBlock(rb.linearVelocity, transform.position))
        {
            Despawn();
            return;
        }

        alreadyHit.Add(enemy);

        HitEnemy(enemy);

        if (explosionRadius > 0f) Explode(enemy);

        if (pierce > 0) pierce--;
        else Despawn();
    }

    // rebota a la paret: la paret que toca és la del costat on és més a prop de sortir de la sala
    void Bounce()
    {
        bounces--;
        Vector2 v = rb.linearVelocity;
        Vector2 p = (Vector2)transform.position - HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f;
        bool sideWall = Mathf.Abs(p.x) / Mathf.Max(0.01f, half.x) > Mathf.Abs(p.y) / Mathf.Max(0.01f, half.y);
        if (sideWall) v.x = -Mathf.Sign(p.x) * Mathf.Abs(v.x);
        else v.y = -Mathf.Sign(p.y) * Mathf.Abs(v.y);
        rb.linearVelocity = v;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
        alreadyHit.Clear(); // de tornada pot tocar els mateixos clients
        dieAt = Mathf.Max(dieAt, Time.time + 0.6f);
        if (SetBonuses.Has(SetId.BounceDamage)) damage += 1; // Mandonguilles amb sípia
        Fx.Hit(transform.position, -v, false);
        AudioManager.Play("bounce");
    }

    void HitEnemy(Enemy e)
    {
        Fx.Hit(e.transform.position, rb != null ? rb.linearVelocity : Vector2.zero, isCrit);
        if (slowDuration > 0f && explosionRadius <= 0f) Fx.Snowflake(e.transform.position); // el gel s'hi queda enganxat
        e.TakeDamage(damage, isCrit);
        if (slowDuration > 0f) e.Slow(slowFactor, slowDuration);
        if (e != null)
        {
            e.Flash(isCrit ? new Color(1f, 0.9f, 0.3f) : Color.white, isCrit ? 0.2f : 0.12f);
        }
    }

    void Explode(Enemy center)
    {
        // plats de conjunt: una explosió crítica és més gran (Calçotada crítica)
        float radius = explosionRadius;
        if (isCrit && SetBonuses.Has(SetId.CritBlast)) radius *= 1.6f;

        bool push = SetBonuses.Has(SetId.Shockwave); // Traca: l'explosió empeny els clients
        Vector2 origin = transform.position;
        Vector2 forward = rb != null && rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : Vector2.right;

        if (push) Push(center, origin, forward);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius);
        foreach (Collider2D h in hits)
        {
            Enemy e = h.GetComponent<Enemy>();
            if (e == null) e = h.GetComponentInParent<Enemy>(); // igual que a l'impacte directe
            if (e == null || e == center || alreadyHit.Contains(e)) continue;
            alreadyHit.Add(e);
            HitEnemy(e);
            if (push) Push(e, origin, forward);
        }
        SpawnExplosionFx(radius);

        // plats de conjunt: Calçots gelats deixen una zona gelada on ha explotat
        if (slowDuration > 0f && SetBonuses.Has(SetId.FrostBlast))
            FrostZone.Spawn(origin, radius, Mathf.Clamp(slowFactor, 0.3f, 0.7f), 3f);
    }

    // empeny un client lluny del centre de l'explosió
    void Push(Enemy e, Vector2 origin, Vector2 fallback)
    {
        if (e == null) return;
        Vector2 away = (Vector2)e.transform.position - origin;
        if (away.sqrMagnitude < 0.04f) away = fallback; // just al centre: cap a on anava el projectil
        e.Knockback(away, 9f, 0.18f);
    }

    // cercle taronja que es veu un instant (es genera sol, no cal cap imatge)
    void SpawnExplosionFx(float radius)
    {
        AudioManager.Play("explosion", 0.45f);
        GamepadInput.Rumble(0.15f, 0.2f, 0.06f);
        // gel (Calçots + Sorbet) o foc
        if (slowDuration > 0f) Fx.FrostBurst(transform.position, radius);
        else Fx.Explosion(transform.position, radius, 0.8f);
    }
}