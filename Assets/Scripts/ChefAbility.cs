using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  L'HABILITAT DE CADA XEF (tecla Q o clic dret; al comandament, el botó superior esquerre)
// =====================================================================
//   EL XEF        ONA DE CALOR     esclat de foc al teu voltant: fa mal, empeny i esborra els trets dels clients
//   EL CARNISSER  GANIVETADA       un cop de ganivet a terra davant teu: mal crític enorme i deixa els clients glaçats
//   EL PASTISSER  GLASSA           glaça tots els clients del voltant i deixa el terra gelat
//   EL SUSHIMAN   TALL DE KATANA   travessa la sala d'una estrebada i talla tot el que troba
//   EL PIZZER     FORN DE LLENYA   un anell de flames et segueix: la vora crema i empeny cap enfora, i acaba en fogonada
// Fa més mal com més nivell tens (experiència). El rellotge de recàrrega surt a baix a l'esquerra.
public class ChefAbility : MonoBehaviour
{
    public class Def
    {
        public string name, desc;
        public float cooldown;
        public Color color;
        public Def(string n, string d, float cd, Color c) { name = n; desc = d; cooldown = cd; color = c; }
    }

    // en el mateix ordre que ChefRoster.All
    public static readonly Def[] Defs =
    {
        new Def("ONA DE CALOR", "Esclat de foc al teu voltant: fa mal, empeny i esborra els trets", 10f, new Color(1f, 0.55f, 0.2f)),
        new Def("GANIVETADA", "Cop de ganivet a terra: mal crític enorme i glaça", 9f, new Color(0.9f, 0.3f, 0.3f)),
        new Def("GLASSA", "Glaça els clients del voltant i el terra", 13f, new Color(0.6f, 0.88f, 1f)),
        new Def("TALL DE KATANA", "Travessa la sala d'una estrebada tallant-ho tot", 8f, new Color(0.5f, 0.6f, 1f)),
        new Def("FORN DE LLENYA", "Un anell de foc et segueix: la vora crema i empeny els clients, i acaba en fogonada", 14f, new Color(1f, 0.4f, 0.15f)),
    };

    public static Def For(int chef) { return Defs[Mathf.Clamp(chef, 0, Defs.Length - 1)]; }

    public static string KeyName()
    {
        return GamepadInput.UsingGamepad ? GamepadInput.LeftShoulder() : "Q";
    }

    private int chef;
    private Def def;
    private float readyAt;
    private bool wasReady = true;
    private PlayerMovement movement;
    private Rigidbody2D rb;

    // HUD
    private Image fill, ring;
    private TextMeshProUGUI keyText, nameText;
    private RectTransform hudRoot;
    private float pop;

