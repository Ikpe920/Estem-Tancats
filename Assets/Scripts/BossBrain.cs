using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  ELS CAPS, UN PER UN (cada un amb els seus atacs, la seva 2a fase i el seu punt feble)
// =====================================================================
// - BossBrain: el que tenen en comú (moure's, disparar els seus projectils, cercles d'avís,
//   tolls a terra, llançar coses en paràbola, la 2a fase a mitja vida...).
// - Fan servir els mateixos efectes que els plats del jugador: Fx.Explosion, Fx.FrostBurst,
//   Fx.Flames, Fx.Shockwave, el terra glaçat, el toll d'oli...
// - Cada cap té un PUNT FEBLE: un tipus de mal que li fa el doble (el diu el cartell).

// ---------- EL TIPUS DE MAL (per als punts febles) ----------
public enum DmgKind { None, Fire, Ice, Explosion, Crit }

public static class DamageKind
{
    // el tipus del cop que s'està fent ara mateix (Enemy.TakeDamage el mira)
    public static DmgKind Current = DmgKind.None;
    static float nextPopup;

    public static void Hit(Enemy e, int damage, DmgKind kind, bool crit = false)
    {
        if (e == null) return;
        DmgKind prev = Current;
        Current = kind;
        try { e.TakeDamage(damage, crit); }
        finally { Current = prev; }
        if (kind == DmgKind.Fire && e != null && !e.IsDead) EnemyBurn.Apply(e, 1.6f); // el client crema una estona
    }

    public static void ShowWeak(Enemy e)
    {
        if (e == null || Time.time < nextPopup) return;
        nextPopup = Time.time + 0.7f;
        HazardFx.Popup((Vector2)e.transform.position + Vector2.up * 2.6f, "PUNT FEBLE!", new Color(1f, 0.55f, 0.15f), 0.7f);
    }

    public static string Name(DmgKind k)
    {
        switch (k)
        {
            case DmgKind.Fire: return "EL FOC";
            case DmgKind.Ice: return "EL GEL";
            case DmgKind.Explosion: return "LES EXPLOSIONS";
            case DmgKind.Crit: return "ELS CRÍTICS";
        }
        return "";
    }

    // quins plats ho fan (per al cartell i el receptari)
    public static string Source(DmgKind k)
    {
        switch (k)
        {
            case DmgKind.Fire: return "lanxa-flames, sopa, foc";
            case DmgKind.Ice: return "trets gelats";
            case DmgKind.Explosion: return "calçots, bombes, cava";
            case DmgKind.Crit: return "cops crítics";
        }
        return "";
    }

    public static DmgKind WeaknessOf(BossType t)
    {
        switch (t)
        {
            case BossType.Critic: return DmgKind.Crit;
            case BossType.Borratxo: return DmgKind.Ice;
            case BossType.Cuiner: return DmgKind.Ice;
            case BossType.Pallas: return DmgKind.Fire;
            case BossType.Inspector: return DmgKind.Explosion;
            case BossType.Sommelier: return DmgKind.Crit;
            case BossType.Gelater: return DmgKind.Fire;
            case BossType.TvCritic: return DmgKind.Explosion;
            case BossType.Ghost: return DmgKind.Fire;
        }
        return DmgKind.None;
    }
}

// ---------- UN CLIENT QUE CREMA (només es veu: flametes i fum a sobre) ----------
public class EnemyBurn : MonoBehaviour
{
    float until, next;

    public static void Apply(Enemy e, float seconds)
    {
        EnemyBurn b = e.GetComponent<EnemyBurn>();
        if (b == null) b = e.gameObject.AddComponent<EnemyBurn>();
        b.until = Mathf.Max(b.until, Time.time + seconds);
    }

    void Update()
    {
        if (Time.time >= until) { Destroy(this); return; }
        if (Time.time < next) return;
        next = Time.time + 0.15f;
        float size = Mathf.Clamp(transform.localScale.x * 0.25f, 0.2f, 0.6f);
        Fx.Flames((Vector2)transform.position + Vector2.up * 0.2f * transform.localScale.y, size, 0.4f);
    }
}

// ---------- EL PROJECTIL D'UN CAP: gira, rebota, deixa rastre, esclata... ----------
public class BossShotFx : MonoBehaviour
{
    public float spin;            // graus per segon
    public int bounces;           // rebots a les parets (0: els trets dels caps no reboten, es trenquen a la paret)
    public float fuse;            // >0: esclata al cap d'aquests segons
    public int burstShots;        // quan esclata: quants trets fa sortir (0 = cap)
    public float burstRadius;     // quan esclata: radi de l'explosió que et fa mal (0 = cap)
    public Sprite burstSprite;
    public float slowOnHit;       // et frena si et toca
    public string trail;          // "foc", "neu", "brillo", "bombolles"
    public Color trailColor = Color.white;
    public float accel;           // acceleració (multiplicador per segon)
    public float curve;           // gira la trajectòria (graus per segon)

    private Rigidbody2D rb;
    private float born, nextTrail;
    private GameObject prefab;

    public void Init(GameObject shotPrefab) { prefab = shotPrefab; }

    void Awake() { rb = GetComponent<Rigidbody2D>(); born = Time.time; }

    void Update()
    {
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (rb != null)
        {
            Vector2 v = rb.linearVelocity;
            if (accel != 0f) v *= 1f + accel * Time.deltaTime;
            if (curve != 0f) v = Quaternion.Euler(0f, 0f, curve * Time.deltaTime) * v;
            rb.linearVelocity = v;

            if (bounces > 0)
            {
                Vector2 p = (Vector2)transform.position - HazardManager.ArenaCenter;
                Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 0.9f;
                bool bounced = false;
                if (Mathf.Abs(p.x) > half.x && Mathf.Sign(v.x) == Mathf.Sign(p.x)) { v.x = -v.x; bounced = true; }
                if (Mathf.Abs(p.y) > half.y && Mathf.Sign(v.y) == Mathf.Sign(p.y)) { v.y = -v.y; bounced = true; }
                if (bounced)
                {
                    bounces--;
                    rb.linearVelocity = v;
                    Fx.Hit(transform.position, -v, false);
                    AudioManager.Play("bounce", 0.5f);
                }
            }
        }

        if (!string.IsNullOrEmpty(trail) && Time.time >= nextTrail)
        {
            nextTrail = Time.time + 0.07f;
            switch (trail)
            {
                case "foc": Fx.Flames(transform.position, 0.15f, 0.25f); break;
                case "neu": if (Random.value < 0.5f) Fx.Snowflake(transform.position); break;
                case "brillo": Fx.Sparkle(transform.position, trailColor); break;
                case "bombolles": Fx.Sparkles(transform.position, trailColor, 1, 0.1f); break;
            }
        }

        if (fuse > 0f && Time.time - born >= fuse) Burst();
    }

    void Burst()
    {
        fuse = 0f;
        Vector2 pos = transform.position;
        if (burstRadius > 0f)
        {
            Fx.Explosion(pos, burstRadius, 0.7f);
            AudioManager.Play("explosion", 0.4f, 1.2f);
            BossBrain.HurtPlayerIn(pos, burstRadius);
        }
        if (burstShots > 0 && prefab != null)
        {
            float off = Random.Range(0f, 360f);
            for (int i = 0; i < burstShots; i++)
            {
                float a = (off + i * 360f / burstShots) * Mathf.Deg2Rad;
                BossBrain.SpawnShot(prefab, burstSprite, pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 5.5f, 0.55f);
            }
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (slowOnHit > 0f && other.GetComponent<PlayerHealth>() != null)
            PlayerSlow.Apply(slowOnHit, 1.4f, "GLAÇAT!");
    }
}

// ---------- UN TOLL A TERRA (vi, cervesa, gel, foc, la zona tancada...) ----------
public class BossPuddle : MonoBehaviour
{
    float radius, life, born, slow, nextTick;
    bool hurts, rect;
    Vector2 size;
    string text;
    SpriteRenderer sr;
    Color color;
    string fx;

