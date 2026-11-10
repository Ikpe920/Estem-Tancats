using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class ActData
{
    public string name = "LA SALA";         // nom de l'acte (es mostra al títol)
    public string subtitle = "";            // la frase que surt a sota del títol
    public string resourceName = "Sala";    // nom del fitxer a Resources/Actes (si no hi poses el sprite a mà)
    public Sprite background;               // el terra de l'acte (opcional: si és buit es busca a Resources)
    public Color tint = Color.white;        // coloració de l'ambient
    public AudioClip music;                 // música de l'acte (opcional)
    public EnemyType[] enemies;             // els clients de l'acte (opcional; firstWave = ona dins de l'acte)
    public int extraEnemies = 0;            // clients extra per onada
    public float enemySpeedBonus = 0f;      // velocitat extra dels clients
    public float enemyHealthMultiplier = 1f;// vida extra dels clients
}

public class ActManager : MonoBehaviour
{
    public ActData[] acts;                  // els actes del joc (5)
    public int currentAct = 0;              // l'acte en què estem

    public SpriteRenderer background;       // el fons del terra (si el deixes buit, es crea sol)
    public Camera cam;                      // la càmera (per moure el fons i el color de fons)
    public int sortingOrder = -100;         // el fons ha d'anar darrere de tot
    public bool tileFloor = true;           // true: el terra es repeteix com a rajola; false: s'estira per cobrir tota la sala
    public AudioSource musicSource;         // opcional: per posar la música de cada acte

    public GameObject actPanel;             // opcional: panell on es mostra el títol de l'acte
    public TextMeshProUGUI actTitleText;
    public TextMeshProUGUI actSubtitleText;

    public GameObject legacyBackground;     // opcional: el panell vell del fons (s'amaga sol)
    public bool autoHideLegacyBackground = true;

    public float fadeDuration = 0.6f;       // durada de la transició
    public float titleDuration = 3.5f;      // temps que dura el títol de l'acte
    public bool showTitleAtStart = true;    // mostra el cartell de l'acte 1 en començar el joc
    public bool pauseForTitle = true;       // el joc s'atura mentre es mostra el cartell

    public static event System.Action<int> OnActChanged;

    // ---------- HORES EXTRES (el mode infinit) ----------
    // Després de l'onada 25 el xef fa hores extres: cada onada és una hora (la 26 és la una de la matinada)
    // i passa el dia i la nit: la llum canvia (nit blava, alba rosa, dia normal, posta taronja) i de nit sonen
    // les versions fosques de les músiques. Les sales continuen canviant (a l'atzar, mai la mateixa) i la
    // dificultat ja no torna a baixar: parteix de la de l'última sala i puja una mica a cada sala nova.
    public bool Overtime { get; private set; }
    public int OvertimeRooms { get; private set; }   // sales visitades fent hores extres
    private int overtimeStartWave;                    // la primera onada de les hores extres (la una)
    private int waveNow;                              // l'onada que s'està jugant
    private bool nightNow;
    private Color skyNow = Color.white, skyTarget = Color.white;
    private bool transitioning;

    public void StartOvertime(int firstWave)
    {
        Overtime = true;
        OvertimeRooms = 0;
        overtimeStartWave = firstWave;
        nightNow = true;
    }

    // l'hora (0-23) d'una onada; Hour és la de l'onada d'ara
    public int HourForWave(int wave) { return (((wave - overtimeStartWave + 1) % 24) + 24) % 24; }
    public int Hour => HourForWave(waveNow);
    public static string Clock(int hour) { return hour.ToString("00") + ":00"; }
    public static bool IsNightHour(int hour) { return hour >= 20 || hour < 6; }
    public bool NightMusic => Overtime && IsNightHour(Hour);

    public static string PartOfDay(int h)
    {
        if (h < 6) return "matinada";
        if (h < 8) return "alba";
        if (h < 12) return "matí";
        if (h < 16) return "migdia";
        if (h < 20) return "tarda";
        return "vespre";
    }

    // el WaveSpawner avisa a cada onada nova: avança el rellotge
    public void OnWaveStarted(int wave)
    {
        waveNow = wave;
        if (!Overtime) return;
        skyTarget = SkyTint(Hour);
        bool night = IsNightHour(Hour);
        if (night != nightNow)
        {
            nightNow = night;
            AudioManager.RefreshActMusic(); // es fa de dia (o de nit): canvia la música
        }
    }

