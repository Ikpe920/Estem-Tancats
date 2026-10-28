using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// =====================================================================
//  TAULES: es poden bolcar amb la tecla E i fan de barricada
// =====================================================================

// els trets dels clients s'aturen contra això (si té durabilitat, es gasta)
public class BlocksShots : MonoBehaviour
{
    public int durability = 0; // 0 = indestructible

    public void OnShotBlocked()
    {
        if (durability <= 0) return;

        durability--;
        if (durability == 0)
        {
            Table table = GetComponent<Table>();
            if (table != null) table.Collapse();
        }
    }
}

public class Table : MonoBehaviour
{
    public int flippedDurability = 8;

    private bool flipped;
    private Transform player;
    private SpriteRenderer sr;
    private TextMeshPro prompt;

    private static readonly Vector3 FlippedScale = new Vector3(1.2f, 2.6f, 1f);
    private static readonly Color UprightColor = new Color(0.45f, 0.28f, 0.15f);
    private static readonly Color FlippedColor = new Color(0.30f, 0.18f, 0.10f);

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;

        // l'avís "E · Bolcar" (no és fill de la taula perquè l'escala el deformaria)
        GameObject pg = new GameObject("AvisTaula");
        pg.transform.position = transform.position + Vector3.up * 1.3f;
        prompt = pg.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) prompt.font = TMP_Settings.defaultFontAsset;
        prompt.text = "E · Bolcar";
        prompt.fontSize = 3.5f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = Color.white;
        prompt.sortingOrder = 60;
        prompt.rectTransform.sizeDelta = new Vector2(6f, 1f);
        prompt.textWrappingMode = TextWrappingModes.NoWrap;
        pg.SetActive(false);
    }

    void Update()
    {
        if (prompt == null || flipped || player == null || Time.timeScale == 0f)
        {
            if (prompt != null) prompt.gameObject.SetActive(false);
            return;
        }

        bool near = Vector2.Distance(player.position, transform.position) < 2.6f;
        prompt.gameObject.SetActive(near);
        if (!near) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame) StartCoroutine(Flip());
    }

    IEnumerator Flip()
    {
        flipped = true;
        if (prompt != null) prompt.gameObject.SetActive(false);

        HazardFx.Shake(0.1f, 0.08f);

        Vector3 from = transform.localScale;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.2f);
            transform.localScale = Vector3.Lerp(from, FlippedScale, k);
            if (sr != null) sr.color = Color.Lerp(UprightColor, FlippedColor, k);
            yield return null;
        }

        // ara atura els trets, però es trenca després d'uns quants
        BlocksShots b = gameObject.AddComponent<BlocksShots>();
        b.durability = flippedDurability;
    }

    public void Collapse()
    {
        SpriteRenderer dust = HazardFx.MakeCircle(transform.position, 1.4f, new Color(0.7f, 0.6f, 0.45f, 0.6f), 4);
        Destroy(dust.gameObject, 0.2f);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }
}

// LA SALA: 4 taules; si se'n trenca una, torna a sortir als 15 segons
public class TableHazard : MonoBehaviour
{
    private Vector2[] spots;
    private GameObject[] tables;
    private float[] respawnAt;

    void Start()
    {
        Vector2 size = HazardManager.ArenaSize;
        Vector2 c = HazardManager.ArenaCenter;
        float dx = size.x * 0.27f;
        float dy = size.y * 0.22f;

        spots = new Vector2[]
        {
            c + new Vector2(-dx, -dy), c + new Vector2(dx, -dy),
            c + new Vector2(-dx, dy),  c + new Vector2(dx, dy)
        };

        tables = new GameObject[spots.Length];
        respawnAt = new float[spots.Length];

        for (int i = 0; i < spots.Length; i++) tables[i] = BuildTable(spots[i]);
    }

    void Update()
    {
        for (int i = 0; i < spots.Length; i++)
        {
            if (tables[i] != null)
            {
                respawnAt[i] = 0f;
                continue;
            }

            if (respawnAt[i] == 0f) respawnAt[i] = Time.time + 15f;
            else if (Time.time >= respawnAt[i])
            {
                tables[i] = BuildTable(spots[i]);
                respawnAt[i] = 0f;
            }
        }
    }

    GameObject BuildTable(Vector2 pos)
    {
        GameObject go = new GameObject("Taula");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2.4f, 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = new Color(0.45f, 0.28f, 0.15f, 1f);
        sr.sortingOrder = -1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        go.AddComponent<Table>();
        return go;
    }
}

