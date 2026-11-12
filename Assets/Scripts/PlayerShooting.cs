using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public GameObject[] projectilePrefabs;
    public float projectileSpeed = 12f;
    public float fireRate = 0.3f;     // segons entre cada tret
    public float spawnOffset = 0.7f;  // distància davant del xef
    public int damage = 1;

    private Camera cam;
    public static int ShotsFired; // trets fets des que ha començat el joc (el tutorial ho mira)
    private float nextFireTime;
    private Vector2 lastPadAim = Vector2.right; // última direcció apuntada amb el joystick dret
    public float aimDeadZone = 0.3f;            // zona morta del joystick dret

    [Header("Comandament")]
    public float aimAssistAngle = 14f;   // graus: si hi ha un client tan a prop de la línia de tret, s'hi apunta
    public float aimAssistRange = 11f;   // fins on mira l'ajuda a apuntar
    private Transform reticle;           // la mira que es veu davant del xef quan es juga amb comandament
    private SpriteRenderer reticleSr;

    // efectes dels plats (els posa el BoonManager)
    public int extraShots = 0;        // projectils extra en ventall
    public float spreadAngle = 12f;   // graus entre projectils
    [Range(0.2f, 1f)] public float sideShotDamage = 0.7f; // els projectils del ventall (no el central) fan aquesta part del dany
    public int pierce = 0;
    [HideInInspector] public bool boomerangShots = false; // el Pizzer: pizzes que travessen i tornen
    [HideInInspector] public int backShots = 0;          // Pernil tallat a ganivet: trets cap enrere
    [HideInInspector] public float projectileScale = 1f; // Allioli: projectils més grans
    [HideInInspector] public float knockback = 0f;       // Tomàquet de penjar: els trets empenyen
    [HideInInspector] public Vector2 LastAim = Vector2.right; // cap a on s'ha disparat l'últim cop (l'Escalivada hi apunta)
    private int shotCount;
    static Sprite pizzaSprite;
    public int bounces = 0;           // rebots a les parets (Mandonguilles)
    public float explosionRadius = 0f;
    public float slowFactor = 1f;
    public float slowDuration = 0f;
    public float critChance = 0f;     // probabilitat de crític (0-1)
    public float critMultiplier = 2f; // el dany d'un crític

    // tret ràpid temporal (el conjunt Sobretaula): el temps entre trets es multiplica per aquest valor
    private float hasteUntil;
    private const float HasteFactor = 0.71f;

    // el temps real entre trets: el dels plats, més el tret ràpid si està actiu
    float CurrentFireRate
    {
        get { return Time.time < hasteUntil ? fireRate * HasteFactor : fireRate; }
    }

    public void GrantHaste(float seconds)
    {
        hasteUntil = Mathf.Max(hasteUntil, Time.time + seconds);
    }

    void Awake()
    {
        cam = Camera.main;
    }

    // ---------- LA CALOR DE LA PAELLA (sobreescalfament) ----------
    // Disparar sense parar escalfa la paella; si arriba al màxim, s'encalla uns segons i no pots disparar.
    // Escalfa igual per segon sigui quin sigui el ritme de tret (el Pastisser no s'escalfa abans que el Pizzer).
    public float heatPerSecond = 0.22f;   // ~4,5 s disparant sense parar per encallar-se
    public float coolPerSecond = 0.5f;    // es refreda quan no dispares
    public float overheatTime = 1.5f;     // segons encallada
    public float Heat { get; private set; }
    public bool Overheated => Time.time < overheatedUntil;
    private float overheatedUntil, lastShotTime;
    private SpriteRenderer heatBg, heatFill;

    void Update()
    {
        if (Time.timeScale == 0) return; // no disparar mentre la botiga està oberta
        UpdateHeat();
        if (Overheated) return;
        if (Time.time < nextFireTime) return;

        if (TryGetAim(out Vector2 dir))
        {
            nextFireTime = Time.time + CurrentFireRate;
            Shoot(dir);
            lastShotTime = Time.time;
            Heat += heatPerSecond * (Progression.Difficulty == Difficulty.Facil ? 0.8f : 1f) * CurrentFireRate;
            if (Heat >= 1f) Overheat();
        }
    }

    void Overheat()
    {
        Heat = 1f;
        overheatedUntil = Time.time + overheatTime;
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.5f, "MASSA CALENT!", new Color(1f, 0.45f, 0.3f), 1f);
        AudioManager.Play("fuse", 1f, 1.4f);
        for (int i = 0; i < 4; i++) Fx.Steam((Vector2)transform.position + Random.insideUnitCircle * 0.4f + Vector2.up * 0.3f);
        Squash.Kick(gameObject, 0.2f, -0.2f);
    }

    void UpdateHeat()
    {
        if (Overheated) Heat = Mathf.Clamp01((overheatedUntil - Time.time) / overheatTime); // es buida mentre està encallada
        else if (Time.time - lastShotTime > 0.25f) Heat = Mathf.Max(0f, Heat - coolPerSecond * Time.deltaTime);

        // la barreta de calor, sota el xef (només quan s'escalfa)
        if (heatBg == null)
        {
            heatBg = MakeBar("CalorFons", new Color(0.1f, 0.06f, 0.06f, 0.85f), 30);
            heatFill = MakeBar("Calor", Color.white, 31);
        }
        bool show = Heat > 0.05f && !PlayerHealth.IsDying;
        heatBg.enabled = heatFill.enabled = show;
        if (!show) return;
        const float W = 1.1f, Hh = 0.13f;
        Vector2 at = (Vector2)transform.position + Vector2.down * 0.75f;
        heatBg.transform.position = at;
        heatBg.transform.localScale = new Vector3(W + 0.08f, Hh + 0.08f, 1f);
        heatFill.transform.position = at + Vector2.left * (W * (1f - Heat) * 0.5f);
        heatFill.transform.localScale = new Vector3(W * Heat, Hh, 1f);
        Color col = Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.25f, 0.15f), Heat);
        if (Overheated || Heat > 0.8f) col = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? col : Color.white; // avisa abans d'encallar-se
        heatFill.color = col;
    }

    SpriteRenderer MakeBar(string name, Color c, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = c;
        sr.sortingOrder = order;
        return sr;
    }

    // ratolí: clic esquerre i apunta al cursor
    // comandament: apunta amb el joystick dret i dispara movent-lo o amb el gallet dret
    bool TryGetAim(out Vector2 dir)
    {
        dir = Vector2.zero;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            if (stick.magnitude > aimDeadZone) lastPadAim = stick.normalized;

            bool trigger = pad.rightTrigger.isPressed;
            if (stick.magnitude > aimDeadZone || trigger)
            {
                dir = AimAssist(lastPadAim);
                return true;
            }
        }

        Mouse mouse = Mouse.current;
        if (DebugMode.MouseOverPanel) return false; // no disparar en clicar el quadre del mode de prova
        if (PauseMenu.SuppressMouseFire)
        {
            // el clic de "Continuar" encara està premut: esperem que el deixi anar
            if (mouse == null || !mouse.leftButton.isPressed) PauseMenu.SuppressMouseFire = false;
            return false;
        }
        if (mouse != null && mouse.leftButton.isPressed && cam != null)
        {
            Vector2 mouseWorld = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            dir = (mouseWorld - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero) dir = lastPadAim;
            return true;
        }

        return false;
    }

    // amb comandament costa apuntar fi: si hi ha un client a prop de la línia de tret, s'hi apunta
    Vector2 AimAssist(Vector2 aim)
    {
        Vector2 me = transform.position;
        Vector2 best = aim;
        float bestAngle = aimAssistAngle;
        foreach (Enemy e in Enemy.All)
        {
            if (e == null || e.IsDead) continue;
            Vector2 to = (Vector2)e.transform.position - me;
            float d = to.magnitude;
            if (d < 0.01f || d > aimAssistRange) continue;
            float a = Vector2.Angle(aim, to);
            if (a < bestAngle) { bestAngle = a; best = to / d; }
        }
        return best;
    }

    // la mira: una fletxeta davant del xef, cap on apunta el joystick dret (només amb comandament)
    void LateUpdate()
    {
        bool show = GamepadInput.UsingGamepad && Time.timeScale > 0f && !PlayerHealth.IsDying;
        if (reticle == null)
        {
            if (!show) return;
            GameObject go = new GameObject("Mira");
            reticle = go.transform;
            reticleSr = go.AddComponent<SpriteRenderer>();
            reticleSr.sprite = SpriteFactory.Arrow();
            reticleSr.color = new Color(1f, 0.9f, 0.5f, 0.85f);
            reticleSr.sortingOrder = 6;
            go.transform.localScale = Vector3.one * 0.38f;
        }
        reticleSr.enabled = show;
        if (!show) return;
        Vector2 aim = lastPadAim.sqrMagnitude > 0.01f ? lastPadAim : Vector2.right;
        reticle.position = (Vector2)transform.position + aim * 1.3f;
        reticle.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
    }

    void OnDestroy()
    {
        if (reticle != null) Destroy(reticle.gameObject);
        if (heatBg != null) Destroy(heatBg.gameObject);
        if (heatFill != null) Destroy(heatFill.gameObject);
    }

    // un tret extra (ganivets, cercle de la paella): com un tret normal, sense crític
    static Sprite knifeSprite;

    // 'clean': un tret net, sense explosions, perforació ni rebots (els ganivets del pernil)
    void FireExtra(float angle, Sprite sprite = null, bool clean = false)
    {
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        GameObject prefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];
        Projectile proj = Projectile.Get(prefab, (Vector2)transform.position + dir * spawnOffset, Quaternion.Euler(0f, 0f, angle));
        if (proj == null) return;
        proj.damage += damage - 1;
        if (!clean)
        {
            proj.pierce = pierce;
            proj.bounces = bounces;
            proj.slowFactor = slowFactor;
            proj.slowDuration = slowDuration;
        }
        proj.explosionRadius = 0f; // els trets extra no exploten (només el tret central)
        proj.knockback = knockback;
        if (projectileScale != 1f) proj.transform.localScale *= projectileScale;
        if (sprite != null) proj.SetSprite(sprite);
        proj.Launch(dir * projectileSpeed);
    }

    void Shoot(Vector2 baseDir)
    {
        if (projectilePrefabs == null || projectilePrefabs.Length == 0) return;
        ShotsFired++;
        shotCount++;
        LastAim = baseDir;
        AudioManager.Play("shoot");
        Squash.Kick(gameObject, -0.04f, 0.06f); // dibuix animat: una petita sacsejada en llançar

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        // plats de conjunt
        bool anyCrit = false;

        int total = 1 + extraShots;
        // el ventall reparteix el mal: com més trets, menys en fa cadascun dels dels costats
        // (3 trets: 0,6 cadascun · 5: 0,36 · 7: 0,3); la Paella exprés en dona més
        float fanShare = total > 1 ? Mathf.Clamp(1.8f / total, 0.3f, 1f) : 1f;
        float sideDamage = Mathf.Min(sideShotDamage, fanShare);
        if (SetBonuses.Has(SetId.FullFan)) sideDamage = Mathf.Min(1f, fanShare * 1.6f); // Paella exprés
        int center = total / 2;
        for (int i = 0; i < total; i++)
        {
            // els projectils es reparteixen en ventall, centrats cap al ratolí
            float angle = baseAngle + (i - (total - 1) / 2f) * spreadAngle;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject prefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];
            Vector2 spawnPos = (Vector2)transform.position + dir * spawnOffset;
            Projectile proj = Projectile.Get(prefab, spawnPos, Quaternion.Euler(0f, 0f, angle)); // de la reserva
            if (proj == null) continue; // prefab mal configurat
            proj.damage += damage - 1; // dany del prefab + millores
            bool isCenter = i == center;
            if (total > 1 && !isCenter && sideDamage < 1f)
            {
                // la part del dany que toca, en mitjana: amb 1 de dany i la meitat, un tret de cada dos fa mal
                float exact = proj.damage * sideDamage;
                int whole = Mathf.FloorToInt(exact);
                if (Random.value < exact - whole) whole++;
                proj.damage = whole;
                proj.transform.localScale *= 0.8f; // i es veuen més petits
            }
            if (critChance > 0f && Random.value < critChance)
            {
                proj.damage = Mathf.RoundToInt(proj.damage * critMultiplier);
                proj.isCrit = true;
                anyCrit = true;
            }
            proj.pierce = pierce;
            proj.bounces = bounces;
            proj.explosionRadius = isCenter ? explosionRadius : 0f; // només explota el tret central (els calçots)
            proj.slowFactor = slowFactor;
            proj.slowDuration = slowDuration;
            proj.knockback = knockback;
            if (projectileScale != 1f) proj.transform.localScale *= projectileScale;
            if (boomerangShots)
            {
                if (pizzaSprite == null) pizzaSprite = Resources.Load<Sprite>("Projectiles/Pizza");
                proj.boomerang = true;
                proj.SetSprite(pizzaSprite);
            }

            proj.Launch(dir * projectileSpeed);
        }

        // Pernil tallat a ganivet: ganivets cap enrere (i, amb el Pernil de gla, també cap als costats)
        int backs = backShots + (backShots > 0 && SetBonuses.Has(SetId.PicaPica) ? 1 : 0); // conjunt Pica-pica
        if (backs > 0)
        {
            if (knifeSprite == null) knifeSprite = Resources.Load<Sprite>("Projectiles/Ganivet");
            for (int k = 0; k < backs; k++)
            {
                float off = (k - (backs - 1) / 2f) * 14f;
                // els ganivets van nets: el seu mal, sense explosions, perforació ni rebots
                FireExtra(baseAngle + 180f + off, knifeSprite, true);
                if (Evo.Has(EvoId.PernilDeGla)) { FireExtra(baseAngle + 90f + off, knifeSprite, true); FireExtra(baseAngle - 90f + off, knifeSprite, true); }
            }
        }
        // Paella gegant: cada 3 trets, un cercle sencer
        if (Evo.Has(EvoId.PaellaGegant) && shotCount % 3 == 0)
            for (int k = 0; k < 10; k++) FireExtra(baseAngle + 18f + k * 36f);

        // Cigaló: un crític et deixa tornar a disparar de seguida
        if (anyCrit && SetBonuses.Has(SetId.Carajillo))
            nextFireTime = Time.time + CurrentFireRate * 0.3f;
    }
}