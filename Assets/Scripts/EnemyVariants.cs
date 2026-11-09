using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  CLIENTS ESPECIALS
// =====================================================================
// Són variants dels clients normals (no dels tiradors ni dels caps): el WaveSpawner, en fer sortir un
// client, de tant en tant li afegeix un d'aquests comportaments. No cal cap prefab nou.
//
//   EL GOLAFRE        (onada 4)   gros i resistent; en morir es divideix en dos de petits i ràpids
//   L'OLLA A PRESSIÓ  (onada 6)   ràpid; a prop teu s'atura, xiula i explota. Si el mates abans,
//                                 explota igual però només fa mal als clients del voltant
//   EL DE LA SAFATA   (onada 8)   la safata para els trets pel davant i gira a poc a poc: ataca'l
//                                 de costat o per darrere (o trenca-la, aguanta 6 cops)
//   EL CAMBRER        (onada 11)  es queda lluny i cada 3 s cura els clients del voltant. Com a molt 2 alhora
//
// La primera vegada que en surt un a la partida, se n'anuncia el nom.
public static class EnemyVariants
{
    public enum Kind { Golafre, Olla, Safata, Cambrer }

    class Def
    {
        public Kind kind;
        public int firstWave;
        public float chance;   // probabilitat per cada client que surt (a partir de firstWave)
        public string name;
        public string hint;
        public Color color;
    }

    static readonly Def[] Defs =
    {
        new Def { kind = Kind.Golafre, firstWave = 4,  chance = 0.12f, name = "EL GOLAFRE",       hint = "Es divideix en dos en morir",        color = new Color(1f, 0.7f, 0.75f) },
        new Def { kind = Kind.Olla,    firstWave = 6,  chance = 0.10f, name = "L'OLLA A PRESSIÓ", hint = "Explota quan és a prop teu",         color = new Color(1f, 0.6f, 0.3f) },
        new Def { kind = Kind.Safata,  firstWave = 8,  chance = 0.10f, name = "EL DE LA SAFATA",  hint = "Para els trets pel davant",          color = new Color(0.8f, 0.85f, 0.95f) },
        new Def { kind = Kind.Cambrer, firstWave = 11, chance = 0.07f, name = "EL CAMBRER",       hint = "Cura els clients del voltant",       color = new Color(0.55f, 0.95f, 0.6f) },
    };

    static readonly HashSet<Kind> announced = new HashSet<Kind>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        announced.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { announced.Clear(); }

    // es crida per cada client que surt: potser el converteix en un d'especial
    public static void MaybeApply(Enemy e, GameObject prefab, int wave)
    {
        if (e == null || e.isBoss || e.isShooter) return;

        float roll = Random.value;
        foreach (Def d in Defs)
        {
            if (wave < d.firstWave) continue;
            if (roll < d.chance)
            {
                if (d.kind == Kind.Cambrer && Cambrer.Count >= 2) return;
                Apply(e, prefab, d);
                return;
            }
            roll -= d.chance;
        }
    }

    // mode de prova: converteix aquest client en el tipus especial que es demana
    public static void Force(Enemy e, GameObject prefab, Kind kind)
    {
        if (e == null) return;
        foreach (Def d in Defs)
            if (d.kind == kind) { Apply(e, prefab, d); return; }
    }

    static void Apply(Enemy e, GameObject prefab, Def d)
    {
        switch (d.kind)
        {
            case Kind.Golafre: e.gameObject.AddComponent<Golafre>().Setup(prefab); break;
            case Kind.Olla: e.gameObject.AddComponent<OllaPressio>(); break;
            case Kind.Safata: e.gameObject.AddComponent<SafataEscut>(); break;
            case Kind.Cambrer: e.gameObject.AddComponent<Cambrer>(); break;
        }

        if (announced.Add(d.kind))
        {
            Vector2 at = (Vector2)e.transform.position + Vector2.up * 1.4f;
            HazardFx.Popup(at, "NOU CLIENT: " + d.name, d.color, 2.2f);
            HazardFx.Popup(at + Vector2.down * 0.55f, d.hint, Color.white, 2.2f);
        }
    }