    public static BossPuddle Spawn(Vector2 pos, float radius, float life, Sprite sprite, Color color,
                                   float slow, string text, bool hurts = false, string fx = null)
    {
        GameObject go = new GameObject("TollCap");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        BossPuddle p = go.AddComponent<BossPuddle>();
        p.radius = radius; p.life = life; p.born = Time.time; p.slow = slow; p.text = text;
        p.hurts = hurts; p.color = color; p.fx = fx;
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = sprite != null ? sprite : HazardFx.Circle();
        p.sr.color = color;
        p.sr.sortingOrder = -3;
        return p;
    }

    // una zona rectangular (la Inspectora tanca un tros de la sala)
    public static BossPuddle SpawnRect(Vector2 pos, Vector2 size, float life, Color color, string text)
    {
        BossPuddle p = Spawn(pos, 0.5f, life, HazardFx.Square(), color, 0f, text, true, null);
        p.rect = true;
        p.size = size;
        p.transform.rotation = Quaternion.identity;
        p.transform.localScale = new Vector3(size.x, size.y, 1f);
        return p;
    }

    bool Inside(Vector2 q)
    {
        Vector2 d = q - (Vector2)transform.position;
        if (rect) return Mathf.Abs(d.x) <= size.x * 0.5f && Mathf.Abs(d.y) <= size.y * 0.5f;
        return d.magnitude <= radius;
    }

    void Update()
    {
        float k = (Time.time - born) / Mathf.Max(0.1f, life);
        if (k >= 1f) { Destroy(gameObject); return; }
        if (sr != null)
        {
            Color c = color;
            c.a = color.a * (k < 0.8f ? 1f : (1f - k) / 0.2f);
            sr.color = c;
        }
        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.15f;

        if (fx == "foc" && Random.value < 0.6f)
            Fx.Flames((Vector2)transform.position + Random.insideUnitCircle * radius * 0.6f, radius * 0.4f, 0.4f);
        else if (fx == "neu" && Random.value < 0.3f)
            Fx.Snowflake((Vector2)transform.position + Random.insideUnitCircle * radius);

        Transform player = PlayerRef.Transform;
        if (player == null || !Inside(player.position)) return;
        if (slow > 0f) PlayerSlow.Apply(slow, 0.8f, text);
        if (hurts && PlayerRef.Health != null) PlayerRef.Health.TakeDamage();
    }
}

// ---------- EL QUE TENEN EN COMÚ TOTS ELS CAPS ----------
public abstract class BossBrain : MonoBehaviour
{
    public GameObject shotPrefab;
    public WaveSpawner spawner;

    protected Enemy enemy;
    protected Rigidbody2D rb;
    protected bool phase2;
    protected bool phase3;          // quan li queda un 20% de vida: l'últim recurs
    private float nextTaunt, nextDesperate, fightStart;

    // moviment: per defecte camina cap a tu; amb keepMax > 0 es manté a distància i va de costat
    protected float keepMin, keepMax;
    protected float moveMul = 1f;
    protected bool hold;               // quiet (apuntant, carregant un atac...)
    protected Vector2? forcedVelocity; // una càrrega, un salt...
    private float strafe = 1f;

    protected abstract IEnumerator Brain();
    protected abstract string Phase2Line { get; }
    protected virtual void OnPhase2() { }
    protected virtual string Phase3Line => "ÚLTIM PLAT!";
    protected virtual Sprite DesperateSprite => null;      // el tret del seu atac desesperat
    protected virtual string[] Taunts => new string[0];    // el que diu mentre lluita
    protected virtual float VoicePitch => 1f;              // la seva veu de dibuix animat (bla-bla)
    protected virtual Color Tone => new Color(1f, 0.85f, 0.4f);

    protected virtual void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        if (enemy != null) enemy.externalMovement = true;
        fightStart = Time.time;
        nextTaunt = Time.time + 2.5f;
        Enemy.OnKilled += OnAnyKilled;
        StartCoroutine(Run());
    }

    void OnDestroy() { Enemy.OnKilled -= OnAnyKilled; }