    // el color de la llum segons l'hora
    static readonly float[] SkyHours = { 0f, 4.5f, 6f, 7f, 9f, 17f, 19f, 20.5f, 22f, 24f };
    static readonly Color[] SkyColors =
    {
        new Color(0.60f, 0.64f, 0.92f), // nit
        new Color(0.60f, 0.64f, 0.92f),
        new Color(0.84f, 0.70f, 0.88f), // alba (lila i rosa)
        new Color(1.00f, 0.82f, 0.68f), // surt el sol (taronja)
        Color.white,                    // dia
        Color.white,
        new Color(1.00f, 0.76f, 0.58f), // posta
        new Color(0.80f, 0.66f, 0.86f), // capvespre
        new Color(0.60f, 0.64f, 0.92f), // nit
        new Color(0.60f, 0.64f, 0.92f),
    };

    public static Color SkyTint(float hour)
    {
        hour = Mathf.Repeat(hour, 24f);
        for (int i = 0; i < SkyHours.Length - 1; i++)
            if (hour <= SkyHours[i + 1])
                return Color.Lerp(SkyColors[i], SkyColors[i + 1], Mathf.InverseLerp(SkyHours[i], SkyHours[i + 1], hour));
        return SkyColors[0];
    }

    // la dificultat de la sala (fent hores extres, la de la sala més difícil i una mica més)
    public int ExtraEnemies
    {
        get
        {
            if (!Overtime) return Current != null ? Current.extraEnemies : 0;
            int max = 0;
            if (acts != null) foreach (ActData a in acts) if (a != null) max = Mathf.Max(max, a.extraEnemies);
            return max + OvertimeRooms;
        }
    }

    public float HealthMultiplier
    {
        get
        {
            if (!Overtime) return Current != null ? Current.enemyHealthMultiplier : 1f;
            float max = 1f;
            if (acts != null) foreach (ActData a in acts) if (a != null) max = Mathf.Max(max, a.enemyHealthMultiplier);
            return max * (1f + 0.12f * OvertimeRooms);
        }
    }

    public float SpeedBonus
    {
        get
        {
            if (!Overtime) return Current != null ? Current.enemySpeedBonus : 0f;
            float max = 0f;
            if (acts != null) foreach (ActData a in acts) if (a != null) max = Mathf.Max(max, a.enemySpeedBonus);
            return max + 0.04f * OvertimeRooms;
        }
    }

    // el color de la sala amb la llum de l'hora
    public Color TintOf(ActData act)
    {
        if (act == null) return Color.white;
        Color t = act.tint;
        return Overtime ? new Color(t.r * skyNow.r, t.g * skyNow.g, t.b * skyNow.b, t.a) : t;
    }

    // la llum canvia a poc a poc d'una hora a l'altra
    void Update()
    {
        if (!Overtime || transitioning) return;
        if (skyNow == skyTarget) return;
        skyNow = Color.Lerp(skyNow, skyTarget, Time.unscaledDeltaTime * 0.8f);
        if (Mathf.Abs(skyNow.r - skyTarget.r) + Mathf.Abs(skyNow.g - skyTarget.g) + Mathf.Abs(skyNow.b - skyTarget.b) < 0.004f) skyNow = skyTarget;
        Color tint = TintOf(Current);
        if (background != null) background.color = new Color(tint.r, tint.g, tint.b, background.color.a);
        if (cam != null) cam.backgroundColor = tint * 0.4f;
    }

