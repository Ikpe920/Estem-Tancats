using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  PLATS NOUS I PLATS ESTRELLA
// =====================================================================
// Especialitats noves (com les armes de Vampire Survivors): cada una és un component al jugador.
//   Pernil tallat a ganivet  (PlayerShooting.backShots)  ganivets cap enrere
//   Sopa de galets           SoupTrail                   tolls que fan mal
//   Pastís de nata           PieRain                     pastissos que cauen del cel
//   Cava                     CavaPop                     esclat de bombolles al teu voltant
//   Escalivada               Flamethrower                flames davant teu
//   Bombes de la Barceloneta BombDropper                 bombes al terra
//   Fuet                     FuetWhip                    cop de fuet als costats
//   Truita de patates        TortillaThrower             truites que van i tornen
// Complements nous: Regen (RegenHeal), Area (DishArea.Mul) i els altres són números de
// PlayerShooting / PlayerHealth.
//
// PLATS ESTRELLA: una especialitat al nivell màxim + el seu complement (a qualsevol nivell) fan sortir
// una carta daurada que la transforma. Evo.Has(EvoId.X) diu si està activa.

public enum EvoId
{
    None,
    PaellaGegant, Calcotada, OlivesAnxova, Granissat, BunyolsEmporda, MandonguillesGegants,
    CastanyadaEterna, CocaSantJoan, RomescoTarragona, PernilDeGla, EscudellaNadal, PastisCasament,
    CavaCapDAny, EscalivadaCaliu, TracaBombes, FuetVic, TruitaIaia,
    PollastreSafra, BotifarraFestaMajor
}

public static class Evo
{
    public class Def
    {
        public EvoId id;
        public string name, desc;
        public BoonType baseType, partner;
    }

    public static readonly Def[] All =
    {
        D(EvoId.PaellaGegant, "Paella gegant", "Cada 3 trets, un cercle sencer de plats al teu voltant", BoonType.MultiShot, BoonType.FireRate),
        D(EvoId.Calcotada, "Calçotada", "Les explosions en fan esclatar d'altres, en cadena", BoonType.Explosive, BoonType.Area),
        D(EvoId.OlivesAnxova, "Olives farcides d'anxova", "Les olives fan el triple de mal, giren més de pressa i més lluny", BoonType.Orbit, BoonType.Damage),
        D(EvoId.Granissat, "Granissat", "Els trets glacen del tot els clients un moment", BoonType.SlowOnHit, BoonType.SlowEnemies),
        D(EvoId.BunyolsEmporda, "Bunyols de l'Empordà", "Els trets travessen tots els clients i fan +1 de mal", BoonType.Pierce, BoonType.ProjectileSpeed),
        D(EvoId.MandonguillesGegants, "Mandonguilles gegants", "+3 rebots i projectils molt més grans", BoonType.Ricochet, BoonType.ProjectileSize),
        D(EvoId.CastanyadaEterna, "Castanyada eterna", "Llences panellets en cercle sense parar, cada 2,5 s", BoonType.DashRing, BoonType.DashCooldown),
        D(EvoId.CocaSantJoan, "Coca de Sant Joan", "Tots els clients esclaten en morir, i més fort", BoonType.KillBurst, BoonType.Crit),
        D(EvoId.RomescoTarragona, "Romesco de Tarragona", "L'ona surt sola cada 5 s, encara que no et toquin", BoonType.HitWave, BoonType.Invulnerability),
        D(EvoId.PernilDeGla, "Pernil de gla", "Els ganivets surten cap enrere i cap als costats", BoonType.BackKnives, BoonType.CritDamage),
        D(EvoId.EscudellaNadal, "Escudella de Nadal", "Els tolls de sopa són molt més grans i duren més", BoonType.Soup, BoonType.Regen),
        D(EvoId.PastisCasament, "Pastís de casament", "Cauen tres pastissos alhora", BoonType.PieRain, BoonType.Rerolls),
        D(EvoId.CavaCapDAny, "Cava de Cap d'Any", "Les bombolles esclaten el doble de sovint i més lluny", BoonType.Cava, BoonType.Knockback),
        D(EvoId.EscalivadaCaliu, "Escalivada al caliu", "Les flames arriben molt més lluny i són més amples", BoonType.Flame, BoonType.MoveSpeed),
        D(EvoId.TracaBombes, "Traca de bombes", "Deixes tres bombes cada vegada", BoonType.Bombs, BoonType.Dodge),
        D(EvoId.FuetVic, "Fuet de Vic", "El fuet pica als dos costats alhora i arriba més lluny", BoonType.Whip, BoonType.MaxLives),
        D(EvoId.TruitaIaia, "Truita de la iaia", "Llences tres truites gegants alhora", BoonType.Tortilla, BoonType.MoreChoices),
        D(EvoId.PollastreSafra, "Pollastre al safrà", "El dash deixa un rastre de foc que crema els clients", BoonType.DashDamage, BoonType.ScoreBoost),
        D(EvoId.BotifarraFestaMajor, "Botifarra de festa major", "Cada cop que la Botifarra et cura, una traca fa mal al voltant (i cura 1 cor més per onada)", BoonType.HealOnKills, BoonType.FlawlessHeal),
    };

