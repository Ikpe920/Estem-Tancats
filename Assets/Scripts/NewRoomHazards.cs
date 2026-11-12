using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  ELS PERILLS PROPIS DE LES TRES ÚLTIMES SALES
// =====================================================================
//  EL CELLER   bótes que rodolen per la sala (avís amb una franja)  ·  barrils que vessen vi (frena)
//  LA CAMBRA   estalactites de gel que cauen del sostre             ·  el fred: si et quedes quiet, et glaces
//  EL PLATÓ    el focus EN DIRECTE (els clients a dins reben més mal) ·  el públic llença tomàquets
//              ·  els focus del sostre que cauen

static class RoomHazardUtil
{
    public static int Wave => WaveSpawner.Instance != null ? WaveSpawner.Instance.currentWave : 30;

    // més sovint com més avançada la sala (de la 1a a la 5a onada de la sala)
    public static float Pace(float slow, float fast)
    {
        int inRoom = (Wave - 1) % 5;
        return Mathf.Lerp(slow, fast, inRoom / 4f);
    }

    public static Vector2 PlayerPos { get { Transform p = PlayerRef.Transform; return p != null ? (Vector2)p.position : HazardManager.ArenaCenter; } }

    public static void HurtEnemiesIn(Vector2 pos, float r, int dmg, DmgKind kind, float push = 0f)
    {
        foreach (Collider2D h in Physics2D.OverlapCircleAll(pos, r))
        {
            Enemy e = h.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            DamageKind.Hit(e, dmg, kind);
            if (push > 0f && !e.IsDead) e.Knockback((Vector2)e.transform.position - pos, push, 0.15f);
        }
    }
}

// ---------- EL CELLER: bótes que rodolen ----------
public class BarrelRollHazard : MonoBehaviour
{
    static Sprite barrel;

    IEnumerator Start()
    {
        if (barrel == null) barrel = Resources.Load<Sprite>("Decor/Barril");
        yield return new WaitForSeconds(4f);
        while (true)
        {
            StartCoroutine(Roll());
            yield return new WaitForSeconds(RoomHazardUtil.Pace(6.5f, 3.8f));
        }
    }

    IEnumerator Roll()
    {
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f;
        Vector2 p = RoomHazardUtil.PlayerPos;
        bool horizontal = Random.value < 0.5f;
        float side = Random.value < 0.5f ? -1f : 1f;
        // passa per on ets (a vegades una mica desviada)
        float lane = horizontal ? Mathf.Clamp(p.y + Random.Range(-1f, 1f), c.y - half.y + 1.5f, c.y + half.y - 1.5f)
                                : Mathf.Clamp(p.x + Random.Range(-1f, 1f), c.x - half.x + 1.5f, c.x + half.x - 1.5f);
        Vector2 dir = horizontal ? new Vector2(-side, 0f) : new Vector2(0f, -side);
        Vector2 from = horizontal ? new Vector2(c.x + side * (half.x - 0.6f), lane) : new Vector2(lane, c.y + side * (half.y - 0.6f));
        float length = horizontal ? half.x * 2f : half.y * 2f;

        // l'avís: una franja de color vi al llarg del camí
        SpriteRenderer warn = BossFx.Line(from, dir, length, 1.6f, new Color(0.6f, 0.15f, 0.25f, 0.12f), "AvisBota");
        warn.transform.SetParent(transform, true);
        for (float t = 0f; t < 1.1f; t += Time.deltaTime)
        {
            if (warn == null) yield break;
            warn.color = new Color(0.6f, 0.15f, 0.25f, Mathf.Lerp(0.12f, 0.4f, t / 1.1f));
            yield return null;
        }
        if (warn != null) Destroy(warn.gameObject);

        // la bóta, rodolant
        GameObject go = new GameObject("BotaQueRodola");
        go.transform.SetParent(transform, true);
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 1.6f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = barrel != null ? barrel : HazardFx.Circle();
        sr.sortingOrder = 8;
        AudioManager.Play("table", 0.6f, 0.6f);
        bool hitPlayer = false;
        HashSet<Enemy> hit = new HashSet<Enemy>();
        float speed = 11f;
        for (float d = 0f; d < length; d += speed * Time.deltaTime)
        {
            if (go == null) yield break;
            Vector2 pos = from + dir * d;
            go.transform.position = pos;
            go.transform.Rotate(0f, 0f, -side * 600f * Time.deltaTime * (horizontal ? 1f : -1f));
            if (Random.value < 0.3f) Fx.Dust(pos);
            if (!hitPlayer && BossBrain.HurtPlayerIn(pos, 0.9f))
            {
                hitPlayer = true;
                PlayerMovement pm = PlayerRef.Movement;
                if (pm != null) pm.Knockback(dir, 12f, 0.2f);
            }
            foreach (Collider2D h in Physics2D.OverlapCircleAll(pos, 0.9f))
            {
                Enemy e = h.GetComponentInParent<Enemy>();
                if (e == null || e.IsDead || hit.Contains(e)) continue;
                hit.Add(e);
                DamageKind.Hit(e, 6, DmgKind.None);
                if (!e.IsDead) e.Knockback(dir + Random.insideUnitCircle * 0.5f, 10f, 0.2f);
            }
            yield return null;
        }
        if (go != null)
        {
            Fx.Glass(go.transform.position, 8);
            BossPuddle.Spawn(go.transform.position, 1.2f, 5f, Fx.OilPuddle(), new Color(0.75f, 0.15f, 0.3f, 0.85f), 0.35f, "VI!");
            AudioManager.Play("box_break", 0.7f, 0.8f);
            Destroy(go);
        }
    }
}

