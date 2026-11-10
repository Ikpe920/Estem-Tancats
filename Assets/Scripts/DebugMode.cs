using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// MODE DE PROVA
// Només existeix a l'Editor i a les builds de desenvolupament ("Development Build"). A la build final
// aquesta classe queda buida. Es crea sola a l'escena del joc: no cal afegir res a cap escena.
//
//   F1  mostra / amaga el quadre
//   F2  salta l'onada (la dona per acabada)
//   F3  salta al proper cap
//   F4  desplega la llista de plats (clica'n un per agafar-lo, sense topall)
//   F5  cura tots els cors
//   F6  invencible sí / no
//   F7  obre ara la pantalla de triar plat
//   F8  fa sortir un client especial de cada tipus (Golafre, Olla, Safata, Cambrer)
//   F9  la propera onada tindrà un esdeveniment (cada cop que el prems, en tria un altre)
//   F10 +500 propines (per provar els desbloquejos)
public class DebugMode : MonoBehaviour
{
    // el ratolí és a sobre del quadre? PlayerShooting ho mira perquè no disparis en clicar els botons
    public static bool MouseOverPanel;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    private WaveSpawner spawner;
    private BoonManager boonManager;
    private PlayerHealth health;

    private bool open = true;
    private bool showDishes;
    private bool showSets;
    private Vector2 scroll;
    private string waveInput = "10";
    private string message = "";
    private float messageUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // a cada escena que tingui partida (WaveSpawner) creem el mode de prova
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MouseOverPanel = false;
        if (FindAnyObjectByType<WaveSpawner>() == null) return;
        if (FindAnyObjectByType<DebugMode>() != null) return;
        new GameObject("ModeDeProva").AddComponent<DebugMode>();
    }

    void Start()
    {
        spawner = FindAnyObjectByType<WaveSpawner>();
        boonManager = FindAnyObjectByType<BoonManager>();
        health = PlayerRef.Health;
    }

    void OnDestroy()
    {
        MouseOverPanel = false;
    }

    void Say(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 3f;
    }

    // ---------- TECLES ----------

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f1Key.wasPressedThisFrame) open = !open;
            if (kb.f2Key.wasPressedThisFrame) SkipWave();
            if (kb.f3Key.wasPressedThisFrame) NextBoss();
            if (kb.f4Key.wasPressedThisFrame) { open = true; showDishes = !showDishes; }
            if (kb.f5Key.wasPressedThisFrame) HealFull();
            if (kb.f6Key.wasPressedThisFrame) ToggleGod();
            if (kb.f7Key.wasPressedThisFrame) OfferNow();
            if (kb.f8Key.wasPressedThisFrame) SpawnSpecials();
            if (kb.f9Key.wasPressedThisFrame) NextEvent();
            if (kb.f10Key.wasPressedThisFrame) { Progression.AddTips(500); Say("+500 propines (" + Progression.Tips + ")"); }
        }

        UpdateMouseOver();
    }

    void UpdateMouseOver()
    {
        MouseOverPanel = false;
        if (!open || Mouse.current == null) return;

        float s = GuiScale();
        Vector2 m = Mouse.current.position.ReadValue();
        m.y = Screen.height - m.y; // el ratolí compta des de baix; l'IMGUI, des de dalt

        Rect r = PanelRect(Screen.width / s, Screen.height / s);
        Rect screenRect = new Rect(r.x * s, r.y * s, r.width * s, r.height * s);
        MouseOverPanel = screenRect.Contains(m);
    }

    // ---------- ACCIONS ----------

    void SkipWave()
    {
        if (spawner == null) return;
        JumpTo(spawner.currentWave + 1);
    }

    void NextBoss()
    {
        if (spawner == null) return;
        int every = Mathf.Max(1, spawner.bossEveryNWaves);
        JumpTo((spawner.currentWave / every + 1) * every);
    }

    void JumpTo(int wave)
    {
        if (spawner == null) return;
        if (spawner.DebugJumpToWave(wave)) Say("Saltant a l'onada " + Mathf.Clamp(wave, 1, spawner.finalWave > 0 ? spawner.finalWave : wave));
        else Say("Ara no es pot saltar (espera que comenci una onada)");
    }

    void HealFull()
    {
        if (health == null) return;
        health.Heal(health.maxLives);
        Say("Cors plens");
    }

    void ToggleGod()
    {
        PlayerHealth.GodMode = !PlayerHealth.GodMode;
        Say(PlayerHealth.GodMode ? "Invencible: SÍ" : "Invencible: no");
    }

    void SpawnSpecials()
    {
        if (spawner == null) return;
        Say(spawner.DebugSpawnSpecials() ? "Clients especials al voltant!" : "No s'han pogut fer sortir");
    }

    void NextEvent()
    {
        int n = System.Enum.GetValues(typeof(WaveEventKind)).Length;
        int k = ((int)WaveEvents.ForcedNext % (n - 1)) + 1; // 1..n-1 (mai "cap")
        WaveEvents.ForcedNext = (WaveEventKind)k;
        Say("Propera onada: " + WaveEvents.Title(WaveEvents.ForcedNext));
    }

    void OfferNow()
    {
        if (boonManager == null) return;
        if (!boonManager.DebugOffer()) Say("Ara no es pot obrir la tria de plats");
    }

    // ---------- EL QUADRE ----------

    float GuiScale()
    {
        return Mathf.Max(1f, Screen.height / 540f);
    }

    Rect PanelRect(float vw, float vh)
    {
        return new Rect(vw - 276f, 8f, 268f, vh - 16f);
    }

    void OnGUI()
    {
        float s = GuiScale();
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float vw = Screen.width / s;
        float vh = Screen.height / s;

        if (!open)
        {
            GUI.Label(new Rect(vw - 96f, vh - 22f, 92f, 20f), "F1: mode de prova"); // a baix, que no tapi els punts
            return;
        }

        Rect r = PanelRect(vw, vh);
        GUI.Box(r, "MODE DE PROVA");
        GUILayout.BeginArea(new Rect(r.x + 6f, r.y + 24f, r.width - 12f, r.height - 30f));
        scroll = GUILayout.BeginScrollView(scroll);

        DrawStatus();
        DrawWaves();
        DrawPlayer();
        DrawDishes();
        DrawSets();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawStatus()
    {
        if (spawner != null)
        {
            ActManager acts = spawner.actManager;
            string act = acts != null ? " · acte " + (acts.currentAct + 1) : "";
            GUILayout.Label("Onada " + spawner.currentWave + act + " · clients: " + Enemy.Alive);
        }
        if (health != null)
            GUILayout.Label("Cors " + health.CurrentLives + "/" + health.maxLives
                + (PlayerHealth.GodMode ? "  (INVENCIBLE)" : ""));
        if (ScoreManager.Instance != null)
            GUILayout.Label("Punts " + ScoreManager.Instance.totalScore + " · ratxa " + ScoreManager.Instance.Combo);

        if (Time.unscaledTime < messageUntil && !string.IsNullOrEmpty(message))
        {
            Color old = GUI.color;
            GUI.color = new Color(1f, 0.9f, 0.4f);
            GUILayout.Label(message);
            GUI.color = old;
        }
        GUILayout.Space(4f);
    }

    void DrawWaves()
    {
        GUILayout.Label("— ONADES —");
        if (GUILayout.Button("Salta l'onada (F2)")) SkipWave();
        if (GUILayout.Button("Proper cap (F3)")) NextBoss();

        GUILayout.BeginHorizontal();
        waveInput = GUILayout.TextField(waveInput, 3, GUILayout.Width(50f));
        if (GUILayout.Button("Anar a l'onada"))
        {
            int w;
            if (int.TryParse(waveInput, out w)) JumpTo(w);
        }
        GUILayout.EndHorizontal();

        if (spawner != null && spawner.finalWave > 0 && GUILayout.Button("Onada final (prova la victòria)"))
            JumpTo(spawner.finalWave);
        GUILayout.Space(4f);
    }

    void DrawPlayer()
    {
        GUILayout.Label("— JUGADOR —");
        if (GUILayout.Button("Cura tots els cors (F5)")) HealFull();
        if (GUILayout.Button(PlayerHealth.GodMode ? "Invencible: SÍ (F6)" : "Invencible: no (F6)")) ToggleGod();
        if (GUILayout.Button("Obre la tria de plats (F7)")) OfferNow();
        if (GUILayout.Button("Clients especials (F8)")) SpawnSpecials();
        if (GUILayout.Button("Esdeveniment a la propera onada (F9)")) NextEvent();
        if (GUILayout.Button("+500 propines (F10)")) { Progression.AddTips(500); Say("+500 propines (" + Progression.Tips + ")"); }
        GUILayout.Space(4f);
    }

    void DrawDishes()
    {
        showDishes = GUILayout.Toggle(showDishes, showDishes ? "v Plats (F4)" : "> Plats (F4)", "Button");
        if (!showDishes || boonManager == null) { GUILayout.Space(4f); return; }

        Boon[] library = boonManager.Library;
        if (library == null) return;

        foreach (Boon b in library)
        {
            if (b == null) continue;
            int n = boonManager.StacksOf(b);
            string label = b.name + (n > 0 ? "   ×" + n : "");

            Color old = GUI.color;
            if (n > 0) GUI.color = new Color(1f, 0.9f, 0.5f);
            if (GUILayout.Button(label))
            {
                boonManager.DebugGive(b);
                Say("Agafat: " + b.name);
            }
            GUI.color = old;
        }
        GUILayout.Space(4f);
    }

    void DrawSets()
    {
        showSets = GUILayout.Toggle(showSets, showSets ? "v Conjunts" : "> Conjunts", "Button");
        if (!showSets) return;

        foreach (SetBonus set in SetBonuses.All)
        {
            bool on = SetBonuses.Has(set.id);
            Color old = GUI.color;
            GUI.color = on ? new Color(0.6f, 1f, 0.6f) : new Color(0.85f, 0.85f, 0.85f);
            GUILayout.Label((on ? "[ACTIU] " : "- ") + set.name + "  (" + set.a + " + " + set.b + ")");
            GUI.color = old;
        }
    }

#endif
}