// =====================================================================
//  CAIXES DESTRUCTIBLES I DROPS
// =====================================================================

public class Breakable : MonoBehaviour
{
    public int health = 3;

    private SpriteRenderer sr;
    private Color baseColor;
    private float flashUntil;
    private bool broken;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void Hit(int damage)
    {
        if (broken) return;

        health -= damage;
        flashUntil = Time.time + 0.08f;
        if (health <= 0) Break();
    }

    void Update()
    {
        if (sr != null) sr.color = Time.time < flashUntil ? Color.white : baseColor;
    }

    void Break()
    {
        broken = true;

        SpriteRenderer dust = HazardFx.MakeCircle(transform.position, 0.9f, new Color(0.85f, 0.75f, 0.55f, 0.6f), 4);
        Destroy(dust.gameObject, 0.15f);
        HazardFx.Shake(0.08f, 0.06f);

        Pickup.SpawnDrop(transform.position);
        Destroy(gameObject);
    }
}

// cada acte té les seves caixes: cadires, barrils, caixes de verdura, testos i caixes
public class PropsHazard : MonoBehaviour
{
    public int act;
    public int maxProps = 4;

    private static readonly Color[] Colors =
    {
        new Color(0.60f, 0.42f, 0.25f),
        new Color(0.55f, 0.30f, 0.12f),
        new Color(0.45f, 0.65f, 0.30f),
        new Color(0.75f, 0.38f, 0.25f),
        new Color(0.50f, 0.45f, 0.40f),
    };

    private static readonly Vector2[] Sizes =
    {
        new Vector2(0.9f, 0.9f),
        new Vector2(1.0f, 1.0f),
        new Vector2(1.2f, 0.9f),
        new Vector2(0.9f, 0.9f),
        new Vector2(1.1f, 1.1f),
    };

    private Transform player;

    IEnumerator Start()
    {
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;

        yield return null; // deixem que els altres perills es creïn primer

        for (int i = 0; i < maxProps; i++) TrySpawn();

        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (transform.childCount < maxProps) TrySpawn();
        }
    }

    void TrySpawn()
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector2 pos = HazardFx.RandomPoint(3f);

            if (player != null && Vector2.Distance(pos, player.position) < 4f) continue;
            if (!IsFree(pos)) continue;

            Build(pos);
            return;
        }
    }

    // no posem una caixa a sobre de res sòlid (taules, parets, clients, el jugador)
    bool IsFree(Vector2 pos)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 1.6f))
        {
            if (!c.isTrigger) return false;
        }
        return true;
    }

    void Build(Vector2 pos)
    {
        int a = Mathf.Clamp(act, 0, 4);

        GameObject root = new GameObject("Caixa");
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size = Sizes[a];

        GameObject body = new GameObject("Cos");
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(Sizes[a].x, Sizes[a].y, 1f);
        SpriteRenderer sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = Colors[a];
        sr.sortingOrder = -1;

        root.AddComponent<Breakable>();
    }
}

public enum PickupType { Heart, Coffee, Pepper, Points }

public class Pickup : MonoBehaviour
{
    public PickupType type;
    public float life = 12f;

    private float born;
    private Vector3 basePos;
    private SpriteRenderer[] renderers;
    private bool collected;

    public static void SpawnDrop(Vector2 pos)
    {
        float r = Random.value;
        if (r < 0.25f) return; // el 25% de les caixes no deixen res

        PickupType t;
        if (r < 0.45f) t = PickupType.Heart;
        else if (r < 0.70f) t = PickupType.Coffee;
        else if (r < 0.90f) t = PickupType.Pepper;
        else t = PickupType.Points;

        // si tens tots els cors, no té sentit que surti un cor
        if (t == PickupType.Heart)
        {
            PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null && ph.CurrentLives >= ph.maxLives) t = PickupType.Coffee;
        }