// ---------- EL CELLER: barrils que vessen vi ----------
public class WineLeakHazard : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        while (true)
        {
            Vector2 p = Random.value < 0.4f ? RoomHazardUtil.PlayerPos + Random.insideUnitCircle * 2f : HazardFx.RandomPoint(2f);
            // el raig de vi cau d'una bóta del sostre: unes gotes i el toll
            for (int i = 0; i < 4; i++) { Fx.Ripple(p + Random.insideUnitCircle * 0.4f, 0.5f, new Color(0.65f, 0.12f, 0.28f)); yield return new WaitForSeconds(0.12f); }
            BossPuddle.Spawn(p, Random.Range(1.1f, 1.6f), 9f, Fx.OilPuddle(), new Color(0.75f, 0.15f, 0.3f, 0.85f), 0.35f, "VI!");
            yield return new WaitForSeconds(RoomHazardUtil.Pace(5f, 3f));
        }
    }
}

// ---------- LA CAMBRA: estalactites de gel ----------
public class IcicleHazard : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);
        while (true)
        {
            Vector2 p = Random.value < 0.6f ? RoomHazardUtil.PlayerPos : HazardFx.RandomPoint(2f);
            StartCoroutine(Fall(p));
            if (Random.value < 0.4f) StartCoroutine(Fall(HazardFx.RandomPoint(2f)));
            yield return new WaitForSeconds(RoomHazardUtil.Pace(3.2f, 2f));
        }
    }

    IEnumerator Fall(Vector2 pos)
    {
        const float r = 1.2f;
        SpriteRenderer shadow = HazardFx.MakeCircle(pos, r, new Color(0.5f, 0.75f, 1f, 0.1f), -2);
        shadow.transform.SetParent(transform, true);
        // l'estalactita que cau (un vidre de gel llarg)
        GameObject ice = new GameObject("Estalactita");
        ice.transform.SetParent(transform, true);
        SpriteRenderer sr = ice.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Fx/IceShard");
        sr.sortingOrder = 20;
        ice.transform.rotation = Quaternion.Euler(0f, 0f, -90f); // la punta cap avall
        ice.transform.localScale = new Vector3(1.6f, 1.2f, 1f);
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            if (shadow == null || ice == null) yield break;
            float k = t;
            shadow.color = new Color(0.5f, 0.75f, 1f, Mathf.Lerp(0.1f, 0.45f, k));
            shadow.transform.localScale = Vector3.one * r * 2f * Mathf.Lerp(0.4f, 1f, k);
            ice.transform.position = pos + Vector2.up * (8f * (1f - k * k) + 0.4f);
            yield return null;
        }
        if (shadow != null) Destroy(shadow.gameObject);
        if (ice != null) Destroy(ice);
        Fx.FrostBurst(pos, r);
        Fx.IceShatter(pos, 8);
        AudioManager.Play("box_break", 0.6f, 1.5f);
        if (BossBrain.HurtPlayerIn(pos, r)) PlayerSlow.Apply(0.5f, 1.5f, "GLAÇAT!");
        RoomHazardUtil.HurtEnemiesIn(pos, r, 6, DmgKind.Ice);
        BossPuddle.Spawn(pos, r, 5f, Fx.FrostGround(), new Color(1f, 1f, 1f, 0.85f), 0.35f, "RELLISCA!", false, "neu");
    }
}

