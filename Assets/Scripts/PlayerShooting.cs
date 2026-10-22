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
    public int pierce = 0;
    public float explosionRadius = 0f;
    public float slowFactor = 1f;
    public float slowDuration = 0f;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Time.timeScale == 0) return; // no disparar mentre la botiga està oberta

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefabs == null || projectilePrefabs.Length == 0) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 baseDir = (mouseWorld - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

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
            proj.pierce = pierce;
            proj.explosionRadius = explosionRadius;
            proj.slowFactor = slowFactor;
            proj.slowDuration = slowDuration;

            p.GetComponent<Rigidbody2D>().linearVelocity = dir * projectileSpeed;
        }
    }
}