    // el rècord de temps contra aquest cap (es veu al Bestiari)
    void OnAnyKilled(Enemy e)
    {
        if (e != enemy || e == null) return;
        float time = Time.time - fightStart;
        string key = "boss_time_" + Codex.ClientKey(e);
        float best = PlayerPrefs.GetFloat(key, 0f);
        bool record = best <= 0f || time < best;
        if (record) { PlayerPrefs.SetFloat(key, time); PlayerPrefs.Save(); }
        HazardFx.Popup(Pos + Vector2.up * 3f, (record ? "NOU RÈCORD! " : "TEMPS: ") + BossRush.Clock(time),
                       record ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.95f, 0.85f), 2f);
        AudioManager.SetBossPhase2(false);
    }

    public static float BestTime(string clientKey) { return PlayerPrefs.GetFloat("boss_time_" + clientKey, 0f); }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(1.2f); // un respir abans de començar
        while (true)
        {
            if (!Alive) { yield return null; continue; }
            yield return StartCoroutine(Brain());
            // 3a fase: de tant en tant, l'atac desesperat
            if (phase3 && Alive && Time.time >= nextDesperate)
            {
                nextDesperate = Time.time + 9f;
                yield return StartCoroutine(Desperate());
            }
            yield return null;
        }
    }

    protected bool Alive => enemy != null && !enemy.IsDead && !PlayerHealth.IsDying && PlayerRef.Transform != null;
    protected Vector2 Pos => transform.position;
    protected Vector2 PlayerPos { get { Transform p = PlayerRef.Transform; return p != null ? (Vector2)p.position : Pos; } }
    protected Vector2 ToPlayer { get { Vector2 d = PlayerPos - Pos; return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right; } }

    void Update()
    {
        if (enemy == null || enemy.IsDead) return;
        if (Alive && Time.time >= nextTaunt && Taunts.Length > 0)
        {
            nextTaunt = Time.time + Random.Range(7f, 12f);
            Talk(Taunts[Random.Range(0, Taunts.Length)]);
        }
        if (phase2 && !phase3 && enemy.CurrentHealth <= enemy.MaxHealth / 5)
        {
            phase3 = true;
            nextDesperate = Time.time + 1f;
            Say(Phase3Line, new Color(1f, 0.25f, 0.2f), 2f);
            Squash.Kick(gameObject, 0.3f, -0.3f);
            Fx.Shockwave(Pos, 3.5f, new Color(1f, 0.25f, 0.2f));
            Fx.Explosion(Pos, 1.2f, 0.5f);
            CameraFX.Shake(0.5f, 0.3f);
            enemy.Flash(new Color(1f, 0.2f, 0.2f), 0.6f);
            AudioManager.Play("phase", 1f, 0.85f);
            moveMul = 1.35f;
            return;
        }
        if (phase2) return;
        if (enemy.CurrentHealth > enemy.MaxHealth / 2) return;
        phase2 = true;
        AudioManager.Play("phase");
        AudioManager.SetBossPhase2(true); // la música s'accelera
        Say(Phase2Line, new Color(1f, 0.4f, 0.35f), 1.8f);
        Squash.Kick(gameObject, -0.25f, 0.3f);
        Fx.Shockwave(Pos, 2.5f, new Color(1f, 0.4f, 0.3f));
        CameraFX.Shake(0.4f, 0.25f);
        enemy.Flash(new Color(1f, 0.3f, 0.3f), 0.4f);
        AudioManager.Play("crit", 0.9f, 0.6f);
        moveMul = 1.2f;
        OnPhase2();
    }

    void FixedUpdate()
    {
        if (enemy == null || rb == null || enemy.IsDead) return;
        if (forcedVelocity.HasValue) { rb.linearVelocity = forcedVelocity.Value; return; }
        if (enemy.IsStunned) return;
        Transform player = PlayerRef.Transform;
        if (player == null || hold || PlayerHealth.IsDying) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 to = (Vector2)player.position - rb.position;
        float dist = to.magnitude;
        Vector2 d = to / Mathf.Max(0.01f, dist);
        Vector2 move;
        if (keepMax <= 0f) move = d;
        else move = dist < keepMin ? -d : dist > keepMax ? d : new Vector2(-d.y, d.x) * strafe;

        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2.5f;
        if (Mathf.Abs(rb.position.x - c.x) > half.x || Mathf.Abs(rb.position.y - c.y) > half.y)
        {
            move += (c - rb.position).normalized * 1.5f;
            if (keepMax > 0f && Random.value < 0.02f) strafe = -strafe;
        }
        rb.linearVelocity = move.normalized * enemy.MoveSpeed * moveMul;
    }

    protected void FlipStrafe() { strafe = -strafe; }

    // ---------- EINES ----------

    protected IEnumerator Wait(float seconds)
    {
        float end = Time.time + seconds * (phase3 ? 0.6f : phase2 ? 0.75f : 1f);
        while (Time.time < end) yield return null;
    }

    protected void Say(string text, Color color, float life = 1.1f)
    {
        HazardFx.Popup(Pos + Vector2.up * 2.4f, text, color, life);
    }

    // parla: el text a sobre i la seva veu de dibuix animat (síl·labes "bla-bla")
    protected void Talk(string text)
    {
        HazardFx.Popup(Pos + Vector2.up * 2.9f, "«" + text + "»", new Color(1f, 0.97f, 0.9f), 2.2f);
        StartCoroutine(Babble(Mathf.Clamp(text.Length / 3, 3, 10)));
    }

    IEnumerator Babble(int syllables)
    {
        for (int i = 0; i < syllables; i++)
        {
            AudioManager.Play("voice_" + Random.Range(0, 4), 0.9f, VoicePitch * Random.Range(0.9f, 1.12f));
            yield return new WaitForSeconds(Random.Range(0.08f, 0.13f));
        }
    }

    // l'atac desesperat de la 3a fase: tres corones del seu tret que giren, i una onada
    protected virtual IEnumerator Desperate()
    {
        hold = true;
        Say(Phase3Line, new Color(1f, 0.3f, 0.25f), 1f);
        Squash.Kick(gameObject, 0.25f, -0.25f);
        for (float t = 0f; t < 0.5f; t += 0.1f)
        {
            Fx.Sparkle(Pos + Random.insideUnitCircle * 1.5f, new Color(1f, 0.4f, 0.3f));
            yield return new WaitForSeconds(0.1f);
        }
        float off = Random.Range(0f, 360f);
        for (int k = 0; k < 3 && Alive; k++)
        {
            foreach (BossShotFx f in Ring(DesperateSprite, 14, 5.5f, off + k * 13f, 0.7f, DesperateSprite == null ? new Color(1f, 0.4f, 0.3f) : (Color?)null))
            {
                f.spin = 400f;
                f.curve = k % 2 == 0 ? 30f : -30f;
            }
            Fx.Shockwave(Pos, 2f, new Color(1f, 0.4f, 0.3f));
            AudioManager.Play("explosion", 0.4f, 1.3f);
            yield return new WaitForSeconds(0.35f);
        }
        hold = false;
        yield return Wait(1f);
    }

    // fa mal al jugador si és dins del cercle (el dash ho esquiva); torna si l'ha tocat
    public static bool HurtPlayerIn(Vector2 p, float radius)
    {
        Transform player = PlayerRef.Transform;
        if (player == null || Vector2.Distance(player.position, p) > radius) return false;
        PlayerMovement pm = PlayerRef.Movement;
        if (pm != null && pm.IsDashing) return false;
        if (PlayerRef.Health != null) PlayerRef.Health.TakeDamage();
        return true;
    }

    public static bool PlayerInCone(Vector2 from, Vector2 dir, float range, float halfAngle)
    {
        Transform player = PlayerRef.Transform;
        if (player == null) return false;
        Vector2 to = (Vector2)player.position - from;
        return to.magnitude <= range && Vector2.Angle(dir, to) <= halfAngle;
    }

    public static Vector2 ClampArena(Vector2 p, float margin)
    {
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * margin;
        p.x = Mathf.Clamp(p.x, c.x - half.x, c.x + half.x);
        p.y = Mathf.Clamp(p.y, c.y - half.y, c.y + half.y);
        return p;
    }

    // un tret amb el dibuix propi del cap
    public static BossShotFx SpawnShot(GameObject prefab, Sprite sprite, Vector2 from, Vector2 dir, float speed, float scale = 0.7f, Color? tint = null)
    {
        if (prefab == null) return null;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        GameObject s = Object.Instantiate(prefab, from + dir * 0.9f, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
        s.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = s.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (sprite != null) sr.sprite = sprite;
            if (tint.HasValue) sr.color = tint.Value;
        }
        EnemyProjectile ep = s.GetComponent<EnemyProjectile>();
        if (ep != null) ep.lifetime = 6f;
        Rigidbody2D srb = s.GetComponent<Rigidbody2D>();
        if (srb != null) srb.linearVelocity = dir * speed;
        BossShotFx fx = s.AddComponent<BossShotFx>();
        fx.Init(prefab);
        return fx;
    }

    protected BossShotFx Shoot(Sprite sprite, Vector2 dir, float speed, float scale = 0.7f, Color? tint = null)
    {
        return SpawnShot(shotPrefab, sprite, Pos, dir, speed * (phase2 ? 1.1f : 1f), scale, tint);
    }

    protected static Vector2 Dir(float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    protected static float Angle(Vector2 v) { return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; }

    protected List<BossShotFx> Fan(Sprite sprite, Vector2 aim, int n, float spread, float speed, float scale = 0.7f, Color? tint = null)
    {
        var list = new List<BossShotFx>();
        float baseA = Angle(aim);
        for (int i = 0; i < n; i++)
        {
            float a = n > 1 ? baseA + (i - (n - 1) / 2f) * spread : baseA;
            BossShotFx f = Shoot(sprite, Dir(a), speed, scale, tint);
            if (f != null) list.Add(f);
        }
        return list;
    }

    protected List<BossShotFx> Ring(Sprite sprite, int n, float speed, float offset = -1f, float scale = 0.7f, Color? tint = null)
    {
        var list = new List<BossShotFx>();
        if (offset < 0f) offset = Random.Range(0f, 360f);
        for (int i = 0; i < n; i++)
        {
            BossShotFx f = Shoot(sprite, Dir(offset + i * 360f / n), speed, scale, tint);
            if (f != null) list.Add(f);
        }
        return list;
    }

    // cercles d'avís a terra; quan s'acaba el temps, crida 'onBlast' a cada punt
    protected IEnumerator Telegraph(List<Vector2> spots, float radius, Color color, float time, System.Action<Vector2> onBlast)
    {
        var rings = new List<SpriteRenderer>();
        foreach (Vector2 p in spots) rings.Add(HazardFx.MakeCircle(p, radius, new Color(color.r, color.g, color.b, 0.1f), -2));
        foreach (SpriteRenderer r in rings) Destroy(r.gameObject, time + 0.3f); // si el cap mor a mig atac
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            foreach (SpriteRenderer r in rings)
                if (r != null)
                {
                    r.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.1f, 0.45f, k) * (0.8f + 0.2f * Mathf.Sin(t * 28f)));
                    r.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.6f, 1f, Mathf.Min(1f, k * 2f));
                }
            yield return null;
            if (enemy == null || enemy.IsDead) break;
        }
        foreach (SpriteRenderer r in rings) if (r != null) Destroy(r.gameObject);
        if (enemy == null || enemy.IsDead) yield break;
        foreach (Vector2 p in spots) onBlast(p);
    }

    // punts al voltant del jugador (un sempre a sota)
    protected List<Vector2> SpotsAroundPlayer(int n, float spread)
    {
        var spots = new List<Vector2> { PlayerPos };
        for (int i = 1; i < n; i++) spots.Add(ClampArena(PlayerPos + Random.insideUnitCircle * spread, 1.5f));
        return spots;
    }

    // llança una cosa en paràbola (una ampolla, una paella...): ombra a terra i, en caure, 'onLand'
    protected IEnumerator Lob(Sprite sprite, Vector2 to, float time, float radius, Color warn, System.Action<Vector2> onLand)
    {
        Vector2 from = Pos + Vector2.up * 0.8f;
        GameObject go = new GameObject("Llançament");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 20;
        go.transform.localScale = Vector3.one * 0.9f;
        SpriteRenderer shadow = HazardFx.MakeCircle(to, radius, new Color(warn.r, warn.g, warn.b, 0.1f), -2);
        Destroy(go, time + 0.2f); Destroy(shadow.gameObject, time + 0.2f); // si el cap mor a mig atac
        float spin = Random.Range(-720f, 720f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            Vector2 p = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 3f;
            go.transform.position = p;
            go.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            if (shadow != null) shadow.color = new Color(warn.r, warn.g, warn.b, Mathf.Lerp(0.1f, 0.45f, k));
            yield return null;
        }
        Destroy(go);
        if (shadow != null) Destroy(shadow.gameObject);
        onLand(to);
    }

    protected void LobMany(Sprite sprite, List<Vector2> spots, float time, float radius, Color warn, System.Action<Vector2> onLand)
    {
        foreach (Vector2 p in spots) StartCoroutine(Lob(sprite, p, time + Random.Range(-0.1f, 0.15f), radius, warn, onLand));
    }

    // salta fins a un punt (i en caure fa 'onLand')
    protected IEnumerator Leap(Vector2 to, float time, System.Action<Vector2> onLand)
    {
        Vector2 from = Pos;
        for (float t = 0f; t < time; t += Time.fixedDeltaTime)
        {
            float k = t / time;
            Vector2 p = Vector2.Lerp(from, to, k);
            forcedVelocity = Vector2.zero;
            if (rb != null) rb.position = p; else transform.position = p;
            yield return new WaitForFixedUpdate();
            if (enemy == null || enemy.IsDead) yield break;
        }
        forcedVelocity = null;
        onLand(to);
    }

    // una càrrega en línia recta (et fa mal si t'atropella)
    protected IEnumerator Charge(Vector2 dir, float speed, float time, System.Action<Vector2> every = null)
    {
        bool hit = false;
        float nextEvery = 0f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            forcedVelocity = dir * speed;
            if (!hit && HurtPlayerIn(Pos, 1.4f))
            {
                hit = true;
                PlayerMovement pm = PlayerRef.Movement;
                if (pm != null) pm.Knockback(dir, 14f, 0.2f);
            }
            if (every != null && t >= nextEvery) { nextEvery = t + 0.12f; every(Pos); }
            yield return null;
            if (enemy == null || enemy.IsDead) break;
            // s'atura en arribar a la paret
            Vector2 p = Pos - HazardManager.ArenaCenter;
            Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 1.6f;
            if ((Mathf.Abs(p.x) > half.x && Mathf.Sign(dir.x) == Mathf.Sign(p.x)) ||
                (Mathf.Abs(p.y) > half.y && Mathf.Sign(dir.y) == Mathf.Sign(p.y))) break;
        }
        forcedVelocity = null;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // la línia d'avís d'una càrrega (segueix el jugador fins que es fixa)
    protected IEnumerator AimLine(float time, float length, float width, Color color, System.Action<Vector2> done)
    {
        hold = true;
        Vector2 dir = ToPlayer;
        SpriteRenderer line = BossFx.Line(Pos, dir, length, width, new Color(color.r, color.g, color.b, 0.15f), "AvisCap");
        Destroy(line.gameObject, time + 0.3f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (t < time * 0.75f) dir = ToPlayer; // l'últim moment ja no et segueix
            line.transform.rotation = Quaternion.Euler(0f, 0f, Angle(dir));
            line.transform.position = Pos + dir * (length * 0.5f);
            line.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.15f, 0.55f, t / time));
            yield return null;
            if (enemy == null || enemy.IsDead) break;
        }
        if (line != null) Destroy(line.gameObject);
        hold = false;
        done(dir);
    }

    // el cap queda marejat una estona (i rep més mal)
    protected IEnumerator Dizzy(float seconds, string text)
    {
        hold = true;
        if (enemy != null) enemy.damageMultiplier = 1.5f;
        Say(text, new Color(1f, 0.95f, 0.5f), seconds);
        for (float t = 0f; t < seconds; t += 0.25f)
        {
            Fx.Sparkle(Pos + Vector2.up * 1.6f + Random.insideUnitCircle * 0.5f, new Color(1f, 0.9f, 0.3f));
            yield return new WaitForSeconds(0.25f);
        }
        if (enemy != null) enemy.damageMultiplier = 1f;
        hold = false;
    }

    protected static Sprite Shot(string name) { return Resources.Load<Sprite>("BossShots/" + name); }
}

