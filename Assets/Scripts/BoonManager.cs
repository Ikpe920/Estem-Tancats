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
    KillBurst,       // els clients de vegades esclaten en morir

    // ---- complements nous (millores simples) ----
    CritDamage,      // els crítics fan més mal
    Regen,           // cures 1 cor cada tants segons
    Dodge,           // probabilitat d'esquivar un cop
    Area,            // explosions, olives, ones... més grans
    ProjectileSize,  // projectils més grans
    Knockback,       // els trets empenyen
    Rerolls,         // +1 canvi de cartes
    Magnet,          // recull les monedes d'experiència des de més lluny

    // ---- especialitats noves (canvien com es juga) ----
    BackKnives,      // ganivets cap enrere a cada tret
    Soup,            // tolls de sopa que fan mal
    PieRain,         // pastissos que cauen del cel
    Cava,            // esclat de bombolles al teu voltant
    Flame,           // flames davant teu
    Bombs,           // bombes que deixes al terra
    Whip,            // cop de fuet als costats
    Tortilla,        // truites que van i tornen

    Evolution        // plat estrella (evolució d'una especialitat)
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
    public int maxStacks = 0;                      // 0 = sense limit (és el nivell màxim del plat)
    public float levelAmount = 0f;                 // quantitat dels nivells 2 i següents (0 = la mateixa)
    [System.NonSerialized] public EvoId evo;       // si és un plat estrella, quin
    [System.NonSerialized] public Boon evolvesFrom; // i de quin plat ve
}

public class BoonManager : MonoBehaviour
{
    public Boon[] boons;              // la llibreria de boons
    [Tooltip("Si està activat, la llista de plats es regenera des del codi cada cop que comença l'escena (ignora la còpia guardada a l'escena). Desactiva'l si vols editar els plats a mà a l'Inspector.")]
    public bool useCodeDishes = true;
    public int optionsPerOffer = 3;   // quantes opcions s'ofereixen
    [Tooltip("Plats per experiència (com Vampire Survivors): els clients deixen monedes i, en pujar de nivell, tries un plat. Si està desactivat, s'ofereixen entre onades com abans.")]
    public bool xpMode = true;
    [Tooltip("Quantes especialitats diferents (plats que canvien com es juga) pots tenir alhora, com les armes de Vampire Survivors.")]
    public int maxSpecialties = 6;
    [Tooltip("Quants complements diferents (millores simples) pots tenir alhora, com els objectes passius de Vampire Survivors.")]
    public int maxSides = 6;
    public int everyNWaves = 1;       // s'ofereix cada tantes onades

    public GameObject panel;          // el panell (si el deixes buit, es crea sol)
    public TextMeshProUGUI takenText; // opcional: llista de boons actius

    public PlayerShooting shooting;
    public PlayerMovement movement;
    public PlayerHealth health;

