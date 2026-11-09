using UnityEngine;

public enum PickupType { Heart, Coffee, Pepper, Points }

public class Pickup : MonoBehaviour
{
    public PickupType type;
    public float life = 12f;

    private float born;
    private Vector3 basePos;
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

        switch (t)
        {
            case PickupType.Heart:
            {
                LifeHearts lh = FindAnyObjectByType<LifeHearts>();
                if (lh != null && lh.heartSprite != null)
                {
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = lh.heartSprite;
                    sr.sortingOrder = 3;
                    go.transform.localScale = Vector3.one * 0.8f;
                }
                else
                {
                    Dot(go.transform, Vector2.zero, 0.35f, new Color(0.9f, 0.15f, 0.2f));
                }
                break;
            }
            case PickupType.Coffee:
                Dot(go.transform, Vector2.zero, 0.32f, new Color(0.45f, 0.28f, 0.14f));
                Dot(go.transform, Vector2.zero, 0.18f, new Color(0.95f, 0.88f, 0.75f));
                break;
            case PickupType.Pepper:
                Dot(go.transform, Vector2.zero, 0.30f, new Color(0.9f, 0.15f, 0.1f));
                Dot(go.transform, new Vector2(0f, 0.28f), 0.12f, new Color(0.2f, 0.7f, 0.25f));
                break;
            case PickupType.Points:
                Dot(go.transform, Vector2.zero, 0.30f, new Color(1f, 0.82f, 0.2f));
                Dot(go.transform, Vector2.zero, 0.17f, new Color(1f, 0.95f, 0.6f));
                break;
        }
    }

    static void Dot(Transform parent, Vector2 offset, float radius, Color color)
    {
        SpriteRenderer sr = HazardFx.MakeCircle((Vector2)parent.position + offset, radius, color, 3);
        sr.transform.SetParent(parent, true);
    }

    void Start()
    {
        born = Time.time;
        basePos = transform.position;
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float age = Time.time - born;

        transform.position = basePos + Vector3.up * Mathf.Sin(age * 5f) * 0.12f;

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
