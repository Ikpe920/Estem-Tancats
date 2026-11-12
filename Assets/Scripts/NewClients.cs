using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================
//  CLIENTS NOUS (variants, com els d'EnemyVariants)
// =====================================================================
//   EL NEN         petit i molt ràpid, fa ziga-zagues per esquivar els plats
//   LA IAIA        lenta però amb molta vida; et llança el bolso, que et frena
//   EL TURISTA     s'atura i fa una foto amb flaix: si és a prop, et deixa enlluernat
//   EL REPARTIDOR  de tant en tant s'atura, apunta i surt disparat en línia recta
//   L'INFLUENCER   es queda lluny gravant i crida seguidors (més clients)
//   EL MIM         es torna gairebé invisible una estona
// Tots fan servir els seus sprites (Resources/Enemies/<nom>_Front i _Back).

// ---------- EL NEN ----------
public class ClientNen : MonoBehaviour
{
    private Enemy enemy;
    private Rigidbody2D rb;
    private float phase;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        transform.localScale *= 0.75f;
        enemy.OverrideStats(Mathf.Max(1, enemy.MaxHealth / 2), enemy.speed * 1.55f);
        enemy.points = Mathf.RoundToInt(enemy.points * 1.3f);
        EnemyVariants.SetLook(enemy, "Nen");
        enemy.externalMovement = true;
        phase = Random.Range(0f, 10f);
    }

    void FixedUpdate()
    {
        if (enemy == null || rb == null || enemy.IsStunned || enemy.IsDead) return;
        Transform player = PlayerRef.Transform;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }
        Vector2 to = ((Vector2)player.position - rb.position).normalized;
        Vector2 side = new Vector2(-to.y, to.x);
        // corre cap a tu fent ziga-zagues (amb canvis bruscos, com un nen)
        float zig = Mathf.Sign(Mathf.Sin(Time.time * 5f + phase)) * 0.9f;
        rb.linearVelocity = (to + side * zig).normalized * enemy.MoveSpeed;
    }
}

// ---------- LA IAIA ----------
public class ClientIaia : MonoBehaviour
{
    const float ThrowEvery = 3.6f;
    const float ThrowRange = 7f;

    private Enemy enemy;
    private float nextThrow;
    static Sprite bagSprite;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 2.5f), enemy.speed * 0.7f);
        enemy.points *= 2;
        EnemyVariants.SetLook(enemy, "Iaia");
        nextThrow = Time.time + Random.Range(1.5f, ThrowEvery);
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead || enemy.IsStunned || Time.time < nextThrow) return;
        Transform player = PlayerRef.Transform;
        if (player == null || PlayerHealth.IsDying) return;
        if (Vector2.Distance(player.position, transform.position) > ThrowRange) return;
        nextThrow = Time.time + ThrowEvery;

        if (bagSprite == null) bagSprite = Resources.Load<Sprite>("Projectiles/Bolso");
        GameObject go = new GameObject("Bolso");
        go.transform.position = transform.position;
        go.transform.localScale = Vector3.one * 0.55f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bagSprite;
        sr.sortingOrder = 6;
        go.AddComponent<HandbagShot>().Launch(((Vector2)player.position - (Vector2)transform.position).normalized * 6.5f);
        Squash.Kick(gameObject, 0.2f, -0.2f);
    }
}

// el bolso que llança la iaia: si et toca, et frena 2 segons (no treu vida)
public class HandbagShot : MonoBehaviour
{
    static float slowedUntil;
    private Vector2 velocity;
    private float dieAt;

    public void Launch(Vector2 v) { velocity = v; dieAt = Time.time + 2.2f; }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, 400f * Time.deltaTime);
        if (Time.time > dieAt) { Destroy(gameObject); return; }

        Transform player = PlayerRef.Transform;
        if (player == null) return;
        if (Vector2.Distance(player.position, transform.position) > 0.55f) return;

        PlayerMovement pm = PlayerRef.Movement;
        if (pm != null && !pm.IsDashing && Time.time > slowedUntil)
        {
            slowedUntil = Time.time + 2f;
            PlayerBuffs buffs = pm.GetComponent<PlayerBuffs>();
            if (buffs == null) buffs = pm.gameObject.AddComponent<PlayerBuffs>();
            buffs.SpeedBoost(-pm.speed * 0.45f, 2f);
            HazardFx.Popup((Vector2)player.position + Vector2.up, "BOLSADA!", new Color(0.9f, 0.5f, 0.7f), 1f);
            AudioManager.Play("hurt", 0.6f, 1.3f);
            Squash.Hit(pm.gameObject, 0.25f);
        }
        Fx.Poof(transform.position);
        Destroy(gameObject);
    }
}