// =====================================================================
//  1. EL CRÍTIC GASTRONÒMIC — estrelles i la "NOTA FINAL" (punt feble: crítics)
// =====================================================================
public class BossCriticBrain : BossBrain
{
    Sprite star;
    protected override string Phase2Line => "RESSENYA NEGATIVA!";
    protected override Sprite DesperateSprite => star;
    protected override float VoicePitch => 1.15f;
    protected override string[] Taunts => new[] { "Massa sal!", "Això està cru!", "Un zero rodó!", "On és el xef?", "Quina vergonya!" };
    protected override void Start() { star = Shot("Star"); keepMin = 3f; keepMax = 6.5f; base.Start(); }

    protected override IEnumerator Brain()
    {
        // ventalls d'estrelles
        Say("UNA ESTRELLA!", new Color(1f, 0.85f, 0.3f));
        int volleys = phase2 ? 4 : 3;
        for (int v = 0; v < volleys && Alive; v++)
        {
            foreach (BossShotFx f in Fan(star, ToPlayer, phase2 ? 7 : 5, 13f, 7f))
            {
                f.spin = 540f; f.trail = "brillo"; f.trailColor = new Color(1f, 0.85f, 0.3f);
            }
            AudioManager.Play("pickup", 0.5f, 1.3f);
            yield return Wait(0.45f);
        }
        yield return Wait(1f);
        if (!Alive) yield break;

        // la NOTA FINAL: salta a sobre teu i esclata en estrelles
        Say("NOTA FINAL!", new Color(1f, 0.5f, 0.3f), 1.2f);
        hold = true;
        Squash.Kick(gameObject, 0.3f, -0.3f);
        Vector2 target = ClampArena(PlayerPos, 2f);
        SpriteRenderer mark = HazardFx.MakeCircle(target, 2.4f, new Color(1f, 0.4f, 0.2f, 0.25f), -2);
        Destroy(mark.gameObject, 1.5f);
        yield return Wait(0.6f);
        yield return StartCoroutine(Leap(target, 0.45f, p =>
        {
            Fx.Shockwave(p, 2.4f, new Color(1f, 0.85f, 0.4f));
            Fx.Dust(p);
            CameraFX.Shake(0.3f, 0.3f);
            AudioManager.Play("explosion", 0.6f, 0.8f);
            HurtPlayerIn(p, 2.4f);
            HazardFx.Popup(p + Vector2.up * 1.5f, Random.value < 0.5f ? "0/10" : "SUSPÈS!", new Color(1f, 0.3f, 0.25f), 1f);
            foreach (BossShotFx f in Ring(star, phase2 ? 16 : 10, 6f)) f.spin = -400f;
        }));
        if (mark != null) Destroy(mark.gameObject);
        hold = false;
        yield return Wait(0.8f);

        // a la segona fase, una corona d'estrelles que es tanca i s'obre
        if (phase2 && Alive)
        {
            Say("ESTRELLES FUGACES!", new Color(1f, 0.85f, 0.3f));
            for (int k = 0; k < 3 && Alive; k++)
            {
                foreach (BossShotFx f in Ring(star, 12, 5f, k * 15f)) { f.spin = 600f; f.curve = k % 2 == 0 ? 45f : -45f; }
                yield return Wait(0.35f);
            }
        }
        yield return Wait(1.6f);
        if (Random.value < 0.4f) FlipStrafe();
    }
}

// =====================================================================
//  2. EL BORRATXO DE LA BARRA — càrregues que deixen cervesa i ampolles (punt feble: gel)
// =====================================================================
public class BossDrunkBrain : BossBrain
{
    Sprite beer;
    protected override string Phase2Line => "UNA ALTRA RONDA!";
    protected override Sprite DesperateSprite => beer;
    protected override float VoicePitch => 0.75f;
    protected override string Phase3Line => "LA DARRERA... HIC!";
    protected override string[] Taunts => new[] { "Una altra ronda!", "Hic!", "Tu ets el meu millor amic...", "On és la barra?", "Paga la casa!" };
    protected override void Start() { beer = Shot("Beer"); base.Start(); }

    static readonly Color Beer = new Color(0.95f, 0.7f, 0.2f);

    void Spill(Vector2 p) { BossPuddle.Spawn(p, 0.9f, 4f, Fx.OilPuddle(), new Color(1f, 0.78f, 0.3f, 0.8f), 0.45f, "RELLISCA!"); }