        Create(pos, t);
    }

    static void Create(Vector2 pos, PickupType t)
    {
        GameObject go = new GameObject("Drop " + t);
        go.transform.position = pos;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;

        Pickup p = go.AddComponent<Pickup>();
        p.type = t;

        switch (t)
        {
            case PickupType.Heart:
            {
                LifeHearts lh = FindAnyObjectByType<LifeHearts>();
                if (lh != null && lh.heartSprite != null)
                {
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = lh.heartSprite;
                    sr.sortingOrder = 3;
                    go.transform.localScale = Vector3.one * 0.8f;
                }
                else
                {
                    Dot(go.transform, Vector2.zero, 0.35f, new Color(0.9f, 0.15f, 0.2f));
                }
                break;
            }
            case PickupType.Coffee:
                Dot(go.transform, Vector2.zero, 0.32f, new Color(0.45f, 0.28f, 0.14f));
                Dot(go.transform, Vector2.zero, 0.18f, new Color(0.95f, 0.88f, 0.75f));
                break;
            case PickupType.Pepper:
                Dot(go.transform, Vector2.zero, 0.30f, new Color(0.9f, 0.15f, 0.1f));
                Dot(go.transform, new Vector2(0f, 0.28f), 0.12f, new Color(0.2f, 0.7f, 0.25f));
                break;
            case PickupType.Points:
                Dot(go.transform, Vector2.zero, 0.30f, new Color(1f, 0.82f, 0.2f));
                Dot(go.transform, Vector2.zero, 0.17f, new Color(1f, 0.95f, 0.6f));
                break;
        }
    }

    static void Dot(Transform parent, Vector2 offset, float radius, Color color)
    {
        SpriteRenderer sr = HazardFx.MakeCircle((Vector2)parent.position + offset, radius, color, 3);
        sr.transform.SetParent(parent, true);
    }

    void Start()
    {
        born = Time.time;
        basePos = transform.position;
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float age = Time.time - born;

        transform.position = basePos + Vector3.up * Mathf.Sin(age * 5f) * 0.12f;

        // l'últim parell de segons parpelleja, i desapareix
        if (life - age < 3f)
        {
            bool on = Mathf.FloorToInt(age * 8f) % 2 == 0;
            foreach (SpriteRenderer sr in renderers)
                if (sr != null) sr.enabled = on;
        }

        if (age >= life) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<PlayerMovement>() == null) return;

        collected = true;
        Vector2 at = transform.position;

        switch (type)
        {
            case PickupType.Heart:
            {
                PlayerHealth ph = other.GetComponent<PlayerHealth>();
                if (ph != null) ph.Heal(1);
                HazardFx.Popup(at, "+1 COR", new Color(1f, 0.4f, 0.5f));
                break;
            }
            case PickupType.Coffee:
                GetBuffs(other.gameObject).SpeedBoost(1.5f, 8f);
                HazardFx.Popup(at, "CAFÈ! + velocitat", new Color(0.9f, 0.7f, 0.45f));
                break;
            case PickupType.Pepper:
                GetBuffs(other.gameObject).DamageBoost(1, 8f);
                HazardFx.Popup(at, "PEBRE! + dany", new Color(1f, 0.45f, 0.35f));
                break;
            case PickupType.Points:
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(25);
                HazardFx.Popup(at, "+25", new Color(1f, 0.9f, 0.4f));
                break;
        }

        Destroy(gameObject);
    }

    static PlayerBuffs GetBuffs(GameObject player)
    {
        PlayerBuffs b = player.GetComponent<PlayerBuffs>();
        if (b == null) b = player.AddComponent<PlayerBuffs>();
        return b;
    }
}

// bonificacions temporals que donen els drops
public class PlayerBuffs : MonoBehaviour
{
    public void SpeedBoost(float amount, float seconds)
    {
        StartCoroutine(SpeedRoutine(amount, seconds));
    }

    public void DamageBoost(int amount, float seconds)
    {
        StartCoroutine(DamageRoutine(amount, seconds));
    }

    IEnumerator SpeedRoutine(float amount, float seconds)
    {
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm == null) yield break;

        pm.speed += amount;
        yield return new WaitForSeconds(seconds);
        pm.speed -= amount;
    }

    IEnumerator DamageRoutine(int amount, float seconds)
    {
        PlayerShooting ps = GetComponent<PlayerShooting>();
        if (ps == null) yield break;

        ps.damage += amount;
        yield return new WaitForSeconds(seconds);
        ps.damage -= amount;
    }
}

public class PopupText : MonoBehaviour
{
    public float life = 0.9f;

    private TextMeshPro tmp;
    private float born;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        born = Time.time;
    }

    void Update()
    {
        float k = (Time.time - born) / life;
        transform.position += Vector3.up * 1.2f * Time.deltaTime;

        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = Mathf.Clamp01(1f - k);
            tmp.color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}

// =====================================================================
//  PORTES PER ON SURTEN ELS CLIENTS
// =====================================================================

public class Door : MonoBehaviour
{
    public Vector2 spawnPoint; // on apareix el client
    public Vector2 along;      // direcció al llarg de la porta

