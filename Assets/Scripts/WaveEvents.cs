using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  ESDEVENIMENTS A LES ONADES
// =====================================================================
// De tant en tant una onada és especial (a partir de l'onada 3, un 30% de les vegades, mai en una
// onada de cap ni dues seguides). Es veu un cartell en començar i un rètol a dalt mentre dura.
//
//   HORA PUNTA       el doble de clients, que surten el doble de ràpid (×1,5 punts)
//   TALL DE LLUM     se'n va la llum: només veus al teu voltant (no a la Teulada, que ja és fosca)
//   CLIENT VIP       un client daurat amb corona fuig de tu: despatxa'l abans de 15 s i dona molts punts
//   HAPPY HOUR       els clients deixen premis en morir
//   RACIONS GEGANTS  la meitat de clients, però enormes i resistents (×3 punts)
public enum WaveEventKind { None, HoraPunta, TallDeLlum, ClientVip, HappyHour, RacionsGegants }

public static class WaveEvents
{
    public const int FirstWave = 3;
    public const float Chance = 0.3f;

    public static WaveEventKind Current { get; private set; }
    public static WaveEventKind ForcedNext = WaveEventKind.None; // el mode de prova (F9) en pot forçar un

    static WaveEventKind last;
    static GameObject darkness;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Reset();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) { Reset(); }

    static void Reset()
    {
        Current = WaveEventKind.None;
        last = WaveEventKind.None;
        ForcedNext = WaveEventKind.None;
        Enemy.OnKilled -= HappyHourDrop;
    }

    public static string Title(WaveEventKind k)
    {
        switch (k)
        {
            case WaveEventKind.HoraPunta: return "HORA PUNTA";
            case WaveEventKind.TallDeLlum: return "TALL DE LLUM";
            case WaveEventKind.ClientVip: return "CLIENT VIP";
            case WaveEventKind.HappyHour: return "HAPPY HOUR";
            case WaveEventKind.RacionsGegants: return "RACIONS GEGANTS";
        }
        return "";
    }

    public static string Subtitle(WaveEventKind k)
    {
        switch (k)
        {
            case WaveEventKind.HoraPunta: return "El doble de clients! (×1,5 punts)";
            case WaveEventKind.TallDeLlum: return "S'ha anat la llum...";
            case WaveEventKind.ClientVip: return "Atrapa el client daurat abans que marxi!";
            case WaveEventKind.HappyHour: return "Els clients deixen premis";
            case WaveEventKind.RacionsGegants: return "Menys clients, però enormes (×3 punts)";
        }
        return "";
    }

    public static Color Tint(WaveEventKind k)
    {
        switch (k)
        {
            case WaveEventKind.HoraPunta: return new Color(1f, 0.55f, 0.3f);
            case WaveEventKind.TallDeLlum: return new Color(0.6f, 0.7f, 1f);
            case WaveEventKind.ClientVip: return new Color(1f, 0.85f, 0.3f);
            case WaveEventKind.HappyHour: return new Color(0.55f, 1f, 0.6f);
            case WaveEventKind.RacionsGegants: return new Color(1f, 0.5f, 0.7f);
        }
        return Color.white;
    }

    // tria l'esdeveniment d'aquesta onada (o cap)
    public static WaveEventKind Roll(int wave, bool bossWave, int act)
    {
        if (ForcedNext != WaveEventKind.None && !bossWave)
        {
            WaveEventKind f = ForcedNext;
            ForcedNext = WaveEventKind.None;
            if (!(f == WaveEventKind.TallDeLlum && (act == 4 || act == 5))) { last = f; return f; }
        }

        if (bossWave || wave < FirstWave || last != WaveEventKind.None || Random.value >= Chance)
        {
            last = WaveEventKind.None;
            return WaveEventKind.None;
        }

        WaveEventKind k;
        do { k = (WaveEventKind)Random.Range(1, 6); }
        while (k == WaveEventKind.TallDeLlum && (act == 4 || act == 5)); // a la Teulada i al Celler ja és fosc

        last = k;
        return k;
    }

    // comença l'onada amb aquest esdeveniment
    public static void Begin(WaveEventKind k, WaveSpawner spawner)
    {
        Current = k;
        if (k == WaveEventKind.None) return;

        WaveEventBanner.Show(Title(k), Subtitle(k), Tint(k));
        AudioManager.Play("event");

        switch (k)
        {
            case WaveEventKind.TallDeLlum:
                darkness = new GameObject("TallDeLlum");
                DarknessHazard d = darkness.AddComponent<DarknessHazard>();
                d.lightRadius = 5f;
                d.darkness = 0.9f;
                break;
            case WaveEventKind.HappyHour:
                Enemy.OnKilled -= HappyHourDrop;
                Enemy.OnKilled += HappyHourDrop;
                break;
            case WaveEventKind.ClientVip:
                if (spawner != null) spawner.StartCoroutine(SpawnVipSoon(spawner));
                break;
        }
    }

    static System.Collections.IEnumerator SpawnVipSoon(WaveSpawner spawner)
    {
        yield return new WaitForSeconds(1.5f); // que primer surtin uns quants clients normals
        if (spawner == null || Current != WaveEventKind.ClientVip) yield break;
        Enemy vip = spawner.SpawnExtra(spawner.FastestWalkerPrefab());
        if (vip != null) vip.gameObject.AddComponent<VipClient>();
    }

    // l'onada s'ha acabat
    public static void End()
    {
        if (darkness != null) Object.Destroy(darkness);
        darkness = null;
        Enemy.OnKilled -= HappyHourDrop;
        Current = WaveEventKind.None;
        WaveEventBanner.HideTag();
    }

    public static int AdjustCount(int count)
    {
        switch (Current)
        {
            case WaveEventKind.HoraPunta: return count * 2;
            case WaveEventKind.RacionsGegants: return Mathf.Max(2, count / 2);
        }
        return count;
    }

    public static float AdjustInterval(float interval)
    {
        return Current == WaveEventKind.HoraPunta ? interval * 0.5f : interval;
    }

    // cada client que surt durant l'onada
    public static void OnEnemySpawned(Enemy e)
    {
        if (e == null || e.isBoss) return;
        switch (Current)
        {
            case WaveEventKind.HoraPunta:
                e.points = Mathf.RoundToInt(e.points * 1.5f);
                break;
            case WaveEventKind.RacionsGegants:
                e.transform.localScale *= 1.35f;
                e.OverrideStats(Mathf.RoundToInt(e.MaxHealth * 2.5f), e.speed * 0.9f);
                e.points *= 3;
                break;
        }
    }

    static void HappyHourDrop(Enemy e)
    {
        if (e == null || Random.value > 0.45f) return;
        Pickup.SpawnDrop(e.transform.position); // ja porta un 25% de no deixar res
    }
}

