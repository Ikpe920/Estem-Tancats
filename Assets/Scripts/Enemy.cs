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
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
            return;
        }
        if (Time.time < frozenUntil)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        UpdateFacingSprite(toPlayer);

        if (externalMovement) return;

        if (isShooter && toPlayer.magnitude <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            float slow = Time.time < slowUntil ? slowFactor : 1f;
            rb.linearVelocity = toPlayer.normalized * speed * speedMultiplier * slow;
        }
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