    protected override IEnumerator Brain()
    {
        // càrregues (a la 2a fase, tres seguides)
        int charges = phase2 ? 3 : 1;
        for (int i = 0; i < charges && Alive; i++)
        {
            Say(i == 0 ? "HIC!" : "I UNA ALTRA!", Beer, 0.8f);
            Vector2 dir = Vector2.right;
            yield return StartCoroutine(AimLine(i == 0 ? 0.8f : 0.5f, 10f, 1.2f, new Color(1f, 0.6f, 0.2f), d => dir = d));
            if (!Alive) yield break;
            CameraFX.Shake(0.2f, 0.2f);
            yield return StartCoroutine(Charge(dir, 17f, 0.65f, Spill));
            Fx.Dust(Pos);
        }
        yield return StartCoroutine(Dizzy(phase2 ? 0.8f : 1.2f, "@#!?"));
        if (!Alive) yield break;

        // ampolles de cervesa: cauen on ets i esclaten
        Say("SALUT!", Beer);
        LobMany(beer, SpotsAroundPlayer(phase2 ? 5 : 3, 3.5f), 0.9f, 1.2f, Beer, p =>
        {
            Fx.Explosion(p, 1.1f, 0.5f);
            Fx.Sparkles(p, Beer, 6, 1f);
            AudioManager.Play("box_break", 0.7f, 1.2f);
            HurtPlayerIn(p, 1.2f);
            Spill(p);
        });
        yield return Wait(1.5f);

        // a la segona fase: el rot (un con que t'empeny)
        if (phase2 && Alive)
        {
            hold = true;
            Say("BUUURP!", new Color(0.6f, 0.9f, 0.4f), 1f);
            Vector2 aim = ToPlayer;
            yield return Wait(0.5f);
            for (int k = 0; k < 6; k++)
            {
                Vector2 p = Pos + Dir(Angle(aim) + Random.Range(-30f, 30f)) * Random.Range(1f, 5f);
                Fx.Ripple(p, 1.2f, new Color(0.6f, 0.9f, 0.4f));
                Fx.Dust(p);
            }
            if (PlayerInCone(Pos, aim, 6f, 35f))
            {
                PlayerMovement pm = PlayerRef.Movement;
                if (pm != null && !pm.IsDashing) pm.Knockback(aim, 18f, 0.35f);
                if (PlayerRef.Health != null && (pm == null || !pm.IsDashing)) PlayerRef.Health.TakeDamage();
            }
            AudioManager.Play("bounce", 0.9f, 0.4f);
            hold = false;
            yield return Wait(1f);
        }
        yield return Wait(1.2f);
    }
}

// =====================================================================
//  3. EL XEF RENEGAT — lanxa-flames i paelles que esclaten (punt feble: gel)
// =====================================================================
public class BossChefBrain : BossBrain
{
    Sprite pan;
    float nextHelpers;
    protected override string Phase2Line => "FOC A LA CUINA!";
    protected override Sprite DesperateSprite => pan;
    protected override float VoicePitch => 0.85f;
    protected override string Phase3Line => "TOT AL FOC!";
    protected override string[] Taunts => new[] { "Aquesta cuina és meva!", "Al forn amb tu!", "Massa poc fet!", "Sí, xef!", "A la paella!" };
    protected override void Start() { pan = Shot("Pan"); keepMin = 3.5f; keepMax = 6.5f; base.Start(); }

    protected override IEnumerator Brain()
    {
        // FLAMBÉ: el lanxa-flames (com el del jugador), que et va seguint a poc a poc
        Say("FLAMBÉ!", new Color(1f, 0.55f, 0.2f));
        hold = true;
        Vector2 aim = ToPlayer;
        for (float t = 0f; t < 0.6f; t += 0.1f)
        {
            Fx.Flames(Pos + aim * 1.2f, 0.25f, 0.3f);
            yield return new WaitForSeconds(0.1f);
        }
        float burn = phase2 ? 2.4f : 1.8f;
        float range = 5.5f, halfA = 24f;
        float nextFx = 0f;
        for (float t = 0f; t < burn && Alive; t += Time.deltaTime)
        {
            aim = Vector3.RotateTowards(aim, ToPlayer, (phase2 ? 1.3f : 0.8f) * Time.deltaTime, 0f);
            Fx.FlameJet(Pos, aim, range, halfA, 1f); // el mateix raig que el lanxa-flames del jugador
            if (PlayerInCone(Pos, aim, range, halfA))
            {
                PlayerMovement pm = PlayerRef.Movement;
                if (PlayerRef.Health != null && (pm == null || !pm.IsDashing)) PlayerRef.Health.TakeDamage();
            }
            yield return null;
        }
        hold = false;
        yield return Wait(0.8f);
        if (!Alive) yield break;

        // paelles que cauen i esclaten (com els calçots), i deixen foc
        Say("A LA PAELLA!", new Color(1f, 0.6f, 0.25f));
        LobMany(pan, SpotsAroundPlayer(phase2 ? 4 : 3, 4f), 1f, 1.7f, new Color(1f, 0.35f, 0.15f), p =>
        {
            Fx.Explosion(p, 1.7f, 0.9f);
            CameraFX.Shake(0.15f, 0.15f);
            AudioManager.Play("explosion", 0.5f, 1f);
            HurtPlayerIn(p, 1.7f);
            BossPuddle.Spawn(p, 1.1f, 3f, HazardFx.Circle(), new Color(1f, 0.45f, 0.1f, 0.3f), 0f, null, true, "foc");
        });
        yield return Wait(1.6f);

        // a la segona fase: els ajudants i l'anell de foc
        if (phase2 && Alive)
        {
            if (Time.time >= nextHelpers && spawner != null)
            {
                nextHelpers = Time.time + 14f;
                Say("AJUDANTS!", new Color(1f, 0.8f, 0.4f));
                spawner.SpawnMinions(Pos, 3);
                yield return Wait(0.8f);
            }
            var ring = new List<Vector2>();
            float off = Random.Range(0f, 360f);
            for (int i = 0; i < 8; i++) ring.Add(ClampArena(Pos + Dir(off + i * 45f) * 3.2f, 1.5f));
            ring.Add(PlayerPos);
            yield return StartCoroutine(Telegraph(ring, 1.3f, new Color(1f, 0.35f, 0.1f), 1f, p =>
            {
                Fx.Flames(p, 1.2f, 1.5f);
                HurtPlayerIn(p, 1.3f);
                BossPuddle.Spawn(p, 1.1f, 2.5f, HazardFx.Circle(), new Color(1f, 0.45f, 0.1f, 0.3f), 0f, null, true, "foc");
            }));
        }
        yield return Wait(1.2f);
    }
}

// =====================================================================
//  4. EL CLIENT VIP — pluja de bitllets, teletransport i bombes de propina (punt feble: foc)
// =====================================================================
public class BossVipBrain : BossBrain
{
    Sprite bill;
    SpriteRenderer body;
    protected override string Phase2Line => "LA FACTURA, SI US PLAU!";
    protected override Color Tone => new Color(0.6f, 0.95f, 0.5f);
    protected override Sprite DesperateSprite => bill;
    protected override float VoicePitch => 1.05f;
    protected override string Phase3Line => "PAGO JO!";
    protected override string[] Taunts => new[] { "Saps qui soc jo?", "Vull parlar amb l'encarregat!", "Taula per a u. Sempre.", "Ho pago tot!", "Quin servei més lent!" };
    protected override void Start() { bill = Shot("Bill"); keepMin = 4f; keepMax = 7f; body = GetComponent<SpriteRenderer>(); base.Start(); }

    static readonly Color Money = new Color(0.5f, 0.9f, 0.45f);

    void TipBomb(Vector2 at)
    {
        StartCoroutine(Telegraph(new List<Vector2> { at }, 2f, new Color(1f, 0.85f, 0.2f), 1.3f, p =>
        {
            Fx.Explosion(p, 2f, 1f);
            CameraFX.Shake(0.2f, 0.2f);
            AudioManager.Play("explosion", 0.5f, 1.1f);
            HurtPlayerIn(p, 2f);
            Fx.Sparkles(p, new Color(1f, 0.85f, 0.2f), 8, 1.5f);
        }));
    }