    // un accessori (pitet, safata, olla...) com a fill del client, per sobre del seu dibuix
    public static SpriteRenderer AddAccessory(Transform parent, string name, Sprite sprite, Vector2 localPos, int order = 1)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // una onada de color que s'eixampla i s'esvaeix (explosions i cures)
    public static void Burst(Vector2 pos, float radius, Color color, float duration = 0.25f)
    {
        SpriteRenderer sr = HazardFx.MakeCircle(pos, radius, color, 5);
        sr.gameObject.AddComponent<BurstFx>().Init(duration, color);
    }
}

// el cercle de Burst: creix una mica i s'esvaeix
public class BurstFx : MonoBehaviour
{
    private SpriteRenderer sr;
    private float born, duration;
    private Color color;
    private Vector3 baseScale;

    public void Init(float seconds, Color c)
    {
        sr = GetComponent<SpriteRenderer>();
        born = Time.time;
        duration = Mathf.Max(0.05f, seconds);
        color = c;
        baseScale = transform.localScale;
    }

    void Update()
    {
        float k = (Time.time - born) / duration;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.localScale = baseScale * Mathf.Lerp(0.6f, 1.1f, k);
        Color c = color;
        c.a = color.a * (1f - k);
        sr.color = c;
    }
}

// ---------- EL GOLAFRE: gros, i en morir es divideix en dos de petits ----------
public class Golafre : MonoBehaviour
{
    private Enemy enemy;
    private GameObject prefab;

    public void Setup(GameObject sourcePrefab)
    {
        prefab = sourcePrefab;
        enemy = GetComponent<Enemy>();
        transform.localScale *= 1.3f;
        enemy.OverrideStats(Mathf.Max(2, enemy.MaxHealth * 2), enemy.speed * 0.85f);
        enemy.points *= 2;
        enemy.SetTint(new Color(1f, 0.82f, 0.85f));
        EnemyVariants.AddAccessory(transform, "Pitet", PropArt.Bib(), new Vector2(0f, -0.12f));
    }

    void OnEnable() { Enemy.OnKilled += HandleKilled; }
    void OnDisable() { Enemy.OnKilled -= HandleKilled; }

    void HandleKilled(Enemy e)
    {
        if (e != enemy || prefab == null) return;

        Vector2 pos = transform.position;
        EnemyVariants.Burst(pos, 0.9f, new Color(1f, 0.85f, 0.85f, 0.6f));
        for (int i = 0; i < 2; i++)
        {
            Vector2 dir = i == 0 ? Vector2.left : Vector2.right;
            GameObject go = Instantiate(prefab, pos + dir * 0.45f, Quaternion.identity);
            Enemy small = go.GetComponent<Enemy>();
            if (small == null) continue;
            go.transform.localScale = prefab.transform.localScale * 0.7f;
            small.OverrideStats(Mathf.Max(1, enemy.MaxHealth / 4), enemy.speed / 0.85f * 1.35f);
            small.points = 5;
            small.SetTint(new Color(1f, 0.82f, 0.85f));
            small.Knockback(dir, 5f, 0.15f);
        }
    }
}

// ---------- L'OLLA A PRESSIÓ: a prop teu s'atura, xiula i explota ----------
public class OllaPressio : MonoBehaviour
{
    const float ArmDistance = 1.7f;  // a aquesta distància del jugador s'activa
    const float FuseTime = 0.8f;     // el temps que tens per apartar-te
    const float Radius = 2f;         // l'abast de l'explosió
    const int EnemyDamage = 3;       // el mal que fa als clients del voltant

