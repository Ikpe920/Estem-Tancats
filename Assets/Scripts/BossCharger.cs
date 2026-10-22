using System.Collections;
using UnityEngine;

// El Borratxo: embesteix en línia recta i, quan s'atura, queda estabornit (rep el doble de mal)
public class BossCharger : MonoBehaviour
{
    public float windupTime = 0.9f;          // avís abans d'embestir
    public float chargeSpeed = 14f;          // velocitat de la càrrega
    public float chargeTime = 0.55f;         // durada de la càrrega
    public float stunTime = 1.4f;            // temps que queda estabornit
    public float pauseBetween = 2.2f;        // descans entre càrregues
    public float chargeDistance = 9f;        // llargada de la línia d'avís
    public float lineWidth = 0.8f;
    public float stunDamageMultiplier = 2f;  // mal extra mentre està estabornit

    private Enemy enemy;
    private Rigidbody2D rb;
    private Transform player;
    private Vector2 chargeDir = Vector2.right;
    private GameObject stunFx;
    private bool charging;
    private bool stunned;
    private bool busy;
    private float nextCharge;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        enemy.externalMovement = true; // aquest cap es mou sol
        nextCharge = Time.time + 1.5f;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (charging)
        {
            rb.linearVelocity = chargeDir * chargeSpeed;
            return;
        }

        if (stunned || busy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // caminar normal cap al jugador
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        rb.linearVelocity = toPlayer.normalized * enemy.speed * Enemy.speedMultiplier;
    }

    void Update()
    {
        if (busy || player == null || Time.time < nextCharge) return;
        StartCoroutine(ChargeRoutine());
    }

    IEnumerator ChargeRoutine()
    {
        busy = true;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        // 1. avís: una línia vermella marca la trajectòria de la càrrega
        SpriteRenderer line = BossFx.Line(transform.position, dir, chargeDistance, lineWidth,
                                         new Color(1f, 0.3f, 0.2f, 0.15f), "BossChargeLine");
        Destroy(line.gameObject, windupTime + 1f);

        float t = 0f;
        Vector2 follow = dir;
        while (t < windupTime)
        {
            t += Time.deltaTime;

            if (player != null)
            {
                follow = ((Vector2)player.position - (Vector2)transform.position).normalized;
                if (follow == Vector2.zero) follow = dir;
            }

            float angle = Mathf.Atan2(follow.y, follow.x) * Mathf.Rad2Deg;
            line.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            line.transform.position = (Vector2)transform.position + follow * (chargeDistance * 0.5f);
            line.color = new Color(1f, 0.3f, 0.2f, Mathf.Lerp(0.15f, 0.5f, t / windupTime));

            yield return null;
        }
        Destroy(line.gameObject);

        // 2. la càrrega en línia recta
        chargeDir = follow;
        charging = true;
        float end = Time.time + chargeTime;
        while (Time.time < end) yield return null;
        charging = false;
        rb.linearVelocity = Vector2.zero;

        // 3. estabornit: moment per fer-li mal
        stunned = true;
        enemy.damageMultiplier = stunDamageMultiplier;
        stunFx = MakeStunFx();
        Destroy(stunFx, stunTime + 1f);

        float st = 0f;
        while (st < stunTime)
        {
            st += Time.deltaTime;
            if (stunFx != null) stunFx.transform.Rotate(0f, 0f, 300f * Time.deltaTime);
            yield return null;
        }

        if (stunFx != null) Destroy(stunFx);
        enemy.damageMultiplier = 1f;
        stunned = false;

        busy = false;
        nextCharge = Time.time + pauseBetween;
    }

    // estrelles grogues que giren sobre el cap
    GameObject MakeStunFx()
    {
        GameObject root = new GameObject("BossStunFx");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        for (int i = 0; i < 3; i++)
        {
            GameObject star = new GameObject("Estrella");
            star.transform.SetParent(root.transform, false);

            float a = i * Mathf.PI * 2f / 3f;
            star.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.4f, Mathf.Sin(a) * 0.15f, 0f);
            star.transform.localScale = Vector3.one * 0.12f;

            SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
            sr.sprite = BossFx.Square();
            sr.color = new Color(1f, 0.9f, 0.3f, 1f);
            sr.sortingOrder = 20;
        }
        return root;
    }
}
