using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Tutorial de la primera partida: uns quants missatges a baix de la pantalla durant la primera onada.
// Cada missatge desapareix quan fas el que diu. Només surt fins que l'acabes un cop
// (es pot tornar a activar des del menú de pausa). Es crea sol a l'escena del joc.
public class TutorialHints : MonoBehaviour
{
    const string DoneKey = "tutorial_done";

    public static bool Done
    {
        get { return PlayerPrefs.GetInt(DoneKey, 0) == 1; }
        set { PlayerPrefs.SetInt(DoneKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    enum Step { Move, Shoot, Dash, ClearWave, Finished }

    private Step step = Step.Move;
    private float stepShownAt;
    private float completedAt = -1f;   // quan s'ha fet el pas actual (es veu el ✓ una estona)
    private Vector3 startPos;
    private bool hasStartPos;
    private int shotsAtStart;

    private CanvasGroup group;
    private TextMeshProUGUI label;
    private float alpha;
    private bool lastPad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // si alguna cosa falla aquí, que no aturi els altres scripts que esperen l'escena (Unity els crida
    // un darrere l'altre i un error talla la resta: per exemple, el que crea les taules i les caixes)
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try { OnSceneLoadedSafe(scene, mode); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    static void OnSceneLoadedSafe(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.Game && !Done && FindAnyObjectByType<TutorialHints>() == null)
            new GameObject("TutorialHints").AddComponent<TutorialHints>();
    }

    void Start()
    {
        Build();
        BeginStep(Step.Move);
    }

    void Update()
    {
        // amb el joc aturat (triar plat, pausa...) el missatge s'amaga i no avança
        bool paused = Time.timeScale == 0f || PlayerHealth.IsDying;
        float target = paused || step == Step.Finished ? 0f : 1f;
        alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime * 4f);
        group.alpha = alpha;

        if (step == Step.Finished)
        {
            if (alpha <= 0f) Destroy(gameObject);
            return;
        }
        if (paused) return;

        // si es canvia de teclat a comandament (o al revés), el text diu els botons que toquen
        if (lastPad != GamepadInput.UsingGamepad)
        {
            lastPad = GamepadInput.UsingGamepad;
            if (completedAt < 0f) label.text = Text(step);
        }

        if (completedAt < 0f)
        {
            if (StepDone())
            {
                completedAt = Time.time;
                label.text = "<color=#7CE38B><b>Fet!</b></color>  " + Text(step); // la font per defecte no té el símbol ✓
            }
        }
        else if (Time.time - completedAt > 0.8f)
        {
            BeginStep(step + 1);
        }
    }

    void BeginStep(Step s)
    {
        step = s;
        stepShownAt = Time.time;
        completedAt = -1f;

        if (s == Step.Shoot) shotsAtStart = PlayerShooting.ShotsFired;
        if (s == Step.Finished)
        {
            Done = true;
            return;
        }
        label.text = Text(s);
    }

    bool StepDone()
    {
        switch (step)
        {
            case Step.Move:
                Transform p = PlayerRef.Transform;
                if (p == null) return false;
                if (!hasStartPos) { startPos = p.position; hasStartPos = true; }
                return Vector2.Distance(p.position, startPos) > 1.5f;

            case Step.Shoot:
                return PlayerShooting.ShotsFired - shotsAtStart >= 3;

            case Step.Dash:
                PlayerMovement m = PlayerRef.Movement;
                return m != null && m.IsDashing;

            case Step.ClearWave:
                // s'acaba amb la primera onada (o al cap d'una estona, si ja anava avançat)
                WaveSpawner ws = WaveSpawner.Instance;
                return (ws != null && ws.currentWave >= 2) || Time.time - stepShownAt > 12f;
        }
        return true;
    }

    static string Text(Step s)
    {
        bool pad = GamepadInput.UsingGamepad;
        switch (s)
        {
            case Step.Move:
                return pad ? "Mou-te amb el <b>joystick esquerre</b>" : "Mou-te amb <b>WASD</b> o les fletxes";
            case Step.Shoot:
                return pad ? "Apunta i dispara amb el <b>joystick dret</b> (o mantén <b>" + GamepadInput.RightTrigger() + "</b>)"
                           : "Apunta amb el ratolí i dispara amb el <b>clic esquerre</b>";
            case Step.Dash:
                return pad ? "Prem <b>" + GamepadInput.RightShoulder() + "</b> per fer un dash: mentre dura, no et poden tocar"
                           : "Prem <b>ESPAI</b> per fer un dash: mentre dura, no et poden tocar";
            case Step.ClearWave:
                return "Despatxa tots els clients per acabar l'onada. Les fletxes vermelles t'indiquen on són";
        }
        return "";
    }

    // ---------- LA INTERFÍCIE ----------

    void Build()
    {
        GameObject canvasGo = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400; // per sota de la pausa
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // una franja fosca a baix amb el text a dins
        GameObject box = new GameObject("Fons", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.14f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1300f, 90f);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.04f, 0.06f, 0.78f);
        bg.raycastTarget = false;

        label = EndScreenUI.CreateText(box.transform, "Text", "", 36f, new Vector2(0.5f, 0.5f),
            new Vector2(1260f, 86f), new Color(1f, 0.95f, 0.85f));
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 36f;
    }
}