    static Def D(EvoId id, string n, string d, BoonType b, BoonType p) { return new Def { id = id, name = n, desc = d, baseType = b, partner = p }; }

    static readonly HashSet<EvoId> active = new HashSet<EvoId>();
    public static bool Has(EvoId id) { return active.Contains(id); }
    public static void Activate(EvoId id) { active.Add(id); }
    public static void ResetAll() { active.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        active.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) { active.Clear(); DishArea.Mul = 1f; }
}

// la mida de les àrees (explosions, olives, ones, tolls...): el Vermut la fa créixer
public static class DishArea
{
    public static float Mul = 1f;
}

// una mica d'ajuda per a tots els plats nous
static class DishUtil
{
    public static int Bonus { get { PlayerShooting s = PlayerRef.Shooting; return s != null ? Mathf.Max(0, s.damage - 1) : 0; } }

    // fa mal a tots els clients d'un cercle; torna quants n'ha tocat
    public static int HitCircle(Vector2 pos, float radius, int damage, float push = 0f, DmgKind kind = DmgKind.None)
    {
        int n = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            DamageKind.Hit(e, damage, kind);
            if (push > 0f && !e.IsDead) e.Knockback((Vector2)e.transform.position - pos, push, 0.15f);
            n++;
        }
        return n;
    }

    public static Enemy RandomEnemyNear(Vector2 pos, float range)
    {
        List<Enemy> list = new List<Enemy>();
        foreach (Enemy e in Enemy.All)
            if (e != null && !e.IsDead && Vector2.Distance(e.transform.position, pos) <= range) list.Add(e);
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    public static Enemy NearestEnemy(Vector2 pos, float range)
    {
        Enemy best = null; float bd = range;
        foreach (Enemy e in Enemy.All)
        {
            if (e == null || e.IsDead) continue;
            float d = Vector2.Distance(e.transform.position, pos);
            if (d < bd) { bd = d; best = e; }
        }
        return best;
    }

    public static bool Active => !PlayerHealth.IsDying && Time.timeScale > 0f;
}

// ---------- POLLASTRE AL SAFRÀ (plat estrella): el dash deixa un rastre de foc ----------
public class DashFireTrail : MonoBehaviour
{
    private PlayerMovement movement;
    private float nextDrop;

    void Awake() { movement = GetComponent<PlayerMovement>(); }

    void Update()
    {
        if (movement == null || !movement.IsDashing || Time.time < nextDrop) return;
        nextDrop = Time.time + 0.06f;
        StartCoroutine(Fire(transform.position));
    }

