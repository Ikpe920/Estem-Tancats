using System.Collections;
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
//   EL PIZZER     FORN DE LLENYA   un anell de foc et segueix uns segons i crema els clients que s'hi acosten
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
        new Def("FORN DE LLENYA", "Un anell de foc et segueix i crema els clients", 14f, new Color(1f, 0.4f, 0.15f)),
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
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.6f, def.name + "!", def.color, 0.9f);
        GamepadInput.Rumble(0.5f, 0.5f, 0.2f);
        switch (chef)
        {
            case 1: Ganivetada(); break;
            case 2: Glassa(); break;
            case 3: StartCoroutine(Katana()); break;
            case 4: StartCoroutine(Forn()); break;
            default: OnaDeCalor(); break;
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

    void OnaDeCalor()
    {
        Vector2 p = transform.position;
        float r = 3.5f * DishArea.Mul;
        Fx.Shockwave(p, r, new Color(1f, 0.6f, 0.25f));
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Fx.Flames(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.7f, 0.5f, 0.6f);
        }
        CameraFX.Shake(0.2f, 0.18f);
        AudioManager.Play("explosion", 0.5f, 1.3f);
        DishUtil.HitCircle(p, r, 6 + 2 * Level + DishUtil.Bonus, 10f, DmgKind.Fire);
        // esborra els trets dels clients que hi ha a prop
        foreach (EnemyProjectile ep in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
            if (ep != null && Vector2.Distance(ep.transform.position, p) <= r + 0.5f)
            {
                Fx.Sparkle(ep.transform.position, new Color(1f, 0.8f, 0.4f));
                Destroy(ep.gameObject);
            }
    }

    void Ganivetada()
    {
        Vector2 p = (Vector2)transform.position + Aim() * 2.2f;
        float r = 2.2f * DishArea.Mul;
        Fx.Shockwave(p, r, new Color(1f, 0.85f, 0.8f));
        Fx.Dust(p);
        Fx.Hit(p, Vector2.down, true);
        CameraFX.Shake(0.3f, 0.3f);
        HitStop.Do(0.06f);
        AudioManager.Play("crit", 0.9f, 0.7f);
        int dmg = 14 + 4 * Level + 2 * DishUtil.Bonus;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            DamageKind.Hit(e, dmg, DmgKind.Crit, true);
            if (e != null && !e.IsDead) e.Freeze(e.isBoss ? 0.4f : 1.2f);
        }
    }

    void Glassa()
    {
        Vector2 p = transform.position;
        float r = 4.5f * DishArea.Mul;
        Fx.FrostBurst(p, r);
        FrostZone.Spawn(p, r * 0.8f, 0.5f, 3.5f);
        AudioManager.Play("crit", 0.7f, 1.5f);
        CameraFX.Shake(0.15f, 0.12f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            DamageKind.Hit(e, 4 + Level + DishUtil.Bonus, DmgKind.Ice);
            if (e != null && !e.IsDead) e.Freeze(e.isBoss ? 0.8f : 2.2f);
        }
    }

    IEnumerator Katana()
    {
        Vector2 dir = Aim();
        Vector2 from = transform.position;
        Vector2 to = from + dir * 6f;
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 1.2f;
        to.x = Mathf.Clamp(to.x, c.x - half.x, c.x + half.x);
        to.y = Mathf.Clamp(to.y, c.y - half.y, c.y + half.y);

        // el tall: una línia blanca que s'esvaeix
        SpriteRenderer line = BossFx.Line(from, (to - from).normalized, Vector2.Distance(from, to), 0.35f, new Color(1f, 1f, 1f, 0.9f), "Tall");
        line.sortingOrder = 30;
        Destroy(line.gameObject, 0.35f);
        Fx.Dust(from);
        if (rb != null) rb.position = to; else transform.position = to;
        Fx.Dust(to);
        AudioManager.Play("dash", 0.9f, 1.4f);

        int dmg = 10 + 3 * Level + 2 * DishUtil.Bonus;
        Vector2 seg = to - from;
        float len = seg.magnitude;
        foreach (Enemy e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector2 q = (Vector2)e.transform.position - from;
            float t = len > 0.01f ? Mathf.Clamp(Vector2.Dot(q, seg) / (len * len), 0f, 1f) : 0f;
            if (Vector2.Distance(e.transform.position, from + seg * t) > 1.3f) continue;
            Fx.Hit(e.transform.position, seg, true);
            DamageKind.Hit(e, dmg, DmgKind.None);
        }
        CameraFX.Shake(0.15f, 0.15f);
        yield return null;
        if (line != null)
            for (float a = 0.9f; a > 0f && line != null; a -= Time.deltaTime * 3f)
            {
                line.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
    }

    IEnumerator Forn()
    {
        AudioManager.Play("explosion", 0.4f, 0.7f);
        float r = 2.8f * DishArea.Mul;
        SpriteRenderer glow = HazardFx.MakeCircle(transform.position, r, new Color(1f, 0.45f, 0.1f, 0.18f), -2);
        Destroy(glow.gameObject, 4.6f);
        float spin = 0f;
        for (int tick = 0; tick < 30; tick++)
        {
            Vector2 p = transform.position;
            if (glow != null) glow.transform.position = p;
            spin += 40f;
            for (int i = 0; i < 4; i++)
            {
                float a = (spin + i * 90f) * Mathf.Deg2Rad;
                Fx.Flames(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.85f, 0.35f, 0.5f);
            }
            if (tick % 3 == 0)
                DishUtil.HitCircle(p, r, 2 + Level / 2 + DishUtil.Bonus, 0f, DmgKind.Fire);
            yield return new WaitForSeconds(0.15f);
        }
        if (glow != null) Destroy(glow.gameObject);
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