// ---------- LA CAMBRA: el fred (si et quedes quiet, et glaces) ----------
public class ColdAirHazard : MonoBehaviour
{
    float still;
    Vector2 last;
    float nextFlake;

    void Update()
    {
        Transform p = PlayerRef.Transform;
        if (p == null || Time.timeScale <= 0f) return;
        // la neu que cau per tota la sala
        if (Time.time >= nextFlake)
        {
            nextFlake = Time.time + 0.08f;
            Vector2 cam = Camera.main != null ? (Vector2)Camera.main.transform.position : (Vector2)p.position;
            Fx.Snowflake(cam + new Vector2(Random.Range(-11f, 11f), Random.Range(-6f, 7f)));
        }
        float moved = Vector2.Distance(p.position, last);
        last = p.position;
        still = moved < 0.01f ? still + Time.deltaTime : Mathf.Max(0f, still - Time.deltaTime * 3f);
        if (still > 1.2f && Random.value < Time.deltaTime * 3f) Fx.Snowflake((Vector2)p.position + Random.insideUnitCircle * 0.6f + Vector2.up * 0.6f);
        if (still > 2.2f)
        {
            still = 0f;
            PlayerSlow.Apply(0.55f, 1.6f, "BRRR! MOU-TE!");
            Fx.FrostBurst(p.position, 0.8f);
        }
    }
}

// ---------- EL PLATÓ: el focus EN DIRECTE ----------
// un cercle de llum que es passeja per la sala: els clients que hi són a dins reben un 50% més de mal
public class LiveSpotHazard : MonoBehaviour
{
    const float R = 2.2f;
    SpriteRenderer spot;
    Vector2 target;
    float nextTarget, nextText;
    readonly HashSet<Enemy> boosted = new HashSet<Enemy>();

    void Start()
    {
        spot = HazardFx.MakeCircle(HazardManager.ArenaCenter, R, new Color(1f, 0.95f, 0.6f, 0.22f), -2);
        spot.transform.SetParent(transform, true);
        target = HazardFx.RandomPoint(3f);
    }

    void Update()
    {
        if (spot == null) return;
        if (Time.time >= nextTarget) { nextTarget = Time.time + Random.Range(3f, 5f); target = HazardFx.RandomPoint(3f); }
        Vector2 pos = Vector2.MoveTowards(spot.transform.position, target, 2.2f * Time.deltaTime);
        spot.transform.position = pos;
        spot.color = new Color(1f, 0.95f, 0.6f, 0.2f + 0.05f * Mathf.Sin(Time.time * 4f));
        if (Random.value < Time.deltaTime * 5f) Fx.Sparkle(pos + Random.insideUnitCircle * R, new Color(1f, 0.95f, 0.6f));

        // els clients a dins: més mal (i tornen al normal quan surten)
        HashSet<Enemy> now = new HashSet<Enemy>();
        foreach (Enemy e in Enemy.All)
            if (e != null && !e.IsDead && !e.isBoss && Vector2.Distance(e.transform.position, pos) <= R) now.Add(e);
        foreach (Enemy e in boosted) if (e != null && !now.Contains(e)) e.damageMultiplier = 1f;
        foreach (Enemy e in now) e.damageMultiplier = 1.5f;
        boosted.Clear();
        foreach (Enemy e in now) boosted.Add(e);
        if (now.Count > 0 && Time.time >= nextText)
        {
            nextText = Time.time + 3f;
            HazardFx.Popup(pos + Vector2.up * 1.6f, "EN DIRECTE! +50% MAL", new Color(1f, 0.9f, 0.4f), 0.9f);
        }
    }

