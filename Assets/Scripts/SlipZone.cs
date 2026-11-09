using UnityEngine;

// ---------- LA BARRA: tolls relliscosos ----------
public class SlipZone : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null) pm.slipUntil = Time.time + 0.15f;
    }
}

// el bassal brilla una mica (com si hi toqués la llum) i de tant en tant fa una onada
public class PuddleShine : MonoBehaviour
{
    private SpriteRenderer sr;
    private float seed;
    private float nextRipple;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;
        nextRipple = Time.time + Random.Range(0.5f, 2.5f);
    }

    void Update()
    {
        if (sr == null) return;
        float shine = 0.88f + 0.12f * Mathf.Sin(Time.time * 1.7f + seed);
        sr.color = new Color(shine, shine, shine * 0.95f, 1f);
        if (Time.time >= nextRipple)
        {
            nextRipple = Time.time + Random.Range(1.5f, 3.5f);
            float r = transform.lossyScale.x * 0.3f;
            Fx.Ripple((Vector2)transform.position + Random.insideUnitCircle * r, Random.Range(0.6f, 1.1f), new Color(1f, 0.9f, 0.6f));
        }
    }
}
