using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 10; // ja no compta: cada cop que toca, el jugador perd un cor
    public float lifetime = 4f;

    void Start()
    {
        Destroy(gameObject, lifetime);
        Highlight();
    }

    // els trets dels enemics, per sobre de tot: contorn negre i un halo vermell al darrere
    void Highlight()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        sr.sortingOrder = 50;
        GameObject o = new GameObject("Contorn");
        o.transform.SetParent(sr.transform, false);
        o.transform.localScale = Vector3.one * 1.3f;
        SpriteRenderer osr = o.AddComponent<SpriteRenderer>();
        osr.sprite = sr.sprite;
        osr.color = new Color(0.08f, 0.03f, 0.03f, 1f);
        osr.sortingOrder = 49;
        GameObject h = new GameObject("Halo");
        h.transform.SetParent(sr.transform, false);
        SpriteRenderer hsr = h.AddComponent<SpriteRenderer>();
        hsr.sprite = HazardFx.Circle();
        float size = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) * 1.9f;
        h.transform.localScale = Vector3.one * size;
        hsr.color = new Color(1f, 0.15f, 0.1f, 0.35f);
        hsr.sortingOrder = 48;
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