    void OnDestroy()
    {
        foreach (Enemy e in boosted) if (e != null) e.damageMultiplier = 1f;
    }
}

// ---------- EL PLATÓ: el públic llença tomàquets ----------
public class TomatoHazard : MonoBehaviour
{
    static Sprite tomato, splat;

    IEnumerator Start()
    {
        if (tomato == null) tomato = Resources.Load<Sprite>("Decor/Tomaquet");
        if (splat == null) splat = Resources.Load<Sprite>("Decor/Esquitx");
        yield return new WaitForSeconds(5f);
        while (true)
        {
            HazardFx.Popup(RoomHazardUtil.PlayerPos + Vector2.up * 2f, "BUUUU!", new Color(1f, 0.5f, 0.4f), 0.9f);
            AudioManager.Play("voice_1", 0.8f, 0.6f);
            int n = Random.Range(3, 6);
            for (int i = 0; i < n; i++)
            {
                Vector2 to = RoomHazardUtil.PlayerPos + Random.insideUnitCircle * (i == 0 ? 0.3f : 3f);
                StartCoroutine(Throw(to));
                yield return new WaitForSeconds(0.15f);
            }
            yield return new WaitForSeconds(RoomHazardUtil.Pace(8f, 5f));
        }
    }

    IEnumerator Throw(Vector2 to)
    {
        const float r = 0.9f;
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f;
        Vector2 from = new Vector2(to.x + Random.Range(-3f, 3f), c.y - half.y - 1f); // des de les butaques (a baix)
        SpriteRenderer shadow = HazardFx.MakeCircle(to, r, new Color(0.8f, 0.2f, 0.15f, 0.1f), -2);
        shadow.transform.SetParent(transform, true);
        GameObject go = new GameObject("Tomaquet");
        go.transform.SetParent(transform, true);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = tomato;
        sr.sortingOrder = 22;
        go.transform.localScale = Vector3.one * 0.8f;
        const float time = 0.9f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (go == null || shadow == null) yield break;
            float k = t / time;
            go.transform.position = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 3.5f;
            go.transform.Rotate(0f, 0f, 500f * Time.deltaTime);
            shadow.color = new Color(0.8f, 0.2f, 0.15f, Mathf.Lerp(0.1f, 0.4f, k));
            yield return null;
        }
        if (shadow != null) Destroy(shadow.gameObject);
        if (go != null) Destroy(go);
        AudioManager.Play("hit", 0.6f, 0.7f);
        BossBrain.HurtPlayerIn(to, r);
        RoomHazardUtil.HurtEnemiesIn(to, r, 3, DmgKind.None);
        BossPuddle.Spawn(to, r, 5f, splat, Color.white, 0.3f, "TOMÀQUET!");
    }
}

// ---------- EL PLATÓ: focus del sostre que cauen ----------
public class FallingLightHazard : MonoBehaviour
{
    static Sprite lamp;

    IEnumerator Start()
    {
        if (lamp == null) lamp = Resources.Load<Sprite>("Decor/Focus");
        yield return new WaitForSeconds(7f);
        while (true)
        {
            Vector2 p = Random.value < 0.5f ? RoomHazardUtil.PlayerPos : HazardFx.RandomPoint(2f);
            StartCoroutine(Drop(p));
            yield return new WaitForSeconds(RoomHazardUtil.Pace(7f, 4.5f));
        }
    }