    IEnumerator Fire(Vector2 pos)
    {
        float radius = 0.7f * DishArea.Mul;
        SpriteRenderer sr = HazardFx.MakeCircle(pos, radius, new Color(1f, 0.55f, 0.15f, 0.4f), -2);
        for (float t = 0f; t < 1.5f; t += 0.3f)
        {
            Fx.Flames(pos, radius * 0.6f, 0.4f);
            DishUtil.HitCircle(pos, radius, 2 + DishUtil.Bonus, 0f, DmgKind.Fire);
            if (sr != null) sr.color = new Color(1f, 0.55f, 0.15f, 0.4f * (1f - t / 1.5f));
            yield return new WaitForSeconds(0.3f);
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}

// ---------- MEL I MATÓ: cures 1 cor cada tants segons ----------
public class RegenHeal : MonoBehaviour
{
    public float every = 50f;
    private float timer;
    private PlayerHealth health;

    void Awake() { health = GetComponent<PlayerHealth>(); }

    void Update()
    {
        if (health == null || health.CurrentLives >= health.maxLives) { timer = 0f; return; }
        timer += Time.deltaTime;
        if (timer < every) return;
        timer = 0f;
        health.Heal(1);
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.6f, "MEL I MATÓ! +1 cor", new Color(1f, 0.85f, 0.45f), 1.2f);
    }
}

// ---------- SOPA DE GALETS: tolls que fan mal ----------
public class SoupTrail : MonoBehaviour
{
    public int level = 1;
    private float next;
    static Sprite soup;

    void Update()
    {
        if (!DishUtil.Active || Time.time < next) return;
        next = Time.time + Mathf.Max(1.8f, 4f - 0.5f * (level - 1));
        bool evo = Evo.Has(EvoId.EscudellaNadal);
        float radius = (1.2f + 0.2f * (level - 1)) * DishArea.Mul * (evo ? 1.6f : 1f);
        StartCoroutine(Puddle(transform.position, radius, evo ? 5f : 3f));
    }

    IEnumerator Puddle(Vector2 pos, float radius, float life)
    {
        if (soup == null) soup = Resources.Load<Sprite>("DishFx/Sopa");
        SpriteRenderer sr = HazardFx.MakeCircle(pos, radius, new Color(0.95f, 0.7f, 0.3f, 0.35f), -2);
        if (soup != null)
        {
            sr.sprite = soup; // el toll de sopa de galets (1 unitat de diàmetre, com el cercle)
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
        int dmg = 1 + (level >= 3 ? 1 : 0) + DishUtil.Bonus / 2;
        bool slow = SetBonuses.Has(SetId.SopaPedra); // conjunt Sopa de pedra: els tolls frenen
        for (float t = 0f; t < life; t += 0.5f)
        {
            DishUtil.HitCircle(pos, radius, dmg, 0f, DmgKind.Fire);
            if (slow)
                foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && !e.IsDead) e.Slow(0.5f, 0.6f);
                }
            if (sr != null) sr.color = soup != null ? new Color(1f, 1f, 1f, Mathf.Clamp01((life - t) / 1f) * 0.9f)
                                                    : new Color(0.95f, 0.7f, 0.3f, 0.35f * (1f - t / life) + 0.1f);
            if (Random.value < 0.3f) Fx.Ripple(pos + Random.insideUnitCircle * radius * 0.6f, 0.6f, new Color(1f, 0.9f, 0.6f));
            if (Random.value < 0.6f) Fx.Sparkle(pos + Random.insideUnitCircle * radius * 0.8f, new Color(1f, 0.9f, 0.6f));
            yield return new WaitForSeconds(0.5f);
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}

// ---------- PASTÍS DE NATA: cau del cel sobre un client ----------
public class PieRain : MonoBehaviour
{
    public int level = 1;
    private float next;

