using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public GameObject[] projectilePrefabs;
    public float projectileSpeed = 12f;
    public float fireRate = 0.3f;     // segons entre cada tret
    public float spawnOffset = 0.7f;  // distància davant del xef
    public int damage = 1;

    private Camera cam;
    private float nextFireTime;

    // efectes dels plats (els posa el BoonManager)
    public int extraShots = 0;        // projectils extra en ventall
    public float spreadAngle = 12f;   // graus entre projectils
    [Range(0.2f, 1f)] public float sideShotDamage = 0.7f; // els projectils del ventall (no el central) fan aquesta part del dany
    public int pierce = 0;
    public float explosionRadius = 0f;
    public float slowFactor = 1f;
    public float slowDuration = 0f;
    public float critChance = 0f;     // probabilitat de crític (0-1)
    public float critMultiplier = 2f; // el dany d'un crític

    // tret ràpid temporal (el conjunt Sobretaula): el temps entre trets es multiplica per aquest valor
    private float hasteUntil;
    private const float HasteFactor = 0.71f;

    // el temps real entre trets: el dels plats, més el tret ràpid si està actiu
    float CurrentFireRate
    {
        get { return Time.time < hasteUntil ? fireRate * HasteFactor : fireRate; }
    }

    public void GrantHaste(float seconds)
    {
        hasteUntil = Mathf.Max(hasteUntil, Time.time + seconds);
    }

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Time.timeScale == 0) return; // no disparar mentre la botiga està oberta

        if (Mouse.current == null) return;
        if (DebugMode.MouseOverPanel) return; // no disparar en clicar el quadre del mode de prova

        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + CurrentFireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefabs == null || projectilePrefabs.Length == 0) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 baseDir = (mouseWorld - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        // plats de conjunt
        float sideDamage = SetBonuses.Has(SetId.FullFan) ? 1f : sideShotDamage; // Paella exprés: el ventall fa tot el dany
        bool anyCrit = false;

        int total = 1 + extraShots;
        for (int i = 0; i < total; i++)
        {
            // els projectils es reparteixen en ventall, centrats cap al ratolí
            float angle = baseAngle + (i - (total - 1) / 2f) * spreadAngle;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject prefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];
            Vector2 spawnPos = (Vector2)transform.position + dir * spawnOffset;
            GameObject p = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

            Projectile proj = p.GetComponent<Projectile>();
            proj.damage += damage - 1; // dany del prefab + millores
            bool isCenter = total % 2 == 1 && i == total / 2;
            if (total > 1 && !isCenter) proj.damage = Mathf.Max(1, Mathf.RoundToInt(proj.damage * sideDamage));
            if (critChance > 0f && Random.value < critChance)
            {
                proj.damage = Mathf.RoundToInt(proj.damage * critMultiplier);
                proj.isCrit = true;
                anyCrit = true;
            }
            proj.pierce = pierce;
            proj.explosionRadius = explosionRadius;
            proj.slowFactor = slowFactor;
            proj.slowDuration = slowDuration;

            p.GetComponent<Rigidbody2D>().linearVelocity = dir * projectileSpeed;
        }

        // Cigaló: un crític et deixa tornar a disparar de seguida
        if (anyCrit && SetBonuses.Has(SetId.Carajillo))
            nextFireTime = Time.time + CurrentFireRate * 0.3f;
    }
}