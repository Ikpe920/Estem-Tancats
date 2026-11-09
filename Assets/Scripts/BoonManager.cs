using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public enum BoonType
{
    Damage,          // +dany a cada tret
    FireRate,        // +disparos per segon
    MoveSpeed,       // +velocitat de moviment
    MaxLives,        // +1 cor màxim
    Heal,            // cura cor
    Invulnerability,  // +temps invulnerable
    DashCooldown,    // -recàrrega del dash
    Score,           // punts immediats
    ScoreBoost,      // +% de punts
    SlowEnemies,      // els clients van més lents
    MultiShot,       // projectils extra en ventall
    Pierce,          // els projectils travessen clients
    Explosive,       // els projectils exploten
    SlowOnHit,       // els projectils frenen els clients
    DashDamage,      // el dash fa mal
    HealOnKills,     // cures un cor cada X clients
    MoreChoices,     // una carta més a triar
    None,            // sense segon efecte
    ProjectileSpeed, // projectils més ràpids
    Crit,            // probabilitat de tret crític (×2 de dany)
    FlawlessHeal,    // cures un cor en acabar una onada sense rebre cap cop
    Ricochet,        // els trets reboten a les parets
    Orbit,           // olives que giren al teu voltant i fan mal
    HitWave,         // quan et toquen, una ona empeny i fa mal als clients de prop
    DashRing,        // en fer el dash, llences projectils en cercle
    KillBurst        // els clients de vegades esclaten en morir
}

[System.Serializable]
public class Boon
{
    public string name = "Punta de gelat";        // nom del boón
    public string description = "+1 de dany";      // què fa
    public BoonType type = BoonType.Damage;
    public float amount = 1f;                      // quantitat
    public BoonType type2 = BoonType.None; // segon efecte (per als plats amb risc)
    public float amount2 = 0f;
    public Color color = new Color(0.95f, 0.75f, 0.45f); // color de la carta
    public Sprite icon;                            // opcional
    public int weight = 10;                        // probabilitat que aparegui
    public int maxStacks = 0;                      // 0 = sense limit
}

public class BoonManager : MonoBehaviour
{
    public Boon[] boons;              // la llibreria de boons
    [Tooltip("Si està activat, la llista de plats es regenera des del codi cada cop que comença l'escena (ignora la còpia guardada a l'escena). Desactiva'l si vols editar els plats a mà a l'Inspector.")]
    public bool useCodeDishes = true;
    public int optionsPerOffer = 3;   // quantes opcions s'ofereixen
    public int everyNWaves = 1;       // s'ofereix cada tantes onades

    public GameObject panel;          // el panell (si el deixes buit, es crea sol)
    public TextMeshProUGUI takenText; // opcional: llista de boons actius

    public PlayerShooting shooting;
    public PlayerMovement movement;
    public PlayerHealth health;

    private int killsPerHeal = 0;
    private int killCounter = 0;
    private int flawlessHeal = 0;

    private readonly Dictionary<Boon, int> taken = new Dictionary<Boon, int>();
    private bool picking;
    private Boon[] currentOffer;              // les cartes que s'estan oferint ara
    private Boon[] previousOffer;             // les de l'oferta anterior (surten menys)

    // parelles de plats que van bé junts: si tens un, l'altre surt més
    static readonly BoonType[][] Synergies =
    {
        new[] { BoonType.Explosive, BoonType.MultiShot },
        new[] { BoonType.Explosive, BoonType.Pierce },
        new[] { BoonType.SlowOnHit, BoonType.Explosive },
        new[] { BoonType.SlowOnHit, BoonType.MultiShot },
        new[] { BoonType.FireRate, BoonType.MultiShot },
        new[] { BoonType.DashDamage, BoonType.DashCooldown },
        new[] { BoonType.HealOnKills, BoonType.Explosive },
        new[] { BoonType.HealOnKills, BoonType.FireRate },
        new[] { BoonType.Crit, BoonType.FireRate },
        new[] { BoonType.Crit, BoonType.MultiShot },
        new[] { BoonType.Crit, BoonType.Explosive },
        new[] { BoonType.ProjectileSpeed, BoonType.Pierce },
        new[] { BoonType.Ricochet, BoonType.Pierce },
        new[] { BoonType.Orbit, BoonType.DashDamage },
        new[] { BoonType.DashRing, BoonType.DashCooldown },
        new[] { BoonType.HitWave, BoonType.Invulnerability },
        new[] { BoonType.KillBurst, BoonType.Explosive },
    };
    private Button healButton;                 // l'opció de curar (en lloc d'un plat)
    private TextMeshProUGUI healLabel;
    private TextMeshProUGUI titleText;
    private RectTransform takenStrip;          // la fila d'icones dels plats que ja tens (dalt a l'esquerra)
    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    public int healAmount = 1;     // cors que cura l'opció de curar
    public int rerollsPerOffer = 1; // quantes vegades pots canviar les cartes a cada oferta (0 = desactivat)

    private int rerollsLeft;
    private Button rerollButton;
    private TextMeshProUGUI rerollLabel;

    void Awake()
    {
        if (shooting == null) shooting = PlayerRef.Shooting;
        if (movement == null) movement = PlayerRef.Movement;
        if (health == null) health = PlayerRef.Health;

        if (useCodeDishes || boons == null || boons.Length == 0) BuildDefaultBoons();

        Enemy.speedMultiplier = 1f; // els boons de lentitud no acumulen entre partides
        SetBonuses.ResetAll();      // els conjunts tampoc
        if (panel != null) panel.SetActive(false);
        if (takenText == null) BuildTakenText();
        Enemy.OnKilled += HandleKill;
        ScoreManager.OnWaveEnded += HandleWaveEnded;
        
    }

        void OnDestroy()
    {
        Enemy.OnKilled -= HandleKill;
        ScoreManager.OnWaveEnded -= HandleWaveEnded;
    }

