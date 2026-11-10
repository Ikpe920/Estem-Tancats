using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// PLATS DE CONJUNT
// Quan tens dos plats que fan parella, s'activa un bonus propi que cap dels dos té per separat.
// Les parelles són les mateixes que ja fan sortir més el plat company al sorteig (Synergies del BoonManager).
// Per afegir un conjunt nou: una entrada a All, i consulta SetBonuses.Has(SetId.X) allà on calgui l'efecte.

public enum SetId
{
    FrostBlast,   // Calçots + Sorbet: les explosions deixen una zona gelada
    Shockwave,    // Calçots + Paella: les explosions empenyen els clients
    FullFan,      // Cafè + Paella: el ventall fa tot el dany
    DashRefund,   // Pollastre + Xurros: cremar amb el dash recarrega el dash
    Carajillo,    // Ratafia + Cafè: un crític et deixa tornar a disparar de seguida
    CritBlast,    // Ratafia + Calçots: les explosions crítiques són més grans
    Dart,         // Pa amb tomàquet + Bunyols: travessen 1 client més
    Aftermeal,    // Botifarra + Cafè: en curar-te per clients, tret ràpid
    BounceDamage, // Mandonguilles + Bunyols: cada rebot fa el tret +1 de dany
    SpicyOlives,  // Olives + Pollastre: les olives fan el doble de mal
    Castanyada,   // Panellets + Xurros: el dash llença 4 panellets més
    Revenge,      // Romesco + Croquetes: l'ona és el doble de gran
    Revetlla,     // Coca + Calçots: els clients sempre esclaten
    PicaPica,     // Pernil + Paella: un ganivet més cap enrere
    SopaPedra,    // Sopa + Bombes: els tolls frenen els clients
    Brindis,      // Cava + Ratafia: les bombolles fan el doble de mal
    Barbacoa,     // Escalivada + Pollastre: les flames fan el doble de mal
    Berenar,      // Pastís + Truita: els pastissos fan el doble de mal
    Embotits      // Fuet + Pernil: el fuet fa el doble de mal i empeny
}

public class SetBonus
{
    public SetId id;
    public string name;     // el nom del conjunt
    public string effect;   // què fa (curt: surt en un avís al joc)
    public BoonType a;      // un dels dos tipus de plat
    public BoonType b;      // l'altre

    public SetBonus(SetId id, string name, string effect, BoonType a, BoonType b)
    {
        this.id = id;
        this.name = name;
        this.effect = effect;
        this.a = a;
        this.b = b;
    }
}