// ---------- EL CLIENT VIP: daurat, amb corona, fuig de tu i marxa al cap de 15 s ----------
public class VipClient : MonoBehaviour
{
    const float Lifetime = 15f;
    const float KeepAway = 6f;

    private Enemy enemy;
    private Rigidbody2D rb;
    private float leaveAt;
    private TextMeshPro timer;
    private float strafe = 1f;
    private bool gone;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        enemy.externalMovement = true;
        enemy.SetTint(new Color(1f, 0.88f, 0.45f));
        enemy.OverrideStats(Mathf.Max(3, enemy.MaxHealth), 3.4f);
        enemy.points = 0; // el premi va a part (en matar-lo)
        EnemyVariants.AddAccessory(transform, "Corona", PropArt.Crown(), new Vector2(0f, 0.62f), 2);
        leaveAt = Time.time + Lifetime;
        strafe = Random.value < 0.5f ? 1f : -1f;

        GameObject go = new GameObject("CompteEnrere");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        timer = go.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) timer.font = TMP_Settings.defaultFontAsset;
        timer.fontSize = 3.5f;
        timer.fontStyle = FontStyles.Bold;
        timer.alignment = TextAlignmentOptions.Center;
        timer.color = new Color(1f, 0.88f, 0.3f);
        timer.outlineWidth = 0.25f;
        timer.outlineColor = new Color32(45, 26, 30, 255);
        timer.sortingOrder = 61;
        timer.rectTransform.sizeDelta = new Vector2(3f, 1f);
    }

    void OnEnable() { Enemy.OnKilled += HandleKilled; }
    void OnDisable() { Enemy.OnKilled -= HandleKilled; }

    void HandleKilled(Enemy e)
    {
        if (e != enemy || gone) return;
        gone = true;
        int wave = WaveSpawner.Instance != null ? WaveSpawner.Instance.currentWave : 1;
        int bonus = 60 * Mathf.Max(1, wave);
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(bonus);
        Progression.OnVipCaught();
        AudioManager.Play("coin_big");
        Vector2 pos = transform.position;
        HazardFx.Popup(pos + Vector2.up * 1.2f, "VIP ATRAPAT! +" + bonus, new Color(1f, 0.85f, 0.3f), 1.8f);
        for (int i = 0; i < 3; i++)
            Pickup.Spawn(pos + Random.insideUnitCircle * 1.2f, PickupType.Points);
    }

    void Update()
    {
        if (gone || enemy == null || enemy.IsDead) return;
        float left = leaveAt - Time.time;
        if (timer != null)
        {
            timer.text = Mathf.CeilToInt(Mathf.Max(0f, left)).ToString();
            timer.transform.rotation = Quaternion.identity;
        }
        if (left <= 0f)
        {
            gone = true;
            HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.2f, "EL VIP HA MARXAT...", new Color(0.8f, 0.8f, 0.85f), 1.6f);
            EnemyVariants.Burst(transform.position, 0.8f, new Color(1f, 0.9f, 0.5f, 0.6f));
            enemy.RemoveSilently();
        }
    }

    void FixedUpdate()
    {
        if (gone || enemy == null || rb == null || enemy.IsStunned) return;
        Transform player = PlayerRef.Transform;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 away = rb.position - (Vector2)player.position;
        float dist = away.magnitude;
        Vector2 dir = away / Mathf.Max(0.01f, dist);
        Vector2 side = new Vector2(-dir.y, dir.x) * strafe;

        // fuig, però de costat: si només fugís en línia recta es quedaria arraconat
        Vector2 move = dist < KeepAway ? (dir + side * 0.8f) : side;

        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2.2f;
        Vector2 p = rb.position;
        if (Mathf.Abs(p.x - c.x) > half.x || Mathf.Abs(p.y - c.y) > half.y)
        {
            move += (c - p).normalized * 2f;
            if (Random.value < 0.02f) strafe = -strafe;
        }

        rb.linearVelocity = move.normalized * enemy.MoveSpeed;
    }
}

