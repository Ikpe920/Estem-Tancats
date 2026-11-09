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

    void OnEnable()  { Alive++; }
    void OnDisable() { Alive--; }

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
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        nextShotTime = Time.time + Random.Range(0.5f, fireInterval);
    }

    public void Freeze(float duration)
    {
        frozenUntil = Mathf.Max(frozenUntil, Time.time + duration);
    }

    public void Slow(float factor, float duration)
    {
        slowFactor = factor;
        slowUntil = Time.time + duration;
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

    public void TakeDamage(int amount)
    {
        if (dead) return;
        health -= Mathf.RoundToInt(amount * damageMultiplier);
        ForceHitFlash();

        if (health <= 0)
        {
            dead = true;
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(points);
            CameraFX.Shake(0.14f, 0.12f);
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