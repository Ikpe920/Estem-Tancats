using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 10; // ja no compta: cada cop que toca, el jugador perd un cor
    public float lifetime = 4f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<ArenaWalls>() != null) { Destroy(gameObject); return; }

        BlocksShots shield = other.GetComponent<BlocksShots>();
        if (shield != null)
        {
            shield.OnShotBlocked();
            Destroy(gameObject);
            return;
        }

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(); // cada cop que toca el jugador, perdi un cor
            Destroy(gameObject);
        }
    }
}