    void Update()
    {
        if (!DishUtil.Active || Time.time < next) return;
        next = Time.time + Mathf.Max(1.4f, 3.2f - 0.45f * (level - 1));
        int n = Evo.Has(EvoId.PastisCasament) ? 3 : 1;
        for (int i = 0; i < n; i++)
        {
            Enemy target = DishUtil.RandomEnemyNear(transform.position, 10f);
            if (target == null) return;
            StartCoroutine(Drop(target.transform.position));
        }
    }

    static Sprite pie, splat;

    IEnumerator Drop(Vector2 pos)
    {
        float radius = 1.4f * DishArea.Mul;
        if (pie == null) pie = Resources.Load<Sprite>("DishFx/Pastis");
        if (splat == null) splat = Resources.Load<Sprite>("DishFx/Nata");
        SpriteRenderer shadow = HazardFx.MakeCircle(pos, radius, new Color(0f, 0f, 0f, 0.15f), -2);
        SpriteRenderer falling = null;
        if (pie != null)
        {
            falling = new GameObject("PastisQueCau").AddComponent<SpriteRenderer>();
            falling.sprite = pie;
            falling.sortingOrder = 20;
            falling.transform.localScale = Vector3.one * 1.3f * Mathf.Sqrt(DishArea.Mul);
            Destroy(falling.gameObject, 1f);
        }
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            if (shadow != null) shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.1f, 0.35f, k));
            if (falling != null) falling.transform.position = pos + Vector2.up * (7f * (1f - k * k) + 0.3f);
            yield return null;
        }
        if (shadow != null) Destroy(shadow.gameObject);
        if (falling != null) Destroy(falling.gameObject);
        if (splat != null) StartCoroutine(Splat(pos, radius));
        Fx.Shockwave(pos, radius * 0.6f, new Color(1f, 0.97f, 0.9f));
        Fx.Sparkles(pos, new Color(1f, 0.95f, 0.9f), 8, radius);
        AudioManager.Play("table", 0.6f, 1.3f);
        int dmg = 3 + level + DishUtil.Bonus;
        if (SetBonuses.Has(SetId.Berenar)) dmg *= 2; // conjunt Berenar: els pastissos fan el doble de mal
        DishUtil.HitCircle(pos, radius, dmg);
    }

    // la taca de nata que queda a terra i s'esvaeix
    IEnumerator Splat(Vector2 pos, float radius)
    {
        SpriteRenderer sr = new GameObject("TacaDeNata").AddComponent<SpriteRenderer>();
        sr.sprite = splat;
        sr.sortingOrder = -2;
        sr.transform.position = pos;
        sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        sr.transform.localScale = Vector3.one * radius * 1.8f;
        Destroy(sr.gameObject, 2.6f);
        for (float t = 0f; t < 2.5f; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(1f, 1f, 1f, t < 1.5f ? 1f : 1f - (t - 1.5f));
            yield return null;
        }
    }
}

// ---------- CAVA: esclat de bombolles al teu voltant ----------
public class CavaPop : MonoBehaviour
{
    public int level = 1;
    private float next;

    void Update()
    {
        if (!DishUtil.Active || Time.time < next) return;
        bool evo = Evo.Has(EvoId.CavaCapDAny);
        next = Time.time + Mathf.Max(2.5f, 6f - 0.8f * (level - 1)) * (evo ? 0.5f : 1f);
        float radius = 2.2f * DishArea.Mul * (evo ? 1.5f : 1f);
        Vector2 pos = transform.position;
        Fx.Shockwave(pos, radius * 0.55f, new Color(1f, 0.95f, 0.7f));
        for (int i = 0; i < 6; i++) Fx.Sparkle(pos + Random.insideUnitCircle * radius, new Color(1f, 0.97f, 0.8f));
        Fx.Bubbles(pos, radius, 14); // bombolles de cava de debò
        AudioManager.Play("pickup", 0.5f, 0.8f);
        int dmg = 2 + level + DishUtil.Bonus;
        if (SetBonuses.Has(SetId.Brindis)) dmg *= 2; // conjunt Brindis: les bombolles fan el doble de mal
        DishUtil.HitCircle(pos, radius, dmg, 9f, DmgKind.Explosion);
    }
}