// ---------- EL TURISTA ----------
public class ClientTurista : MonoBehaviour
{
    const float PhotoEvery = 5f;
    const float PhotoRange = 7.5f;
    const float AimTime = 0.9f;

    private Enemy enemy;
    private Rigidbody2D rb;
    private float nextPhoto;
    private bool aiming;
    private float aimStart;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 1.2f), enemy.speed * 0.95f);
        enemy.points = Mathf.RoundToInt(enemy.points * 1.5f);
        EnemyVariants.SetLook(enemy, "Turista");
        nextPhoto = Time.time + Random.Range(2f, PhotoEvery);
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead) return;
        Transform player = PlayerRef.Transform;
        if (player == null || PlayerHealth.IsDying) return;

        if (!aiming)
        {
            if (Time.time < nextPhoto || enemy.IsStunned) return;
            if (Vector2.Distance(player.position, transform.position) > PhotoRange) return;
            // s'atura i apunta: parpelleja en blanc perquè sàpigues que vindrà el flaix
            aiming = true;
            aimStart = Time.time;
            enemy.externalMovement = true;
            HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.1f, "PATATA!", Color.white, AimTime);
            return;
        }

        float k = (Time.time - aimStart) / AimTime;
        if (Mathf.Repeat(Time.time * Mathf.Lerp(5f, 14f, k), 1f) < 0.5f) enemy.Flash(Color.white, 0.05f);
        if (k < 1f) return;

        // FLAIX
        aiming = false;
        enemy.externalMovement = false;
        nextPhoto = Time.time + PhotoEvery;
        AudioManager.Play("pickup", 0.8f, 1.6f);
        Fx.Shockwave(transform.position, 1.2f, Color.white);
        PlayerMovement pm = PlayerRef.Movement;
        bool dodged = pm != null && pm.IsDashing;
        if (!dodged && Vector2.Distance(player.position, transform.position) <= PhotoRange)
            ScreenFlash.Blind(1.3f);
    }

    void FixedUpdate()
    {
        if (aiming && rb != null && enemy != null && !enemy.IsStunned) rb.linearVelocity = Vector2.zero;
    }
}

// el flaix que t'enlluerna: la pantalla es torna blanca i es va aclarint
public class ScreenFlash : MonoBehaviour
{
    static ScreenFlash instance;
    private Image img;
    private float until, duration;

    public static void Blind(float seconds)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("Enlluernat", typeof(Canvas));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 300; // per sobre del joc i del HUD, per sota de la pausa
            GameObject ig = new GameObject("Blanc", typeof(RectTransform), typeof(Image));
            ig.transform.SetParent(go.transform, false);
            RectTransform rt = ig.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            instance = go.AddComponent<ScreenFlash>();
            instance.img = ig.GetComponent<Image>();
            instance.img.raycastTarget = false;
        }
        instance.duration = seconds;
        instance.until = Time.time + seconds;
    }

    void Update()
    {
        float left = until - Time.time;
        float a = left <= 0f ? 0f : Mathf.Clamp01(left / duration);
        img.color = new Color(1f, 1f, 0.95f, 0.92f * a * a * (Accessibility.Flashes ? 1f : 0.3f)); // accessibilitat: flaixos suaus
    }
}

// ---------- EL REPARTIDOR ----------
public class ClientRepartidor : MonoBehaviour
{
    const float ChargeEvery = 4f;
    const float AimTime = 0.6f;
    const float ChargeTime = 0.55f;
    const float ChargeSpeed = 13f;

