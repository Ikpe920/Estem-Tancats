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

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;

        // triar un projectil a l'atzar
        GameObject prefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];

        Vector2 spawnPos = (Vector2)transform.position + dir * spawnOffset;
        GameObject p = Instantiate(prefab, spawnPos, Quaternion.identity);

        Projectile proj = p.GetComponent<Projectile>();
        proj.damage += damage - 1; // dany del prefab + millores comprades

        p.GetComponent<Rigidbody2D>().linearVelocity = dir * projectileSpeed;
    }
}