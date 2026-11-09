using UnityEngine;

// ---------- LA BARRA: ampolles que exploten si les dispares ----------
public class Bottle : MonoBehaviour
{
    public float blastRadius = 2.4f;
    public int enemyDamage = 6;

    private bool exploded;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded) return;
        if (other.GetComponent<Projectile>() == null) return;

        exploded = true;
        Explode();
    }

    void Explode()
    {
        foreach (Collider2D h in Physics2D.OverlapCircleAll(transform.position, blastRadius))
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage();
                continue;
            }

            Enemy e = h.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(enemyDamage);
        }

        Fx.Explosion(transform.position, blastRadius, 1.2f);
        HazardFx.Shake(0.22f, 0.2f);

        Destroy(gameObject);
    }
}
