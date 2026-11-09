using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Enemy : MonoBehaviour
{
    public static float speedMultiplier = 1f;
    public static int Alive;   // enemics vius a l'escena (el WaveSpawner el fa servir en lloc de FindObjectsByType)

    // reseteja els estàtics a l'inici i cada cop que es carrega una escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        speedMultiplier = 1f;
        Alive = 0;
        All.Clear();
        obstacleCache.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        obstacleCache.Clear(); // els col·lisionadors de l'escena anterior ja no hi són
        speedMultiplier = 1f; // els boons de lentitud no s'arrosseguen entre partides
    }

    [Header("Estadístiques")]
    public float speed = 2f;
    public int maxHealth = 3;
    public float damageCooldown = 1f;
    public int points = 10;
    public float damageMultiplier = 1f;

    [Header("Tirador (opcional)")]
    public bool isShooter = false;
    public float stopDistance = 6f;
    public float fireInterval = 2f;
    public float shotSpeed = 6f;
    public GameObject shotPrefab;

    public Sprite spriteFront;
    public Sprite spriteBack;

    private int health;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private float nextDamageTime;
    private float nextShotTime;
    public int CurrentHealth => health;
    public bool canBeKnockedBack = true;
    [HideInInspector] public bool isBoss;   // el posa el WaveSpawner: els caps tenen més "suc" als impactes
    [HideInInspector] public bool externalMovement;
    private Vector2 knockbackVelocity;
    private float knockbackUntil;
    private float frozenUntil;
    private bool mirarAmunt = false;

    public static event System.Action<Enemy> OnKilled;

    private float slowUntil;
    private float slowFactor = 1f;
    private Color baseColor = Color.white;
    private bool hasBaseColor;
    private bool dead;
    private float hitFlashUntil;

    private enum ColorState { Unset, Base, Flash, Slow }
    private ColorState colorState = ColorState.Unset;

    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private readonly List<Color> baseColors = new List<Color>();
    private bool renderersCached;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.interpolation = RigidbodyInterpolation2D.Interpolate; // moviment suau
        health = maxHealth;
        CacheRenderers();
    }

    // tots els clients vius (les fletxes de la vora de la pantalla els fan servir)
    public static readonly List<Enemy> All = new List<Enemy>();

    void OnEnable()  { Alive++; All.Add(this); }
    void OnDisable() { Alive--; All.Remove(this); }

    void CacheRenderers()
    {
        if (renderersCached && spriteRenderers.Count > 0) return;
        spriteRenderers.Clear();
        baseColors.Clear();
        GetComponentsInChildren<SpriteRenderer>(true, spriteRenderers);
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] != null)
            {
                baseColors.Add(spriteRenderers[i].color);
                if (!hasBaseColor)
                {
                    baseColor = spriteRenderers[i].color;
                    hasBaseColor = true;
                }
                if (spriteRenderer == null)
                    spriteRenderer = spriteRenderers[i];
            }
            else
            {
                baseColors.Add(Color.white);
            }
        }
        renderersCached = spriteRenderers.Count > 0;
    }

    void Start()
    {
        GameObject p = PlayerRef.Root;
        if (p != null) player = p.transform;
        if (rb != null) lastProgressPos = rb.position;
        lastProgressTime = Time.time;
        nextShotTime = Time.time + Random.Range(0.5f, fireInterval);
    }

    public void Freeze(float duration)
    {
        frozenUntil = Mathf.Max(frozenUntil, Time.time + duration);
    }

    public void Slow(float factor, float duration)
    {
        // si ja està frenat, es queda el frenat més fort i el que dura més (així una zona gelada
        // no afluixa el frenat d'un projectil de Sorbet, ni al revés)
        if (Time.time < slowUntil)
        {
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }
        else
        {
            slowFactor = factor;
            slowUntil = Time.time + duration;
        }
    }

    public void ApplyActDifficulty(float healthMultiplier, float speedBonus)
    {
        if (healthMultiplier != 1f)
        {
            maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * healthMultiplier));
            health = maxHealth;
        }
        if (speedBonus != 0f) speed += speedBonus;
    }

    public void OverrideStats(int newMaxHealth, float newSpeed)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        health = maxHealth;
        speed = newSpeed;
    }

    public void SetTint(Color color)
    {
        baseColor = color;
        hasBaseColor = true;
        if (!renderersCached || spriteRenderers.Count == 0)
            CacheRenderers();
        if (baseColors.Count == 0 && spriteRenderers.Count > 0)
        {
            for (int i = 0; i < spriteRenderers.Count; i++)
                baseColors.Add(spriteRenderers[i] != null ? spriteRenderers[i].color : color);
        }
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] != null)
            {
                if (baseColors.Count > i) baseColors[i] = color;
                spriteRenderers[i].color = color;
            }
        }
        colorState = ColorState.Unset;
    }

    void FixedUpdate()
    {
        if (Time.time < knockbackUntil)
        {
            rb.linearVelocity = knockbackVelocity;
            lastProgressTime = Time.time; // empès o glaçat no vol dir encallat
            return;
        }
        if (Time.time < frozenUntil)
        {
            rb.linearVelocity = Vector2.zero;
            lastProgressTime = Time.time;
            return;
        }
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        UpdateFacingSprite(toPlayer);

        if (externalMovement) { lastProgressTime = Time.time; return; }

        if (isShooter && toPlayer.magnitude <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            float slow = Time.time < slowUntil ? slowFactor : 1f;
            Vector2 dir = SteerAround(toPlayer.normalized, toPlayer.magnitude);
            rb.linearVelocity = dir * speed * speedMultiplier * slow;
            return;
        }
        lastProgressTime = Time.time; // quiet a propòsit (tirador): no compta com encallat
    }

    // ---------- ESQUIVAR CAIXES, TAULES I PARETS ----------
    // Abans anaven en línia recta cap al jugador i, si hi havia una caixa al mig, s'hi quedaven enganxats.
    // Ara miren una mica endavant: si hi ha un obstacle, el voregen pel costat que els queda més a prop.
    // I si tot i així porten una estona sense avançar, fan un pas de costat per desencallar-se.

    [Header("Esquivar obstacles")]
    public float avoidLookAhead = 1.4f;   // fins on miren endavant (unitats)

    static readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    static readonly Dictionary<int, bool> obstacleCache = new Dictionary<int, bool>(); // col·lisionador -> és obstacle?

    private float bodyRadius = -1f;
    private int avoidSide;              // per quin costat el voregem (1 o -1), perquè no dubti a cada pas
    private float avoidUntil;
    private Vector2 lastProgressPos;
    private float lastProgressTime;
    private float unstickUntil;
    private Vector2 unstickDir;

    Vector2 SteerAround(Vector2 desired, float distToPlayer)
    {
        float now = Time.time;
        if (now < unstickUntil) return unstickDir;

        // fa estona que no avança? un pas de costat
        if ((rb.position - lastProgressPos).sqrMagnitude > 0.04f)
        {
            lastProgressPos = rb.position;
            lastProgressTime = now;
        }
        else if (now - lastProgressTime > 0.6f)
        {
            lastProgressTime = now;
            float side = Random.value < 0.5f ? 1f : -1f;
            unstickDir = (new Vector2(desired.y, -desired.x) * side - desired * 0.3f).normalized;
            unstickUntil = now + 0.45f;
            return unstickDir;
        }

        if (bodyRadius < 0f) bodyRadius = BodyRadius();
        float look = Mathf.Min(avoidLookAhead, distToPlayer - bodyRadius);
        if (look <= 0.05f) return desired; // ja és a sobre del jugador

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        int n = Physics2D.CircleCast(rb.position, bodyRadius * 0.9f, desired, filter, castHits, look);

        for (int i = 0; i < n; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (!IsObstacle(hit.collider)) continue;

            // per quin costat: el contrari d'on queda el centre de l'obstacle (i sense canviar a mig camí)
            if (avoidSide == 0 || now > avoidUntil)
            {
                Vector2 toCenter = (Vector2)hit.collider.bounds.center - rb.position;
                float cross = desired.x * toCenter.y - desired.y * toCenter.x; // > 0: el centre queda a l'esquerra
                avoidSide = cross > 0f ? 1 : -1;                               // 1 = cap a la dreta
            }
            avoidUntil = now + 0.4f;

            // llisquem per la cara de l'obstacle cap al costat triat
            Vector2 sidePerp = new Vector2(desired.y, -desired.x) * avoidSide;
            Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
            if (Vector2.Dot(tangent, sidePerp) < 0f) tangent = -tangent;

            float closeness = 1f - Mathf.Clamp01(hit.distance / look);
            return (tangent + desired * (1f - closeness) * 0.5f).normalized;
        }

        return desired;
    }

    // un obstacle és qualsevol cosa sòlida que no sigui un client ni el jugador
    bool IsObstacle(Collider2D c)
    {
        if (c == null || c.isTrigger) return false;
        if (c.attachedRigidbody != null && c.attachedRigidbody == rb) return false; // ell mateix

        int id = c.GetInstanceID();
        if (obstacleCache.TryGetValue(id, out bool cached)) return cached;

        bool obstacle = c.GetComponentInParent<Enemy>() == null && c.GetComponentInParent<PlayerHealth>() == null;
        obstacleCache[id] = obstacle;
        return obstacle;
    }

    float BodyRadius()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return 0.3f;
        Vector3 ext = col.bounds.extents;
        float r = Mathf.Min(ext.x, ext.y);
        return r > 0.01f ? r : 0.3f;
    }

    void UpdateFacingSprite(Vector2 toPlayer)
    {
        if (!renderersCached || spriteRenderers.Count == 0)
            CacheRenderers();
        if (spriteRenderers.Count == 0) return;
        SpriteRenderer targetSr = spriteRenderer != null ? spriteRenderer : spriteRenderers[0];
        float dist = toPlayer.magnitude;
        if (dist < 0.001f) return;
        float relY = toPlayer.y / dist;
        if (relY > 0.35f) mirarAmunt = true;
        else if (relY < -0.35f) mirarAmunt = false;
        Sprite target = mirarAmunt ? spriteBack : spriteFront;
        if (target != null && targetSr.sprite != target) targetSr.sprite = target;
    }

    void Update()
    {
        if (!renderersCached || spriteRenderers.Count == 0)
            CacheRenderers();

        UpdateColor();
        UpdateShooting(); // abans el "return" del flaix/frenat se saltava el tir
    }

    // només toca els sprites quan canvia l'estat (abans els reescrivia cada frame)
    void UpdateColor()
    {
        float now = Time.time;
        ColorState wanted = now < hitFlashUntil ? ColorState.Flash
                          : now < slowUntil ? ColorState.Slow
                          : ColorState.Base;
        if (wanted == colorState) return;
        colorState = wanted;

        Color slowColor = new Color(0.5f, 0.8f, 1f);
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] == null) continue;

            if (wanted == ColorState.Flash)
            {
                spriteRenderers[i].color = Color.white;
                continue;
            }

            Color baseCol = baseColors.Count > i ? baseColors[i] : (hasBaseColor ? baseColor : spriteRenderers[i].color);
            spriteRenderers[i].color = wanted == ColorState.Slow ? Color.Lerp(baseCol, slowColor, 0.6f) : baseCol;
        }
    }

    void UpdateShooting()
    {
        if (!isShooter || player == null || shotPrefab == null) return;
        float d = Vector2.Distance(transform.position, player.position);
        if (d <= stopDistance + 1f && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireInterval;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        GameObject s = Instantiate(shotPrefab, (Vector2)transform.position + dir * 0.6f, Quaternion.identity);
        Rigidbody2D srb = s.GetComponent<Rigidbody2D>();
        if (srb != null) srb.linearVelocity = dir * shotSpeed;
    }

    public void TakeDamage(int amount, bool crit = false)
    {
        if (dead) return;
        int dealt = Mathf.RoundToInt(amount * damageMultiplier);
        health -= dealt;
        ForceHitFlash();

        // "suc" de l'impacte: número de dany (groc als crítics) i una mica de pausa als crítics
        DamageNumber.Spawn((Vector2)transform.position + Vector2.up * 0.7f, dealt, crit);
        if (crit) HitStop.Do(0.045f);

        // els caps trontollen una mica a cada cop
        if (isBoss && health > 0) CameraFX.Shake(0.07f, crit ? 0.1f : 0.05f);

        if (health <= 0)
        {
            dead = true;
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddKill(points, transform.position);
            CameraFX.Shake(0.14f, 0.12f);
            if (isBoss)
            {
                // matar un cap: pausa llarga i un bon xoc de càmera (el de després guanya al de dalt)
                HitStop.Do(0.2f, true);
                CameraFX.Shake(0.6f, 0.35f);
            }
            OnKilled?.Invoke(this);
            Destroy(gameObject);
        }
    }

    public void ForceHitFlash()
    {
        if (dead) return;
        Flash(Color.white, 0.1f);
    }

    public void Flash(Color color, float duration)
    {
        if (dead) return;
        if (!renderersCached || spriteRenderers.Count == 0)
            CacheRenderers();
        if (spriteRenderers.Count == 0) return;
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = color;
        }
        hitFlashUntil = Time.time + duration;
        colorState = ColorState.Flash;
    }

    public void Knockback(Vector2 direction, float force, float duration)
    {
        if (!canBeKnockedBack) return;
        knockbackVelocity = direction.normalized * force;
        knockbackUntil = Time.time + duration;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextDamageTime) return;
        PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage();
            nextDamageTime = Time.time + damageCooldown;
        }
    }
}