    // Crema catalana: onada acabada sense rebre cap cop → cures un cor
    void HandleWaveEnded(bool flawless)
    {
        if (!flawless || flawlessHeal <= 0 || health == null) return;
        if (health.CurrentLives >= health.maxLives) return;

        health.Heal(flawlessHeal);
        HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 2f, "CREMA CATALANA! +1 cor", new Color(0.98f, 0.85f, 0.45f));
    }

    void HandleKill(Enemy e)
    {
        if (killsPerHeal <= 0 || health == null) return;

        killCounter++;
        if (killCounter >= killsPerHeal)
        {
            killCounter = 0;
            health.Heal(1);

            // Sobretaula (conjunt): en curar-te per clients, uns segons de tret més ràpid
            if (SetBonuses.Has(SetId.Aftermeal) && shooting != null)
            {
                shooting.GrantHaste(4f);
                HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 2f, "SOBRETAULA! Tret ràpid", new Color(1f, 0.7f, 0.3f), 1.4f);
            }
        }
    }

    void Reset()
    {
        BuildDefaultBoons();
    }

    void BuildDefaultBoons()
    {
        boons = new Boon[]
        {
            // ---- plats que donen un buff ----
            MakeBoon("Salsa brava", "+1 de dany a cada tret", BoonType.Damage, 1f, new Color(0.95f, 0.45f, 0.35f), 12),
            MakeBoon("Cafè sol", "Disposes un 25% més ràpid", BoonType.FireRate, 0.06f, new Color(0.65f, 0.45f, 0.3f), 12),
            MakeBoon("Macarrons amb tomàquet", "Camines més ràpid", BoonType.MoveSpeed, 0.6f, new Color(0.95f, 0.6f, 0.4f), 12),
            MakeBoon("Escudella i carn d'olla", "+1 cor màxim (i el deixes ple)", BoonType.MaxLives, 1f, new Color(0.95f, 0.45f, 0.6f), 5),
            MakeBoon("Gaspatxo fresquet", "Cures 1 cor", BoonType.Heal, 1f, new Color(0.5f, 0.9f, 0.55f), 8),
            MakeBoon("Croquetes cruixents", "+0,3 s d'invulnerabilitat", BoonType.Invulnerability, 0.3f, new Color(0.9f, 0.75f, 0.45f), 8),
            MakeBoon("Xurros amb xocolata", "-0,25 s de recàrrega del dash", BoonType.DashCooldown, 0.25f, new Color(0.7f, 0.45f, 0.35f), 8),
            MakeBoon("Pa amb tomàquet", "Els projectils van més ràpids", BoonType.ProjectileSpeed, 3f, new Color(0.95f, 0.55f, 0.4f), 8, 2),
            MakeBoon("Ratafia", "Cada tret té un 15% de ser crític (×2 de dany)", BoonType.Crit, 0.15f, new Color(0.55f, 0.3f, 0.2f), 7, 3),
            MakeBoon("Oli d'oliva verge", "Els clients van un 10% més lents", BoonType.SlowEnemies, 0.1f, new Color(0.65f, 0.75f, 0.3f), 7),

            // ---- plats que canvien com es juga ----
            MakeBoon("Paella mixta", "Cada tret en llença 2 més, en ventall", BoonType.MultiShot, 2f, new Color(0.95f, 0.75f, 0.25f), 5, 1),
            MakeBoon("Bunyols de bacallà", "Els teus projectils travessen 1 client", BoonType.Pierce, 1f, new Color(0.9f, 0.85f, 0.6f), 6, 3),
            MakeBoon("Calçots amb salvitxada", "Els teus projectils exploten i fan mal als clients de prop", BoonType.Explosive, 1.5f, new Color(0.95f, 0.4f, 0.2f), 5, 1),
            MakeBoon("Sorbet de llimona", "Els teus projectils frenen els clients durant 2 s", BoonType.SlowOnHit, 0.4f, new Color(0.7f, 0.95f, 0.9f), 6, 2),
            MakeBoon("Pollastre al curri", "El dash crema els clients que travessis", BoonType.DashDamage, 2f, new Color(0.95f, 0.65f, 0.15f), 5, 3),
            MakeBoon("Botifarra amb mongetes", "Cada 12 clients que despatxis, cures 1 cor", BoonType.HealOnKills, 12f, new Color(0.75f, 0.5f, 0.4f), 5, 2),
            MakeBoon("Crema catalana", "Cures 1 cor en acabar una onada sense rebre cap cop", BoonType.FlawlessHeal, 1f, new Color(0.98f, 0.85f, 0.45f), 5, 1),
            MakeBoon("Menú de degustació", "A partir d'ara pots triar entre una carta més", BoonType.MoreChoices, 1f, new Color(0.85f, 0.7f, 0.9f), 3, 2),
            MakeBoon("Mandonguilles", "Els trets reboten 1 cop a les parets", BoonType.Ricochet, 1f, new Color(0.7f, 0.42f, 0.28f), 6, 2),
            MakeBoon("Olives farcides", "2 olives giren al teu voltant i fan mal als clients", BoonType.Orbit, 2f, new Color(0.55f, 0.7f, 0.3f), 5, 2),
            MakeBoon("Romesco", "Quan et toquen, una ona empeny i fa mal als clients de prop", BoonType.HitWave, 1f, new Color(0.9f, 0.45f, 0.25f), 5, 1),
            MakeBoon("Panellets", "En fer el dash, llences 6 panellets en cercle", BoonType.DashRing, 6f, new Color(0.92f, 0.8f, 0.55f), 5, 2),
            MakeBoon("Coca de recapte", "Els clients tenen un 30% d'esclatar en morir", BoonType.KillBurst, 1f, new Color(0.85f, 0.6f, 0.35f), 5, 1),

            // ---- plats de punts ----
            MakeBoon("Safrà", "Guanyes un 25% més de punts", BoonType.ScoreBoost, 0.25f, new Color(0.98f, 0.7f, 0.2f), 6),
            MakeBoon("Torrons de Xixona", "+150 punts ara mateix", BoonType.Score, 150f, new Color(0.9f, 0.8f, 0.6f), 5),
            MakeBoon("Menú del dia", "Cures 1 cor i +75 punts", BoonType.Heal, 1f, new Color(0.4f, 0.6f, 0.45f), 5, 0, BoonType.Score, 75f),

            // ---- plats amb risc i recompensa ----
            MakeBoon("Xili amb carn", "+2 de dany, però els clients van un 15% més ràpids", BoonType.Damage, 2f, new Color(0.85f, 0.25f, 0.2f), 6, 0, BoonType.SlowEnemies, -0.15f),
            MakeBoon("Fondue de formatge", "Dispares molt més ràpid, però camines més lent", BoonType.FireRate, 0.1f, new Color(0.98f, 0.9f, 0.5f), 6, 0, BoonType.MoveSpeed, -0.8f),
            MakeBoon("Fideuà picant", "+1 de dany, però el dash triga més a recarregar-se", BoonType.Damage, 1f, new Color(0.9f, 0.5f, 0.25f), 6, 2, BoonType.DashCooldown, -0.25f),
            MakeBoon("Calamars a la romana", "Camines més ràpid, però ets menys temps invulnerable", BoonType.MoveSpeed, 0.9f, new Color(0.95f, 0.85f, 0.5f), 5, 2, BoonType.Invulnerability, -0.3f),
        };
    }

    static Boon MakeBoon(string n, string d, BoonType t, float a, Color c, int w,
                         int maxStacks = 0, BoonType t2 = BoonType.None, float a2 = 0f)
    {
        Boon b = new Boon();
        b.name = n;
        b.description = d;
        b.type = t;
        b.amount = a;
        b.color = c;
        b.weight = w;
        b.maxStacks = maxStacks;
        b.type2 = t2;
        b.amount2 = a2;
        return b;
    }

    // clic dret sobre el component → "Regenerar plats": posa la llista nova sense tocar la resta
    [ContextMenu("Regenerar plats")]
    void RegenerateDishes()
    {
        Reset();
    }

    // ---------- OFERIR BOONS ----------

    public bool ShouldOffer(int wave)
    {
        if (boons == null || boons.Length == 0) return false;
        if (everyNWaves <= 0) return false;
        return wave % everyNWaves == 0;
    }

    // tecles per triar carta: 1-5 (fila de dalt i teclat numèric)
    static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
    static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5 };

    public IEnumerator OfferBoons()
    {
        Boon[] picked = PickBoons(optionsPerOffer);
        if (picked == null || picked.Length == 0) yield break;

        currentOffer = picked;

        BuildCards(picked);
        BuildTitle();
        BuildHealButton();
        RefreshHealButton();
        rerollsLeft = rerollsPerOffer;
        BuildRerollButton();
        RefreshRerollButton();

        panel.SetActive(true);
        bool wasPaused = Time.timeScale == 0f;
        Time.timeScale = 0f; // el joc s'atura per triar

        while (!picking) // espera que el jugador trii un boó
        {
            // es pot triar amb el ratolí o amb les tecles 1-5 (també el teclat numèric); H per curar-se
            Keyboard kb = Keyboard.current;
            if (kb != null && !picking)
            {
                // currentOffer, no "picked": si es canvien les cartes (reroll), les tecles han de triar les noves
                Boon[] offer = currentOffer != null ? currentOffer : picked;
                for (int i = 0; i < offer.Length && i < DigitKeys.Length; i++)
                {
                    if (kb[DigitKeys[i]].wasPressedThisFrame || kb[NumpadKeys[i]].wasPressedThisFrame)
                    {
                        Choose(offer[i]);
                        break;
                    }
                }

                if (!picking && kb.hKey.wasPressedThisFrame) ChooseHeal();
                if (!picking && kb.rKey.wasPressedThisFrame) TryReroll();
            }
            yield return null;
        }

        previousOffer = currentOffer;
        panel.SetActive(false);
        if (!wasPaused) Time.timeScale = 1f;
    }

    // ---------- TOPALLS DELS PLATS ----------

    // quantes còpies d'un tipus de plat es poden acumular (encara que el plat no porti topall propi)
    int MaxStacks(Boon b)
    {
        if (b.maxStacks > 0) return b.maxStacks;

        switch (b.type)
        {
            case BoonType.Damage: return 4;
            case BoonType.FireRate: return 3;
            case BoonType.MoveSpeed: return 2;
            case BoonType.Invulnerability: return 1;
            case BoonType.DashCooldown: return 3;
            case BoonType.SlowEnemies: return 2;
            case BoonType.ScoreBoost: return 4;
            case BoonType.MultiShot: return 1;
            case BoonType.Pierce: return 2;
            case BoonType.Explosive: return 1;
            case BoonType.SlowOnHit: return 2;
            case BoonType.DashDamage: return 2;
            case BoonType.HealOnKills: return 1;
            case BoonType.MoreChoices: return 1;
            case BoonType.ProjectileSpeed: return 2;
            case BoonType.Crit: return 3;
            case BoonType.FlawlessHeal: return 1;
            case BoonType.Ricochet: return 2;
            case BoonType.Orbit: return 2;
            case BoonType.HitWave: return 1;
            case BoonType.DashRing: return 2;
            case BoonType.KillBurst: return 1;
            default: return 0; // sense topall (curar, punts...)
        }
    }

    // còpies acumulades de tots els plats que fan el mateix efecte
    int TypeStacks(BoonType type)
    {
        int total = 0;
        foreach (KeyValuePair<Boon, int> kv in taken)
        {
            if (kv.Key != null && kv.Key.type == type) total += kv.Value;
        }
        return total;
    }

    // aquest plat encara es pot oferir?
    bool CanPick(Boon b)
    {
        if (b == null) return false;

        int stacks = taken.ContainsKey(b) ? taken[b] : 0;
        if (b.maxStacks > 0 && stacks >= b.maxStacks) return false; // ja el té al màxim

        int cap = MaxStacks(b);
        if (cap > 0 && TypeStacks(b.type) >= cap) return false; // el tipus ja està al màxim

        if (b.type == BoonType.Heal && health != null && health.CurrentLives >= health.maxLives) return false; // vida plena: carta morta
        if (b.type == BoonType.MaxLives && health != null && health.maxLives >= PlayerHealth.MaxLivesCap) return false;
        return true;
    }

    // ---------- REROLL (canviar les cartes de l'oferta) ----------

    public bool CanReroll()
    {
        if (boons == null || boons.Length == 0) return false;
        foreach (Boon b in boons)
        {
            if (b == null) continue;
            if (CanPick(b)) return true;
        }
        return false;
    }

    public bool RerollCards()
    {
        // intentem que les cartes noves no siguin les mateixes que ja s'estan oferint
        List<Boon> exclou = currentOffer != null ? new List<Boon>(currentOffer) : null;
        Boon[] picked = PickBoons(optionsPerOffer, exclou);
        if (picked == null || picked.Length == 0) picked = PickBoons(optionsPerOffer);
        if (picked == null || picked.Length == 0) return false;

        currentOffer = picked;
        BuildCards(picked);
        RefreshHealButton();
        RefreshRerollButton();
        return true;
    }

    // el jugador demana canviar les cartes (botó o tecla R)
    void TryReroll()
    {
        if (picking || rerollsLeft <= 0) return;
        if (!CanReroll()) return;
        if (RerollCards()) rerollsLeft--;
        RefreshRerollButton();
    }

    // pes real d'un plat al sorteig: el que ja ha sortit fa poc pesa menys, les sinergies pesen més
    float EffectiveWeight(Boon b)
    {
        float w = Mathf.Max(1, b.weight);

        if (previousOffer != null && System.Array.IndexOf(previousOffer, b) >= 0) w *= 0.25f;

        float syn = 1f;
        foreach (BoonType[] pair in Synergies)
        {
            BoonType other;
            if (pair[0] == b.type) other = pair[1];
            else if (pair[1] == b.type) other = pair[0];
            else continue;
            if (TypeStacks(other) > 0) syn *= 1.7f;
        }
        return w * Mathf.Min(syn, 2.5f);
    }

    Boon[] PickBoons(int howMany, List<Boon> exclou = null)
    {
        List<Boon> pool = new List<Boon>();
        foreach (Boon b in boons)
        {
            if (b == null) continue;
            if (!CanPick(b)) continue; // topall propi o del tipus
            if (exclou != null && exclou.Contains(b)) continue; // no repetim la mateixa carta
            pool.Add(b);
        }
        if (pool.Count == 0) return null;

        List<Boon> result = new List<Boon>();
        while (result.Count < howMany && pool.Count > 0)
        {
            float total = 0f;
            foreach (Boon b in pool) total += EffectiveWeight(b);

            float roll = Random.Range(0f, total);
            int chosen = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= EffectiveWeight(pool[i]);
                if (roll < 0f) { chosen = i; break; }
                chosen = i;
            }

            result.Add(pool[chosen]);
            pool.RemoveAt(chosen);
        }
        return result.ToArray();
    }

    void Choose(Boon boon)
    {
        if (picking) return;
        picking = true;

        taken[boon] = (taken.ContainsKey(boon) ? taken[boon] : 0) + 1;
        Apply(boon);
        CheckSets();
        RefreshTakenText();
        Debug.Log("Plats: " + boon.name + " (" + boon.description + ")");
    }

    // ---------- PLATS DE CONJUNT ----------

    // activa els conjunts que ara es completen (tens un plat de cada tipus de la parella)
    void CheckSets()
    {
        foreach (SetBonus s in SetBonuses.All)
        {
            if (SetBonuses.Has(s.id)) continue;
            if (TypeStacks(s.a) > 0 && TypeStacks(s.b) > 0) ActivateSet(s);
        }
    }

    void ActivateSet(SetBonus s)
    {
        SetBonuses.Activate(s.id);
        Progression.OnSetCompleted();

        // efectes que es posen una sola vegada, en activar-se
        if (s.id == SetId.Dart && shooting != null) shooting.pierce += 1;

        // la resta d'efectes els consulten directament Projectile, PlayerShooting, DashDamage...
        if (health != null)
        {
            HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 2.4f,
                "CONJUNT! " + s.name + "\n<size=65%>" + s.effect + "</size>",
                new Color(1f, 0.82f, 0.3f), 2.4f);
        }
        Debug.Log("Conjunt: " + s.name + " (" + s.effect + ")");
    }

    // el conjunt que completaria aquest plat si el triessis ara (o null)
    SetBonus SetCompletedBy(Boon b)
    {
        if (b == null) return null;
        foreach (SetBonus s in SetBonuses.All)
        {
            if (SetBonuses.Has(s.id)) continue;

            BoonType other;
            if (s.a == b.type) other = s.b;
            else if (s.b == b.type) other = s.a;
            else continue;

            if (TypeStacks(other) > 0) return s;
        }
        return null;
    }

    // ---------- MODE DE PROVA ----------

    public Boon[] Library { get { return boons; } }

    public int StacksOf(Boon b)
    {
        return b != null && taken.ContainsKey(b) ? taken[b] : 0;
    }

    // dona un plat sense topall (el mode de prova)
    public void DebugGive(Boon b)
    {
        if (b == null) return;
        taken[b] = StacksOf(b) + 1;
        Apply(b);
        CheckSets();
        RefreshTakenText();
    }

    // obre ara la pantalla de triar plat
    public bool DebugOffer()
    {
        if (panel != null && panel.activeSelf) return false;
        if (Time.timeScale == 0f) return false;
        StartCoroutine(OfferBoons());
        return true;
    }

    void Apply(Boon boon)
    {
        ApplyEffect(boon.type, boon.amount);
        if (boon.type2 != BoonType.None) ApplyEffect(boon.type2, boon.amount2);
    }

    void ApplyEffect(BoonType type, float amount)
    {
        switch (type)
        {
            case BoonType.Damage:
                if (shooting != null) shooting.damage = Mathf.Max(1, shooting.damage + Mathf.RoundToInt(amount));
                break;

            case BoonType.FireRate:
                if (shooting != null) shooting.fireRate = Mathf.Clamp(shooting.fireRate - amount, 0.10f, 1f);
                break;

            case BoonType.MoveSpeed:
                if (movement != null) movement.speed = Mathf.Max(2f, movement.speed + amount);
                break;

            case BoonType.MaxLives:
                if (health != null)
                {
                    int add = Mathf.Min(Mathf.RoundToInt(amount), PlayerHealth.MaxLivesCap - health.maxLives);
                    if (add > 0)
                    {
                        health.maxLives += add;
                        health.Heal(add);
                    }
                }
                break;

            case BoonType.Heal:
                if (health != null) health.Heal(Mathf.RoundToInt(amount));
                break;

            case BoonType.Invulnerability:
                if (health != null) health.invulnerabilityTime = Mathf.Max(0.4f, health.invulnerabilityTime + amount);
                break;

            case BoonType.DashCooldown:
                if (movement != null) movement.dashCooldown = Mathf.Max(0.15f, movement.dashCooldown - amount);
                break;

            case BoonType.Score:
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(Mathf.RoundToInt(amount));
                break;

            case BoonType.ScoreBoost:
                if (ScoreManager.Instance != null) ScoreManager.Instance.scoreMultiplier += amount;
                break;

            case BoonType.SlowEnemies:
                Enemy.speedMultiplier *= (1f - amount); // amb amount negatiu, els clients van més ràpid
                break;

            case BoonType.MultiShot:
                if (shooting != null) shooting.extraShots += Mathf.RoundToInt(amount);
                break;

            case BoonType.Pierce:
                if (shooting != null) shooting.pierce += Mathf.RoundToInt(amount);
                break;

            case BoonType.Explosive:
                if (shooting != null) shooting.explosionRadius += amount;
                break;

            case BoonType.SlowOnHit:
                if (shooting != null)
                {
                    // cada còpia frena més (0,6 → 0,36) i dura més (2 s → 3 s)
                    shooting.slowFactor = Mathf.Max(0.3f, shooting.slowFactor * (1f - amount));
                    shooting.slowDuration = shooting.slowDuration <= 0f ? 2f : shooting.slowDuration + 1f;
                }
                break;

            case BoonType.DashDamage:
                if (movement != null)
                {
                    DashDamage dd = movement.GetComponent<DashDamage>();
                    if (dd == null) dd = movement.gameObject.AddComponent<DashDamage>();
                    dd.damage += Mathf.RoundToInt(amount);
                }
                break;

            case BoonType.HealOnKills:
                killsPerHeal = killsPerHeal == 0 ? Mathf.RoundToInt(amount) : Mathf.Max(5, killsPerHeal - 3);
                break;

            case BoonType.MoreChoices:
                optionsPerOffer = Mathf.Min(5, optionsPerOffer + Mathf.RoundToInt(amount));
                break;

            case BoonType.ProjectileSpeed:
                if (shooting != null) shooting.projectileSpeed += amount;
                break;

            case BoonType.Crit:
                if (shooting != null) shooting.critChance = Mathf.Min(0.75f, shooting.critChance + amount);
                break;

            case BoonType.FlawlessHeal:
                flawlessHeal += Mathf.RoundToInt(amount);
                break;

            case BoonType.Ricochet:
                if (shooting != null) shooting.bounces += Mathf.RoundToInt(amount);
                break;

            case BoonType.Orbit:
                if (movement != null) Ensure<OrbitingOlives>(movement.gameObject).AddOlives(Mathf.RoundToInt(amount));
                break;

            case BoonType.HitWave:
                if (health != null) Ensure<RomescoWave>(health.gameObject);
                break;

            case BoonType.DashRing:
                if (movement != null)
                {
                    PanelletsRing ring = Ensure<PanelletsRing>(movement.gameObject);
                    ring.count += ring.count == 0 ? Mathf.RoundToInt(amount) : 3; // la segona còpia: +3
                }
                break;

            case BoonType.KillBurst:
                if (movement != null) Ensure<KillBurst>(movement.gameObject);
                break;
        }
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    void RefreshTakenText()
    {
        // si algú ha cablejat un text a mà, el fem servir com abans
        if (takenText != null)
        {
            string s = "Plats: ";
            bool first = true;
            foreach (Boon b in taken.Keys)
            {
                if (!first) s += " · ";
                s += b.name + (taken[b] > 1 ? " x" + taken[b] : "");
                first = false;
            }
            takenText.text = s;
            return;
        }

        RefreshTakenStrip();
    }

    // ---------- LA INTERFÍCIE (es crea sola si no la cableges) ----------
    //
    // Estil: targetes de carta de restaurant en pixel art. Marc fosc gruixut, paper cru,
    // capçalera de tovalles de quadres amb el plat dibuixat a sobre, i la tecla per triar-lo.
    // Les icones dels plats són a Assets/Resources/Dishes (una per plat, pel nom).

    static readonly Color Frame = new Color(0.16f, 0.11f, 0.13f, 1f);   // marc fosc
    static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f, 1f);   // paper de la carta
    static readonly Color Ink = new Color(0.24f, 0.15f, 0.12f, 1f);     // tinta del nom
    static readonly Color InkSoft = new Color(0.45f, 0.33f, 0.26f, 1f); // tinta de la descripció
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);

    // "píxel" de la interfície: marcs i detalls es dibuixen amb múltiples d'aquest valor
    float PixelUnit()
    {
        return Mathf.Max(2f, Mathf.Round(Mathf.Max(240f, Screen.height) / 270f));
    }

    // ---- petites eines per muntar la interfície ----

    RectTransform MakeRect(Transform parent, string objName, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin;
        r.anchorMax = aMax;
        r.offsetMin = oMin;
        r.offsetMax = oMax;
        return r;
    }

    // rectangle de mida fixa, ancorat a un punt
    RectTransform MakeFixed(Transform parent, string objName, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }

    Image Box(Transform parent, string objName, Color col, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        RectTransform r = MakeRect(parent, objName, aMin, aMax, oMin, oMax);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return img;
    }

    Image FixedBox(Transform parent, string objName, Color col, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform r = MakeFixed(parent, objName, anchor, pivot, pos, size);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return img;
    }

    // text que omple el seu pare
    TextMeshProUGUI Label(Transform parent, string text, float size, TextAlignmentOptions align, Color color, bool bold, bool autoSize)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        if (autoSize)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = Mathf.Max(8f, size * 0.6f);
        }
        return tmp;
    }

    // ---- icones dels plats ----

    // "Escudella i carn d'olla" -> "escudella_i_carn_d_olla" (el nom del fitxer a Resources/Dishes)
    static string Slug(string name)
    {
        string d = (name ?? "").Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char ch in d.ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }
        return sb.ToString().Trim('_');
    }

    // la icona del plat: la que tingui el boon, i si no, la de Resources/Dishes amb el seu nom
    Sprite GetIcon(Boon boon)
    {
        if (boon.icon != null) return boon.icon;

        Sprite s;
        if (iconCache.TryGetValue(boon.name, out s)) return s;

        s = Resources.Load<Sprite>("Dishes/" + Slug(boon.name));
        iconCache[boon.name] = s;
        return s;
    }

    // etiqueta de la carta segons el tipus de plat
    // ---------- DESCRIPCIONS AMB L'EFECTE REAL ("Dany 2 → 3") ----------

    static string F(float v, int decimals)
    {
        return v.ToString(decimals == 0 ? "0" : (decimals == 1 ? "0.0" : "0.00"), CultureInfo.InvariantCulture).Replace('.', ',');
    }

    string DescribeBoon(Boon b)
    {
        string a = DescribeEffect(b.type, b.amount);
        if (b.type2 == BoonType.None) return a ?? b.description;

        string c = DescribeEffect(b.type2, b.amount2);
        if (a == null || c == null) return b.description;
        return a + "\n" + c;
    }

    // torna null si no es pot calcular (llavors es fa servir la descripció escrita)
    string DescribeEffect(BoonType type, float amount)
    {
        int r = Mathf.RoundToInt(amount);
        switch (type)
        {
            case BoonType.Damage:
                if (shooting == null) return null;
                return "Dany " + shooting.damage + " → " + Mathf.Max(1, shooting.damage + r);

            case BoonType.FireRate:
                if (shooting == null) return null;
                return "Trets per segon " + F(1f / shooting.fireRate, 1) + " → "
                    + F(1f / Mathf.Clamp(shooting.fireRate - amount, 0.06f, 1f), 1);

            case BoonType.MoveSpeed:
                if (movement == null) return null;
                return "Velocitat " + F(movement.speed, 1) + " → " + F(Mathf.Max(2f, movement.speed + amount), 1);

            case BoonType.MaxLives:
                if (health == null) return null;
                return "Cors màxims " + health.maxLives + " → " + Mathf.Min(PlayerHealth.MaxLivesCap, health.maxLives + r) + " (i ple)";

            case BoonType.Heal:
                return "Cures " + r + (r > 1 ? " cors" : " cor");

            case BoonType.Invulnerability:
                if (health == null) return null;
                return "Invulnerable " + F(health.invulnerabilityTime, 1) + " s → " + F(Mathf.Max(0.4f, health.invulnerabilityTime + amount), 1) + " s";

            case BoonType.DashCooldown:
                if (movement == null) return null;
                return "Recàrrega del dash " + F(movement.dashCooldown, 2) + " s → " + F(Mathf.Max(0.15f, movement.dashCooldown - amount), 2) + " s";

            case BoonType.Score:
                return "+" + r + " punts";

            case BoonType.ScoreBoost:
                if (ScoreManager.Instance == null) return null;
                return "Punts ×" + F(ScoreManager.Instance.scoreMultiplier, 2) + " → ×" + F(ScoreManager.Instance.scoreMultiplier + amount, 2);

            case BoonType.SlowEnemies:
            {
                float cur = Enemy.speedMultiplier * 100f;
                return "Velocitat dels clients " + Mathf.RoundToInt(cur) + "% → " + Mathf.RoundToInt(cur * (1f - amount)) + "%";
            }

            case BoonType.MultiShot:
                if (shooting == null) return null;
                return "Projectils per tret " + (1 + shooting.extraShots) + " → " + (1 + shooting.extraShots + r);

            case BoonType.Pierce:
                if (shooting == null) return null;
                return "Travessen " + shooting.pierce + " → " + (shooting.pierce + r) + " clients";

            case BoonType.Explosive:
                if (shooting == null) return null;
                return shooting.explosionRadius <= 0f
                    ? "Explosió de radi " + F(amount, 1)
                    : "Radi de l'explosió " + F(shooting.explosionRadius, 1) + " → " + F(shooting.explosionRadius + amount, 1);

            case BoonType.SlowOnHit:
            {
                if (shooting == null) return null;
                float newFactor = Mathf.Max(0.3f, shooting.slowFactor * (1f - amount));
                float newDur = shooting.slowDuration <= 0f ? 2f : shooting.slowDuration + 1f;
                return "Frenada " + Mathf.RoundToInt((1f - shooting.slowFactor) * 100f) + "% → " + Mathf.RoundToInt((1f - newFactor) * 100f) + "% (" + F(newDur, 0) + " s)";
            }

            case BoonType.DashDamage:
            {
                if (movement == null) return null;
                DashDamage dd = movement.GetComponent<DashDamage>();
                int cur = dd != null ? dd.damage : 0;
                return "Dany del dash " + cur + " → " + (cur + r);
            }

            case BoonType.HealOnKills:
                return killsPerHeal == 0
                    ? "Cures 1 cor cada " + r + " clients"
                    : "1 cor cada " + killsPerHeal + " → " + Mathf.Max(5, killsPerHeal - 3) + " clients";

            case BoonType.ProjectileSpeed:
                if (shooting == null) return null;
                return "Velocitat dels projectils " + F(shooting.projectileSpeed, 0) + " → " + F(shooting.projectileSpeed + amount, 0);

            case BoonType.Crit:
                if (shooting == null) return null;
                return "Crítics " + Mathf.RoundToInt(shooting.critChance * 100f) + "% → " + Mathf.RoundToInt(Mathf.Min(0.75f, shooting.critChance + amount) * 100f) + "% (×2 de dany)";

            case BoonType.FlawlessHeal:
                return "Cures " + Mathf.Max(1, r) + " cor en acabar una onada sense cops";

            case BoonType.MoreChoices:
                return "Cartes per oferta " + optionsPerOffer + " → " + Mathf.Min(5, optionsPerOffer + r);

            case BoonType.Ricochet:
                if (shooting == null) return null;
                return "Rebots a les parets " + shooting.bounces + " → " + (shooting.bounces + r);

            case BoonType.Orbit:
            {
                OrbitingOlives o = movement != null ? movement.GetComponent<OrbitingOlives>() : null;
                int cur = o != null ? o.count : 0;
                return "Olives que giren " + cur + " → " + (cur + r);
            }

            case BoonType.HitWave:
                return "En rebre un cop: ona que empeny i fa 2 de mal";

            case BoonType.DashRing:
            {
                PanelletsRing p = movement != null ? movement.GetComponent<PanelletsRing>() : null;
                int cur = p != null ? p.count : 0;
                return "Panellets per dash " + cur + " → " + (cur == 0 ? r : cur + 3);
            }

            case BoonType.KillBurst:
                return "30% d'esclatar en morir (fa el teu dany al voltant)";
        }
        return null;
    }

    static string TagFor(Boon boon, out Color col)
    {
        if (boon.type2 != BoonType.None && boon.amount2 < 0f) // el segon efecte és dolent
        {
            col = new Color(0.72f, 0.16f, 0.14f);
            return "AMB RISC";
        }

        switch (boon.type)
        {
            case BoonType.MultiShot:
            case BoonType.Pierce:
            case BoonType.Explosive:
            case BoonType.SlowOnHit:
            case BoonType.DashDamage:
            case BoonType.HealOnKills:
            case BoonType.MoreChoices:
            case BoonType.Crit:
            case BoonType.FlawlessHeal:
            case BoonType.Ricochet:
            case BoonType.Orbit:
            case BoonType.HitWave:
            case BoonType.DashRing:
            case BoonType.KillBurst:
                col = new Color(0.42f, 0.26f, 0.64f);
                return "ESPECIALITAT";
        }

        col = new Color(0.17f, 0.45f, 0.30f);
        return "DEL DIA";
    }

    // tovalles de quadres amb el color del plat (textura petita, es veu amb píxels grossos)
    Texture2D MakeGingham(Color baseCol, int tw, int th)
    {
        Color mid = new Color(baseCol.r * 0.88f, baseCol.g * 0.88f, baseCol.b * 0.88f, 1f);
        Color light = Color.Lerp(mid, Color.white, 0.38f);
        Color deep = new Color(mid.r * 0.72f, mid.g * 0.72f, mid.b * 0.72f, 1f);

        Texture2D tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] px = new Color[tw * th];
        for (int y = 0; y < th; y++)
        {
            for (int x = 0; x < tw; x++)
            {
                bool sx = ((x / 2) % 2) == 0;
                bool sy = ((y / 2) % 2) == 0;
                px[y * tw + x] = (sx && sy) ? deep : (sx || sy) ? mid : light;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ---- les cartes ----

    void BuildCards(Boon[] picked)
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;

        // esborra les cartes de l'oferta anterior
        for (int i = panel.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = panel.transform.GetChild(i).gameObject;
            if (child.name.StartsWith("Carta"))
            {
                child.SetActive(false); // així no es veu un parpelleig d'un fotograma
                Destroy(child);
            }
        }

        picking = false;

        // les cartes es dimensionen segons la pantalla (el Canvas treballa en píxels)
        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float w = Mathf.Clamp(sw * 0.17f, 150f, 320f);
        float h = Mathf.Clamp(sh * 0.42f, 190f, 460f);
        float gap = w * 0.07f;
        float cardY = sh * 0.10f; // les cartes van una mica amunt, els serveis queden a sota

        float totalW = picked.Length * w + (picked.Length - 1) * gap;

        for (int i = 0; i < picked.Length; i++)
        {
            Vector2 pos = new Vector2(-totalW / 2f + w / 2f + i * (w + gap), cardY);
            BuildCard(picked[i], i, pos, w, h);
        }
    }

    void BuildCard(Boon boon, int index, Vector2 pos, float w, float h)
    {
        float u = PixelUnit();
        float edge = u;                          // gruix del marc
        float pad = u * 2f;
        float hh = Mathf.Round(h * 0.46f);       // alçada de la capçalera
        float iconSize = hh * 0.8f;
        float keyH = u * 7f;
        int titleSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.105f, 15f, 30f));
        int descSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.078f, 12f, 22f));
        int tagSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.05f, 9f, 15f));

        Vector2 top = new Vector2(0.5f, 1f);
        Vector2 mid = new Vector2(0.5f, 0.5f);

        // arrel: invisible, rep els clics, i DishCardFx la mou i l'escala
        GameObject card = new GameObject("Carta" + index, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(DishCardFx));
        card.transform.SetParent(panel.transform, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = mid;
        rt.anchorMax = mid;
        rt.pivot = mid;
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = pos;

        Image hit = card.GetComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);

        Button btn = card.GetComponent<Button>();
        btn.targetGraphic = hit;
        btn.transition = Selectable.Transition.None; // el ressalt el fa DishCardFx
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        Boon chosen = boon;
        btn.onClick.AddListener(() => Choose(chosen));

        // ombra dura (desplaçada), marc i paper
        Box(card.transform, "Ombra", new Color(0f, 0f, 0f, 0.5f), Vector2.zero, Vector2.one,
            new Vector2(u * 2f, -u * 2f), new Vector2(u * 2f, -u * 2f));
        Image frame = Box(card.transform, "Vora", Frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Box(card.transform, "Paper", Paper, Vector2.zero, Vector2.one, new Vector2(edge, edge), new Vector2(-edge, -edge));

        // capçalera de tovalles de quadres
        RectTransform hdr = MakeRect(card.transform, "Capcalera", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(edge, -(edge + hh)), new Vector2(-edge, -edge));

        Texture2D tex = MakeGingham(boon.color,
            Mathf.Max(8, Mathf.RoundToInt((w - 2f * edge) / (u * 2f))),
            Mathf.Max(6, Mathf.RoundToInt(hh / (u * 2f))));
        RectTransform ginghamRt = MakeRect(hdr, "Quadres", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RawImage gingham = ginghamRt.gameObject.AddComponent<RawImage>();
        gingham.texture = tex;
        gingham.raycastTarget = false;

        Box(hdr, "Linia", Frame, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, u));

        // l'ombra del plat a terra, i el plat
        Image shadow = FixedBox(hdr, "OmbraPlat", new Color(0f, 0f, 0f, 0.28f), mid, mid,
            new Vector2(0f, -iconSize * 0.34f), new Vector2(iconSize * 0.85f, iconSize * 0.16f));
        shadow.sprite = SpriteFactory.Circle();

        RectTransform iconRt = MakeFixed(hdr, "Plat", mid, mid, new Vector2(0f, hh * 0.02f), new Vector2(iconSize, iconSize));
        Image iimg = iconRt.gameObject.AddComponent<Image>();
        iimg.raycastTarget = false;
        iimg.preserveAspect = true;
        Sprite iconSprite = GetIcon(boon);
        if (iconSprite != null)
        {
            iimg.sprite = iconSprite;
            iimg.color = Color.white;
        }
        else
        {
            // si no hi ha dibuix d'aquest plat, un cercle del seu color
            iimg.sprite = SpriteFactory.Circle();
            iimg.color = boon.color;
        }

        // etiqueta del tipus de plat (a dalt a l'esquerra)
        Color tagCol;
        string tag = TagFor(boon, out tagCol);
        float tagH = tagSize * 1.5f + u;
        float tagW = tag.Length * tagSize * 0.68f + u * 4f;
        RectTransform tagRt = MakeFixed(hdr, "Etiqueta", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(u * 2f, -u * 2f), new Vector2(tagW, tagH));
        Image tagFrame = tagRt.gameObject.AddComponent<Image>();
        tagFrame.color = Frame;
        tagFrame.raycastTarget = false;
        Image tagFace = Box(tagRt, "Fons", tagCol, Vector2.zero, Vector2.one,
            new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
        Label(tagFace.rectTransform, tag, tagSize, TextAlignmentOptions.Center, Paper, true, false);

        // si ja en tens algun, ho diu a dalt a la dreta
        int have = taken.ContainsKey(boon) ? taken[boon] : 0;
        if (have > 0)
        {
            string haveText = "TENS x" + have;
            float haveW = haveText.Length * tagSize * 0.68f + u * 4f;
            RectTransform haveRt = MakeFixed(hdr, "Tens", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-u * 2f, -u * 2f), new Vector2(haveW, tagH));
            Image haveFrame = haveRt.gameObject.AddComponent<Image>();
            haveFrame.color = Frame;
            haveFrame.raycastTarget = false;
            Image haveFace = Box(haveRt, "Fons", new Color(0.24f, 0.17f, 0.15f, 1f), Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
            Label(haveFace.rectTransform, haveText, tagSize, TextAlignmentOptions.Center, Gold, true, false);
        }

        // el nom (fins a dues línies)
        float nameH = titleSize * 2.4f;
        float bodyTop = edge + hh + pad;
        RectTransform nameRt = MakeRect(card.transform, "Nom", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(edge + pad, -(bodyTop + nameH)), new Vector2(-(edge + pad), -bodyTop));
        Label(nameRt, boon.name, titleSize, TextAlignmentOptions.Center, Ink, true, true);

        // separador: dues ratlles i un quadradet al mig
        float sepY = bodyTop + nameH + u;
        float lineW = w * 0.14f;
        Color sepCol = new Color(0.72f, 0.58f, 0.40f, 1f);
        FixedBox(card.transform, "SepEsq", sepCol, top, top, new Vector2(-(lineW / 2f + u * 3f), -sepY), new Vector2(lineW, u));
        FixedBox(card.transform, "SepDre", sepCol, top, top, new Vector2(lineW / 2f + u * 3f, -sepY), new Vector2(lineW, u));
        FixedBox(card.transform, "SepCentre", new Color(0.85f, 0.52f, 0.2f, 1f), top, top,
            new Vector2(0f, -sepY + u), new Vector2(u * 3f, u * 3f));

        // la descripció, entre el separador i la tecla
        float descTop = sepY + u * 4f;
        RectTransform descRt = MakeRect(card.transform, "Descripcio", Vector2.zero, Vector2.one,
            new Vector2(edge + pad, edge + pad + keyH + pad), new Vector2(-(edge + pad), -descTop));
        string descText = DescribeBoon(boon);
        SetBonus completes = SetCompletedBy(boon);
        if (completes != null)
            descText += "\n<color=#A8650A>Conjunt " + completes.name + ": " + completes.effect + "</color>";
        Label(descRt, descText, descSize, TextAlignmentOptions.Top, InkSoft, false, true);

        // si aquest plat completa un conjunt, una cinta daurada a la capçalera
        if (completes != null)
        {
            string ribbon = "CONJUNT";
            float ribW = ribbon.Length * tagSize * 0.68f + u * 4f;
            RectTransform ribRt = MakeFixed(hdr, "EtiquetaConjunt", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(u * 2f, u * 3f), new Vector2(ribW, tagH));
            Image ribFrame = ribRt.gameObject.AddComponent<Image>();
            ribFrame.color = Frame;
            ribFrame.raycastTarget = false;
            Image ribFace = Box(ribRt, "Fons", Gold, Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
            Label(ribFace.rectTransform, ribbon, tagSize, TextAlignmentOptions.Center, Ink, true, false);
        }

        // la tecla per triar-lo (1, 2, 3...), com una tecla de teclat
        RectTransform keyRt = MakeFixed(card.transform, "Tecla", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, edge + pad), new Vector2(keyH, keyH));
        Image keyFrame = keyRt.gameObject.AddComponent<Image>();
        keyFrame.color = Frame;
        keyFrame.raycastTarget = false;
        Image keyFace = Box(keyRt, "Cara", Paper, Vector2.zero, Vector2.one,
            new Vector2(u * 0.5f, u * 1.5f), new Vector2(-u * 0.5f, -u * 0.5f));
        Label(keyFace.rectTransform, (index + 1).ToString(), keyH * 0.6f, TextAlignmentOptions.Center, Ink, true, false);

        // animació: entrada, plat que floteja i ressalt en passar-hi el ratolí
        DishCardFx fx = card.GetComponent<DishCardFx>();
        fx.border = frame;
        fx.icon = iconRt;
        fx.borderNormal = Frame;
        fx.borderHover = Gold;
        fx.basePos = pos;
        fx.delay = index * 0.07f;
        fx.lift = u * 3f;
        fx.bob = u * 0.8f;
        fx.Own(tex);
        fx.Init();
    }

    // Un canvas propi per a la pantalla de plats i la tira de plats actius. Abans s'agafava "un canvas
    // qualsevol" de l'escena, però ara n'hi ha més (pausa, fletxes, tutorial) i podia agafar-ne un
    // que s'escala amb la pantalla: les cartes sortien petites. Aquest és com el de l'escena: 1 píxel = 1 píxel.
    private Canvas uiCanvas;

    Canvas UiCanvas()
    {
        if (uiCanvas != null) return uiCanvas;

        GameObject canvasGo = new GameObject("PlatsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        uiCanvas = canvasGo.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100; // per sobre del HUD i de les fletxes, per sota del tutorial i la pausa
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        return uiCanvas;
    }

    void BuildPanel()
    {
        Canvas canvas = UiCanvas();

        GameObject panelGo = new GameObject("BoonsPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        panelGo.transform.SetAsLastSibling();

        RectTransform rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panelGo.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.04f, 0.06f, 0.86f);

        panel = panelGo;
    }

    // ---------- EL TÍTOL ("Tria un plat") ----------

    void BuildTitle()
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;
        if (titleText != null) return;

        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float u = PixelUnit();
        int size = Mathf.RoundToInt(Mathf.Clamp(sh * 0.045f, 22f, 52f));
        float bannerH = size * 2.5f;
        float bannerW = Mathf.Clamp(sh * 0.9f, 360f, sw * 0.8f);
        Vector2 top = new Vector2(0.5f, 1f);

        // un cartell granat amb filets daurats
        RectTransform ban = MakeFixed(panel.transform, "Titol", top, top, new Vector2(0f, -sh * 0.04f), new Vector2(bannerW, bannerH));
        Image banFrame = ban.gameObject.AddComponent<Image>();
        banFrame.color = Frame;
        banFrame.raycastTarget = false;
        Box(ban, "Fons", new Color(0.30f, 0.12f, 0.14f, 1f), Vector2.zero, Vector2.one, new Vector2(u, u), new Vector2(-u, -u));
        Box(ban, "FiletDalt", Gold, new Vector2(0f, 1f), Vector2.one, new Vector2(u * 2f, -u * 3f), new Vector2(-u * 2f, -u * 2f));
        Box(ban, "FiletBaix", Gold, Vector2.zero, new Vector2(1f, 0f), new Vector2(u * 2f, u * 2f), new Vector2(-u * 2f, u * 3f));

        RectTransform titleRt = MakeRect(ban, "Text", Vector2.zero, Vector2.one,
            new Vector2(u * 4f, bannerH * 0.30f), new Vector2(-u * 4f, -u * 3f));
        titleText = Label(titleRt, "Tria un plat", size, TextAlignmentOptions.Center, Paper, true, false);

        RectTransform subRt = MakeRect(ban, "Subtitol", Vector2.zero, new Vector2(1f, 0f),
            new Vector2(u * 4f, u * 4f), new Vector2(-u * 4f, bannerH * 0.34f));
        Label(subRt, "Clica un plat o prem la seva tecla", size * 0.4f, TextAlignmentOptions.Center,
            new Color(0.95f, 0.8f, 0.5f), false, false);

        // una paella a cada costat del cartell
        Sprite deco = Resources.Load<Sprite>("Dishes/paella_mixta");
        if (deco != null)
        {
            float ds = bannerH * 0.95f;
            Image left = FixedBox(ban, "DecoEsq", Color.white, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-u * 2f, 0f), new Vector2(ds, ds));
            left.sprite = deco;
            left.preserveAspect = true;

            Image right = FixedBox(ban, "DecoDre", Color.white, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(u * 2f, 0f), new Vector2(ds, ds));
            right.sprite = deco;
            right.preserveAspect = true;
        }
    }

    // ---------- L'OPCIÓ DE CURAR (en lloc d'agafar un plat) ----------

    void BuildHealButton()
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;
        if (healButton != null) return;

        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float u = PixelUnit();
        float w = Mathf.Clamp(sw * 0.34f, 280f, 560f);
        float h = Mathf.Clamp(sh * 0.08f, 46f, 80f);
        float y = -sh * 0.30f; // a sota de les cartes

        GameObject go = new GameObject("BotoCurar", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panel.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, y);

        // mateix llenguatge que les cartes: marc fosc, cara verda i una ratlla de llum a dalt
        Image frame = go.GetComponent<Image>();
        frame.color = Frame;

        Image face = Box(rt, "Fons", new Color(0.20f, 0.42f, 0.28f, 1f), Vector2.zero, Vector2.one,
            new Vector2(u, u), new Vector2(-u, -u));
        Box(face.rectTransform, "Llum", new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -u), Vector2.zero);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = face;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(ChooseHeal);

        TextMeshProUGUI label = Label(face.rectTransform, "", Mathf.Clamp(h * 0.34f, 13f, 26f),
            TextAlignmentOptions.Center, Color.white, true, true);

        healButton = btn;
        healLabel = label;
    }

    void RefreshHealButton()
    {
        if (healButton == null || healLabel == null) return;

        bool full = health == null || health.CurrentLives >= health.maxLives;

        healButton.interactable = !full;
        healLabel.text = full
            ? "Cors plens"
            : "Curar " + healAmount + (healAmount > 1 ? " cors" : " cor") + " en lloc d'un plat (H)";
        healLabel.color = full ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white;
    }

    void BuildRerollButton()
    {
        if (panel == null) BuildPanel();
        if (panel == null) return;
        if (rerollButton != null) return;

        float sw = Mathf.Max(320f, Screen.width);
        float sh = Mathf.Max(240f, Screen.height);
        float u = PixelUnit();
        float w = Mathf.Clamp(sw * 0.34f, 280f, 560f);
        float h = Mathf.Clamp(sh * 0.08f, 46f, 80f);
        float y = -sh * 0.30f - h * 1.2f; // just a sota del botó de curar

        GameObject go = new GameObject("BotoCanviar", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panel.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, y);

        Image frame = go.GetComponent<Image>();
        frame.color = Frame;

        Image face = Box(rt, "Fons", new Color(0.25f, 0.36f, 0.55f, 1f), Vector2.zero, Vector2.one,
            new Vector2(u, u), new Vector2(-u, -u));
        Box(face.rectTransform, "Llum", new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -u), Vector2.zero);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = face;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(TryReroll);

        TextMeshProUGUI label = Label(face.rectTransform, "", Mathf.Clamp(h * 0.34f, 13f, 26f),
            TextAlignmentOptions.Center, Color.white, true, true);

        rerollButton = btn;
        rerollLabel = label;
    }

    void RefreshRerollButton()
    {
        if (rerollButton == null || rerollLabel == null) return;

        bool hidden = rerollsPerOffer <= 0;
        rerollButton.gameObject.SetActive(!hidden);
        if (hidden) return;

        bool can = rerollsLeft > 0 && CanReroll();
        rerollButton.interactable = can;
        rerollLabel.text = rerollsLeft > 0
            ? "Canviar les cartes (R) · " + rerollsLeft + (rerollsLeft > 1 ? " canvis" : " canvi")
            : "Cap canvi restant";
        rerollLabel.color = can ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
    }

    void ChooseHeal()
    {
        if (picking) return;
        if (health == null || health.CurrentLives >= health.maxLives) return;

        picking = true; // això tanca la pantalla, i no s'agafa cap plat
        health.Heal(healAmount);
        Debug.Log("Has triat curar-te en lloc d'agafar un plat");
    }

    // ---------- ELS PLATS ACTIUS (sota els cors, a dalt a l'esquerra) ----------

    void BuildTakenText()
    {
        Canvas canvas = UiCanvas();

        GameObject go = new GameObject("BoonsActius", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(Mathf.Max(240f, Screen.width * 0.32f), Mathf.Max(90f, Screen.height * 0.2f));
        rt.anchoredPosition = new Vector2(12f, -60f);

        takenStrip = rt;
    }

    // una icona petita per cada plat que has agafat, amb el número si en tens més d'un
    void RefreshTakenStrip()
    {
        if (takenStrip == null) return;

        for (int i = takenStrip.childCount - 1; i >= 0; i--)
            Destroy(takenStrip.GetChild(i).gameObject);

        float u = PixelUnit();
        float sh = Mathf.Max(240f, Screen.height);
        float s = Mathf.Round(Mathf.Clamp(sh * 0.06f, 36f, 68f));
        float gap = u;
        int perRow = Mathf.Max(1, Mathf.FloorToInt(takenStrip.sizeDelta.x / (s + gap)));
        Vector2 tl = new Vector2(0f, 1f);

        int n = 0;
        foreach (KeyValuePair<Boon, int> kv in taken)
        {
            int col = n % perRow;
            int row = n / perRow;

            RectTransform chip = MakeFixed(takenStrip, "Plat" + n, tl, tl,
                new Vector2(col * (s + gap), -row * (s + gap)), new Vector2(s, s));
            Image fr = chip.gameObject.AddComponent<Image>();
            fr.color = Frame;
            fr.raycastTarget = false;

            Box(chip, "Fons", Color.Lerp(kv.Key.color, Paper, 0.45f), Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));

            Sprite sp = GetIcon(kv.Key);
            Image ic = Box(chip, "Icona", sp != null ? Color.white : kv.Key.color, Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
            ic.sprite = sp != null ? sp : SpriteFactory.Circle();
            ic.preserveAspect = true;

            if (kv.Value > 1)
            {
                Vector2 br = new Vector2(1f, 0f);
                RectTransform badge = MakeFixed(chip, "Quantitat", br, br, new Vector2(u * 0.5f, -u * 0.5f),
                    new Vector2(s * 0.46f, s * 0.38f));
                Image bf = badge.gameObject.AddComponent<Image>();
                bf.color = Frame;
                bf.raycastTarget = false;
                Label(badge, "x" + kv.Value, s * 0.28f, TextAlignmentOptions.Center, Gold, true, false);
            }

            n++;
        }

        // sota les icones, els conjunts activats
        int rows = (n + perRow - 1) / perRow;
        float y = -rows * (s + gap) - u;
        float lh = s * 0.46f;
        foreach (SetBonus set in SetBonuses.All)
        {
            if (!SetBonuses.Has(set.id)) continue;

            string line = set.name;
            float lw = line.Length * lh * 0.5f + u * 4f;
            RectTransform chip = MakeFixed(takenStrip, "Conjunt_" + set.id, tl, tl, new Vector2(0f, y), new Vector2(lw, lh));
            Image fr = chip.gameObject.AddComponent<Image>();
            fr.color = Frame;
            fr.raycastTarget = false;
            Image face = Box(chip, "Fons", new Color(0.30f, 0.20f, 0.12f, 1f), Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
            Label(face.rectTransform, line, lh * 0.55f, TextAlignmentOptions.Center, Gold, true, false);
            y -= lh + u;
        }
    }
}