    private Enemy enemy;
    private Rigidbody2D rb;
    private Transform valve;
    private bool armed, exploded;
    private float armedAt;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        enemy.speed += 0.6f;
        enemy.points = Mathf.RoundToInt(enemy.points * 1.5f);
        enemy.SetTint(new Color(1f, 0.8f, 0.6f));
        valve = EnemyVariants.AddAccessory(transform, "Olla", PropArt.Cooker(), new Vector2(0f, -0.15f)).transform;
    }

    void OnEnable() { Enemy.OnKilled += HandleKilled; }
    void OnDisable() { Enemy.OnKilled -= HandleKilled; }

    // si el mates abans que exploti, l'olla esclata igual, però només fa mal als clients
    void HandleKilled(Enemy e)
    {
        if (e == enemy) Explode(false);
    }

    void Update()
    {
        if (exploded || enemy == null || enemy.IsDead) return;
        Transform player = PlayerRef.Transform;
        if (player == null || PlayerHealth.IsDying) return;

        if (!armed)
        {
            if (enemy.IsStunned) return;
            if (Vector2.Distance(player.position, transform.position) > ArmDistance) return;
            armed = true;
            armedAt = Time.time;
            enemy.externalMovement = true; // ja no camina: s'atura i xiula
        }

        // parpelleja en vermell i l'olla tremola, cada cop més de pressa
        float k = (Time.time - armedAt) / FuseTime;
        if (Mathf.Repeat(Time.time * Mathf.Lerp(6f, 16f, k), 1f) < 0.5f) enemy.Flash(new Color(1f, 0.35f, 0.25f), 0.05f);
        if (valve != null) valve.localScale = Vector3.one * (1f + 0.25f * k + 0.08f * Mathf.Sin(Time.time * 60f));

        if (k >= 1f)
        {
            Explode(true);
            enemy.RemoveSilently(); // explotar sol no dona punts
        }
    }

    void FixedUpdate()
    {
        if (armed && rb != null && enemy != null && !enemy.IsStunned) rb.linearVelocity = Vector2.zero;
    }

    void Explode(bool hurtPlayer)
    {
        if (exploded) return;
        exploded = true;

        Vector2 pos = transform.position;
        EnemyVariants.Burst(pos, Radius, new Color(1f, 0.55f, 0.15f, 0.55f), 0.3f);
        CameraFX.Shake(0.2f, 0.2f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, Radius))
        {
            Enemy other = c.GetComponentInParent<Enemy>();
            if (other != null && other != enemy && !other.IsDead)
            {
                other.TakeDamage(EnemyDamage);
                other.Knockback((Vector2)other.transform.position - pos, 7f, 0.15f);
                continue;
            }
            if (!hurtPlayer) continue;
            PlayerHealth ph = c.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(); // el dash ho esquiva, com qualsevol altre cop
        }
    }
}

// ---------- EL DE LA SAFATA: para els trets pel davant ----------
public class SafataEscut : MonoBehaviour
{
    const float TurnSpeed = 110f;   // graus per segon: si l'envoltes amb el dash, li veus l'esquena
    const int Durability = 6;       // trets que aguanta la safata abans de trencar-se

    private Enemy enemy;
    private Transform tray;
    private SpriteRenderer traySr;
    private Vector2 facing = Vector2.down;
    private int hitsLeft = Durability;
    private float flashUntil;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 1.5f), enemy.speed * 0.9f);
        enemy.points = Mathf.RoundToInt(enemy.points * 1.5f);
        traySr = EnemyVariants.AddAccessory(transform, "Safata", PropArt.BigTray(), Vector2.zero, 2);
        tray = traySr.transform;

        Transform player = PlayerRef.Transform;
        if (player != null) facing = ((Vector2)(player.position - transform.position)).normalized;
        PlaceTray();
    }

    void Update()
    {
        if (tray == null) return;
        Transform player = PlayerRef.Transform;
        if (player != null)
        {
            Vector2 want = (Vector2)(player.position - transform.position);
            if (want.sqrMagnitude > 0.01f)
            {
                float from = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
                float to = Mathf.Atan2(want.y, want.x) * Mathf.Rad2Deg;
                float a = Mathf.MoveTowardsAngle(from, to, TurnSpeed * Time.deltaTime) * Mathf.Deg2Rad;
                facing = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
        }
        PlaceTray();
        traySr.color = Time.time < flashUntil ? new Color(1f, 1f, 0.7f) : Color.white;
    }

    void PlaceTray()
    {
        if (tray == null) return;
        // la safata va davant del client, de cara al jugador (fixa al món, encara que el client sigui gran)
        float s = Mathf.Max(0.01f, transform.lossyScale.x);
        tray.localPosition = facing * (0.55f / s);
        tray.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
    }

    // un tret que ve per davant topa amb la safata (i la va trencant). Torna true si l'ha parat
    public bool TryBlock(Vector2 shotVelocity, Vector2 shotPos)
    {
        if (tray == null || shotVelocity.sqrMagnitude < 0.01f) return false;
        Vector2 incoming = -shotVelocity.normalized; // d'on ve el tret
        if (Vector2.Dot(incoming, facing) < 0.35f) return false;

        hitsLeft--;
        flashUntil = Time.time + 0.08f;
        EnemyVariants.Burst(shotPos, 0.25f, new Color(1f, 1f, 0.8f, 0.8f), 0.12f);

        if (hitsLeft <= 0)
        {
            HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.1f, "SAFATA TRENCADA!", new Color(0.85f, 0.9f, 1f));
            EnemyVariants.Burst(tray.position, 0.6f, new Color(0.85f, 0.88f, 0.95f, 0.7f));
            Destroy(tray.gameObject);
            tray = null;
        }
        return true;
    }
}