    IEnumerator Teleport()
    {
        Vector2 from = Pos;
        Fx.Poof(from);
        AudioManager.Play("pickup", 0.6f, 0.6f);
        if (body != null) body.enabled = false;
        TipBomb(from);
        if (phase2) { TipBomb(ClampArena(from + Random.insideUnitCircle * 3f, 1.5f)); TipBomb(ClampArena(from + Random.insideUnitCircle * 3f, 1.5f)); }
        hold = true;
        yield return Wait(0.4f);
        Vector2 to = HazardFx.RandomPoint(3f);
        for (int i = 0; i < 6 && Vector2.Distance(to, PlayerPos) < 4.5f; i++) to = HazardFx.RandomPoint(3f);
        if (rb != null) rb.position = to; else transform.position = to;
        Fx.Poof(to);
        if (body != null) body.enabled = true;
        Squash.Pop(gameObject);
        hold = false;
    }

    protected override IEnumerator Brain()
    {
        // pluja de bitllets: cauen on ets
        Say("PROPINA!", Money);
        int n = phase2 ? 9 : 6;
        var spots = SpotsAroundPlayer(n, 4.5f);
        yield return StartCoroutine(Telegraph(spots, 0.9f, Money, 0.9f, p =>
        {
            Fx.Sparkles(p, Money, 5, 0.8f);
            Fx.Dust(p);
            HurtPlayerIn(p, 0.9f);
        }));
        AudioManager.Play("pickup", 0.6f, 1.4f);
        yield return Wait(0.8f);
        if (!Alive) yield break;

        yield return StartCoroutine(Teleport());
        yield return Wait(0.6f);
        if (!Alive) yield break;

        // ventalls de bitllets que van fent ziga-zaga
        for (int v = 0; v < (phase2 ? 3 : 2) && Alive; v++)
        {
            int k = 0;
            foreach (BossShotFx f in Fan(bill, ToPlayer, 5, 16f, 5.5f, 0.75f)) { f.curve = (k++ % 2 == 0 ? 60f : -60f); f.spin = 200f; }
            yield return Wait(0.5f);
        }
        if (phase2 && Alive)
        {
            yield return StartCoroutine(Teleport());
            Say("TOT PAGAT!", Money);
            float a = Random.Range(0f, 360f);
            for (int i = 0; i < 14 && Alive; i++)
            {
                a += 33f;
                foreach (BossShotFx f in new[] { Shoot(bill, Dir(a), 5f, 0.75f), Shoot(bill, Dir(a + 180f), 5f, 0.75f) })
                    if (f != null) f.spin = 250f;
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield return Wait(1.4f);
    }
}

// =====================================================================
//  5. LA INSPECTORA — espiral de paperassa, segells i zones tancades (punt feble: explosions)
// =====================================================================
public class BossInspectorBrain : BossBrain
{
    Sprite paper;
    protected override string Phase2Line => "TANCAT PER INSPECCIÓ!";
    protected override Sprite DesperateSprite => paper;
    protected override float VoicePitch => 1.25f;
    protected override string Phase3Line => "CLAUSURAT!";
    protected override string[] Taunts => new[] { "Article 34-B!", "Això no compleix la norma.", "Multa!", "On és el llibre de queixes?", "Inspecció!" };
    protected override void Start() { paper = Shot("Paper"); keepMin = 4.5f; keepMax = 7.5f; base.Start(); }

    static readonly Color Ink = new Color(0.85f, 0.2f, 0.25f);

    protected override IEnumerator Brain()
    {
        // l'informe: espiral de papers
        Say("INFORME!", new Color(0.95f, 0.95f, 0.85f));
        hold = true;
        float a = Random.Range(0f, 360f);
        int arms = phase2 ? 3 : 2;
        for (float t = 0f; t < 2.2f && Alive; t += 0.13f)
        {
            a += 19f;
            for (int k = 0; k < arms; k++)
            {
                BossShotFx f = Shoot(paper, Dir(a + k * 360f / arms), 5f, 0.7f);
                if (f != null) f.spin = 300f;
            }
            yield return new WaitForSeconds(0.13f);
        }
        hold = false;
        yield return Wait(0.7f);
        if (!Alive) yield break;

        // els segells: quadres a terra que piquen
        Say("SEGELL!", Ink);
        int stamps = phase2 ? 3 : 2;
        for (int s = 0; s < stamps && Alive; s++)
        {
            Vector2 p = ClampArena(PlayerPos, 2f);
            SpriteRenderer sq = BossFx.Line(p - Vector2.right * 1.6f, Vector2.right, 3.2f, 3.2f, new Color(Ink.r, Ink.g, Ink.b, 0.12f), "Segell");
            Destroy(sq.gameObject, 1f);
            for (float t = 0f; t < 0.75f; t += Time.deltaTime)
            {
                if (sq != null) sq.color = new Color(Ink.r, Ink.g, Ink.b, Mathf.Lerp(0.12f, 0.5f, t / 0.75f));
                yield return null;
            }
            if (sq != null) Destroy(sq.gameObject);
            Fx.Shockwave(p, 1.8f, Ink);
            Fx.Dust(p);
            CameraFX.Shake(0.15f, 0.18f);
            AudioManager.Play("box_break", 0.6f, 0.7f);
            HazardFx.Popup(p, "DENEGAT", Ink, 0.8f);
            Transform pl = PlayerRef.Transform;
            if (pl != null && Mathf.Abs(pl.position.x - p.x) <= 1.6f && Mathf.Abs(pl.position.y - p.y) <= 1.6f) HurtPlayerIn(pl.position, 0.1f);
            yield return Wait(0.35f);
        }
        yield return Wait(1f);

        // a la segona fase: tanca un terç de la sala (el que és més lluny d'ella)
        if (phase2 && Alive)
        {
            Vector2 c = HazardManager.ArenaCenter;
            Vector2 size = HazardManager.ArenaSize;
            bool vertical = Random.value < 0.5f;
            Vector2 zoneSize = vertical ? new Vector2(size.x / 3f, size.y) : new Vector2(size.x, size.y / 3f);
            Vector2 zone;
            if (vertical) zone = new Vector2(c.x + (Pos.x < c.x ? 1f : -1f) * size.x / 3f, c.y);
            else zone = new Vector2(c.x, c.y + (Pos.y < c.y ? 1f : -1f) * size.y / 3f);

            Say("ZONA TANCADA!", Ink, 1.4f);
            SpriteRenderer warn = BossFx.Line(zone - Vector2.right * zoneSize.x * 0.5f, Vector2.right, zoneSize.x, zoneSize.y, new Color(Ink.r, Ink.g, Ink.b, 0.08f), "ZonaAvís");
            Destroy(warn.gameObject, 2f);
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                if (warn != null) warn.color = new Color(Ink.r, Ink.g, Ink.b, 0.08f + 0.18f * Mathf.Abs(Mathf.Sin(t * 9f)));
                yield return null;
            }
            if (warn != null) Destroy(warn.gameObject);
            BossPuddle.SpawnRect(zone, zoneSize, 6f, new Color(Ink.r, Ink.g, Ink.b, 0.3f), null);
            HazardFx.Popup(zone, "TANCAT", Ink, 2f);
            Fx.Shockwave(zone, 2f, Ink);
            AudioManager.Play("explosion", 0.4f, 0.6f);
        }
        yield return Wait(1.2f);
        if (Random.value < 0.5f) FlipStrafe();
    }
}

// =====================================================================
//  6. EL SOMMELIER — ampolles, tolls de vi, taps que reboten i cava (punt feble: crítics)
// =====================================================================
public class BossSommelierBrain : BossBrain
{
    Sprite cork;
    protected override string Phase2Line => "UN RESERVA DEL 82!";
    protected override Sprite DesperateSprite => cork;
    protected override float VoicePitch => 0.95f;
    protected override string Phase3Line => "UNA GRAN RESERVA!";
    protected override string[] Taunts => new[] { "Notes de cuir i desesperació...", "Un vi amb molt de cos!", "Massa àcid!", "Salut!", "Aquest no marida." };
    protected override void Start() { cork = Shot("Cork"); keepMin = 3f; keepMax = 6f; base.Start(); }

