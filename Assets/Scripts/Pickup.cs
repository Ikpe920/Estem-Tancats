using UnityEngine;

public enum PickupType { Heart, Coffee, Pepper, Points }

public class Pickup : MonoBehaviour
{
    public PickupType type;
    public float life = 12f;

    private float born;
    private Transform body;          // el dibuix (fa el moviment; l'ombra es queda a terra)
    private SpriteRenderer glow;     // la llum de sota, que batega
    private SpriteRenderer[] renderers;
    private bool collected;

    public static void SpawnDrop(Vector2 pos)
    {
        float r = Random.value;
        if (r < 0.25f) return; // el 25% de les caixes no deixen res

        PickupType t;
        if (r < 0.45f) t = PickupType.Heart;
        else if (r < 0.70f) t = PickupType.Coffee;
        else if (r < 0.90f) t = PickupType.Pepper;
        else t = PickupType.Points;

        // si tens tots els cors, no té sentit que surti un cor
        if (t == PickupType.Heart)
        {
            PlayerHealth ph = PlayerRef.Health;
            if (ph != null && ph.CurrentLives >= ph.maxLives) t = PickupType.Coffee;
        }

        Create(pos, t);
    }

    static void Create(Vector2 pos, PickupType t)
    {
        GameObject go = new GameObject("Drop " + t);
        go.transform.position = pos;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;

        Pickup p = go.AddComponent<Pickup>();
        p.type = t;

        // el sprite de cada premi (el cor, si pot, és el mateix que el dels cors del HUD)
        Sprite sprite;
        Color glow;
        switch (t)
        {
            case PickupType.Heart:
                LifeHearts lh = FindAnyObjectByType<LifeHearts>();
                sprite = lh != null && lh.heartSprite != null ? lh.heartSprite : PropArt.Heart();
                glow = new Color(1f, 0.35f, 0.45f);
                break;
            case PickupType.Coffee:
                sprite = PropArt.Coffee();
                glow = new Color(1f, 0.85f, 0.6f);
                break;
            case PickupType.Pepper:
                sprite = PropArt.Pepper();
                glow = new Color(1f, 0.4f, 0.25f);
                break;
            default:
                sprite = PropArt.Coin();
                glow = new Color(1f, 0.9f, 0.35f);
                break;
        }

        // a sota: una ombra que es queda a terra i una llum suau del color del premi
        p.glow = Layer(go.transform, "Llum", PropArt.Glow(), new Color(glow.r, glow.g, glow.b, 0.45f), 2,
            new Vector3(0f, 0.08f, 0f), new Vector3(1.4f, 1.4f, 1f));
        Layer(go.transform, "Ombra", PropArt.Shadow(), new Color(1f, 1f, 1f, 0.3f), 2,
            new Vector3(0f, -0.38f, 0f), new Vector3(0.7f, 0.5f, 1f));

        // el dibuix, que és el que fa el moviment amunt i avall
        SpriteRenderer body = Layer(go.transform, "Dibuix", sprite, Color.white, 3, Vector3.zero, Vector3.one);
        // els sprites de PropArt fan 0,8 unitats; el del HUD es fa de la mateixa mida
        float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (size > 0.01f) body.transform.localScale = Vector3.one * (0.8f / size);
        p.body = body.transform;
    }

    static SpriteRenderer Layer(Transform parent, string name, Sprite sprite, Color color, int order, Vector3 pos, Vector3 scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void Start()
    {
        born = Time.time;
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float age = Time.time - born;

        if (body != null) body.localPosition = Vector3.up * (0.08f + Mathf.Sin(age * 5f) * 0.12f);
        if (glow != null)
        {
            Color g = glow.color;
            g.a = 0.3f + 0.2f * Mathf.Sin(age * 4f);
            glow.color = g;
        }

        // l'últim parell de segons parpelleja, i desapareix
        if (life - age < 3f)
        {
            bool on = Mathf.FloorToInt(age * 8f) % 2 == 0;
            foreach (SpriteRenderer sr in renderers)
                if (sr != null) sr.enabled = on;
        }

        if (age >= life) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<PlayerMovement>() == null) return;

        collected = true;
        Vector2 at = transform.position;

        switch (type)
        {
            case PickupType.Heart:
            {
                PlayerHealth ph = other.GetComponent<PlayerHealth>();
                if (ph != null) ph.Heal(1);
                HazardFx.Popup(at, "+1 COR", new Color(1f, 0.4f, 0.5f));
                break;
            }
            case PickupType.Coffee:
                GetBuffs(other.gameObject).SpeedBoost(1.5f, 8f);
                HazardFx.Popup(at, "CAFÈ! + velocitat", new Color(0.9f, 0.7f, 0.45f));
                break;
            case PickupType.Pepper:
                GetBuffs(other.gameObject).DamageBoost(1, 8f);
                HazardFx.Popup(at, "PEBRE! + dany", new Color(1f, 0.45f, 0.35f));
                break;
            case PickupType.Points:
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(25);
                HazardFx.Popup(at, "+25", new Color(1f, 0.9f, 0.4f));
                break;
        }

        Destroy(gameObject);
    }

    static PlayerBuffs GetBuffs(GameObject player)
    {
        PlayerBuffs b = player.GetComponent<PlayerBuffs>();
        if (b == null) b = player.AddComponent<PlayerBuffs>();
        return b;
    }
}
