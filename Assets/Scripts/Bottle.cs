using UnityEngine;

// ---------- LA BARRA: ampolles que exploten si les dispares ----------
public class Bottle : MonoBehaviour
{
    public float blastRadius = 2.4f;
    public int enemyDamage = 6;

    [HideInInspector] public SpriteRenderer body; // el dibuix animat (el posa BottleHazard)

    private bool exploded;
    private float phase;

    static Sprite[] frames;
    static bool loaded;

    // els 4 fotogrames de l'ampolla que bull (Resources/Hazards/Ampolla_0..3)
    public static Sprite[] Frames()
    {
        if (loaded) return frames;
        loaded = true;
        var list = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < 4; i++)
        {
            Sprite s = Resources.Load<Sprite>("Hazards/Ampolla_" + i);
            if (s == null) return frames = null;
            list.Add(s);
        }
        return frames = list.ToArray();
    }

    void Start() { phase = Random.Range(0f, 4f); }

    // bull: canvia de fotograma i es gronxa; de tant en tant treu una bombolla
    void Update()
    {
        if (body == null || frames == null) return;
        float t = Time.time * 8f + phase;
        body.sprite = frames[(int)t % frames.Length];
        body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * 4f);
        if (Random.value < Time.deltaTime * 1.5f) Fx.Sparkle((Vector2)transform.position + new Vector2(Random.Range(-0.2f, 0.2f), 0.7f), new Color(0.75f, 1f, 0.6f));
    }

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
            if (e != null) DamageKind.Hit(e, enemyDamage, DmgKind.Explosion);
        }

        Fx.Explosion(transform.position, blastRadius, 1.2f);
        Fx.Glass(transform.position, 14); // estelles de vidre verd
        HazardFx.Shake(0.22f, 0.2f);

        Destroy(gameObject);
    }
}
