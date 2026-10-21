using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float fireRate = 0.3f;     // segons entre cada tret
    public float spawnOffset = 0.7f;  // distància davant del xef

    private Camera cam;
    private float nextFireTime;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;

        Vector2 spawnPos = (Vector2)transform.position + dir * spawnOffset;
        GameObject p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        p.GetComponent<Rigidbody2D>().linearVelocity = dir * projectileSpeed;
    }
}