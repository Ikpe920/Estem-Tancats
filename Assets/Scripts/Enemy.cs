using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public int maxHealth = 3;
    public int contactDamage = 10;
    public float damageCooldown = 1f;
    public int points = 10;

    private int health;
    private Rigidbody2D rb;
    private Transform player;
    private float nextDamageTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = maxHealth;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = dir * speed;
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

    void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextDamageTime) return;

        PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(contactDamage);
            nextDamageTime = Time.time + damageCooldown;
        }
    }
}