// ---------- ESCALIVADA: flames davant teu ----------
public class Flamethrower : MonoBehaviour
{
    public int level = 1;
    private float next;

    void Update()
    {
        if (!DishUtil.Active) return;
        PlayerShooting s = PlayerRef.Shooting;
        Vector2 aim = s != null ? s.LastAim : Vector2.right;
        if (aim.sqrMagnitude < 0.01f) aim = Vector2.right;
        aim.Normalize();
        bool evo = Evo.Has(EvoId.EscalivadaCaliu);
        float range = (2.4f + 0.35f * (level - 1)) * DishArea.Mul * (evo ? 1.5f : 1f);
        float half = evo ? 32f : 20f;
        Vector2 pos = transform.position;
        Fx.FlameJet(pos, aim, range, half, 0.8f); // el raig de flames, a cada fotograma

        if (Time.time < next) return;
        next = Time.time + 0.35f;
        int dmg = 1 + (level - 1) / 2 + DishUtil.Bonus / 2;
        if (SetBonuses.Has(SetId.Barbacoa)) dmg *= 2; // conjunt Barbacoa: les flames fan el doble de mal
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, range))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            Vector2 to = (Vector2)e.transform.position - pos;
            if (Vector2.Angle(aim, to) > half) continue;
            DamageKind.Hit(e, dmg, DmgKind.Fire);
        }
    }
}

// ---------- BOMBES DE LA BARCELONETA: bombes al terra ----------
public class BombDropper : MonoBehaviour
{
    public int level = 1;
    private float next;
    static Sprite bombSprite;

    void Update()
    {
        if (!DishUtil.Active || Time.time < next) return;
        next = Time.time + Mathf.Max(2f, 5f - 0.8f * (level - 1));
        int n = Evo.Has(EvoId.TracaBombes) ? 3 : 1;
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = (Vector2)transform.position + (n > 1 ? Random.insideUnitCircle.normalized * 1.4f : Vector2.zero);
            StartCoroutine(Bomb(pos));
        }
    }

    IEnumerator Bomb(Vector2 pos)
    {
        if (bombSprite == null) bombSprite = Resources.Load<Sprite>("DishFx/Bomba");
        SpriteRenderer sr;
        if (bombSprite != null)
        {
            sr = new GameObject("Bomba").AddComponent<SpriteRenderer>();
            sr.sprite = bombSprite;
            sr.transform.position = pos;
            sr.transform.localScale = Vector3.one * 0.8f;
            sr.sortingOrder = 3;
        }
        else sr = HazardFx.MakeCircle(pos, 0.3f, new Color(0.25f, 0.18f, 0.15f, 1f), 3);
        float radius = 2f * DishArea.Mul;
        float armedAt = Time.time + 0.6f;
        float dieAt = Time.time + 6f;
        while (Time.time < dieAt)
        {
            if (sr != null && Time.time > armedAt)
            {
                bool on = Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
                if (bombSprite != null) sr.color = on ? new Color(1f, 0.6f, 0.55f) : Color.white;
                else sr.color = on ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.25f, 0.18f, 0.15f);
                if (bombSprite != null && Random.value < 0.15f) Fx.Sparkle(pos + new Vector2(0.27f, 0.4f), new Color(1f, 0.85f, 0.3f)); // la metxa
            }
            if (Time.time > armedAt && DishUtil.NearestEnemy(pos, 1.1f) != null) break;
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
        Fx.Explosion(pos, radius, 1.2f);
        AudioManager.Play("explosion", 0.5f);
        DishUtil.HitCircle(pos, radius, 5 + 2 * level + DishUtil.Bonus, 6f, DmgKind.Explosion);
    }
}

// ---------- FUET: cop de fuet als costats ----------
public class FuetWhip : MonoBehaviour
{
    public int level = 1;
    private float next;
    private float side = 1f;