    private SpriteRenderer frame;
    private SpriteRenderer panel;
    private SpriteRenderer glow;
    private float flash;

    public void Build(bool horizontal)
    {
        Vector2 frameSize = horizontal ? new Vector2(3.4f, 1.0f) : new Vector2(1.0f, 3.4f);
        Vector2 panelSize = horizontal ? new Vector2(3.0f, 0.7f) : new Vector2(0.7f, 3.0f);

        frame = Part("Marc", frameSize, -4);
        panel = Part("Porta", panelSize, -3);
        glow = Part("Llum", panelSize, -2);
        glow.color = new Color(1f, 0.8f, 0.3f, 0f);
    }

    SpriteRenderer Part(string partName, Vector2 size, int order)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.sortingOrder = order;
        return sr;
    }

    public void SetColors(Color frameColor, Color panelColor)
    {
        if (frame != null) frame.color = frameColor;
        if (panel != null) panel.color = panelColor;
    }

    public void Open()
    {
        flash = 1f;
    }

    void Update()
    {
        flash = Mathf.MoveTowards(flash, 0f, 2.2f * Time.deltaTime);
        if (glow != null) glow.color = new Color(1f, 0.8f, 0.3f, flash * 0.85f);
    }
}

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance { get; private set; }

    private readonly List<Door> doors = new List<Door>();

    private static readonly Color[] FrameColors =
    {
        new Color(0.62f, 0.45f, 0.28f),
        new Color(0.50f, 0.35f, 0.20f),
        new Color(0.75f, 0.80f, 0.82f),
        new Color(0.55f, 0.42f, 0.25f),
        new Color(0.40f, 0.43f, 0.55f),
    };

    private static readonly Color[] PanelColors =
    {
        new Color(0.30f, 0.18f, 0.10f),
        new Color(0.18f, 0.10f, 0.07f),
        new Color(0.40f, 0.45f, 0.50f),
        new Color(0.22f, 0.30f, 0.20f),
        new Color(0.15f, 0.16f, 0.24f),
    };

    void Awake()
    {
        Instance = this;

        Vector2 size = HazardManager.ArenaSize;
        Vector2 c = HazardManager.ArenaCenter;
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float spread = size.x * 0.27f;

        AddDoor(c + new Vector2(-spread, hy), true, Vector2.down);
        AddDoor(c + new Vector2(spread, hy), true, Vector2.down);
        AddDoor(c + new Vector2(-spread, -hy), true, Vector2.up);
        AddDoor(c + new Vector2(spread, -hy), true, Vector2.up);
        AddDoor(c + new Vector2(-hx, 0f), false, Vector2.right);
        AddDoor(c + new Vector2(hx, 0f), false, Vector2.left);

        SetAct(0);
    }

    void AddDoor(Vector2 wallPoint, bool horizontal, Vector2 inward)
    {
        GameObject go = new GameObject("Porta");
        go.transform.SetParent(transform, false);
        go.transform.position = wallPoint - inward * 0.35f; // una mica dins de la paret

        Door d = go.AddComponent<Door>();
        d.Build(horizontal);
        d.spawnPoint = wallPoint + inward * 1.4f;
        d.along = horizontal ? Vector2.right : Vector2.up;
        doors.Add(d);
    }

    public void SetAct(int act)
    {
        act = ((act % 5) + 5) % 5;
        foreach (Door d in doors) d.SetColors(FrameColors[act], PanelColors[act]);
    }

    // tria una porta lluny del jugador, perquè no li surtin a sobre
    public Vector2 GetSpawnPoint(Vector2 playerPos)
    {
        List<Door> far = new List<Door>();
        Door farthest = null;
        float best = -1f;

        foreach (Door d in doors)
        {
            float dist = Vector2.Distance(d.spawnPoint, playerPos);
            if (dist >= 7f) far.Add(d);
            if (dist > best)
            {
                best = dist;
                farthest = d;
            }
        }

        Door chosen = far.Count > 0 ? far[Random.Range(0, far.Count)] : farthest;
        if (chosen == null) return playerPos + Random.insideUnitCircle.normalized * 8f;

        chosen.Open();
        return chosen.spawnPoint + chosen.along * Random.Range(-0.8f, 0.8f);
    }
}

// =====================================================================
//  PARETS DECORADES (textures generades per codi, estil píxel)
// =====================================================================

public static class WallSkin
{
    private static readonly Sprite[] cache = new Sprite[5];