    private int killsPerHeal = 0;
    private int killCounter = 0;
    private int healsPerWave = 0;      // Botifarra: com a molt tants cors per onada
    private int healedThisWave = 0;
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
        new[] { BoonType.BackKnives, BoonType.MultiShot },
        new[] { BoonType.Soup, BoonType.Bombs },
        new[] { BoonType.Cava, BoonType.Crit },
        new[] { BoonType.Flame, BoonType.DashDamage },
        new[] { BoonType.PieRain, BoonType.Tortilla },
        new[] { BoonType.Whip, BoonType.BackKnives },
    };
    private Button healButton;                 // l'opció de curar (en lloc d'un plat)
    private TextMeshProUGUI healLabel;
    private TextMeshProUGUI titleText;
    private RectTransform takenStrip;          // la graella d'icones dels plats que ja tens (dins la làmina)
    private GameObject collectionRoot;         // la làmina "ELS TEUS PLATS" (surt mentre mantens Tab / Select)
    private TextMeshProUGUI collectionCounter; // el comptador petit sota els cors
    private TextMeshProUGUI collectionTitle;
    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    public int healAmount = 1;     // cors que cura l'opció de curar
    public int rerollsPerOffer = 1; // quantes vegades pots canviar les cartes a cada oferta (0 = desactivat)

    private int rerollsLeft;
    private Button rerollButton;
    private TextMeshProUGUI rerollLabel;

    public static BoonManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        DamageKind.FireMul = 1f; // el safrà es torna a guanyar a cada partida
        if (shooting == null) shooting = PlayerRef.Shooting;
        if (movement == null) movement = PlayerRef.Movement;
        if (health == null) health = PlayerRef.Health;

        if (useCodeDishes || boons == null || boons.Length == 0) BuildDefaultBoons();

        Enemy.speedMultiplier = 1f; // els boons de lentitud no acumulen entre partides
        SetBonuses.ResetAll();      // els conjunts tampoc
        Evo.ResetAll();             // ni els plats estrella
        DishArea.Mul = 1f;
        if (panel != null) panel.SetActive(false);
        // les millores del Rebost
        maxSpecialties += PowerUps.Level("esp");
        maxSides += PowerUps.Level("compl");
        rerollsPerOffer += PowerUps.Level("canvis");
        optionsPerOffer = Mathf.Min(5, optionsPerOffer + PowerUps.Level("cartes"));
        if (takenText == null) BuildTakenText();
        Enemy.OnKilled += HandleKill;
        ScoreManager.OnWaveEnded += HandleWaveEnded;
        
    }

        void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Enemy.OnKilled -= HandleKill;
        ScoreManager.OnWaveEnded -= HandleWaveEnded;
    }

    // Crema catalana: onada acabada sense rebre cap cop → cures un cor
    void HandleWaveEnded(bool flawless)
    {
        healedThisWave = 0; // la Botifarra torna a poder curar a l'onada següent
        if (!flawless || flawlessHeal <= 0 || health == null) return;
        if (health.CurrentLives >= health.maxLives) return;

        health.Heal(flawlessHeal);
        HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 2f, "CREMA CATALANA! +1 cor", new Color(0.98f, 0.85f, 0.45f));
    }

    void HandleKill(Enemy e)
    {
        if (killsPerHeal <= 0 || health == null) return;
        if (health.CurrentLives >= health.maxLives) return; // amb la vida plena no compta (no es guarden cures per després)
        if (healedThisWave >= healsPerWave) return;          // ja ha curat el que podia en aquesta onada

        killCounter++;
        if (killCounter >= killsPerHeal)
        {
            killCounter = 0;
            healedThisWave++;
            health.Heal(1);
            if (Evo.Has(EvoId.BotifarraFestaMajor)) // plat estrella: una traca al voltant
            {
                Fx.Explosion(health.transform.position, 3f * DishArea.Mul, 1f);
                DishUtil.HitCircle(health.transform.position, 3f * DishArea.Mul, 6 + DishUtil.Bonus, 8f, DmgKind.Explosion);
            }
            HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 1.6f, "BOTIFARRA! +1 cor", new Color(0.85f, 0.6f, 0.45f), 1.2f);

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
        boons = DefaultDishes();
    }

    // la carta de plats del codi (també la fa servir el Receptari, des del menú)
    public static Boon[] DefaultDishes()
    {
        return new Boon[]
        {
            // ---- plats que donen un buff ----
            MakeBoon("Salsa brava", "+1 de dany a cada tret", BoonType.Damage, 1f, new Color(0.95f, 0.45f, 0.35f), 12, 4),
            MakeBoon("Cafè sol", "Llances un 20% més ràpid", BoonType.FireRate, 0.2f, new Color(0.65f, 0.45f, 0.3f), 12, 4),
            MakeBoon("Macarrons amb tomàquet", "Camines més ràpid", BoonType.MoveSpeed, 0.6f, new Color(0.95f, 0.6f, 0.4f), 12, 3),
            MakeBoon("Escudella i carn d'olla", "+1 cor màxim (i el deixes ple)", BoonType.MaxLives, 1f, new Color(0.95f, 0.45f, 0.6f), 5, 2),
            MakeBoon("Croquetes cruixents", "+0,3 s d'invulnerabilitat", BoonType.Invulnerability, 0.3f, new Color(0.9f, 0.75f, 0.45f), 8, 3),
            MakeBoon("Xurros amb xocolata", "-0,2 s de recàrrega del dash", BoonType.DashCooldown, 0.2f, new Color(0.7f, 0.45f, 0.35f), 8, 3),
            MakeBoon("Pa amb tomàquet", "Els projectils van més ràpids", BoonType.ProjectileSpeed, 3f, new Color(0.95f, 0.55f, 0.4f), 8, 3),
            MakeBoon("Ratafia", "Cada tret té un 15% de ser crític (×2 de dany)", BoonType.Crit, 0.15f, new Color(0.55f, 0.3f, 0.2f), 7, 3),
            MakeBoon("Oli d'oliva verge", "Els clients van un 10% més lents", BoonType.SlowEnemies, 0.1f, new Color(0.65f, 0.75f, 0.3f), 7, 3),

            // ---- plats que canvien com es juga (surten més a les primeres onades) ----
            MakeBoon("Paella mixta", "Cada tret en llença 2 més, en ventall (i 1 més a cada nivell)", BoonType.MultiShot, 2f, new Color(0.95f, 0.75f, 0.25f), 5, 4, BoonType.None, 0f, 1f),
            MakeBoon("Bunyols de bacallà", "Els teus projectils travessen 1 client més", BoonType.Pierce, 1f, new Color(0.9f, 0.85f, 0.6f), 6, 4),
            MakeBoon("Calçots amb salvitxada", "Els teus projectils exploten (i l'explosió creix a cada nivell)", BoonType.Explosive, 1.5f, new Color(0.95f, 0.4f, 0.2f), 5, 4, BoonType.None, 0f, 0.5f),
            MakeBoon("Sorbet de llimona", "Els teus projectils frenen els clients", BoonType.SlowOnHit, 0.4f, new Color(0.7f, 0.95f, 0.9f), 6, 4),
            MakeBoon("Pollastre al curri", "El dash crema els clients que travessis", BoonType.DashDamage, 2f, new Color(0.95f, 0.65f, 0.15f), 5, 4),
            MakeBoon("Botifarra amb mongetes", "Cures 1 cor cada 20, 16, 12 i 8 clients (com a molt 1 per onada, 2 al nivell 3)", BoonType.HealOnKills, 20f, new Color(0.75f, 0.5f, 0.4f), 5, 4),
            MakeBoon("Crema catalana", "Cures en acabar una onada sense rebre cap cop (1 cor, 2 al nivell 2)", BoonType.FlawlessHeal, 1f, new Color(0.98f, 0.85f, 0.45f), 5, 2),
            MakeBoon("Menú de degustació", "A partir d'ara pots triar entre una carta més", BoonType.MoreChoices, 1f, new Color(0.85f, 0.7f, 0.9f), 3, 2),
            MakeBoon("Mandonguilles", "Els trets reboten 1 cop més a les parets", BoonType.Ricochet, 1f, new Color(0.7f, 0.42f, 0.28f), 6, 4),
            MakeBoon("Olives farcides", "Olives que giren al teu voltant i fan mal (2, i 1 més a cada nivell)", BoonType.Orbit, 2f, new Color(0.55f, 0.7f, 0.3f), 5, 4, BoonType.None, 0f, 1f),
            MakeBoon("Romesco", "Quan et toquen, una ona empeny i fa mal (més gran a cada nivell)", BoonType.HitWave, 1f, new Color(0.9f, 0.45f, 0.25f), 5, 4),
            MakeBoon("Panellets", "En fer el dash, llences panellets en cercle (6, i 3 més a cada nivell)", BoonType.DashRing, 6f, new Color(0.92f, 0.8f, 0.55f), 5, 4),
            MakeBoon("Coca de recapte", "Els clients poden esclatar en morir (30%, 45%, 60%, 75%)", BoonType.KillBurst, 1f, new Color(0.85f, 0.6f, 0.35f), 5, 4),

            // ---- el safrà: el foc fa més mal (abans donava punts) ----
            MakeBoon("Safrà", "El foc fa un 30% més de mal (i un 30% més a cada nivell)", BoonType.ScoreBoost, 0.3f, new Color(0.98f, 0.7f, 0.2f), 6, 4),

            // ---- plats amb risc i recompensa ----
            MakeBoon("Xili amb carn", "+2 de dany, però els clients van un 15% més ràpids", BoonType.Damage, 2f, new Color(0.85f, 0.25f, 0.2f), 6, 2, BoonType.SlowEnemies, -0.15f),
            MakeBoon("Fondue de formatge", "Llances un 30% més ràpid, però camines més lent", BoonType.FireRate, 0.3f, new Color(0.98f, 0.9f, 0.5f), 6, 2, BoonType.MoveSpeed, -0.8f),
            MakeBoon("Fideuà picant", "+1 de dany, però el dash triga més a recarregar-se", BoonType.Damage, 1f, new Color(0.9f, 0.5f, 0.25f), 6, 2, BoonType.DashCooldown, -0.25f),
            MakeBoon("Calamars a la romana", "Camines més ràpid, però ets menys temps invulnerable", BoonType.MoveSpeed, 0.9f, new Color(0.95f, 0.85f, 0.5f), 5, 2, BoonType.Invulnerability, -0.3f),

            // ---- complements nous ----
            MakeBoon("Pebrots de Padró", "Uns piquen i altres no: els crítics fan +50% de mal", BoonType.CritDamage, 0.5f, new Color(0.35f, 0.65f, 0.25f), 7, 3),
            MakeBoon("Mel i mató", "Cures 1 cor cada 50 s (40 i 30 als nivells següents)", BoonType.Regen, 50f, new Color(0.98f, 0.85f, 0.45f), 5, 3),
            MakeBoon("Carquinyolis", "Un 8% de probabilitat d'esquivar un cop", BoonType.Dodge, 0.08f, new Color(0.9f, 0.75f, 0.5f), 7, 3),
            MakeBoon("Vermut", "Explosions, olives, ones, tolls... un 15% més grans", BoonType.Area, 0.15f, new Color(0.7f, 0.2f, 0.25f), 7, 4),
            MakeBoon("Allioli", "Els teus projectils són un 20% més grans", BoonType.ProjectileSize, 0.2f, new Color(0.98f, 0.95f, 0.8f), 7, 3),
            MakeBoon("Tomàquet de penjar", "Els teus trets empenyen els clients", BoonType.Knockback, 2.5f, new Color(0.9f, 0.3f, 0.25f), 7, 3),
            MakeBoon("Cireres", "+1 canvi de cartes a cada oferta", BoonType.Rerolls, 1f, new Color(0.8f, 0.1f, 0.2f), 5, 2),
            MakeBoon("Coca de llardons", "Recolleixes les monedes d'experiència des d'un 40% més lluny", BoonType.Magnet, 0.4f, new Color(0.9f, 0.7f, 0.4f), 8, 3),

            // ---- especialitats noves ----
            MakeBoon("Pernil tallat a ganivet", "A cada tret, un ganivet surt cap enrere (i 1 més a cada nivell)", BoonType.BackKnives, 1f, new Color(0.8f, 0.35f, 0.35f), 6, 4),
            MakeBoon("Sopa de galets", "Deixes tolls de sopa bullint que fan mal als clients", BoonType.Soup, 1f, new Color(0.95f, 0.75f, 0.4f), 6, 4),
            MakeBoon("Pastís de nata", "De tant en tant cau un pastís sobre un client i esquitxa els del voltant", BoonType.PieRain, 1f, new Color(0.98f, 0.9f, 0.85f), 6, 4),
            MakeBoon("Cava", "Cada pocs segons, un esclat de bombolles al teu voltant empeny i fa mal", BoonType.Cava, 1f, new Color(0.95f, 0.9f, 0.6f), 6, 4),
            MakeBoon("Escalivada", "Flames curtes davant teu que cremen els clients", BoonType.Flame, 1f, new Color(0.85f, 0.35f, 0.2f), 6, 4),
            MakeBoon("Bombes de la Barceloneta", "Deixes bombes al terra que exploten quan s'hi acosta un client", BoonType.Bombs, 1f, new Color(0.6f, 0.4f, 0.3f), 6, 4),
            MakeBoon("Fuet", "Un cop de fuet als costats que travessa tots els clients", BoonType.Whip, 1f, new Color(0.7f, 0.3f, 0.25f), 6, 4),
            MakeBoon("Truita de patates", "Llances truites que van i tornen, travessant els clients", BoonType.Tortilla, 1f, new Color(0.98f, 0.8f, 0.35f), 6, 4),
        };
    }

    static Boon MakeBoon(string n, string d, BoonType t, float a, Color c, int w,
                         int maxStacks = 0, BoonType t2 = BoonType.None, float a2 = 0f, float levelAmount = 0f)
    {
        Boon b = new Boon();
        b.levelAmount = levelAmount;
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
        if (xpMode) return false; // els plats ara surten en pujar de nivell (experiència)
        if (boons == null || boons.Length == 0) return false;
        if (everyNWaves <= 0) return false;
        return wave % everyNWaves == 0;
    }

    // tecles per triar carta: 1-5 (fila de dalt i teclat numèric)
    static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
    static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5 };

    // comandament: la carta marcada (-1 = cap) i les cartes que hi ha ara
    private int padFocus = -1;
    private float padNextMove;
    private readonly List<DishCardFx> cardFx = new List<DishCardFx>();
    private TextMeshProUGUI subtitleText;

    public IEnumerator OfferBoons()
    {
        Boon[] picked = PickBoons(optionsPerOffer);
        if (picked == null || picked.Length == 0)
        {
            // ja ho tens tot al màxim: les postres (curar-te), si et serveixen de res
            picked = RestCards();
            if (picked == null)
            {
                if (health != null) HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 1.6f, "TOT AL MÀXIM!", new Color(1f, 0.85f, 0.35f), 1.2f);
                yield break;
            }
        }
        RollShiny(picked);

        currentOffer = picked;

        BuildCards(picked);
        BuildTitle();
        if (titleText != null) titleText.text = offerTitle ?? "Tria un plat";
        BuildHealButton();
        RefreshHealButton();
        rerollsLeft = rerollsPerOffer;
        BuildRerollButton();
        RefreshRerollButton();

        panel.SetActive(true);
        bool wasPaused = Time.timeScale == 0f;
        Time.timeScale = 0f; // el joc s'atura per triar

        // amb comandament, la primera carta ja surt marcada; i la pantalla es controla sola (no amb l'EventSystem)
        GamepadInput.CustomScreens++;
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        SetPadFocus(GamepadInput.UsingGamepad ? 0 : -1);
        GamepadInput.OnModeChanged += RefreshPadTexts;
        RefreshPadTexts();

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

            // comandament: creu o joystick per moure's per les cartes, A per agafar, X per curar, Y per canviar
            Gamepad pad = Gamepad.current;
            if (pad != null && !picking)
            {
                Boon[] offer = currentOffer != null ? currentOffer : picked;
                float x = pad.leftStick.ReadValue().x;
                int dir = 0;
                if (pad.dpad.left.wasPressedThisFrame) dir = -1;
                else if (pad.dpad.right.wasPressedThisFrame) dir = 1;
                else if (Mathf.Abs(x) > 0.6f && Time.unscaledTime >= padNextMove) dir = x > 0 ? 1 : -1;
                if (Mathf.Abs(x) < 0.3f) padNextMove = 0f;
                if (dir != 0)
                {
                    padNextMove = Time.unscaledTime + 0.25f;
                    SetPadFocus(padFocus < 0 ? 0 : Mathf.Clamp(padFocus + dir, 0, offer.Length - 1));
                    AudioManager.Play("ui_click");
                }
                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    if (padFocus < 0) SetPadFocus(0);
                    else Choose(offer[Mathf.Clamp(padFocus, 0, offer.Length - 1)]);
                }
                if (!picking && pad.buttonWest.wasPressedThisFrame) ChooseHeal();
                if (!picking && pad.buttonNorth.wasPressedThisFrame) TryReroll();
            }
            yield return null;
        }

        GamepadInput.CustomScreens = Mathf.Max(0, GamepadInput.CustomScreens - 1);
        GamepadInput.OnModeChanged -= RefreshPadTexts;
        SetPadFocus(-1);
        previousOffer = currentOffer;
        panel.SetActive(false);
        if (!wasPaused) Time.timeScale = 1f;
    }

    void SetPadFocus(int i)
    {
        padFocus = i;
        for (int k = 0; k < cardFx.Count; k++)
            if (cardFx[k] != null) cardFx[k].focused = k == i;
    }

    // els textos d'ajuda diuen les tecles o els botons del comandament, segons amb què es jugui
    void RefreshPadTexts()
    {
        if (subtitleText != null)
            subtitleText.text = GamepadInput.UsingGamepad
                ? "Creu: triar · " + GamepadInput.South() + ": agafar · " + GamepadInput.West() + ": curar · " + GamepadInput.North() + ": canviar"
                : "Clica un plat o prem la seva tecla";
        if (subtitleText != null) subtitleText.text = TablesText() + "  ·  " + subtitleText.text;
        RefreshHealButton();
        RefreshRerollButton();
        if (GamepadInput.UsingGamepad && padFocus < 0 && panel != null && panel.activeSelf) SetPadFocus(0);
    }

    // plats que canvien com es juga (no només números)
    static bool IsMechanic(BoonType t)
    {
        switch (t)
        {
            case BoonType.MultiShot: case BoonType.Pierce: case BoonType.Explosive: case BoonType.SlowOnHit:
            case BoonType.DashDamage: case BoonType.Ricochet: case BoonType.Orbit: case BoonType.HitWave:
            case BoonType.DashRing: case BoonType.KillBurst: case BoonType.HealOnKills:
            case BoonType.BackKnives: case BoonType.Soup: case BoonType.PieRain: case BoonType.Cava:
            case BoonType.Flame: case BoonType.Bombs: case BoonType.Whip: case BoonType.Tortilla:
                return true;
        }
        return false;
    }

    private WaveSpawner waveSpawner;
    int CurrentWave()
    {
        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        return waveSpawner != null ? waveSpawner.currentWave : 1;
    }

    // ---------- EXPERIÈNCIA I NIVELLS DEL XEF ----------
    // Cada nivell demana una mica més: 5, 7, 9, 11... (uns 40 nivells en una partida de 40 onades)
    public int ChefLevel { get; private set; }
    public float Xp { get; private set; }
    public float XpToNext => 5f + 2f * ChefLevel;
    private int pendingLevels;
    private bool offering;
    private string offerTitle;

    public void AddXp(float v)
    {
        Xp += v * (1f + 0.1f * PowerUps.Level("experiencia"));
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            ChefLevel++;
            pendingLevels++;
            Stats.Max("best_level", ChefLevel + 1);
            if (health != null)
                HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 1.8f, "NIVELL " + (ChefLevel + 1) + "!", new Color(1f, 0.85f, 0.35f), 1.2f);
            AudioManager.Play("boon", 0.6f, 1.2f);
        }
    }

    // es pot obrir la pantalla de triar plat ara mateix? (no amb la pausa, ni mentre mors, ni enmig d'una altra pantalla)
    bool CanOfferNow()
    {
        return Time.timeScale > 0.95f && !PlayerHealth.IsDying && !PauseMenu.IsOpen && GamepadInput.CustomScreens == 0
               && (panel == null || !panel.activeSelf);
    }

    IEnumerator LevelUpRoutine()
    {
        offering = true;
        pendingLevels--;
        offerTitle = "NIVELL " + (ChefLevel + 1 - pendingLevels) + "!  Tria un plat";
        yield return OfferBoons();
        offerTitle = null;
        offering = false;
    }

    // el cofre dels caps: dos plats de regal
    public IEnumerator OpenChest(string title = null)
    {
        while (offering) yield return null;
        offering = true;
        AudioManager.Play("coin_big");
        for (int i = 0; i < 2; i++)
        {
            offerTitle = (title ?? "COFRE DEL CAP!") + "  Plat " + (i + 1) + " de 2";
            yield return OfferBoons();
        }
        offerTitle = null;
        offering = false;
    }

    // ---------- ESPAIS I NIVELLS (com Vampire Survivors / Brotato) ----------
    // Només pots tenir 'maxSlots' plats diferents a la taula. Tornar a triar un plat que ja tens el puja
    // de nivell, fins al seu màxim. Quan la taula és plena, només surten millores dels que ja tens
    // (i els plats de curar o de punts, que no ocupen espai).

    public static bool IsConsumable(Boon b)
    {
        return b != null && (b.type == BoonType.Heal || b.type == BoonType.Score);
    }

    // especialitat (canvia com es juga) o complement (millora simple)
    public static bool IsSpecialty(Boon b)
    {
        return b != null && (b.type == BoonType.Evolution || IsMechanic(b.type));
    }

    public int SpecialtiesUsed { get { int n = 0; foreach (KeyValuePair<Boon, int> kv in taken) if (IsSpecialty(kv.Key)) n++; return n; } }
    public int SidesUsed { get { int n = 0; foreach (KeyValuePair<Boon, int> kv in taken) if (!IsSpecialty(kv.Key) && !IsConsumable(kv.Key)) n++; return n; } }
    public int SlotsUsed => SpecialtiesUsed + SidesUsed;
    public int MaxSlotsFor(Boon b) { return IsSpecialty(b) ? maxSpecialties : maxSides; }
    public int UsedFor(Boon b) { return IsSpecialty(b) ? SpecialtiesUsed : SidesUsed; }
    string TablesText() { return "Especialitats " + SpecialtiesUsed + "/" + maxSpecialties + " · Complements " + SidesUsed + "/" + maxSides; }

    public int LevelOf(Boon b) { return b != null && taken.ContainsKey(b) ? taken[b] : 0; }

    public int MaxLevel(Boon b)
    {
        if (b == null) return 1;
        if (b.maxStacks > 0) return b.maxStacks;
        int cap = MaxStacks(b);
        return cap > 0 ? cap : 5;
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
            if (kv.Key == null) continue;
            if (kv.Key.type == type) total += kv.Value;
            else if (kv.Key.evolvesFrom != null && kv.Key.evolvesFrom.type == type) total += MaxLevel(kv.Key.evolvesFrom);
        }
        return total;
    }

    // aquest plat encara es pot oferir?
    bool CanPick(Boon b)
    {
        if (b == null) return false;

        int stacks = taken.ContainsKey(b) ? taken[b] : 0;
        if (b.maxStacks > 0 && stacks >= b.maxStacks) return false; // ja el té al màxim


        if (!IsConsumable(b))
        {
            foreach (KeyValuePair<Boon, int> kv in taken) if (kv.Key != null && kv.Key.evolvesFrom == b) return false; // ja l'has convertit en plat estrella
            if (!taken.ContainsKey(b) && UsedFor(b) >= MaxSlotsFor(b)) return false; // aquesta taula és plena: només millores
            if (stacks >= MaxLevel(b)) return false;                          // ja és al nivell màxim
        }

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
        AudioManager.Play("ui_click");
        if (RerollCards()) rerollsLeft--;
        RefreshRerollButton();
    }

    // pes real d'un plat al sorteig: el que ja ha sortit fa poc pesa menys, les sinergies pesen més
    float EffectiveWeight(Boon b)
    {
        float w = RarityWeight(RarityOf(b)); // comuns, poc comuns, rars i llegendaris

        if (previousOffer != null && System.Array.IndexOf(previousOffer, b) >= 0) w *= 0.25f;
        if (taken.ContainsKey(b) && !IsConsumable(b)) w *= 1.4f; // pujar de nivell el que ja tens
        if (IsMechanic(b.type) && CurrentWave() <= 8) w *= 1.5f;  // a les primeres onades, plats que canvien com es juga
        // el complement que fa evolucionar una especialitat que ja tens surt una mica més (com a Vampire Survivors)
        if (!IsSpecialty(b) && !taken.ContainsKey(b))
            foreach (Evo.Def d in Evo.All)
                if (d.partner == b.type && !Evo.Has(d.id) && TypeStacks(d.baseType) > 0) { w *= 1.5f; break; }

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

        // plat estrella: si una especialitat és al màxim i tens el seu complement, la carta daurada surt sempre
        Boon star = AvailableEvolution();
        if (star != null)
        {
            if (result.Count >= howMany && result.Count > 0) result[0] = star;
            else result.Insert(0, star);
        }
        return result.ToArray();
    }

    // després que s'hagi posat el xef triat (es fa en carregar l'escena, després de l'Awake)
    void Start()
    {
        if (PowerUps.Level("esmorzar") > 0) GiveStartingSide();
    }

    // Esmorzar de forquilla (Rebost): comences amb un complement a l'atzar
    void GiveStartingSide()
    {
        List<Boon> options = new List<Boon>();
        foreach (Boon b in boons) if (b != null && !IsSpecialty(b) && !IsConsumable(b) && b.type2 == BoonType.None && CanPick(b)) options.Add(b);
        if (options.Count == 0) return;
        Boon pick = options[Random.Range(0, options.Count)];
        taken[pick] = 1;
        Codex.DiscoverDish(pick);
        Apply(pick);
        RefreshTakenText();
        if (health != null)
            HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 2f, "ESMORZAR: " + pick.name, new Color(1f, 0.85f, 0.5f), 2f);
    }

    // ---------- PLATS ESTRELLA ----------

    private readonly Dictionary<EvoId, Boon> starBoons = new Dictionary<EvoId, Boon>();

    Boon AvailableEvolution()
    {
        foreach (Evo.Def d in Evo.All)
        {
            if (Evo.Has(d.id)) continue;
            Boon baseDish = null;
            foreach (KeyValuePair<Boon, int> kv in taken)
                if (kv.Key != null && kv.Key.type == d.baseType && kv.Value >= MaxLevel(kv.Key)) { baseDish = kv.Key; break; }
            if (baseDish == null || TypeStacks(d.partner) <= 0) continue;

            if (!starBoons.TryGetValue(d.id, out Boon star))
            {
                star = new Boon();
                star.name = d.name;
                star.description = d.desc;
                star.type = BoonType.Evolution;
                star.color = new Color(1f, 0.78f, 0.25f);
                star.maxStacks = 1;
                star.weight = 0;
                star.evo = d.id;
                starBoons[d.id] = star;
            }
            star.evolvesFrom = baseDish;
            star.icon = GetIcon(baseDish);
            return star;
        }
        return null;
    }

    void ChooseEvolution(Boon star)
    {
        // el plat estrella ocupa el lloc del plat del qual ve (els seus efectes es queden)
        if (star.evolvesFrom != null) taken.Remove(star.evolvesFrom);
        taken[star] = 1;
        Evo.Activate(star.evo);
        Codex.DiscoverEvo(star.evo);
        AudioManager.Play("challenge");

        if (star.evo == EvoId.PollastreSafra && movement != null) Ensure<DashFireTrail>(movement.gameObject);
        if (star.evo == EvoId.BotifarraFestaMajor) healsPerWave += 1;

        // els que canvien números es posen ara; la resta els miren els seus components
        if (star.evo == EvoId.BunyolsEmporda && shooting != null) { shooting.pierce += 50; shooting.damage += 1; }
        if (star.evo == EvoId.MandonguillesGegants && shooting != null) { shooting.bounces += 3; shooting.projectileScale += 0.4f; }

        if (health != null)
            HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 2.4f,
                "PLAT ESTRELLA! " + star.name + "\n<size=65%>" + star.description + "</size>", new Color(1f, 0.8f, 0.25f), 2.6f);
    }

    void Choose(Boon boon)
    {
        if (picking) return;
        picking = true;
        if (restBoons.Contains(boon))
        {
            // les postres: no ocupen lloc ni compten com a plat
            AudioManager.Play("heal");
            if (boon.type == BoonType.MaxLives) restMaxLivesGiven++;
            ApplyEffect(boon.type, boon.amount);
            RefreshTakenText();
            return;
        }
        if (boon.type == BoonType.Evolution)
        {
            ChooseEvolution(boon);
            CheckSets();
            RefreshTakenText();
            return;
        }
        AudioManager.Play("boon");

        taken[boon] = (taken.ContainsKey(boon) ? taken[boon] : 0) + 1;
        Codex.DiscoverDish(boon);
        Apply(boon);
        if (shiny.Contains(boon) && taken[boon] < MaxLevel(boon))
        {
            // carta brillant: un nivell de regal
            taken[boon]++;
            Apply(boon);
            if (health != null) HazardFx.Popup((Vector2)health.transform.position + Vector2.up * 1.8f, "BRILLANT! +2 NIVELLS", new Color(1f, 0.9f, 0.4f), 1.2f);
            AudioManager.Play("set", 0.7f, 1.3f);
        }
        shiny.Clear();
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
        Codex.DiscoverSet(s.id);
        AudioManager.Play("set");
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

    // el nivell que acaba d'aconseguir (ja comptat a 'taken')
    void Apply(Boon boon)
    {
        int level = Mathf.Max(1, LevelOf(boon));
        ApplyEffect(boon.type, AmountFor(boon, level), level);
        if (boon.type2 != BoonType.None) ApplyEffect(boon.type2, boon.amount2, level);
    }

    // ---------- RARESA (comuns, poc comuns, rars i llegendaris) ----------
    public enum Rarity { Comu, PocComu, Rar, Llegendari }

    static readonly string[] Rars = { "Pollastre al curri", "Olives farcides", "Romesco", "Panellets", "Coca de recapte",
        "Pernil tallat a ganivet", "Escalivada", "Bombes de la Barceloneta", "Botifarra amb mongetes", "Menú de degustació" };
    static readonly string[] PocComuns = { "Ratafia", "Escudella i carn d'olla", "Xili amb carn", "Fondue de formatge", "Fideuà picant",
        "Calamars a la romana", "Mandonguilles", "Bunyols de bacallà", "Sorbet de llimona", "Cireres", "Mel i mató", "Crema catalana",
        "Sopa de galets", "Fuet", "Truita de patates", "Cava", "Pastís de nata", "Safrà" };

    public static Rarity RarityOf(Boon b)
    {
        if (b == null) return Rarity.Comu;
        if (b.name == "Paella mixta" || b.name == "Calçots amb salvitxada") return Rarity.Llegendari;
        if (System.Array.IndexOf(Rars, b.name) >= 0) return Rarity.Rar;
        if (System.Array.IndexOf(PocComuns, b.name) >= 0) return Rarity.PocComu;
        return Rarity.Comu;
    }

    static float RarityWeight(Rarity r)
    {
        switch (r)
        {
            case Rarity.PocComu: return 8f;
            case Rarity.Rar: return 4.5f;
            case Rarity.Llegendari: return 2f;
        }
        return 12f;
    }

    public static string RarityName(Rarity r)
    {
        switch (r)
        {
            case Rarity.PocComu: return "POC COMÚ";
            case Rarity.Rar: return "RAR";
            case Rarity.Llegendari: return "LLEGENDARI";
        }
        return "COMÚ";
    }

    public static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.PocComu: return new Color(0.35f, 0.75f, 0.35f);
            case Rarity.Rar: return new Color(0.30f, 0.55f, 0.95f);
            case Rarity.Llegendari: return new Color(1f, 0.75f, 0.15f);
        }
        return new Color(0.85f, 0.82f, 0.78f);
    }

    // ---------- CARTES BRILLANTS: de tant en tant, una carta dona 2 nivells de cop ----------
    private readonly HashSet<Boon> shiny = new HashSet<Boon>();

    void RollShiny(Boon[] offer)
    {
        shiny.Clear();
        foreach (Boon b in offer)
        {
            if (b == null || b.type == BoonType.Evolution || restBoons.Contains(b)) continue;
            int have = taken.ContainsKey(b) ? taken[b] : 0;
            if (MaxLevel(b) - have >= 2 && Random.value < 0.08f) shiny.Add(b);
        }
    }

    // ---------- LES POSTRES: quan ja ho tens tot al màxim ----------
    // Només curar-se, i amb mesura: 1 cor si no tens la vida plena, o +1 cor màxim (com a molt 2 cops per partida).
    // Si no et serveix res d'això, no surt cap carta.
    private readonly HashSet<Boon> restBoons = new HashSet<Boon>();
    private Boon restHeal, restMax;
    private int restMaxLivesGiven;

    Boon[] RestCards()
    {
        if (restHeal == null)
        {
            restHeal = MakeBoon("Plat de cullera", "Postres: cures 1 cor", BoonType.Heal, 1f, new Color(0.55f, 0.85f, 0.55f), 1);
            restMax = MakeBoon("Escudella i carn d'olla", "Postres: +1 cor màxim (i el deixes ple). Com a molt 2 cops per partida", BoonType.MaxLives, 1f, new Color(0.95f, 0.45f, 0.6f), 1);
            restBoons.Add(restHeal);
            restBoons.Add(restMax);
        }
        List<Boon> list = new List<Boon>();
        if (health != null && health.CurrentLives < health.maxLives) list.Add(restHeal);
        if (health != null && restMaxLivesGiven < 2 && health.maxLives < PlayerHealth.MaxLivesCap) list.Add(restMax);
        return list.Count > 0 ? list.ToArray() : null;
    }

    // la quantitat d'un nivell: el primer fa 'amount', i els següents 'levelAmount' (si en té)
    static float AmountFor(Boon b, int level)
    {
        return level > 1 && b.levelAmount != 0f ? b.levelAmount : b.amount;
    }

    // rendiment decreixent: cada vegada que una millora de tret es repeteix, val un 20% menys que l'anterior
    private readonly Dictionary<BoonType, int> timesApplied = new Dictionary<BoonType, int>();

    float Diminish(BoonType type, float amount)
    {
        int n;
        timesApplied.TryGetValue(type, out n);
        timesApplied[type] = n + 1;
        return amount * Mathf.Pow(0.8f, n);
    }

    void ApplyEffect(BoonType type, float amount, int level = 1)
    {
        switch (type)
        {
            case BoonType.FireRate: case BoonType.Crit: case BoonType.Explosive: case BoonType.ProjectileSpeed:
            case BoonType.CritDamage: case BoonType.Area: case BoonType.ProjectileSize: case BoonType.Knockback:
                amount = Diminish(type, amount);
                break;
        }
        switch (type)
        {
            case BoonType.Damage:
                if (shooting != null) shooting.damage = Mathf.Max(1, shooting.damage + Mathf.RoundToInt(amount));
                break;

            case BoonType.FireRate:
                // un percentatge del temps entre trets (així serveix igual al xef que llança lent i al que llança ràpid)
                if (shooting != null) shooting.fireRate = Mathf.Clamp(shooting.fireRate * (1f - amount), 0.07f, 1.5f);
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

            case BoonType.ScoreBoost: // el safrà: el foc fa més mal
                DamageKind.FireMul += amount;
                break;

            case BoonType.SlowEnemies:
                Enemy.speedMultiplier *= (1f - amount); // amb amount negatiu, els clients van més ràpid
                break;

            case BoonType.MultiShot:
                // el ventall té un màxim: 7 trets en total
                if (shooting != null) shooting.extraShots = Mathf.Min(6, shooting.extraShots + Mathf.RoundToInt(amount));
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
                // 20 clients, després 16 i 12; com a molt 1 cor per onada (2 al nivell màxim)
                killsPerHeal = killsPerHeal == 0 ? Mathf.RoundToInt(amount) : Mathf.Max(8, killsPerHeal - 4);
                healsPerWave = level >= 3 ? 2 : 1;
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
                if (health != null)
                {
                    RomescoWave wave = Ensure<RomescoWave>(health.gameObject);
                    if (level > 1) { wave.damage += 1; wave.radius += 0.6f; } // cada nivell, més gran i més fort
                }
                break;

            case BoonType.DashRing:
                if (movement != null)
                {
                    PanelletsRing ring = Ensure<PanelletsRing>(movement.gameObject);
                    ring.count += ring.count == 0 ? Mathf.RoundToInt(amount) : 3; // la segona còpia: +3
                }
                break;

            case BoonType.KillBurst:
                if (movement != null) Ensure<KillBurst>(movement.gameObject).chance = 0.3f + 0.15f * (level - 1); // 30%, 45%, 60%
                break;

            case BoonType.CritDamage:
                if (shooting != null) shooting.critMultiplier += amount;
                break;
            case BoonType.Regen:
                if (health != null) Ensure<RegenHeal>(health.gameObject).every = Mathf.Max(20f, amount - 10f * (level - 1));
                break;
            case BoonType.Dodge:
                if (health != null) health.dodgeChance = Mathf.Min(0.4f, health.dodgeChance + amount);
                break;
            case BoonType.Area:
                DishArea.Mul += amount;
                break;
            case BoonType.ProjectileSize:
                if (shooting != null) shooting.projectileScale += amount;
                break;
            case BoonType.Knockback:
                if (shooting != null) shooting.knockback += amount;
                break;
            case BoonType.Rerolls:
                rerollsPerOffer += Mathf.RoundToInt(amount);
                break;
            case BoonType.Magnet:
                XpCoin.MagnetMul += amount;
                break;

            case BoonType.BackKnives:
                if (shooting != null) shooting.backShots += 1;
                break;
            case BoonType.Soup:
                if (movement != null) Ensure<SoupTrail>(movement.gameObject).level = level;
                break;
            case BoonType.PieRain:
                if (movement != null) Ensure<PieRain>(movement.gameObject).level = level;
                break;
            case BoonType.Cava:
                if (movement != null) Ensure<CavaPop>(movement.gameObject).level = level;
                break;
            case BoonType.Flame:
                if (movement != null) Ensure<Flamethrower>(movement.gameObject).level = level;
                break;
            case BoonType.Bombs:
                if (movement != null) Ensure<BombDropper>(movement.gameObject).level = level;
                break;
            case BoonType.Whip:
                if (movement != null) Ensure<FuetWhip>(movement.gameObject).level = level;
                break;
            case BoonType.Tortilla:
                if (movement != null) Ensure<TortillaThrower>(movement.gameObject).level = level;
                break;

            case BoonType.Evolution:
                break; // els plats estrella s'activen a ChooseEvolution
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

    // la icona d'un plat, des de qualsevol lloc (el Receptari)
    public static Sprite DishIcon(Boon boon)
    {
        if (boon == null) return null;
        return boon.icon != null ? boon.icon : Resources.Load<Sprite>("Dishes/" + Slug(boon.name));
    }

    public static string DishKey(Boon boon) { return boon != null ? Slug(boon.name) : ""; }

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
        if (b.type == BoonType.Evolution)
            return b.description + "\n<color=#A8650A>Transforma: " + (b.evolvesFrom != null ? b.evolvesFrom.name : "") + "</color>";
        int next = LevelOf(b) + 1;
        string a = DescribeEffect(b.type, AmountFor(b, next), next);
        if (b.type2 == BoonType.None) return a ?? b.description;

        string c = DescribeEffect(b.type2, b.amount2);
        if (a == null || c == null) return b.description;
        return a + "\n" + c;
    }

    // torna null si no es pot calcular (llavors es fa servir la descripció escrita)
    string DescribeEffect(BoonType type, float amount, int level = 1)
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
                    + F(1f / Mathf.Clamp(shooting.fireRate * (1f - amount), 0.07f, 1.5f), 1);

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
                return "Mal del foc ×" + F(DamageKind.FireMul, 2) + " → ×" + F(DamageKind.FireMul + amount, 2);

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
                    ? "Cures 1 cor cada " + r + " clients (com a molt 1 per onada)"
                    : "1 cor cada " + killsPerHeal + " → " + Mathf.Max(8, killsPerHeal - 4) + " clients" + (level >= 3 ? " (fins a 2 per onada)" : "");

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
                return "En rebre un cop: ona que empeny i fa " + (1 + level) + " de mal";

            case BoonType.DashRing:
            {
                PanelletsRing p = movement != null ? movement.GetComponent<PanelletsRing>() : null;
                int cur = p != null ? p.count : 0;
                return "Panellets per dash " + cur + " → " + (cur == 0 ? r : cur + 3);
            }

            case BoonType.KillBurst:
                return Mathf.RoundToInt((0.3f + 0.15f * (level - 1)) * 100f) + "% d'esclatar en morir (fa el teu dany al voltant)";

            // ---- complements nous ----
            case BoonType.CritDamage:
                if (shooting == null) return null;
                return "Mal dels crítics ×" + F(shooting.critMultiplier, 1) + " → ×" + F(shooting.critMultiplier + amount, 1);
            case BoonType.Regen:
                return "Cures 1 cor cada " + Mathf.RoundToInt(Mathf.Max(20f, amount - 10f * (level - 1))) + " s";
            case BoonType.Dodge:
                if (health == null) return null;
                return "Esquivar " + Mathf.RoundToInt(health.dodgeChance * 100f) + "% → " + Mathf.RoundToInt(Mathf.Min(0.4f, health.dodgeChance + amount) * 100f) + "%";
            case BoonType.Area:
                return "Mida de les àrees ×" + F(DishArea.Mul, 2) + " → ×" + F(DishArea.Mul + amount, 2);
            case BoonType.ProjectileSize:
                if (shooting == null) return null;
                return "Mida dels projectils ×" + F(shooting.projectileScale, 1) + " → ×" + F(shooting.projectileScale + amount, 1);
            case BoonType.Knockback:
                if (shooting == null) return null;
                return "Empenta dels trets " + F(shooting.knockback, 1) + " → " + F(shooting.knockback + amount, 1);
            case BoonType.Rerolls:
                return "Canvis de cartes per oferta " + rerollsPerOffer + " → " + (rerollsPerOffer + r);
            case BoonType.Magnet:
                return "Abast per recollir monedes ×" + F(XpCoin.MagnetMul, 1) + " → ×" + F(XpCoin.MagnetMul + amount, 1);

            // ---- especialitats noves (què fa el nivell que agafaràs) ----
            case BoonType.BackKnives:
                return "Ganivets cap enrere: " + level;
            case BoonType.Soup:
                return "Un toll cada " + F(Mathf.Max(1.8f, 4f - 0.5f * (level - 1)), 1) + " s" + (level >= 3 ? ", fa 2 de mal" : "");
            case BoonType.PieRain:
                return "Un pastís cada " + F(Mathf.Max(1.4f, 3.2f - 0.45f * (level - 1)), 1) + " s, fa " + (3 + level) + " de mal";
            case BoonType.Cava:
                return "Bombolles cada " + F(Mathf.Max(2.5f, 6f - 0.8f * (level - 1)), 1) + " s, fan " + (2 + level) + " de mal";
            case BoonType.Flame:
                return "Flames d'abast " + F(2.4f + 0.35f * (level - 1), 1) + ", fan " + (1 + (level - 1) / 2) + " de mal";
            case BoonType.Bombs:
                return "Una bomba cada " + F(Mathf.Max(2f, 5f - 0.8f * (level - 1)), 1) + " s, fa " + (5 + 2 * level) + " de mal";
            case BoonType.Whip:
                return "Un cop de fuet cada " + F(Mathf.Max(0.7f, 1.3f - 0.15f * (level - 1)), 2) + " s, fa " + (2 + level) + " de mal";
            case BoonType.Tortilla:
                return "Una truita cada " + F(Mathf.Max(1.2f, 2.6f - 0.35f * (level - 1)), 1) + " s, fa " + (2 + level) + " de mal";
        }
        return null;
    }

    static string TagFor(Boon boon, out Color col)
    {
        if (boon.type == BoonType.Evolution) { col = new Color(0.80f, 0.55f, 0.08f); return "PLAT ESTRELLA"; }
        if (boon.type2 != BoonType.None && boon.amount2 < 0f) // el segon efecte és dolent
        {
            col = new Color(0.72f, 0.16f, 0.14f);
            return "AMB RISC";
        }
        if (IsSpecialty(boon)) { col = new Color(0.42f, 0.26f, 0.64f); return "ESPECIALITAT"; }
        if (IsConsumable(boon)) { col = new Color(0.17f, 0.45f, 0.30f); return "DEL DIA"; }
        col = new Color(0.20f, 0.38f, 0.60f);
        return "COMPLEMENT";
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
        cardFx.Clear();

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
        // el marc, del color de la raresa (les postres i els plats estrella, el de sempre)
        Rarity rarity = RarityOf(boon);
        bool showRarity = boon.type != BoonType.Evolution && !restBoons.Contains(boon);
        Color frameCol = showRarity && rarity != Rarity.Comu ? Color.Lerp(Frame, RarityColor(rarity), 0.85f) : Frame;
        if (shiny.Contains(boon)) frameCol = new Color(1f, 0.9f, 0.4f);
        Image frame = Box(card.transform, "Vora", frameCol, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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

        // a baix de la capçalera: la raresa (i si la carta és brillant)
        if (showRarity && (rarity != Rarity.Comu || shiny.Contains(boon)))
        {
            string rtxt = (shiny.Contains(boon) ? "BRILLANT ×2 · " : "") + RarityName(rarity);
            float rW = rtxt.Length * tagSize * 0.68f + u * 4f;
            RectTransform rRt = MakeFixed(hdr, "Raresa", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, u * 2f), new Vector2(rW, tagSize * 1.5f + u));
            Image rFrame = rRt.gameObject.AddComponent<Image>();
            rFrame.color = Frame;
            rFrame.raycastTarget = false;
            Image rFace = Box(rRt, "Fons", shiny.Contains(boon) ? new Color(0.85f, 0.65f, 0.1f) : Color.Lerp(RarityColor(rarity), Color.black, 0.25f), Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
            Label(rFace.rectTransform, rtxt, tagSize, TextAlignmentOptions.Center, Paper, true, false);
        }

        // a dalt a la dreta: NOU, o el nivell al qual el pujaràs
        int have = taken.ContainsKey(boon) ? taken[boon] : 0;
        if (!IsConsumable(boon) && !restBoons.Contains(boon))
        {
            int next = have + 1;
            string haveText = have == 0 ? "NOU" : (next >= MaxLevel(boon) ? "NIVELL MÀX" : "NIVELL " + next);
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
        cardFx.Add(fx);
        fx.focused = index == padFocus; // en canviar les cartes, la marca es manté al mateix lloc
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
        subtitleText = Label(subRt, "Clica un plat o prem la seva tecla", size * 0.4f, TextAlignmentOptions.Center,
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
            : "Curar " + healAmount + (healAmount > 1 ? " cors" : " cor") + " en lloc d'un plat (" + (GamepadInput.UsingGamepad ? GamepadInput.West() : "H") + ")";
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
            ? "Canviar les cartes (" + (GamepadInput.UsingGamepad ? GamepadInput.North() : "R") + ") · " + rerollsLeft + (rerollsLeft > 1 ? " canvis" : " canvi")
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

    // ---------- ELS PLATS QUE TENS ----------
    // Mentre jugues només es veu un comptador petit sota els cors ("34 plats · 13 conjunts · Tab").
    // Mantenint premut Tab (o Select / Share al comandament) surt la làmina sencera al mig de la pantalla,
    // amb tots els plats i els conjunts (nom i què fan). També funciona amb el menú de pausa obert.

    void BuildTakenText()
    {
        // el comptador
        Canvas canvas = UiCanvas();
        float fs = Mathf.Max(15f, Screen.height * 0.021f);
        RectTransform cr = MakeFixed(canvas.transform, "ComptadorPlats", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -Mathf.Max(52f, Screen.height * 0.058f)), new Vector2(fs * 26f, fs * 1.5f));
        collectionCounter = Label(cr, "", fs, TextAlignmentOptions.Left, Gold, true, false);
        collectionCounter.textWrappingMode = TextWrappingModes.NoWrap;
        UIStyle.Outline(collectionCounter);
        BuildXpBar(canvas);

        // la làmina: un canvas propi per sobre de tot (també de la pausa)
        GameObject cgo = new GameObject("ColleccioPlats", typeof(Canvas));
        collectionRoot = cgo;
        Canvas c = cgo.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 650;
        Box(cgo.transform, "Fosc", new Color(0f, 0f, 0f, 0.62f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        float u = PixelUnit();
        Vector2 size = new Vector2(Mathf.Max(480f, Screen.width * 0.74f), Mathf.Max(320f, Screen.height * 0.82f));
        Vector2 mid = new Vector2(0.5f, 0.5f);
        RectTransform sheet = MakeFixed(cgo.transform, "Lamina", mid, mid, Vector2.zero, size);
        Image fr = sheet.gameObject.AddComponent<Image>();
        fr.color = Frame;
        fr.raycastTarget = false;
        Box(sheet, "Fons", new Color(0.24f, 0.16f, 0.11f, 1f), Vector2.zero, Vector2.one, new Vector2(u * 1.5f, u * 1.5f), new Vector2(-u * 1.5f, -u * 1.5f));
        Box(sheet, "Vora", Gold, new Vector2(0f, 1f), Vector2.one, new Vector2(u * 4f, -u * 4f - u), new Vector2(-u * 4f, -u * 4f));

        float titleH = Mathf.Max(28f, Screen.height * 0.05f);
        RectTransform title = MakeFixed(sheet, "Titol", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -u * 6f), new Vector2(size.x, titleH));
        collectionTitle = Label(title, "ELS TEUS PLATS", titleH * 0.75f, TextAlignmentOptions.Center, Gold, true, false);

        float hintH = Mathf.Max(18f, Screen.height * 0.025f);
        RectTransform hint = MakeFixed(sheet, "Ajuda", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, u * 4f), new Vector2(size.x, hintH));
        Label(hint, "Deixa anar per tornar al joc", hintH * 0.8f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.8f, 0.7f), false, false);

        float top = u * 6f + titleH + u * 6f;
        takenStrip = MakeFixed(sheet, "Graella", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(u * 10f, -top),
            new Vector2(size.x - u * 20f, size.y - top - hintH - u * 8f));

        cgo.SetActive(false);
        RefreshCounter();
    }

    private RectTransform xpFill;
    private TextMeshProUGUI xpLabel;

    void BuildXpBar(Canvas canvas)
    {
        float h = Mathf.Max(12f, Screen.height * 0.016f);
        Image bg = Box(canvas.transform, "BarraExperiencia", new Color(0.08f, 0.05f, 0.04f, 0.85f), Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, h));
        Image fill = Box(bg.rectTransform, "Ple", Gold, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 2f), new Vector2(0f, -2f));
        xpFill = fill.rectTransform;
        RectTransform lr = MakeFixed(canvas.transform, "NivellXef", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, h + 2f), new Vector2(200f, h * 1.8f));
        xpLabel = Label(lr, "", h * 1.3f, TextAlignmentOptions.BottomLeft, Gold, true, false);
        xpLabel.textWrappingMode = TextWrappingModes.NoWrap;
        UIStyle.Outline(xpLabel);
        bg.gameObject.SetActive(xpMode);
        lr.gameObject.SetActive(xpMode);
    }

    void UpdateXpBar()
    {
        if (xpFill == null) return;
        float k = Mathf.Clamp01(Xp / XpToNext);
        xpFill.anchorMax = new Vector2(Mathf.MoveTowards(xpFill.anchorMax.x, k, Time.unscaledDeltaTime * 2f), 1f);
        if (xpLabel != null) xpLabel.text = "NIVELL " + (ChefLevel + 1);
    }

    void RefreshCounter()
    {
        if (collectionCounter == null) return;
        int dishes = 0;
        foreach (KeyValuePair<Boon, int> kv in taken) dishes += kv.Value;
        int sets = 0;
        foreach (SetBonus set in SetBonuses.All) if (SetBonuses.Has(set.id)) sets++;
        string key = GamepadInput.UsingGamepad ? GamepadInput.SelectButton() : "Tab";
        collectionCounter.text = dishes == 0 ? "" :
            "Esp. " + SpecialtiesUsed + "/" + maxSpecialties + " · Compl. " + SidesUsed + "/" + maxSides + (sets > 0 ? "  ·  " + sets + (sets == 1 ? " conjunt" : " conjunts") : "")
            + "   <color=#FFFFFFAA><size=80%>[" + key + "]</size></color>";
    }

    // mantenint premut Tab / Select es veu la làmina sencera
    private bool lastUsingPad;
    void Update()
    {
        // pujar de nivell: la pantalla de triar plat surt enmig de l'onada (com Vampire Survivors)
        if (xpMode && pendingLevels > 0 && !offering && CanOfferNow()) StartCoroutine(LevelUpRoutine());
        UpdateXpBar();

        if (collectionRoot == null) return;
        bool held = (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
                 || (GamepadInput.ActivePad != null && GamepadInput.ActivePad.selectButton.isPressed);
        if (taken.Count == 0) held = false;
        if (collectionRoot.activeSelf != held) collectionRoot.SetActive(held);
        if (GamepadInput.UsingGamepad != lastUsingPad) { lastUsingPad = GamepadInput.UsingGamepad; RefreshCounter(); }
    }

    void OnDisable()
    {
        if (collectionRoot != null) collectionRoot.SetActive(false);
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

            if (kv.Value > 1 || !IsConsumable(kv.Key)) // el nivell (o les vegades, si és de curar o de punts)
            {
                Vector2 br = new Vector2(1f, 0f);
                RectTransform badge = MakeFixed(chip, "Quantitat", br, br, new Vector2(u * 0.5f, -u * 0.5f),
                    new Vector2(s * 0.46f, s * 0.38f));
                Image bf = badge.gameObject.AddComponent<Image>();
                bf.color = Frame;
                bf.raycastTarget = false;
                Label(badge, IsConsumable(kv.Key) ? "x" + kv.Value : (kv.Value >= MaxLevel(kv.Key) ? "MÀX" : "Nv" + kv.Value),
                    s * 0.26f, TextAlignmentOptions.Center, Gold, true, true);
            }

            n++;
        }

        if (collectionTitle != null) collectionTitle.text = "ELS TEUS PLATS  ·  " + TablesText().ToUpperInvariant();

        // sota les icones, els conjunts activats (a dues columnes, amb el nom i què fan)
        int rows = (n + perRow - 1) / perRow;
        float y = -rows * (s + gap) - u * 4f;
        float lh = Mathf.Round(s * 0.62f);
        float colW = (takenStrip.sizeDelta.x - u * 2f) / 2f;
        int k = 0;
        foreach (SetBonus set in SetBonuses.All)
        {
            if (!SetBonuses.Has(set.id)) continue;
            int col = k % 2;
            float yy = y - (k / 2) * (lh + u);
            RectTransform chip = MakeFixed(takenStrip, "Conjunt_" + set.id, tl, tl, new Vector2(col * (colW + u * 2f), yy), new Vector2(colW, lh));
            Image fr = chip.gameObject.AddComponent<Image>();
            fr.color = Frame;
            fr.raycastTarget = false;
            Image face = Box(chip, "Fons", new Color(0.30f, 0.20f, 0.12f, 1f), Vector2.zero, Vector2.one,
                new Vector2(u * 0.5f, u * 0.5f), new Vector2(-u * 0.5f, -u * 0.5f));
            TextMeshProUGUI t = Label(face.rectTransform, "<color=#FFCC4D><b>" + set.name + "</b></color>  " + set.effect,
                lh * 0.42f, TextAlignmentOptions.Left, new Color(1f, 0.94f, 0.84f), false, true);
            t.margin = new Vector4(u * 3f, 0f, u * 2f, 0f);
            k++;
        }
        RefreshCounter();
    }
}
