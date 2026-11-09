using UnityEngine;

// =====================================================================
//  CAIXES DESTRUCTIBLES I DROPS
// =====================================================================

public class Breakable : MonoBehaviour
{
    public int health = 3;

    private SpriteRenderer sr;
    private Color baseColor;
    private float flashUntil;
    private bool broken;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void Hit(int damage)
    {
        if (broken) return;

        health -= damage;
        flashUntil = Time.time + 0.08f;
        if (health <= 0) Break();
    }

    void Update()
    {
        if (sr != null) sr.color = Time.time < flashUntil ? Color.white : baseColor;
    }

    void Break()
    {
        broken = true;

        SpriteRenderer dust = HazardFx.MakeCircle(transform.position, 0.9f, new Color(0.85f, 0.75f, 0.55f, 0.6f), 4);
        Destroy(dust.gameObject, 0.15f);
        HazardFx.Shake(0.08f, 0.06f);

        Pickup.SpawnDrop(transform.position);
        Destroy(gameObject);
    }
}