    static readonly Color Wine = new Color(0.6f, 0.1f, 0.25f);
    static readonly Color Gold = new Color(1f, 0.9f, 0.5f);

    protected override IEnumerator Brain()
    {
        // ampolles que cauen on ets i deixen vi a terra
        Say("MARIDATGE!", Wine);
        Sprite bottle = Shot("Beer");
        LobMany(bottle, SpotsAroundPlayer(phase2 ? 5 : 4, 3.5f), 0.9f, 1.2f, Wine, p =>
        {
            Fx.Hit(p, Vector2.down, false);
            Fx.Sparkles(p, Wine, 7, 1f);
            AudioManager.Play("box_break", 0.7f, 1.2f);
            HurtPlayerIn(p, 1.2f);
            BossPuddle.Spawn(p, 1.2f, 5f, Fx.OilPuddle(), new Color(0.75f, 0.15f, 0.3f, 0.85f), 0.45f, "VI!");
        });
        yield return Wait(1.6f);
        if (!Alive) yield break;

        // TAST: taps de suro que reboten per les parets (com les mandonguilles)
        Say("TAST!", Wine);
        Squash.Kick(gameObject, 0.15f, -0.15f);
        foreach (BossShotFx f in Ring(cork, phase2 ? 12 : 9, 6.5f)) { f.spin = 500f; }
        AudioManager.Play("pickup", 0.6f, 0.7f);
        yield return Wait(1.4f);

        // a la segona fase: el cava (bombolles que esclaten i fan sortir més bombolles)
        if (phase2 && Alive)
        {
            Say("CAVA!", Gold);
            yield return StartCoroutine(Telegraph(SpotsAroundPlayer(3, 4f), 1.2f, Gold, 0.9f, p =>
            {
                Fx.Shockwave(p, 1.2f, Gold);
                Fx.Sparkles(p, Gold, 10, 1.2f);
                HurtPlayerIn(p, 1.2f);
                float off = Random.Range(0f, 360f);
                for (int i = 0; i < 6; i++)
                {
                    BossShotFx f = SpawnShot(shotPrefab, null, p, Dir(off + i * 60f), 5f, 0.45f, Gold);
                    if (f != null) { f.trail = "bombolles"; f.trailColor = Gold; }
                }
                AudioManager.Play("pickup", 0.5f, 1.6f);
            }));
        }
        yield return Wait(1.2f);
    }
}

// =====================================================================
//  7. EL GELATER BOIG — boles de gelat, terra glaçat i tempesta de neu (punt feble: foc)
// =====================================================================
public class BossGelaterBrain : BossBrain
{
    Sprite ball;
    protected override string Phase2Line => "TEMPESTA DE NEU!";
    protected override Sprite DesperateSprite => ball;
    protected override float VoicePitch => 1.35f;
    protected override string Phase3Line => "ERA GLACIAL!";
    protected override string[] Taunts => new[] { "Una bola o dues?", "Fresquet, eh?", "Et deixaré glaçat!", "Gelat de xef!", "Brrrr!" };
    protected override void Start() { ball = Shot("IceBall"); keepMin = 3.5f; keepMax = 6.5f; base.Start(); }

    static readonly Color Ice = new Color(0.65f, 0.88f, 1f);

    protected override IEnumerator Brain()
    {
        // ventalls de boles de gelat (si et toquen, et frenen)
        for (int v = 0; v < (phase2 ? 3 : 2) && Alive; v++)
        {
            foreach (BossShotFx f in Fan(ball, ToPlayer, phase2 ? 7 : 5, 11f, 6.5f)) { f.trail = "neu"; f.slowOnHit = 0.4f; f.spin = 120f; }
            yield return Wait(0.6f);
        }
        yield return Wait(0.6f);
        if (!Alive) yield break;

        // GELADA: cercles que es glacen (com els Calçots gelats del jugador)
        Say("GELADA!", Ice);
        yield return StartCoroutine(Telegraph(SpotsAroundPlayer(phase2 ? 6 : 4, 4f), 1.4f, Ice, 1.1f, p =>
        {
            Fx.FrostBurst(p, 1.4f);
            if (HurtPlayerIn(p, 1.4f)) PlayerSlow.Apply(0.55f, 2f, "GLAÇAT!");
            BossPuddle.Spawn(p, 1.4f, 4f, Fx.FrostGround(), new Color(1f, 1f, 1f, 0.85f), 0.4f, "RELLISCA!", false, "neu");
        }));
        AudioManager.Play("crit", 0.6f, 1.4f);
        yield return Wait(1.2f);

        // a la segona fase: espiral de gelats i boles que esclaten en estelles
        if (phase2 && Alive)
        {
            hold = true;
            float a = Random.Range(0f, 360f);
            for (float t = 0f; t < 2f && Alive; t += 0.12f)
            {
                a += 27f;
                for (int k = 0; k < 2; k++)
                {
                    BossShotFx f = Shoot(ball, Dir(a + k * 180f), 5f, 0.6f);
                    if (f != null) { f.trail = "neu"; f.slowOnHit = 0.3f; }
                }
                if (Random.value < 0.5f) Fx.Snowflake(PlayerPos + Random.insideUnitCircle * 4f + Vector2.up * 3f);
                yield return new WaitForSeconds(0.12f);
            }
            hold = false;
            foreach (BossShotFx f in Fan(ball, ToPlayer, 3, 30f, 4.5f, 0.95f))
            {
                f.fuse = 1f; f.burstShots = 6; f.burstSprite = ball; f.trail = "neu";
            }
        }
        yield return Wait(1.2f);
    }
}

// =====================================================================
//  8. LA CRÍTICA DE LA TELE — focus, flaix de càmera, confeti i aplaudiments (punt feble: explosions)
// =====================================================================
public class BossTvBrain : BossBrain
{
    protected override string Phase2Line => "PUBLICITAT? NO! MÉS EMOCIÓ!";
    protected override float VoicePitch => 1.2f;
    protected override string Phase3Line => "EN DIRECTE... I FINAL!";
    protected override string[] Taunts => new[] { "Somriu a la càmera!", "Audiència rècord!", "Tornem després de la publicitat!", "Quin espectacle!", "Aplaudiments!" };
    protected override void Start() { keepMin = 5f; keepMax = 8f; base.Start(); }

    static readonly Color Spot = new Color(1f, 0.95f, 0.5f);
    float nextApplause = 0f;