// ---------- EL CAMBRER: es queda lluny i cura els clients del voltant ----------
public class Cambrer : MonoBehaviour
{
    public static int Count; // quants n'hi ha ara mateix (com a molt 2 alhora)

    const float HealEvery = 3f;
    const float HealRadius = 4.5f;
    const float KeepMin = 5f, KeepMax = 7.5f; // la distància que vol mantenir amb el jugador

    private Enemy enemy;
    private Rigidbody2D rb;
    private float nextHeal;
    private float strafeSide = 1f;
    private float nextSideChange;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCount() { Count = 0; }

    void OnEnable() { Count++; }
    void OnDisable() { Count--; }

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        enemy.OverrideStats(Mathf.RoundToInt(enemy.MaxHealth * 1.2f), enemy.speed * 1.1f);
        enemy.points *= 2;
        enemy.SetTint(new Color(0.8f, 0.92f, 1f));
        enemy.externalMovement = true; // es mou pel seu compte
        EnemyVariants.AddAccessory(transform, "Safata", PropArt.WaiterTray(), new Vector2(0.15f, 0.62f));
        nextHeal = Time.time + 1.5f;
        strafeSide = Random.value < 0.5f ? 1f : -1f;
    }

    void FixedUpdate()
    {
        if (enemy == null || rb == null || enemy.IsStunned) return; // si l'empenyen, mana l'Enemy
        Transform player = PlayerRef.Transform;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;
        Vector2 dir = toPlayer / Mathf.Max(0.01f, dist);

        if (Time.time >= nextSideChange)
        {
            nextSideChange = Time.time + Random.Range(1.5f, 3f);
            if (Random.value < 0.4f) strafeSide = -strafeSide;
        }

        Vector2 move;
        if (dist < KeepMin) move = -dir;                                  // massa a prop: s'allunya
        else if (dist > KeepMax) move = dir;                              // massa lluny: s'hi acosta
        else move = new Vector2(-dir.y, dir.x) * strafeSide;              // a la distància bona: va de costat

        // que no s'arrambi a les parets
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2f;
        Vector2 p = rb.position;
        if (Mathf.Abs(p.x - c.x) > half.x || Mathf.Abs(p.y - c.y) > half.y)
            move += (c - p).normalized * 1.5f;

        rb.linearVelocity = move.normalized * enemy.MoveSpeed;
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead || Time.time < nextHeal) return;
        nextHeal = Time.time + HealEvery;

        bool healedAny = false;
        foreach (Enemy other in Enemy.All)
        {
            if (other == null || other == enemy || other.IsDead || other.isBoss) continue;
            if (other.CurrentHealth >= other.MaxHealth) continue;
            if (Vector2.Distance(other.transform.position, transform.position) > HealRadius) continue;
            other.Heal(Mathf.Max(1, other.MaxHealth / 4));
            other.Flash(new Color(0.5f, 1f, 0.55f), 0.2f);
            healedAny = true;
        }

        // es veu l'abast de la cura (encara que no hagi curat ningú, perquè sàpigues què fa)
        EnemyVariants.Burst(transform.position, HealRadius, new Color(0.45f, 1f, 0.5f, healedAny ? 0.28f : 0.12f), 0.45f);
    }
}
