using System.Collections.Generic;
using UnityEngine;

public class DashDamage : MonoBehaviour
{
    public int damage = 0;
    public float radius = 1f;

    private PlayerMovement movement;
    private readonly HashSet<Enemy> hitThisDash = new HashSet<Enemy>();
    private bool wasDashing;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (movement == null || damage <= 0) return;

        if (!movement.IsDashing)
        {
            wasDashing = false;
            return;
        }

        if (!wasDashing)
        {
            hitThisDash.Clear();
            wasDashing = true;
        }

        foreach (Collider2D h in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Enemy e = h.GetComponent<Enemy>();
            if (e == null || hitThisDash.Contains(e)) continue;
            hitThisDash.Add(e);
            e.TakeDamage(damage);
        }
    }
}