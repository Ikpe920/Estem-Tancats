using System.Collections;
using UnityEngine;

// =====================================================================
//  CLIENTS D'ELIT: clients normals amb corona que copien un atac d'un cap, en petit
// =====================================================================
// A partir de l'onada 6, de tant en tant (cada cop més sovint), un client surt d'elit:
// més gran, amb corona i un brill daurat, el triple de vida i el quàdruple de punts; i en morir deixa
// un grapat de monedes d'experiència. Cada un té un atac:
//   CARREGADOR  (com el Borratxo)   avisa amb una línia i fa una envestida
//   TAPERS      (com el Sommelier)  llença ventalls de taps de suro
//   GELAT       (com el Gelater)    deixa terra glaçat al seu pas
//   PETARD      (com el Xef)        en morir, s'encén i esclata (aparta't!)
public class EliteClient : MonoBehaviour
{
    public enum Kind { Carregador, Tapers, Gelat, Petard }

    Kind kind;
    Enemy enemy;
    Rigidbody2D rb;
    float next;
    GameObject crown;
    static Sprite cork;

    public static void MaybeApply(Enemy e, int wave)
    {
        if (e == null || e.isBoss || wave < 6 || BossRush.Active) return;
        float chance = Mathf.Lerp(0.05f, 0.14f, Mathf.InverseLerp(6f, 35f, wave));
        if (Random.value > chance) return;
        EliteClient ec = e.gameObject.AddComponent<EliteClient>();
        ec.kind = (Kind)Random.Range(0, 4);
    }

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        if (enemy == null) { Destroy(this); return; }
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 3f), enemy.speed);
        enemy.points *= 4;
        transform.localScale *= 1.25f;
        // la corona
        crown = new GameObject("CoronaElit");
        crown.transform.SetParent(transform, false);
        crown.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        float inv = 1f / Mathf.Max(0.01f, Mathf.Abs(transform.localScale.x));
        crown.transform.localScale = Vector3.one * 0.9f * inv;
        SpriteRenderer cs = crown.AddComponent<SpriteRenderer>();
        cs.sprite = PropArt.Crown();
        cs.sortingOrder = 30;
        next = Time.time + Random.Range(2f, 3.5f);
        Enemy.OnKilled += OnKilled;
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.4f, "CLIENT D'ELIT!", new Color(1f, 0.85f, 0.3f), 1.2f);
    }

    void OnDestroy() { Enemy.OnKilled -= OnKilled; }

    void Update()
    {
        if (enemy == null || enemy.IsDead || PlayerHealth.IsDying) return;
        if (Random.value < Time.deltaTime * 4f) Fx.Sparkle((Vector2)transform.position + Random.insideUnitCircle * 0.6f, new Color(1f, 0.85f, 0.3f));
        if (Time.time < next) return;
        switch (kind)
        {
            case Kind.Carregador: next = Time.time + 4.5f; StartCoroutine(Charge()); break;
            case Kind.Tapers: next = Time.time + 3.5f; Corks(); break;
            case Kind.Gelat: next = Time.time + 1.2f; Frost(); break;
            default: next = Time.time + 999f; break;
        }
    }

    IEnumerator Charge()
    {
        Transform p = PlayerRef.Transform;
        if (p == null) yield break;
        Vector2 dir = ((Vector2)p.position - (Vector2)transform.position).normalized;
        SpriteRenderer line = BossFx.Line(transform.position, dir, 6f, 0.9f, new Color(1f, 0.6f, 0.2f, 0.3f), "AvisElit");
        Destroy(line.gameObject, 0.8f);
        enemy.externalMovement = true;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            if (enemy == null || enemy.IsDead) yield break;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (line != null) line.transform.position = (Vector2)transform.position + dir * 3f;
            yield return null;
        }
        Squash.Kick(gameObject, 0.25f, -0.2f);
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            if (enemy == null || enemy.IsDead) yield break;
            if (rb != null) rb.linearVelocity = dir * 11f;
            if (Random.value < 0.4f) Fx.Dust(transform.position);
            yield return null;
        }
        if (enemy != null) enemy.externalMovement = false;
    }

    void Corks()
    {
        GameObject prefab = WaveSpawner.Instance != null ? WaveSpawner.Instance.ShotPrefab() : null;
        Transform p = PlayerRef.Transform;
        if (prefab == null || p == null) return;
        if (cork == null) cork = Resources.Load<Sprite>("BossShots/Cork");
        Vector2 to = ((Vector2)p.position - (Vector2)transform.position).normalized;
        float a0 = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        for (int i = -1; i <= 1; i++)
        {
            float a = (a0 + i * 16f) * Mathf.Deg2Rad;
            BossShotFx f = BossBrain.SpawnShot(prefab, cork, transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 5.5f, 0.55f);
            if (f != null) f.spin = 500f;
        }
        Squash.Kick(gameObject, 0.15f, -0.15f);
        AudioManager.Play("pickup", 0.4f, 0.7f);
    }

    void Frost()
    {
        BossPuddle.Spawn(transform.position, 0.8f, 4f, Fx.FrostGround(), new Color(1f, 1f, 1f, 0.8f), 0.35f, "RELLISCA!", false, "neu");
    }

    void OnKilled(Enemy e)
    {
        if (e != enemy) return;
        Vector2 pos = transform.position;
        for (int i = 0; i < 5; i++) XpCoin.Spawn(pos + Random.insideUnitCircle * 0.6f, 1);
        Fx.Sparkles(pos, new Color(1f, 0.85f, 0.3f), 10, 1f);
        if (kind == Kind.Petard && WaveSpawner.Instance != null) WaveSpawner.Instance.StartCoroutine(Blast(pos));
    }

    // el petard: s'encén i esclata un segon després (també et fa mal a tu)
    static IEnumerator Blast(Vector2 pos)
    {
        SpriteRenderer warn = HazardFx.MakeCircle(pos, 1.8f, new Color(1f, 0.35f, 0.1f, 0.15f), -2);
        AudioManager.Play("fuse", 0.8f);
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            if (warn != null) warn.color = new Color(1f, 0.35f, 0.1f, Mathf.Lerp(0.15f, 0.5f, t) * (0.8f + 0.2f * Mathf.Sin(t * 30f)));
            if (Random.value < 0.3f) Fx.Flames(pos, 0.3f, 0.5f);
            yield return null;
        }
        if (warn != null) Object.Destroy(warn.gameObject);
        Fx.Explosion(pos, 1.8f, 1f);
        AudioManager.Play("explosion", 0.6f);
        BossBrain.HurtPlayerIn(pos, 1.8f);
        DishUtil.HitCircle(pos, 1.8f, 6, 6f, DmgKind.Explosion);
    }
}
