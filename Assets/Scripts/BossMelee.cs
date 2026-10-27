using System.Collections;
using UnityEngine;

public class BossMelee : MonoBehaviour
{
    [Header("Atac")]
    public int damage = 25; // ja no compta: cada cop que toca, el jugador perd un cor
    public float range = 4.5f;
    public float arcAngle = 100f;
    public float triggerDistance = 5f;
    public float cooldown = 3.5f;

    [Header("Temps")]
    public float windupTime = 0.8f;   // temps d'avís abans del cop
    public float recoverTime = 0.4f;  // temps que es queda quiet després

    [Header("Cop al jugador")]
    public float knockbackForce = 16f;
    public float knockbackTime = 0.25f;

    [Header("Visual")]
    public GameObject swingPrefab;
    public float swingDesignRange = 2.2f;

    private Enemy enemy;
    private Transform player;
    private Sprite coneSprite;
    private GameObject telegraph;
    private float nextAttackTime;
    private bool attacking;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        coneSprite = BuildConeSprite();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        nextAttackTime = Time.time + 2f;
    }

    void Update()
    {
        if (attacking || player == null || Time.time < nextAttackTime) return;

        if (Vector2.Distance(transform.position, player.position) <= triggerDistance)
            StartCoroutine(MeleeRoutine());
    }

    IEnumerator MeleeRoutine()
    {
        attacking = true;

        Vector2 origin = transform.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        enemy.Freeze(windupTime + recoverTime);

        // 1. avís: con vermell que es va omplint
        telegraph = new GameObject("BossTelegraph");
        telegraph.transform.position = origin;
        telegraph.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        SpriteRenderer sr = telegraph.AddComponent<SpriteRenderer>();
        sr.sprite = coneSprite;
        sr.sortingOrder = -2; // per sobre del terra, per sota dels personatges

        float t = 0f;
        while (t < windupTime)
        {
            t += Time.deltaTime;
            sr.color = new Color(1f, 0.2f, 0.2f, Mathf.Lerp(0.15f, 0.55f, t / windupTime));
            yield return null;
        }
        Destroy(telegraph);

        // 2. el cop
        if (swingPrefab != null)
        {
            GameObject fx = Instantiate(swingPrefab, origin, Quaternion.identity);
            fx.transform.localScale = Vector3.one * (range / swingDesignRange);
            fx.GetComponent<SwingEffect>().Init(angle, arcAngle);
        }
        DoHit(origin, dir);

        // 3. es queda un moment quiet
        yield return new WaitForSeconds(recoverTime);

        nextAttackTime = Time.time + cooldown;
        attacking = false;
    }

    void DoHit(Vector2 origin, Vector2 dir)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);
        foreach (Collider2D h in hits)
        {
            if (!h.CompareTag("Player")) continue;

            Vector2 closest = h.ClosestPoint(origin);
            if (Vector2.Angle(dir, closest - origin) > arcAngle / 2f) continue;

            if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(0.18f, 0.16f);
            else if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.18f, 0.16f);
            else if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.18f, 0.16f);

            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(); // cada cop que toca el jugador, perdi un cor

            PlayerMovement pm = h.GetComponent<PlayerMovement>();
            if (pm != null) pm.Knockback((Vector2)h.transform.position - origin, knockbackForce, knockbackTime);
        }
    }

    // genera el sprite del con vermell sense necessitar cap imatge
    Sprite BuildConeSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        float half = arcAngle / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f);
                bool inside = p.magnitude <= size / 2f && Vector2.Angle(Vector2.right, p) <= half;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / (2f * range));
    }

    void OnDestroy()
    {
        if (telegraph != null) Destroy(telegraph);
    }
}