    private Enemy enemy;
    private Rigidbody2D rb;
    private float nextCharge;
    private int state; // 0 camina, 1 apunta, 2 carrega
    private float stateAt;
    private Vector2 dir;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        enemy.OverrideStats(enemy.MaxHealth, enemy.speed * 1.15f);
        enemy.points = Mathf.RoundToInt(enemy.points * 1.5f);
        EnemyVariants.SetLook(enemy, "Repartidor");
        nextCharge = Time.time + Random.Range(1.5f, ChargeEvery);
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead) return;
        Transform player = PlayerRef.Transform;
        if (player == null) return;

        if (state == 0 && Time.time >= nextCharge && !enemy.IsStunned
            && Vector2.Distance(player.position, transform.position) < 9f)
        {
            state = 1; stateAt = Time.time;
            enemy.externalMovement = true;
            dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.1f, "COMANDA!", new Color(1f, 0.8f, 0.3f), AimTime);
        }
        else if (state == 1)
        {
            if (Mathf.Repeat(Time.time * 12f, 1f) < 0.5f) enemy.Flash(new Color(1f, 0.8f, 0.3f), 0.05f);
            if (Time.time - stateAt >= AimTime) { state = 2; stateAt = Time.time; AudioManager.Play("dash", 0.6f, 1.2f); }
        }
        else if (state == 2 && Time.time - stateAt >= ChargeTime)
        {
            state = 0;
            enemy.externalMovement = false;
            nextCharge = Time.time + ChargeEvery;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || enemy == null || enemy.IsStunned) return;
        if (state == 1) rb.linearVelocity = Vector2.zero;
        else if (state == 2)
        {
            rb.linearVelocity = dir * ChargeSpeed;
            if (Random.value < 0.3f) Fx.Dust(transform.position);
        }
    }
}

// ---------- L'INFLUENCER ----------
public class ClientInfluencer : MonoBehaviour
{
    public static int Count;
    const float CallEvery = 5.5f;
    const int MaxFollowers = 6;
    const float KeepMin = 6f, KeepMax = 9f;

    private Enemy enemy;
    private Rigidbody2D rb;
    private GameObject prefab;
    private float nextCall;
    private int called;
    private float strafe = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCount() { Count = 0; }
    void OnEnable() { Count++; }
    void OnDisable() { Count--; }

    public void Setup(GameObject sourcePrefab) { prefab = sourcePrefab; }

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 1.3f), enemy.speed * 1.05f);
        enemy.points *= 3;
        EnemyVariants.SetLook(enemy, "Influencer");
        enemy.externalMovement = true;
        nextCall = Time.time + 2.5f;
        strafe = Random.value < 0.5f ? 1f : -1f;
    }

    void FixedUpdate()
    {
        if (enemy == null || rb == null || enemy.IsStunned) return;
        Transform player = PlayerRef.Transform;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }
        Vector2 to = (Vector2)player.position - rb.position;
        float dist = to.magnitude;
        Vector2 d = to / Mathf.Max(0.01f, dist);
        Vector2 move = dist < KeepMin ? -d : dist > KeepMax ? d : new Vector2(-d.y, d.x) * strafe;
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2f;
        if (Mathf.Abs(rb.position.x - c.x) > half.x || Mathf.Abs(rb.position.y - c.y) > half.y) move += (c - rb.position).normalized * 1.5f;
        rb.linearVelocity = move.normalized * enemy.MoveSpeed;
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead || prefab == null || Time.time < nextCall || called >= MaxFollowers) return;
        nextCall = Time.time + CallEvery;
        if (Random.value < 0.3f) strafe = -strafe;

        // "SEGUIU-ME!": arriben dos seguidors
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.2f, "SEGUIU-ME!", new Color(1f, 0.5f, 0.85f), 1.2f);
        AudioManager.Play("coin", 0.7f, 1.4f);
        for (int i = 0; i < 2 && called < MaxFollowers; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 1.2f;
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            Enemy f = go.GetComponent<Enemy>();
            if (f == null) continue;
            f.OverrideStats(Mathf.Max(1, enemy.MaxHealth / 3), f.speed * 1.1f);
            f.points = 3; // els seguidors donen pocs punts
            Fx.Poof(pos);
            called++;
        }
    }
}

// ---------- EL MIM ----------
public class ClientMim : MonoBehaviour
{
    const float VisibleTime = 3f;
    const float HiddenTime = 2.2f;

    private Enemy enemy;
    private SpriteRenderer[] renderers;
    private bool hidden;
    private float switchAt;
    private float alpha = 1f;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 1.2f), enemy.speed * 1.1f);
        enemy.points = Mathf.RoundToInt(enemy.points * 1.8f);
        EnemyVariants.SetLook(enemy, "Mim");
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        switchAt = Time.time + Random.Range(1f, VisibleTime);
    }

    void LateUpdate()
    {
        if (enemy == null || enemy.IsDead) return;
        if (Time.time >= switchAt)
        {
            hidden = !hidden;
            switchAt = Time.time + (hidden ? HiddenTime : VisibleTime);
            Fx.Poof(transform.position);
        }
        alpha = Mathf.MoveTowards(alpha, hidden ? 0.12f : 1f, Time.deltaTime * 4f);
        // l'Enemy torna a posar el color cada fotograma: aquí només hi canviem la transparència
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