    public static void Attach(GameObject player, int chefIndex)
    {
        if (player == null) return;
        ChefAbility a = player.GetComponent<ChefAbility>();
        if (a == null) a = player.AddComponent<ChefAbility>();
        a.chef = chefIndex;
        a.def = For(chefIndex);
    }

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        if (def == null) def = For(chef);
        readyAt = Time.time + 3f; // no ja de bon principi
        wasReady = false;
        BuildHud();
    }

    static int Level => BoonManager.Instance != null ? BoonManager.Instance.ChefLevel : 0;

    // ---------- L'HABILITAT MILLORA AMB EL NIVELL DE XEF (cada 5 nivells, fins al IV) ----------
    //   ONA DE CALOR    II: una segona ona   III: deixa un anell de brases   IV: més gran
    //   GANIVETADA      II: dos ganivets     III: l'esquerda talla en línia   IV: tres ganivets
    //   GLASSA          II: més gran i dura més   III: la cúpula esclata en estelles   IV: glaça més els caps
    //   TALL DE KATANA  II: dos talls en X   III: tres talls en estrella   IV: més llarg i més mal
    //   FORN DE LLENYA  II: dura més   III: més gran   IV: la fogonada final és el doble
    public static int Tier => Mathf.Clamp(1 + Level / 5, 1, 4);
    static readonly string[] Roman = { "", "", " II", " III", " IV" };
    int shownTier = 1;

    void WatchTier()
    {
        if (Tier == shownTier) return;
        shownTier = Tier;
        if (shownTier >= 4) Achievements.Unlock("tier4");
        if (nameText != null) nameText.text = def.name + Roman[shownTier];
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 2.2f, def.name + Roman[shownTier] + "!", def.color, 1.6f);
        AudioManager.Play("set", 0.8f, 1.1f);
        pop = 1f;
    }
    bool Ready => Time.time >= readyAt;

    bool Pressed()
    {
        Keyboard kb = Keyboard.current;
        Mouse ms = Mouse.current;
        Gamepad pad = GamepadInput.ActivePad;
        return (kb != null && kb.qKey.wasPressedThisFrame)
            || (ms != null && ms.rightButton.wasPressedThisFrame)
            || (pad != null && pad.leftShoulder.wasPressedThisFrame);
    }

    void Update()
    {
        UpdateHud();
        WatchTier();
        if (PlayerHealth.IsDying || Time.timeScale < 0.5f || PauseMenu.IsOpen || GamepadInput.CustomScreens > 0) return;

        if (!wasReady && Ready)
        {
            wasReady = true;
            pop = 1f;
            AudioManager.Play("boon", 0.4f, 1.5f);
        }
        if (!Pressed()) return;
        if (!Ready)
        {
            AudioManager.Play("ui_back", 0.4f);
            return;
        }

        readyAt = Time.time + def.cooldown;
        wasReady = false;
        Stats.Add("abilities");
        if (Stats.Get("abilities") >= 100) Achievements.Unlock("abil100");
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.6f, def.name + "!", def.color, 0.9f);
        GamepadInput.Rumble(0.5f, 0.5f, 0.2f);
        switch (chef)
        {
            case 1: StartCoroutine(Ganivetada()); break;
            case 2: StartCoroutine(Glassa()); break;
            case 3: StartCoroutine(Katana()); break;
            case 4: StartCoroutine(Forn()); break;
            default: StartCoroutine(OnaDeCalor()); break;
        }
    }

    // cap a on apunta el jugador (el ratolí o l'últim tret amb el comandament)
    Vector2 Aim()
    {
        Vector2 aim = Vector2.right;
        PlayerShooting s = PlayerRef.Shooting;
        if (s != null && s.LastAim.sqrMagnitude > 0.01f) aim = s.LastAim;
        Mouse ms = Mouse.current;
        Camera cam = Camera.main;
        if (!GamepadInput.UsingGamepad && ms != null && cam != null)
        {
            Vector2 w = cam.ScreenToWorldPoint(ms.position.ReadValue());
            Vector2 d = w - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.01f) aim = d;
        }
        return aim.normalized;
    }

    // ---------- LES HABILITATS ----------

    // ONA DE CALOR: una ona de flames que s'expandeix des del xef; esborra els trets que toca
    IEnumerator OnaDeCalor()
    {
        int waves = Tier >= 2 ? 2 : 1;
        float r = 3.5f * DishArea.Mul * (Tier >= 4 ? 1.3f : 1f);
        int dmg = 6 + 2 * Level + DishUtil.Bonus;
        CameraFX.Shake(0.2f, 0.18f);
        for (int w = 0; w < waves; w++)
        {
            Vector2 p = transform.position;
            AudioManager.Play("explosion", 0.5f, 1.3f - w * 0.15f);
            Fx.Shockwave(p, r, new Color(1f, 0.6f, 0.25f));
            DishUtil.HitCircle(p, r, w == 0 ? dmg : dmg / 2 + 1, 10f, DmgKind.Fire);
            const int N = 18;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float rr = r * (t / 0.35f);
                for (int i = 0; i < N; i++)
                {
                    float a = (i * 360f / N + w * 10f) * Mathf.Deg2Rad;
                    if (Random.value < 0.55f) Fx.FlameAt(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr, Mathf.Lerp(0.5f, 0.85f, t / 0.35f), 0.4f);
                }
                // els trets dels clients que l'ona toca es fonen
                foreach (EnemyProjectile ep in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (ep != null && Vector2.Distance(ep.transform.position, p) <= rr + 0.4f)
                    {
                        Fx.Sparkles(ep.transform.position, new Color(1f, 0.8f, 0.4f), 3, 0.3f);
                        Destroy(ep.gameObject);
                    }
                yield return null;
            }
            if (w + 1 < waves) yield return new WaitForSeconds(0.25f);
        }
        // III: deixa un anell de brases que crema uns segons
        if (Tier >= 3)
        {
            Vector2 p = transform.position;
            for (float t = 0f; t < 3f; t += 0.3f)
            {
                for (int i = 0; i < 10; i++)
                {
                    float a = (i * 36f + t * 40f) * Mathf.Deg2Rad;
                    Fx.Flames(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.9f, 0.3f, 0.6f);
                }
                DishUtil.HitCircle(p, r, 1 + Level / 4, 0f, DmgKind.Fire);
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    // GANIVETADA: un ganivet gegant cau del cel i es clava a terra davant teu
    IEnumerator Ganivetada()
    {
        Vector2 aim = Aim();
        int knives = Tier >= 4 ? 3 : Tier >= 2 ? 2 : 1;
        for (int k = 0; k < knives; k++)
        {
            float off = knives == 1 ? 0f : (k - (knives - 1) / 2f) * 50f;
            Vector2 dir = Quaternion.Euler(0f, 0f, off) * aim;
            StartCoroutine(Cleaver((Vector2)transform.position + dir * 2.4f, dir));
            yield return new WaitForSeconds(0.12f);
        }
    }

    static Sprite knife;

    IEnumerator Cleaver(Vector2 p, Vector2 dir)
    {
        float r = 2.2f * DishArea.Mul;
        if (knife == null) knife = Resources.Load<Sprite>("Projectiles/Ganivet");
        // l'ombra a terra i el ganivet que cau (la punta cap avall)
        SpriteRenderer shadow = HazardFx.MakeCircle(p, 0.8f, new Color(0f, 0f, 0f, 0.15f), -2);
        GameObject go = new GameObject("Ganivetada");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = knife != null ? knife : HazardFx.Square();
        sr.sortingOrder = 25;
        go.transform.localScale = Vector3.one * 3f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, -90f + Random.Range(-8f, 8f));
        for (float t = 0f; t < 0.22f; t += Time.deltaTime)
        {
            float k = t / 0.22f;
            go.transform.position = p + Vector2.up * (7f * (1f - k * k) + 1.2f);
            if (shadow != null) shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, k);
            yield return null;
        }
        go.transform.position = p + Vector2.up * 1.2f;
        if (shadow != null) Destroy(shadow.gameObject);

        // el cop: esquerdes a terra, ona, pols i espurnes
        Fx.Shockwave(p, r, new Color(1f, 0.85f, 0.8f));
        Fx.Dust(p);
        Fx.Hit(p, Vector2.down, true);
        for (int i = 0; i < 5; i++)
        {
            Vector2 cd = Random.insideUnitCircle.normalized;
            SpriteRenderer crack = BossFx.Line(p, cd, Random.Range(1f, 2f), 0.12f, new Color(0.15f, 0.08f, 0.06f, 0.8f), "Esquerda");
            crack.sortingOrder = -1;
            Destroy(crack.gameObject, 1.2f);
        }
        CameraFX.Shake(0.3f, 0.3f);
        HitStop.Do(0.06f);
        AudioManager.Play("crit", 0.9f, 0.7f);
        AudioManager.Play("table", 0.8f, 0.6f);
        int dmg = 14 + 4 * Level + 2 * DishUtil.Bonus;
        List<Collider2D> hits = Phys.Circle(p, r);
        for (int i = 0; i < hits.Count; i++)
        {
            Enemy e = hits[i] != null ? hits[i].GetComponentInParent<Enemy>() : null;
            if (e == null || e.IsDead) continue;
            DamageKind.Hit(e, dmg, DmgKind.Crit, true);
            if (!e.IsDead) e.Freeze(e.isBoss ? 0.4f : 1.2f);
        }
        Phys.Release(hits);
        // III: l'esquerda continua endavant i talla en línia
        if (Tier >= 3)
        {
            for (int s = 1; s <= 4; s++)
            {
                Vector2 q = p + dir * (s * 1.2f);
                Fx.Dust(q);
                Fx.Hit(q, dir, false);
                List<Collider2D> h2 = Phys.Circle(q, 0.9f);
                for (int i = 0; i < h2.Count; i++)
                {
                    Enemy e = h2[i] != null ? h2[i].GetComponentInParent<Enemy>() : null;
                    if (e != null && !e.IsDead) DamageKind.Hit(e, dmg / 2, DmgKind.Crit, true);
                }
                Phys.Release(h2);
                yield return new WaitForSeconds(0.05f);
            }
        }
        // el ganivet es queda clavat una estona i s'esvaeix
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            sr.color = new Color(1f, 1f, 1f, t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.2f);
            yield return null;
        }
        Destroy(go);
    }

    // GLASSA: una cúpula de gel es forma al teu voltant i glaça els clients
    IEnumerator Glassa()
    {
        Vector2 p = transform.position;
        float r = 4.5f * DishArea.Mul * (Tier >= 2 ? 1.2f : 1f);
        float freeze = Tier >= 2 ? 3f : 2.2f;
        float bossFreeze = Tier >= 4 ? 1.6f : 0.8f;
        Fx.FrostBurst(p, r * 0.6f);
        FrostZone.Spawn(p, r * 0.8f, 0.5f, Tier >= 2 ? 5f : 3.5f);
        AudioManager.Play("crit", 0.7f, 1.5f);
        CameraFX.Shake(0.15f, 0.12f);

        // la cúpula: una corona de gel que creix
        SpriteRenderer dome = HazardFx.MakeCircle(p, r, new Color(0.75f, 0.92f, 1f, 0.9f), -1);
        dome.sprite = Fx.RingBand();
        SpriteRenderer fill = HazardFx.MakeCircle(p, r, new Color(0.7f, 0.9f, 1f, 0.12f), -2);
        Destroy(dome.gameObject, 3f); Destroy(fill.gameObject, 3f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            dome.transform.localScale = fill.transform.localScale = Vector3.one * r * 2f * Mathf.Lerp(0.2f, 1f, 1f - (1f - k) * (1f - k));
            if (Random.value < 0.6f) Fx.Snowflake(p + Random.insideUnitCircle * r);
            yield return null;
        }
        int dmg = 4 + Level + DishUtil.Bonus;
        List<Collider2D> hits = Phys.Circle(p, r);
        for (int i = 0; i < hits.Count; i++)
        {
            Enemy e = hits[i] != null ? hits[i].GetComponentInParent<Enemy>() : null;
            if (e == null || e.IsDead) continue;
            DamageKind.Hit(e, dmg, DmgKind.Ice);
            if (!e.IsDead) e.Freeze(e.isBoss ? bossFreeze : freeze);
        }
        Phys.Release(hits);
        // la cúpula brilla i es va fonent
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            if (dome == null) yield break;
            dome.color = new Color(0.75f, 0.92f, 1f, 0.9f * (1f - t / 1.5f) + 0.1f * Mathf.Sin(t * 20f));
            if (Random.value < 0.3f) Fx.Sparkle(p + Random.insideUnitCircle.normalized * r, new Color(0.85f, 0.97f, 1f));
            yield return null;
        }
        // III: la cúpula esclata en estelles que tornen a fer mal
        if (Tier >= 3)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Fx.IceShatter(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, 3);
            }
            Fx.FrostBurst(p, r * 0.5f);
            AudioManager.Play("box_break", 0.7f, 1.6f);
            DishUtil.HitCircle(p, r, dmg, 0f, DmgKind.Ice);
        }
        if (dome != null) Destroy(dome.gameObject);
        if (fill != null) Destroy(fill.gameObject);
    }

    // TALL DE KATANA: una estrebada que travessa la sala; un instant després, els clients tallats cauen
    IEnumerator Katana()
    {
        int cuts = Tier >= 3 ? 3 : Tier >= 2 ? 2 : 1;
        float length = Tier >= 4 ? 7.5f : 6f;
        int dmg = 10 + 3 * Level + 2 * DishUtil.Bonus + (Tier >= 4 ? 6 : 0);
        Vector2 aim = Aim();
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 1.2f;
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        var hitAll = new List<Enemy>();
        for (int k = 0; k < cuts; k++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, k == 0 ? 0f : (k == 1 ? 135f : -135f)) * aim; // en X o en estrella
            Vector2 from = transform.position;
            Vector2 to = from + dir * length;
            to.x = Mathf.Clamp(to.x, c.x - half.x, c.x + half.x);
            to.y = Mathf.Clamp(to.y, c.y - half.y, c.y + half.y);

            // ombres del xef al llarg del camí
            if (body != null)
                for (int i = 0; i < 6; i++)
                {
                    GameObject g = new GameObject("OmbraKatana");
                    g.transform.position = Vector2.Lerp(from, to, i / 5f);
                    g.transform.localScale = transform.localScale;
                    SpriteRenderer gs = g.AddComponent<SpriteRenderer>();
                    gs.sprite = body.sprite;
                    gs.flipX = body.flipX;
                    gs.color = new Color(0.6f, 0.7f, 1f, 0.5f - i * 0.06f);
                    gs.sortingOrder = body.sortingOrder - 1;
                    Destroy(g, 0.25f);
                }
            // el tall: una línia blanca amb vora daurada
            Vector2 seg = to - from;
            float len = seg.magnitude;
            SpriteRenderer glow = BossFx.Line(from, seg.normalized, len, 0.6f, new Color(1f, 0.85f, 0.4f, 0.6f), "TallVora");
            SpriteRenderer line = BossFx.Line(from, seg.normalized, len, 0.22f, Color.white, "Tall");
            glow.sortingOrder = 29; line.sortingOrder = 30;
            Destroy(glow.gameObject, 0.4f); Destroy(line.gameObject, 0.4f);
            Fx.Dust(from);
            if (rb != null) rb.position = to; else transform.position = to;
            Fx.Dust(to);
            AudioManager.Play("dash", 0.9f, 1.4f + k * 0.1f);
            foreach (Enemy e in Enemy.All.ToArray())
            {
                if (e == null || e.IsDead || hitAll.Contains(e)) continue;
                Vector2 q = (Vector2)e.transform.position - from;
                float t = len > 0.01f ? Mathf.Clamp(Vector2.Dot(q, seg) / (len * len), 0f, 1f) : 0f;
                if (Vector2.Distance(e.transform.position, from + seg * t) > 1.3f) continue;
                hitAll.Add(e);
            }
            yield return new WaitForSeconds(0.08f);
        }
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.6f, "ZAS!", Color.white, 0.6f);
        // un instant de silenci... i els clients tallats cauen
        yield return new WaitForSeconds(0.18f);
        foreach (Enemy e in hitAll)
        {
            if (e == null || e.IsDead) continue;
            Vector2 ep = e.transform.position;
            SpriteRenderer cut = BossFx.Line(ep - new Vector2(0.6f, -0.3f), new Vector2(1f, -0.5f).normalized, 1.4f, 0.1f, Color.white, "TallClient");
            cut.sortingOrder = 31;
            Destroy(cut.gameObject, 0.15f);
            Fx.Hit(ep, Vector2.right, true);
            DamageKind.Hit(e, dmg, DmgKind.None);
        }
        CameraFX.Shake(0.15f, 0.15f);
    }

    // EL FORN DE LLENYA: un anell de flames que s'encén al teu voltant i et segueix.
    // - La vora és un mur de foc: els clients que la travessen es cremen i surten empesos cap enfora.
    // - A dins, el caliu també crema (menys).
    // - En acabar, l'anell esclata cap enfora en una fogonada.
    IEnumerator Forn()
    {
        float Dur = Tier >= 2 ? 5.5f : 4.5f;
        const int Points = 14;
        float r = 2.8f * DishArea.Mul * (Tier >= 3 ? 1.25f : 1f);
        int ringDmg = 3 + Level / 2 + DishUtil.Bonus;     // la vora (el mur de foc)
        int coreDmg = 1 + Level / 4 + DishUtil.Bonus / 2; // el caliu de dins

        AudioManager.Play("explosion", 0.45f, 0.65f);
        AudioManager.Play("fuse", 0.8f, 0.8f);
        Fx.Shockwave(transform.position, r, new Color(1f, 0.55f, 0.15f));

        // el terra: el caliu a dins i una vora més encesa
        // una corona encesa a la vora (no un disc pla) i, a dins, només una mica d'escalfor
        SpriteRenderer edge = HazardFx.MakeCircle(transform.position, r, new Color(1f, 0.5f, 0.1f, 0.85f), -2);
        edge.sprite = Fx.RingBand();
        SpriteRenderer core = HazardFx.MakeCircle(transform.position, r * 0.9f, new Color(1f, 0.55f, 0.15f, 0.07f), -3);
        Destroy(edge.gameObject, Dur + 1f);
        Destroy(core.gameObject, Dur + 1f);

        var nextHit = new Dictionary<Enemy, float>();
        float spin = Random.Range(0f, 360f);
        float nextFx = 0f, nextDmg = 0f;
        for (float t = 0f; t < Dur; t += Time.deltaTime)
        {
            Vector2 p = transform.position;
            // s'encén: l'anell creix de dins cap enfora durant 0,3 s
            float grow = Mathf.Clamp01(t / 0.3f);
            float rr = r * Mathf.Lerp(0.3f, 1f, 1f - (1f - grow) * (1f - grow));
            float pulse = 1f + 0.04f * Mathf.Sin(t * 12f);
            if (edge != null) { edge.transform.position = p; edge.transform.localScale = Vector3.one * rr * 2f * pulse; edge.color = new Color(1f, 0.5f + 0.1f * Mathf.Sin(t * 7f), 0.1f, 0.75f + 0.15f * Mathf.Sin(t * 9f)); }
            if (core != null) { core.transform.position = p; core.transform.localScale = Vector3.one * rr * 1.76f; }

            // les flames de la vora, que giren a poc a poc
            spin += 70f * Time.deltaTime;
            if (t >= nextFx)
            {
                nextFx = t + 0.12f;
                // flames grosses, una a cada punt de la vora (com un cercle d'espelmes enceses)
                for (int i = 0; i < Points; i++)
                {
                    float a = (spin + i * 360f / Points) * Mathf.Deg2Rad;
                    Fx.FlameAt(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr, Random.Range(0.75f, 1.05f));
                }
                if (Random.value < 0.3f) Fx.Flames(p + Random.insideUnitCircle.normalized * rr, 0.3f, 1f); // alguna espurna i fum
            }

            // el mal: cada client, com a molt cada 0,3 s
            if (t >= nextDmg && grow >= 1f)
            {
                nextDmg = t + 0.1f;
                List<Collider2D> hits = Phys.Circle(p, rr + 0.5f);
                for (int i = 0; i < hits.Count; i++)
                {
                    Enemy e = hits[i] != null ? hits[i].GetComponentInParent<Enemy>() : null;
                    if (e == null || e.IsDead) continue;
                    float when;
                    if (nextHit.TryGetValue(e, out when) && t < when) continue;
                    nextHit[e] = t + 0.3f;
                    Vector2 out_ = (Vector2)e.transform.position - p;
                    float d = out_.magnitude;
                    bool onRing = d > rr - 0.7f; // a la vora: el mur de foc
                    DamageKind.Hit(e, onRing ? ringDmg : coreDmg, DmgKind.Fire);
                    if (onRing && !e.IsDead && !e.isBoss) e.Knockback(d > 0.01f ? out_ : Random.insideUnitCircle, 9f, 0.18f);
                }
                Phys.Release(hits);
            }
            yield return null;
        }

        // la fogonada final: l'anell esclata cap enfora
        Vector2 end = transform.position;
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            for (int k = 0; k < 3; k++) Fx.FlameJet(end + dir * r * 0.7f, dir, 2f, 12f, 1f);
        }
        Fx.Shockwave(end, r * 1.3f, new Color(1f, 0.5f, 0.12f));
        Fx.Scorch(end, r, 1f);
        CameraFX.Shake(0.2f, 0.18f);
        AudioManager.Play("explosion", 0.6f, 0.9f);
        DishUtil.HitCircle(end, r + 1f, (ringDmg + 2) * (Tier >= 4 ? 2 : 1), 8f, DmgKind.Fire);
        if (edge != null) Destroy(edge.gameObject);
        if (core != null) Destroy(core.gameObject);
    }

    // ---------- EL RELLOTGE (a baix a l'esquerra) ----------

    void BuildHud()
    {
        GameObject go = new GameObject("HabilitatXef", typeof(Canvas), typeof(CanvasScaler));
        Canvas cv = go.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 35;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;

        GameObject r = new GameObject("Rellotge", typeof(RectTransform), typeof(Image));
        hudRoot = r.GetComponent<RectTransform>();
        hudRoot.SetParent(go.transform, false);
        hudRoot.anchorMin = hudRoot.anchorMax = new Vector2(0f, 0f);
        hudRoot.sizeDelta = new Vector2(96f, 96f);
        hudRoot.anchoredPosition = new Vector2(120f, 160f);
        ring = r.GetComponent<Image>();
        ring.sprite = HazardFx.Circle();
        ring.color = new Color(0.12f, 0.08f, 0.09f, 0.9f);
        ring.raycastTarget = false;

        GameObject f = new GameObject("Carrega", typeof(RectTransform), typeof(Image));
        RectTransform frt = f.GetComponent<RectTransform>();
        frt.SetParent(hudRoot, false);
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(7f, 7f); frt.offsetMax = new Vector2(-7f, -7f);
        fill = f.GetComponent<Image>();
        fill.sprite = HazardFx.Circle();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.raycastTarget = false;

        keyText = Text(hudRoot, 34f, Vector2.zero, new Vector2(96f, 96f));
        nameText = Text(hudRoot, 18f, new Vector2(0f, -64f), new Vector2(220f, 28f));
        nameText.text = def.name;
        nameText.color = UIStyle.Gold;
    }

    static TextMeshProUGUI Text(RectTransform parent, float size, Vector2 pos, Vector2 box)
    {
        GameObject g = new GameObject("Text", typeof(RectTransform));
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        UIStyle.Outline(t);
        return t;
    }

    void UpdateHud()
    {
        if (hudRoot == null || def == null) return;
        float left = Mathf.Max(0f, readyAt - Time.time);
        float k = 1f - Mathf.Clamp01(left / def.cooldown);
        fill.fillAmount = k;
        fill.color = Ready ? def.color : new Color(def.color.r * 0.5f, def.color.g * 0.5f, def.color.b * 0.5f, 0.9f);
        keyText.text = Ready ? KeyName() : Mathf.CeilToInt(left).ToString();
        pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 2f);
        float pulse = Ready ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 5f) : 1f;
        hudRoot.localScale = Vector3.one * (pulse + 0.35f * pop);
        bool show = !PlayerHealth.IsDying;
        if (hudRoot.gameObject.activeSelf != show) hudRoot.gameObject.SetActive(show);
    }

    void OnDestroy()
    {
        if (hudRoot != null) Destroy(hudRoot.parent.gameObject);
    }
}
