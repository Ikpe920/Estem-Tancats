using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Estadístiques")]
    public float speed = 2f;
    public int maxHealth = 3;
    public int contactDamage = 10; // ja no compta: cada cop que toca, el jugador perd un cor
    public float damageCooldown = 1f;
    public int points = 10;

    [Header("Tirador (opcional)")]
    public bool isShooter = false;
    public float stopDistance = 6f;
    public float fireInterval = 2f;
    public float shotSpeed = 6f;
    public GameObject shotPrefab;

    public Sprite spriteFront; // sprite del client mirant al jugador (de cara)
    public Sprite spriteBack;  // sprite del client donant l'esquena (pujant)

    private int health;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private float nextDamageTime;
    private float nextShotTime;
    public int CurrentHealth => health;
    public bool canBeKnockedBack = true;
    private Vector2 knockbackVelocity;
    private float knockbackUntil;
    private float frozenUntil;
    private bool mirarAmunt = false; // true = el client puja cap al jugador, es veu l'esquena

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = maxHealth;
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

        if (isShooter && toPlayer.magnitude <= stopDistance)
            rb.linearVelocity = Vector2.zero;
        else
            rb.linearVelocity = toPlayer.normalized * speed;
    }

    // ---------- SPRITE SEGONS LA DIRECCIO ----------

    void UpdateFacingSprite(Vector2 toPlayer)
    {
        if (spriteRenderer == null) return;

        float dist = toPlayer.magnitude;
        if (dist < 0.001f) return;

        // nomes gira si el movement es clarament vertical, perquè no faci pampallamps
        float relY = toPlayer.y / dist;
        if (relY > 0.35f) mirarAmunt = true;
        else if (relY < -0.35f) mirarAmunt = false;

        Sprite target = mirarAmunt ? spriteBack : spriteFront;
        if (target != null && spriteRenderer.sprite != target) spriteRenderer.sprite = target;
    }

    void Update()
    {
        if (!isShooter || player == null || shotPrefab == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= stopDistance + 1f && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireInterval;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        GameObject s = Instantiate(shotPrefab, (Vector2)transform.position + dir * 0.6f, Quaternion.identity);
        s.GetComponent<Rigidbody2D>().linearVelocity = dir * shotSpeed;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(points);
            Destroy(gameObject);
        }
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
            ph.TakeDamage(); // cada cop que toca el jugador, perdi un cor
            nextDamageTime = Time.time + damageCooldown;
        }
    }
}