using UnityEngine;

// =====================================================================
//  CAIXES DESTRUCTIBLES I DROPS
// =====================================================================

public class Breakable : MonoBehaviour
{
    public int health = 3;

    static readonly Color HitColor = new Color(1f, 0.6f, 0.55f);      // vermellós en rebre un cop
    static readonly Color DamagedColor = new Color(0.62f, 0.56f, 0.52f); // com més tocada, més fosca

    private SpriteRenderer sr;
    private int maxHealth;
    private float flashUntil;
    private float hitAt = -1f;
    private bool broken;

    void Start()
    {
        maxHealth = Mathf.Max(1, health);
        Transform body = transform.Find("Dibuix");
        sr = body != null ? body.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
    }

    public void Hit(int damage)
    {
        if (broken) return;

        health -= damage;
        flashUntil = Time.time + 0.08f;
        hitAt = Time.time;
        if (health <= 0) Break();
    }

    void Update()
    {
        if (sr == null) return;

        // el color: vermellós un instant en rebre, i cada cop més fosc com més tocada està
        float damaged = 1f - Mathf.Clamp01((float)health / Mathf.Max(1, maxHealth));
        Color baseColor = Color.Lerp(Color.white, DamagedColor, damaged);
        sr.color = Time.time < flashUntil ? HitColor : baseColor;

        // i trontolla una mica
        float t = hitAt < 0f ? 1f : (Time.time - hitAt) / 0.2f;
        sr.transform.localRotation = t < 1f ? Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 8f * (1f - t)) : Quaternion.identity;
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