    IEnumerator Drop(Vector2 pos)
    {
        const float r = 1.5f;
        SpriteRenderer warn = HazardFx.MakeCircle(pos, r, new Color(1f, 0.9f, 0.4f, 0.12f), -2);
        warn.transform.SetParent(transform, true);
        GameObject go = new GameObject("FocusQueCau");
        go.transform.SetParent(transform, true);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = lamp;
        sr.sortingOrder = 22;
        go.transform.localScale = Vector3.one * 1.5f;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            if (go == null || warn == null) yield break;
            float k = t / 1.2f;
            // primer es gronxa penjat, i després cau
            float y = k < 0.5f ? 8f : Mathf.Lerp(8f, 0.3f, (k - 0.5f) / 0.5f * ((k - 0.5f) / 0.5f));
            go.transform.position = pos + Vector2.up * y;
            go.transform.rotation = Quaternion.Euler(0f, 0f, k < 0.5f ? Mathf.Sin(t * 25f) * 12f : 0f);
            warn.color = new Color(1f, 0.9f, 0.4f, Mathf.Lerp(0.12f, 0.45f, k) * (0.8f + 0.2f * Mathf.Sin(t * 30f)));
            yield return null;
        }
        if (warn != null) Destroy(warn.gameObject);
        if (go != null) Destroy(go);
        Fx.Glass(pos, 10);
        Fx.Shockwave(pos, r, new Color(1f, 0.95f, 0.6f));
        Fx.Sparkles(pos, new Color(1f, 0.9f, 0.4f), 10, r);
        HazardFx.Shake(0.2f, 0.2f);
        AudioManager.Play("box_break", 0.8f, 0.7f);
        BossBrain.HurtPlayerIn(pos, r);
        RoomHazardUtil.HurtEnemiesIn(pos, r, 8, DmgKind.Explosion, 6f);
    }
}

// ---------- ELS CLIENTS DE CADA SALA NOVA ----------
//  EL CELLER  van alegrois: de tant en tant fan "HIC!" i, en caure, vessen vi a terra
//  LA CAMBRA  van mig glaçats (més lents i més durs) i, en caure, es trenquen i glacen els del costat
//  EL PLATÓ   alguns són estrelles convidades (daurats): en caure, "BRAVO!" i el doble de punts
public class RoomClient : MonoBehaviour
{
    int room;
    Enemy enemy;
    float nextHic;

    public static void Apply(Enemy e, int room)
    {
        if (e == null || e.isBoss || room < 5 || room > 7) return;
        if (room == 7 && Random.value > 0.3f) return; // al plató, només algunes estrelles
        RoomClient rc = e.gameObject.AddComponent<RoomClient>();
        rc.room = room;
        rc.enemy = e;
        switch (room)
        {
            case 6:
                if (e.BaseTint == Color.white) e.SetTint(new Color(0.75f, 0.9f, 1f)); // els clients especials mantenen el seu color
                e.speed *= 0.85f;
                e.OverrideStats(Mathf.RoundToInt(e.maxHealth * 1.2f), e.speed);
                break;
            case 7:
                if (e.BaseTint == Color.white) e.SetTint(new Color(1f, 0.88f, 0.45f));
                e.points *= 2;
                break;
        }
    }

    void Start()
    {
        Enemy.OnKilled += OnKilled;
        nextHic = Time.time + Random.Range(2f, 6f);
    }

    void OnDestroy() { Enemy.OnKilled -= OnKilled; }

    void Update()
    {
        if (room != 5 || enemy == null || enemy.IsDead || Time.time < nextHic) return;
        nextHic = Time.time + Random.Range(4f, 8f);
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.1f, "HIC!", new Color(0.9f, 0.5f, 0.6f), 0.7f);
        Squash.Kick(gameObject, 0.2f, -0.15f);
    }

    void OnKilled(Enemy e)
    {
        if (e != enemy) return;
        Vector2 p = transform.position;
        switch (room)
        {
            case 5:
                if (Random.value < 0.5f)
                    BossPuddle.Spawn(p, 0.9f, 5f, Fx.OilPuddle(), new Color(0.75f, 0.15f, 0.3f, 0.85f), 0.3f, "VI!");
                break;
            case 6:
                Fx.FrostBurst(p, 1.2f);
                FrostZone.Spawn(p, 1.4f, 0.5f, 3f); // glaça (frena) els clients del costat
                break;
            case 7:
                HazardFx.Popup(p + Vector2.up * 1.2f, "BRAVO!", new Color(1f, 0.85f, 0.3f), 0.9f);
                Fx.Sparkles(p, new Color(1f, 0.85f, 0.3f), 8, 0.8f);
                break;
        }
    }
}