    public ActData Current
    {
        get
        {
            if (acts == null || acts.Length == 0) return null;
            return acts[Mathf.Clamp(currentAct, 0, acts.Length - 1)];
        }
    }

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        PrepareBackground();
        HideLegacyBackground();
        ApplyAct(currentAct);
    }

    void Start()
    {
        // l'acte 1 també presenta el cartell, en començar el joc
        if (showTitleAtStart && actPanel != null && actTitleText != null)
            StartCoroutine(StartWithTitle());
    }

    IEnumerator StartWithTitle()
    {
        bool wasPaused = Time.timeScale == 0f;
        if (pauseForTitle) Time.timeScale = 0f;

        // el cartell de pel·lícula muda del principi
        yield return Intertitle.Show(Intertitle.Opening(), false, 2.6f);

        // el terra apareix des de la foscor
        ActData act = Current;
        Color tint = act != null ? act.tint : Color.white;
        if (background != null) background.color = new Color(tint.r, tint.g, tint.b, 0f);
        if (cam != null) cam.backgroundColor = Color.black;

        yield return StartCoroutine(FadeTo(1f, tint * 0.4f));
        yield return StartCoroutine(ShowTitle());

        if (pauseForTitle && !wasPaused) Time.timeScale = 1f;
    }

    void Reset()
    {
        acts = new ActData[]
        {
            MakeAct("LA SALA", "Les taules ja no serveixen per a ningú", "Sala", new Color(1f, 0.97f, 0.93f), 0, 0f, 1f),
            MakeAct("LA BARRA", "Les copes s'han buidat... i ells no", "Barra", new Color(1f, 0.93f, 0.82f), 2, 0.15f, 1.2f),
            MakeAct("LA CUINA", "A la cuina no hi ha foc, i ells han vingut a engegar-lo", "Cuina", new Color(0.88f, 0.95f, 1f), 4, 0.3f, 1.4f),
            MakeAct("LA TERRASSA", "Fins i tot fora, s'han quedat", "Terrassa", new Color(1f, 0.97f, 0.86f), 6, 0.45f, 1.6f),
            MakeAct("LA TEULADA", "L'últim recurs: el sostre", "Teulada", new Color(0.85f, 0.89f, 1f), 8, 0.6f, 1.8f),
        };
    }

    static ActData MakeAct(string actName, string actSubtitle, string resName, Color actTint, int extra, float speed, float hp)
    {
        ActData a = new ActData();
        a.name = actName;
        a.subtitle = actSubtitle;
        a.resourceName = resName;
        a.tint = actTint;
        a.extraEnemies = extra;
        a.enemySpeedBonus = speed;
        a.enemyHealthMultiplier = hp;
        return a;
    }

    // ---------- EL FONS ----------

    void PrepareBackground()
    {
        if (background == null)
        {
            GameObject go = new GameObject("FonsActe");
            background = go.AddComponent<SpriteRenderer>();
        }
        background.sortingOrder = sortingOrder;
        FitBackgroundToArena();
    }

    // el terra és fix al món: cobreix tota la sala (parets incloses) i no segueix la càmera,
    // així quan el jugador es mou, el terra passa per sota seu i no marea
    void FitBackgroundToArena()
    {
        if (background == null) return;

        Vector2 size;
        Vector2 center;
        ArenaWalls walls = ArenaWalls.Instance != null ? ArenaWalls.Instance : FindAnyObjectByType<ArenaWalls>();
        if (walls != null)
        {
            size = walls.size + Vector2.one * walls.thickness * 2f;
            center = walls.transform.position;
        }
        else
        {
            size = HazardManager.ArenaSize + Vector2.one * 4f;
            center = HazardManager.ArenaCenter;
        }

        Transform t = background.transform;
        if (t.parent != null) t.SetParent(null, true); // abans estava enganxat a la càmera
        t.position = new Vector3(center.x, center.y, 5f);
        t.rotation = Quaternion.identity;

        if (tileFloor)
        {
            t.localScale = Vector3.one;
            background.drawMode = SpriteDrawMode.Tiled;
            background.size = size;
        }
        else if (background.sprite != null)
        {
            background.drawMode = SpriteDrawMode.Simple;
            Vector2 native = background.sprite.bounds.size;
            t.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }
    }

    void HideLegacyBackground()
    {
        if (!autoHideLegacyBackground) return;
        if (legacyBackground == null) legacyBackground = GameObject.Find("Background");
        if (legacyBackground != null) legacyBackground.SetActive(false);
    }

    // ---------- ELS ACTES ----------

    public void ApplyAct(int index)
    {
        if (acts == null || acts.Length == 0) return;
        currentAct = ((index % acts.Length) + acts.Length) % acts.Length;
        OnActChanged?.Invoke(currentAct);

        ActData act = Current;
        if (act == null) return;

        if (background != null)
        {
            Sprite sprite = GetActSprite(act);
            if (sprite != null) background.sprite = sprite;
            background.color = TintOf(act);
            FitBackgroundToArena();
        }
        if (cam != null) cam.backgroundColor = TintOf(act) * 0.4f;
        if (musicSource != null && act.music != null)
        {
            musicSource.clip = act.music;
            musicSource.Play();
        }
    }

    Sprite GetActSprite(ActData act)
    {
        if (act.background != null) return act.background;
        if (!string.IsNullOrEmpty(act.resourceName)) return Resources.Load<Sprite>("Actes/" + act.resourceName);
        return null;
    }

    public IEnumerator TransitionToNextAct()
    {
        if (acts == null || acts.Length == 0) yield break;

        bool wasPaused = Time.timeScale == 0f;
        if (pauseForTitle) Time.timeScale = 0f;

        yield return StartCoroutine(FadeTo(0f, Color.black));

        transitioning = true;

        // quina sala ve ara (fent hores extres, una a l'atzar i mai la mateixa)
        int next = currentAct + 1;
        bool night = Overtime && acts.Length > 1;
        bool firstNight = night && OvertimeRooms == 0;
        if (night)
        {
            next = Random.Range(0, acts.Length - 1);
            if (next >= currentAct) next++;
        }
        int nextIndex = ((next % acts.Length) + acts.Length) % acts.Length;

        // el cartell de pel·lícula muda: "Mentrestant, a la cuina..."
        ActData upcoming = acts[nextIndex];
        int nextHour = Overtime ? HourForWave(waveNow + 1) : 0;
        yield return Intertitle.Show(Intertitle.ActIntro(nextIndex, upcoming != null ? upcoming.name : "", night, firstNight, nextHour));

        if (night) OvertimeRooms++;
        if (Overtime) skyNow = skyTarget = SkyTint(nextHour); // la sala nova ja surt amb la llum de la seva hora
        ApplyAct(next);

        ActData act = Current;
        Color tint = TintOf(act);
        if (background != null) background.color = new Color(tint.r, tint.g, tint.b, 0f);
        if (cam != null) cam.backgroundColor = Color.black;

        yield return StartCoroutine(FadeTo(1f, tint * 0.4f));

        yield return StartCoroutine(ShowTitle());

        transitioning = false;
        if (pauseForTitle && !wasPaused) Time.timeScale = 1f;
    }

    IEnumerator FadeTo(float targetAlpha, Color targetCamColor)
    {
        if (background == null) yield break;

        Color start = background.color;
        Color startCam = cam != null ? cam.backgroundColor : Color.black;
        float duration = Mathf.Max(0.05f, fadeDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // el temps pot estar aturat (cartell)
            float k = Mathf.Clamp01(t / duration);
            background.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, targetAlpha, k));
            if (cam != null) cam.backgroundColor = Color.Lerp(startCam, targetCamColor, k);
            yield return null;
        }
    }

    // el cartell de l'acte nou (ActBanner: franja fosca, "ACTE N", el nom que cau de cop i la frase)
    IEnumerator ShowTitle()
    {
        ActData act = Current;
        if (act == null) yield break;
        if (actPanel != null) actPanel.SetActive(false);
        if (Overtime)
        {
            int h = HourForWave(waveNow + 1);
            yield return ActBanner.ActTitle("—  HORES EXTRES · " + Clock(h) + "  —", act.name, act.subtitle, titleDuration, TintOf(act));
        }
        else yield return ActBanner.ActTitle(currentAct + 1, act.name, act.subtitle, titleDuration, act.tint);
    }

    // el segell d'onada completada (o "SERVEI PERFECTE!" si no t'han tocat)
    public IEnumerator ShowRoundComplete(int wave)
    {
        if (actPanel != null) actPanel.SetActive(false);
        yield return ActBanner.RoundComplete(wave, 1.2f);
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }

    // ----- per provar els actes sense arribar al cap -----

    [ContextMenu("Acte següent")]
    void DebugNextAct()
    {
        ApplyAct(currentAct + 1);
    }

    [ContextMenu("Acte anterior")]
    void DebugPrevAct()
    {
        ApplyAct(currentAct - 1);
    }

    [ContextMenu("Fer el canvi d'acte")]
    void DebugTransition()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(TransitionToNextAct());
    }
}