// ---------- EL CARTELL ----------
// Un cartell gran en començar l'onada, i després un rètol petit a dalt mentre dura l'esdeveniment.
public class WaveEventBanner : MonoBehaviour
{
    static WaveEventBanner instance;

    private TextMeshProUGUI title, subtitle, tag;
    private CanvasGroup big;
    private float shownAt = -10f;

    public static void Show(string t, string sub, Color color)
    {
        if (instance == null) instance = new GameObject("CartellEsdeveniment").AddComponent<WaveEventBanner>();
        instance.title.text = t;
        instance.title.color = color;
        instance.subtitle.text = sub;
        instance.tag.text = "— " + t + " —"; // (la font no té el caràcter de l'estrella)
        instance.tag.color = color;
        instance.tag.gameObject.SetActive(true);
        instance.shownAt = Time.unscaledTime;
    }

    public static void HideTag()
    {
        if (instance != null) instance.tag.gameObject.SetActive(false);
    }

    void Awake()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bigGo = new GameObject("Gran", typeof(RectTransform), typeof(CanvasGroup));
        bigGo.transform.SetParent(canvasGo.transform, false);
        RectTransform brt = bigGo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
        big = bigGo.GetComponent<CanvasGroup>();
        big.alpha = 0f;
        big.blocksRaycasts = false;

        title = EndScreenUI.CreateText(bigGo.transform, "Titol", "", 110f, new Vector2(0.5f, 0.66f), new Vector2(1600f, 150f), Color.white, true);
        title.outlineWidth = 0.25f;
        title.outlineColor = new Color32(30, 20, 24, 255);
        subtitle = EndScreenUI.CreateText(bigGo.transform, "Subtitol", "", 44f, new Vector2(0.5f, 0.56f), new Vector2(1600f, 70f), new Color(1f, 0.95f, 0.85f));
        subtitle.outlineWidth = 0.2f;
        subtitle.outlineColor = new Color32(30, 20, 24, 255);
        tag = EndScreenUI.CreateText(canvasGo.transform, "Retol", "", 30f, new Vector2(0.5f, 0.86f), new Vector2(800f, 50f), Color.white, true);
        tag.outlineWidth = 0.2f;
        tag.outlineColor = new Color32(30, 20, 24, 255);
        foreach (TextMeshProUGUI t in new[] { title, subtitle, tag }) t.raycastTarget = false;
        tag.gameObject.SetActive(false);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void Update()
    {
        // entra, s'hi està una estona i se'n va (en temps real: el joc no s'atura)
        float t = Time.unscaledTime - shownAt;
        float a = t < 0.25f ? t / 0.25f : t < 2.2f ? 1f : Mathf.Clamp01(1f - (t - 2.2f) / 0.5f);
        big.alpha = a;
        float s = t < 0.25f ? Mathf.Lerp(1.4f, 1f, t / 0.25f) : 1f;
        title.transform.localScale = Vector3.one * s;

        if (tag.gameObject.activeSelf)
        {
            Color c = tag.color;
            c.a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
            tag.color = c;
        }
    }
}