    void Update()
    {
        if (!DishUtil.Active || Time.time < next) return;
        next = Time.time + Mathf.Max(0.7f, 1.3f - 0.15f * (level - 1));
        bool evo = Evo.Has(EvoId.FuetVic);
        if (evo) { Lash(1f); Lash(-1f); }
        else { Lash(side); side = -side; }
    }

    void Lash(float dir)
    {
        bool evo = Evo.Has(EvoId.FuetVic);
        float w = (3.2f + 0.3f * (level - 1)) * DishArea.Mul * (evo ? 1.4f : 1f);
        float h = 0.9f * Mathf.Sqrt(DishArea.Mul);
        Vector2 center = (Vector2)transform.position + new Vector2(dir * (w * 0.5f + 0.3f), 0.1f);
        StartCoroutine(Show(center, w, h, dir));
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center, new Vector2(w, h), 0f))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            bool embotits = SetBonuses.Has(SetId.Embotits); // conjunt Embotits: el fuet fa el doble de mal i empeny
            e.TakeDamage((2 + level + DishUtil.Bonus) * (embotits ? 2 : 1));
            if (embotits && !e.IsDead) e.Knockback(new Vector2(dir, 0f), 7f, 0.12f);
        }
        AudioManager.Play("dash", 0.35f, 1.6f);
    }

    static Sprite[] frames;

    // el cop de fuet de debò (Tools/SpriteGen/dish_fx.py): s'enrotlla, s'estira i peta
    IEnumerator Show(Vector2 center, float w, float h, float dir)
    {
        if (frames == null)
            frames = new[] { Resources.Load<Sprite>("DishFx/Fuet_0"), Resources.Load<Sprite>("DishFx/Fuet_1"), Resources.Load<Sprite>("DishFx/Fuet_2") };
        if (frames[0] == null) yield break;
        GameObject go = new GameObject("CopDeFuet");
        go.transform.position = center;
        go.transform.localScale = new Vector3(w * dir, Mathf.Max(1.2f, h * 2f), 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            int f = Mathf.Min(2, (int)(t / 0.07f));
            sr.sprite = frames[f] != null ? frames[f] : frames[0];
            if (t > 0.21f) sr.color = new Color(1f, 1f, 1f, 1f - (t - 0.21f) / 0.09f);
            yield return null;
        }
        if (go != null) Destroy(go);
        Fx.Hit(center + new Vector2(dir * w * 0.45f, 0f), new Vector2(-dir, 0f), false);
    }
}

// ---------- TRUITA DE PATATES: truites que van i tornen ----------
public class TortillaThrower : MonoBehaviour
{
    public int level = 1;
    private float next;
    static Sprite tortilla;

    void Update()
    {
        if (!DishUtil.Active || Time.time < next) return;
        next = Time.time + Mathf.Max(1.2f, 2.6f - 0.35f * (level - 1));
        PlayerShooting s = PlayerRef.Shooting;
        if (s == null || s.projectilePrefabs == null || s.projectilePrefabs.Length == 0) return;
        if (tortilla == null) tortilla = Resources.Load<Sprite>("Projectiles/Truita");

        Enemy target = DishUtil.NearestEnemy(transform.position, 12f);
        Vector2 dir = target != null ? ((Vector2)target.transform.position - (Vector2)transform.position).normalized : s.LastAim.normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        bool evo = Evo.Has(EvoId.TruitaIaia);
        int n = evo ? 3 : 1;
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + (i - (n - 1) / 2f) * 25f;
            Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            Projectile p = Projectile.Get(s.projectilePrefabs[0], (Vector2)transform.position + d * 0.6f, Quaternion.identity);
            if (p == null) continue;
            p.damage = 2 + level + DishUtil.Bonus;
            p.boomerang = true;
            p.SetSprite(tortilla);
            if (evo) p.transform.localScale *= 1.5f;
            p.Launch(d * s.projectileSpeed * 0.9f);
        }
    }
}