    protected override IEnumerator Brain()
    {
        // FOCUS que s'encenen
        Say("LLUMS!", Spot);
        yield return StartCoroutine(Telegraph(SpotsAroundPlayer(phase2 ? 5 : 3, 5f), 1.6f, Spot, 1f, p =>
        {
            Fx.Shockwave(p, 1f, new Color(1f, 0.97f, 0.7f));
            Fx.Sparkles(p, Spot, 5, 1.6f);
            if (HurtPlayerIn(p, 1.6f)) ScreenFlash.Blind(0.6f);
        }));
        AudioManager.Play("crit", 0.7f, 0.8f);
        yield return Wait(0.8f);
        if (!Alive) yield break;

        // SOMRIU!: el flaix de la càmera (un con: surt-ne!)
        Say("SOMRIU!", Color.white, 1.2f);
        Vector2 aim = Vector2.right;
        yield return StartCoroutine(AimLine(1f, 7f, 4f, Color.white, d => aim = d));
        if (!Alive) yield break;
        Fx.Shockwave(Pos + aim * 2f, 2.5f, Color.white);
        for (int k = 0; k < 8; k++) Fx.Sparkle(Pos + aim * Random.Range(1f, 7f) + Random.insideUnitCircle, Color.white);
        AudioManager.Play("crit", 0.9f, 1.6f);
        if (PlayerInCone(Pos, aim, 7.5f, 20f))
        {
            PlayerMovement pm = PlayerRef.Movement;
            if (pm == null || !pm.IsDashing)
            {
                ScreenFlash.Blind(0.9f);
                if (PlayerRef.Health != null) PlayerRef.Health.TakeDamage();
            }
        }
        yield return Wait(0.6f);

        // confeti de colors
        Color[] cols = { new Color(1f, 0.4f, 0.5f), new Color(0.4f, 0.8f, 1f), new Color(1f, 0.9f, 0.3f), new Color(0.5f, 1f, 0.5f) };
        for (int v = 0; v < (phase2 ? 3 : 2) && Alive; v++)
        {
            int i = 0;
            foreach (BossShotFx f in Ring(null, phase2 ? 18 : 14, 5.5f, -1f, 0.4f, cols[v % cols.Length]))
            { f.curve = (i++ % 2 == 0 ? 35f : -35f); f.trail = "brillo"; f.trailColor = cols[(v + i) % cols.Length]; }
            yield return Wait(0.4f);
        }
        yield return Wait(0.6f);

        // APLAUDIMENTS: el públic baixa a la pista
        if (Alive && Time.time >= nextApplause && spawner != null)
        {
            nextApplause = Time.time + (phase2 ? 10f : 14f);
            Say("APLAUDIMENTS!", new Color(1f, 0.8f, 0.3f), 1.4f);
            AudioManager.Play("wave_clear", 0.8f, 1.1f);
            Squash.Kick(gameObject, -0.15f, 0.2f);
            spawner.SpawnMinions(Pos, phase2 ? 4 : 3);
        }

        // a la segona fase: un focus que et persegueix
        if (phase2 && Alive)
        {
            Say("PRIMER PLA!", Spot);
            SpriteRenderer follow = HazardFx.MakeCircle(PlayerPos, 1.5f, new Color(Spot.r, Spot.g, Spot.b, 0.2f), -2);
            Destroy(follow.gameObject, 2.8f);
            Vector2 p = PlayerPos;
            for (float t = 0f; t < 2.4f && Alive; t += Time.deltaTime)
            {
                p = Vector2.MoveTowards(p, PlayerPos, 4.2f * Time.deltaTime);
                if (follow != null)
                {
                    follow.transform.position = p;
                    follow.color = new Color(Spot.r, Spot.g, Spot.b, 0.2f + 0.25f * t / 2.4f);
                }
                yield return null;
            }
            if (follow != null) Destroy(follow.gameObject);
            Fx.Shockwave(p, 1.5f, Spot);
            if (HurtPlayerIn(p, 1.5f)) ScreenFlash.Blind(0.6f);
        }
        yield return Wait(1f);
        if (Random.value < 0.35f) FlipStrafe();
    }
}

// =====================================================================
//  9. EL FANTASMA DEL RESTAURANT (cap secret) — es fa invisible, crida i llença plats encantats
//     (punt feble: foc). Surt si trobes prou receptes perdudes en una partida d'El Servei.
// =====================================================================
public class BossGhostBrain : BossBrain
{
    Sprite plate;
    SpriteRenderer body;
    float alpha = 0.8f;
    readonly System.Collections.Generic.List<GameObject> decoys = new System.Collections.Generic.List<GameObject>();

    protected override string Phase2Line => "SOM MOLTS, ARA!";
    protected override string Phase3Line => "EL RESTAURANT ÉS MEU!";
    protected override Sprite DesperateSprite => plate;
    protected override float VoicePitch => 0.6f;
    protected override string[] Taunts => new[] { "Buuuu!", "Aquí no tanca ningú...", "El servei no s'acaba mai!", "Fa cent anys que espero!", "Qui ha tocat la meva recepta?" };

    static readonly Color Ecto = new Color(0.6f, 1f, 0.85f);

    protected override void Start()
    {
        plate = Shot("PlatFantasma");
        body = GetComponent<SpriteRenderer>();
        keepMin = 4f; keepMax = 7f;
        base.Start();
    }

    void LateUpdate()
    {
        // translúcid, i batega una mica
        if (body != null) body.color = new Color(1f, 1f, 1f, alpha * (0.85f + 0.15f * Mathf.Sin(Time.time * 4f)));
        if (Random.value < Time.deltaTime * 6f) Fx.Sparkle(Pos + Random.insideUnitCircle * 1.4f, Ecto);
    }

    void OnDisable()
    {
        foreach (GameObject d in decoys) if (d != null) Destroy(d);
    }

    protected override void OnPhase2()
    {
        // dues còpies falses que el segueixen i també disparen
        for (int i = 0; i < 2; i++) StartCoroutine(Decoy(i == 0 ? 1f : -1f));
    }

    IEnumerator Decoy(float side)
    {
        GameObject go = new GameObject("CopiaFantasma");
        decoys.Add(go);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body != null ? body.sprite : null;
        sr.sortingOrder = body != null ? body.sortingOrder : 0;
        go.transform.localScale = transform.localScale;
        float a = side > 0 ? 0f : 180f;
        float nextShot = Time.time + 2f;
        while (Alive)
        {
            a += Time.deltaTime * 50f * side;
            Vector2 p = PlayerPos + Dir(a) * 5.5f;
            go.transform.position = Vector2.Lerp(go.transform.position, ClampArena(p, 2f), Time.deltaTime * 2f);
            sr.color = new Color(1f, 1f, 1f, 0.45f + 0.15f * Mathf.Sin(Time.time * 5f + side));
            if (Time.time >= nextShot)
            {
                nextShot = Time.time + 3.2f;
                Vector2 to = PlayerPos - (Vector2)go.transform.position;
                BossShotFx f = SpawnShot(shotPrefab, plate, go.transform.position, to, 4.5f, 0.6f);
                if (f != null) f.spin = 300f;
            }
            yield return null;
        }
        if (go != null) { Fx.Poof(go.transform.position); Destroy(go); }
    }

    protected override IEnumerator Brain()
    {
        // BUUU!: desapareix, travessa la sala i surt al teu costat cridant
        Say("...", Ecto, 0.6f);
        hold = true;
        if (enemy != null) enemy.damageMultiplier = 0.25f; // mig invisible: costa tocar-lo
        for (float t = 0f; t < 0.5f; t += Time.deltaTime) { alpha = Mathf.Lerp(0.8f, 0.08f, t / 0.5f); yield return null; }
        Vector2 to = ClampArena(PlayerPos + Random.insideUnitCircle.normalized * 3.2f, 2f);
        Vector2 from = Pos;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            Vector2 p = Vector2.Lerp(from, to, t / 0.8f);
            if (rb != null) rb.position = p; else transform.position = p;
            yield return null;
            if (!Alive) break;
        }
        for (float t = 0f; t < 0.25f; t += Time.deltaTime) { alpha = Mathf.Lerp(0.08f, 0.9f, t / 0.25f); yield return null; }
        alpha = 0.8f;
        if (enemy != null) enemy.damageMultiplier = 1f;
        Say("BUUUU!", Ecto, 1f);
        Squash.Kick(gameObject, -0.3f, 0.35f);
        Vector2 aim = ToPlayer;
        for (int k = 0; k < 4; k++) Fx.Ripple(Pos + aim * (1.2f + k * 1.1f), 1.2f + k * 0.5f, Ecto);
        AudioManager.Play("voice_2", 1f, 0.45f);
        if (PlayerInCone(Pos, aim, 5.5f, 35f))
        {
            PlayerMovement pm = PlayerRef.Movement;
            if (pm == null || !pm.IsDashing)
            {
                PlayerSlow.Apply(0.55f, 1.8f, "ESPANTAT!");
                if (PlayerRef.Health != null) PlayerRef.Health.TakeDamage();
            }
        }
        hold = false;
        yield return Wait(1f);
        if (!Alive) yield break;

        // plats encantats que fan espirals
        Say("A TAULA!", Ecto);
        for (int v = 0; v < (phase2 ? 3 : 2) && Alive; v++)
        {
            int i = 0;
            foreach (BossShotFx f in Fan(plate, ToPlayer, 5, 18f, 5f, 0.7f))
            { f.spin = 360f; f.curve = (i++ % 2 == 0 ? 55f : -55f); f.trail = "brillo"; f.trailColor = Ecto; }
            AudioManager.Play("pickup", 0.4f, 0.6f);
            yield return Wait(0.55f);
        }
        yield return Wait(0.8f);

        // els clients de l'altre món
        if (Alive && spawner != null && Random.value < 0.5f)
        {
            Say("CLIENTS DE L'ALTRE MÓN!", Ecto, 1.4f);
            spawner.SpawnMinions(Pos, phase2 ? 4 : 3);
            yield return Wait(1f);
        }
        yield return Wait(1f);
        if (Random.value < 0.5f) FlipStrafe();
    }
}
