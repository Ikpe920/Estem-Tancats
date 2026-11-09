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
    private Vector2 lastPadAim = Vector2.right; // última direcció apuntada amb el joystick dret
    public float aimDeadZone = 0.3f;            // zona morta del joystick dret

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

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Time.timeScale == 0) return; // no disparar mentre la botiga està oberta
        if (Time.time < nextFireTime) return;

        if (TryGetAim(out Vector2 dir))
        {
            nextFireTime = Time.time + fireRate;
            Shoot(dir);
        }
    }

    // ratolí: clic esquerre i apunta al cursor
    // comandament: apunta amb el joystick dret i dispara movent-lo o amb el gallet dret
    bool TryGetAim(out Vector2 dir)
    {
        dir = Vector2.zero;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            if (stick.magnitude > aimDeadZone) lastPadAim = stick.normalized;

            bool trigger = pad.rightTrigger.isPressed;
            if (stick.magnitude > aimDeadZone || trigger)
            {
                dir = lastPadAim;
                return true;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && cam != null)
        {
            Vector2 mouseWorld = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            dir = (mouseWorld - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero) dir = lastPadAim;
            return true;
        }

        return false;
    }

    void Shoot(Vector2 baseDir)
    {
        if (projectilePrefabs == null || projectilePrefabs.Length == 0) return;

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
            if (proj == null) { Destroy(p); continue; } // prefab mal configurat
            proj.damage += damage - 1; // dany del prefab + millores
            bool isCenter = total % 2 == 1 && i == total / 2;
            if (total > 1 && !isCenter) proj.damage = Mathf.Max(1, Mathf.RoundToInt(proj.damage * sideShotDamage));
            if (critChance > 0f && Random.value < critChance)
            {
                proj.damage = Mathf.RoundToInt(proj.damage * critMultiplier);
                proj.isCrit = true;
            }
            proj.pierce = pierce;
            proj.explosionRadius = explosionRadius;
            proj.slowFactor = slowFactor;
            proj.slowDuration = slowDuration;

            Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
            if (prb != null) prb.linearVelocity = dir * projectileSpeed;
        }
    }
}