    public static Sprite Get(int act)
    {
        act = ((act % 5) + 5) % 5;
        if (cache[act] != null) return cache[act];

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = Pixel(act, x, y);

        tex.SetPixels32(px);
        tex.Apply();

        cache[act] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        return cache[act];
    }

    static Color32 Pixel(int act, int x, int y)
    {
        switch (act)
        {
            case 0: // la sala: llistons de fusta
            {
                int plank = x / 8;
                if (x % 8 == 0) return C(0.22f, 0.13f, 0.07f);
                float shade = 0.85f + 0.15f * (Hash(plank, 3) % 100) / 100f;
                if ((y + plank * 7) % 13 == 0) shade *= 0.85f;
                return C(0.55f * shade, 0.36f * shade, 0.20f * shade);
            }
            case 1: // la barra: prestatge amb ampolles
            {
                if (y < 3) return C(0.45f, 0.30f, 0.17f);
                if (y == 3) return C(0.28f, 0.18f, 0.10f);

                int bx = x % 8;
                bool body = y >= 4 && y < 20 && bx >= 2 && bx <= 5;
                bool neck = y >= 20 && y < 27 && bx >= 3 && bx <= 4;
                if (body || neck)
                {
                    int b = Hash(x / 8, 1) % 4;
                    if (b == 0) return C(0.25f, 0.60f, 0.30f);
                    if (b == 1) return C(0.70f, 0.35f, 0.15f);
                    if (b == 2) return C(0.30f, 0.45f, 0.75f);
                    return C(0.75f, 0.65f, 0.25f);
                }
                return C(0.20f, 0.13f, 0.10f);
            }
            case 2: // la cuina: rajoles
            {
                if (x % 16 == 0 || y % 16 == 0) return C(0.55f, 0.60f, 0.62f);
                float s = 0.86f + 0.10f * (Hash(x / 16, y / 16) % 100) / 100f;
                if (x % 16 == 1 || y % 16 == 1) s += 0.05f;
                return C(0.82f * s, 0.90f * s, 0.92f * s);
            }
            case 3: // la terrassa: bardissa
            {
                int h = Hash(x, y) % 100;
                if (h < 12) return C(0.30f, 0.62f, 0.28f);
                if (h < 26) return C(0.09f, 0.26f, 0.11f);
                return C(0.16f, 0.40f, 0.18f);
            }
            default: // la teulada: teules
            {
                int row = y / 8;
                int shift = (x + (row % 2) * 8);
                if (y % 8 == 0 || shift % 16 == 0) return C(0.12f, 0.14f, 0.22f);
                float s = 0.80f + 0.20f * (y % 8) / 8f;
                float v = 0.88f + 0.12f * (Hash(shift / 16, row) % 100) / 100f;
                return C(0.33f * s * v, 0.38f * s * v, 0.52f * s * v);
            }
        }
    }

    static Color32 C(float r, float g, float b)
    {
        return new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);
    }

    static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7fffffff;
        }
    }
}

// =====================================================================
//  LA TEULADA: foscor amb un cercle de llum al voltant del xef
// =====================================================================

public class DarknessHazard : MonoBehaviour
{
    private static DarknessHazard instance;

    public float lightRadius = 6f;  // com de gran és el cercle de llum
    public float darkness = 0.82f;  // 0 = res de foscor, 1 = negre total

    private SpriteRenderer overlay;
    private Transform player;
    private float level = 1f;
    private float flashUntil;

    // el llamp il·lumina l'escena un instant
    public static void Flash()
    {
        if (instance != null) instance.flashUntil = Time.time + 0.2f;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;

        int size = 512;
        float worldSize = 70f; // unitats que cobreix la capa
        float unitsPerPixel = worldSize / size;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) * unitsPerPixel;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lightRadius * 0.45f, lightRadius, dist));
                px[y * size + x] = new Color32(4, 6, 18, (byte)(a * darkness * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        GameObject go = new GameObject("Foscor");
        go.transform.SetParent(transform, false);
        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / worldSize);
        overlay.sortingOrder = 90; // per sobre de tot el món, per sota de la interfície
    }

    void LateUpdate()
    {
        if (overlay == null) return;

        if (player != null)
            overlay.transform.position = new Vector3(player.position.x, player.position.y, 0f);

        float target = Time.time < flashUntil ? 0.15f : 1f;
        level = Mathf.MoveTowards(level, target, 8f * Time.unscaledDeltaTime);
        overlay.color = new Color(1f, 1f, 1f, level);
    }
}