public static class SetBonuses
{
    public static readonly SetBonus[] All =
    {
        new SetBonus(SetId.FrostBlast, "Calçots gelats", "Les explosions deixen una zona gelada", BoonType.Explosive, BoonType.SlowOnHit),
        new SetBonus(SetId.Shockwave, "Traca", "Les explosions empenyen els clients", BoonType.Explosive, BoonType.MultiShot),
        new SetBonus(SetId.FullFan, "Paella exprés", "Tots els projectils del ventall fan el dany complet", BoonType.FireRate, BoonType.MultiShot),
        new SetBonus(SetId.DashRefund, "Dash picant", "Cremar un client amb el dash recarrega'l 0,4 s", BoonType.DashDamage, BoonType.DashCooldown),
        new SetBonus(SetId.Carajillo, "Cigaló", "Un crític et deixa disparar de seguida", BoonType.Crit, BoonType.FireRate),
        new SetBonus(SetId.CritBlast, "Calçotada crítica", "Les explosions crítiques són un 60% més grans", BoonType.Crit, BoonType.Explosive),
        new SetBonus(SetId.Dart, "Bunyols exprés", "Els projectils travessen 1 client més", BoonType.ProjectileSpeed, BoonType.Pierce),
        new SetBonus(SetId.Aftermeal, "Sobretaula", "En curar-te per clients, 4 s de tret un 40% més ràpid", BoonType.HealOnKills, BoonType.FireRate),
        new SetBonus(SetId.BounceDamage, "Mandonguilles amb sípia", "Cada rebot fa el tret +1 de dany", BoonType.Ricochet, BoonType.Pierce),
        new SetBonus(SetId.SpicyOlives, "Aperitiu picant", "Les olives fan el doble de mal", BoonType.Orbit, BoonType.DashDamage),
        new SetBonus(SetId.Castanyada, "Castanyada", "El dash llença 4 panellets més", BoonType.DashRing, BoonType.DashCooldown),
        new SetBonus(SetId.Revenge, "Salsa de revenja", "L'ona de romesco és el doble de gran", BoonType.HitWave, BoonType.Invulnerability),
        new SetBonus(SetId.Revetlla, "Revetlla", "Els clients sempre esclaten en morir", BoonType.KillBurst, BoonType.Explosive),
        new SetBonus(SetId.PicaPica, "Pica-pica", "Un ganivet més cap enrere a cada tret", BoonType.BackKnives, BoonType.MultiShot),
        new SetBonus(SetId.SopaPedra, "Sopa de pedra", "Els tolls de sopa frenen els clients", BoonType.Soup, BoonType.Bombs),
        new SetBonus(SetId.Brindis, "Brindis", "Les bombolles de cava fan el doble de mal", BoonType.Cava, BoonType.Crit),
        new SetBonus(SetId.Barbacoa, "Barbacoa", "Les flames de l'escalivada fan el doble de mal", BoonType.Flame, BoonType.DashDamage),
        new SetBonus(SetId.Berenar, "Berenar", "Els pastissos fan el doble de mal", BoonType.PieRain, BoonType.Tortilla),
        new SetBonus(SetId.Embotits, "Embotits", "El fuet fa el doble de mal i empeny", BoonType.Whip, BoonType.BackKnives),
    };

    static readonly HashSet<SetId> active = new HashSet<SetId>();

    public static bool Has(SetId id) { return active.Contains(id); }
    public static void Activate(SetId id) { active.Add(id); }
    public static void ResetAll() { active.Clear(); }

    public static SetBonus Get(SetId id)
    {
        foreach (SetBonus s in All)
            if (s.id == id) return s;
        return null;
    }

    // els conjunts actius no s'arrosseguen entre partides
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        active.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        active.Clear();
    }
}

// zona gelada que deixen els Calçots gelats: frena els clients que hi trepitgen
public class FrostZone : MonoBehaviour
{
    private float radius;
    private float slowFactor;
    private float duration;
    private float born;
    private float nextTick;
    private SpriteRenderer sr;
    private Color baseColor = new Color(0.55f, 0.85f, 1f, 0.38f);

    public static void Spawn(Vector2 pos, float radius, float slowFactor, float duration)
    {
        GameObject go = new GameObject("ZonaGelada");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;

        FrostZone z = go.AddComponent<FrostZone>();
        z.radius = radius;
        z.slowFactor = slowFactor;
        z.duration = duration;
        z.born = Time.time;

        z.sr = go.AddComponent<SpriteRenderer>();
        z.sr.sprite = Fx.FrostGround(); // terra glaçat amb esquerdes
        z.baseColor = new Color(1f, 1f, 1f, 0.85f);
        z.sr.color = z.baseColor;
        Fx.FrostBurst(pos, radius * 0.6f);
        z.sr.sortingOrder = -2;
    }

    void Update()
    {
        float k = (Time.time - born) / Mathf.Max(0.1f, duration);
        if (k >= 1f) { Destroy(gameObject); return; }

        // es veu clara i s'esvaeix al final
        if (sr != null)
        {
            float fade = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            Color c = baseColor;
            c.a = baseColor.a * fade;
            sr.color = c;
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.15f;
        if (Random.value < 0.6f) Fx.Sparkle((Vector2)transform.position + Random.insideUnitCircle * radius * 0.9f, new Color(0.8f, 0.95f, 1f)); // lluentons de gel

        foreach (Collider2D h in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Enemy e = h.GetComponent<Enemy>();
            if (e == null) e = h.GetComponentInParent<Enemy>();
            if (e != null) e.Slow(slowFactor, 0.4f);
        }
    }
}
