using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =====================================================================
//  COMANDAMENTS
// =====================================================================
// Es crea sol i viu entre escenes. S'encarrega de:
//  - saber si ara s'està jugant amb comandament o amb teclat i ratolí (UsingGamepad)
//  - els noms dels botons segons el comandament (Xbox: A B X Y · PlayStation: Creu Cercle Quadrat Triangle
//    · Switch: B A Y X), perquè els textos d'ajuda diguin el botó que toca
//  - la vibració (Rumble), que es pot treure al menú de pausa
//  - als menús, seleccionar sol el primer botó quan es toca el comandament (si no, no es pot fer res)
//  - amagar el cursor del ratolí mentre es juga amb comandament
public class GamepadInput : MonoBehaviour
{
    const string VibrationKey = "vibration";

    public static GamepadInput Instance { get; private set; }
    public static bool UsingGamepad { get; private set; }
    public static event System.Action OnModeChanged; // per refrescar els textos d'ajuda

    // pantalles que es controlen soles amb el comandament (no hi seleccionem botons)
    public static int CustomScreens;

    private Coroutine rumbleStop;

    public static bool Vibration
    {
        get { return PlayerPrefs.GetInt(VibrationKey, 1) == 1; }
        set { PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0); PlayerPrefs.Save(); if (!value) StopRumble(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (Instance != null) return;
        UsingGamepad = false;
        CustomScreens = 0;
        GameObject go = new GameObject("GamepadInput");
        DontDestroyOnLoad(go);
        go.AddComponent<GamepadInput>();
    }

    void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopRumble();
        if (Instance == this) Instance = null;
    }

    void OnApplicationQuit() { StopRumble(); }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        StopRumble();
        CustomScreens = 0;
    }

    void Update()
    {
        DetectMode();
        AutoSelectInMenus();
    }

    // ---------- AMB QUÈ S'ESTÀ JUGANT ----------

    void DetectMode()
    {
        bool pad = false, kbm = false;

        Gamepad g = Gamepad.current;
        if (g != null)
        {
            if (g.leftStick.ReadValue().sqrMagnitude > 0.09f || g.rightStick.ReadValue().sqrMagnitude > 0.09f) pad = true;
            else if (g.wasUpdatedThisFrame)
            {
                foreach (var c in g.allControls)
                {
                    if (c is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame) { pad = true; break; }
                }
            }
        }

        Keyboard k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) kbm = true;
        Mouse ms = Mouse.current;
        if (ms != null && (ms.delta.ReadValue().sqrMagnitude > 4f || ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame)) kbm = true;

        if (pad && !UsingGamepad) SetMode(true);
        else if (kbm && !pad && UsingGamepad) SetMode(false);
    }

    void SetMode(bool gamepad)
    {
        UsingGamepad = gamepad;
        Cursor.visible = !gamepad; // amb comandament, el cursor només fa nosa
        if (!gamepad && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        OnModeChanged?.Invoke();
    }

    // ---------- MENÚS: SELECCIONAR UN BOTÓ ----------

    void AutoSelectInMenus()
    {
        if (!UsingGamepad || CustomScreens > 0) return;
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject cur = es.currentSelectedGameObject;
        if (cur != null && cur.activeInHierarchy)
        {
            Selectable sel = cur.GetComponent<Selectable>();
            if (sel == null || sel.IsInteractable()) return; // ja hi ha un botó bo seleccionat
        }

        Selectable best = null;
        float bestScore = float.MinValue;
        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (s == null || !s.IsInteractable() || !s.gameObject.activeInHierarchy) continue;
            if (s.navigation.mode == Navigation.Mode.None) continue;
            Canvas c = s.GetComponentInParent<Canvas>();
            int order = c != null ? c.rootCanvas.sortingOrder : 0;
            // el canvas de més amunt, i dins seu el botó de més amunt i a l'esquerra
            Vector3 p = s.transform.position;
            float score = order * 100000f + p.y * 10f - p.x * 0.01f;
            if (score > bestScore) { bestScore = score; best = s; }
        }
        if (best != null) es.SetSelectedGameObject(best.gameObject);
    }

    // ---------- NOMS DELS BOTONS ----------

    enum PadKind { Xbox, PlayStation, Switch }

    static PadKind Kind()
    {
        Gamepad g = Gamepad.current;
        if (g == null) return PadKind.Xbox;
        string l = (g.layout ?? "") + " " + (g.name ?? "");
        if (l.Contains("DualShock") || l.Contains("DualSense") || l.Contains("PlayStation") || l.Contains("PS4") || l.Contains("PS5")) return PadKind.PlayStation;
        if (l.Contains("Switch")) return PadKind.Switch;
        return PadKind.Xbox;
    }

    public static string South() { switch (Kind()) { case PadKind.PlayStation: return "Creu"; case PadKind.Switch: return "B"; } return "A"; }
    public static string East() { switch (Kind()) { case PadKind.PlayStation: return "Cercle"; case PadKind.Switch: return "A"; } return "B"; }
    public static string West() { switch (Kind()) { case PadKind.PlayStation: return "Quadrat"; case PadKind.Switch: return "Y"; } return "X"; }
    public static string North() { switch (Kind()) { case PadKind.PlayStation: return "Triangle"; case PadKind.Switch: return "X"; } return "Y"; }
    public static string RightShoulder() { switch (Kind()) { case PadKind.PlayStation: return "R1"; case PadKind.Switch: return "R"; } return "RB"; }
    public static string LeftShoulder() { switch (Kind()) { case PadKind.PlayStation: return "L1"; case PadKind.Switch: return "L"; } return "LB"; }
    public static string RightTrigger() { switch (Kind()) { case PadKind.PlayStation: return "R2"; case PadKind.Switch: return "ZR"; } return "RT"; }
    public static string StartButton() { switch (Kind()) { case PadKind.PlayStation: return "Options"; case PadKind.Switch: return "+"; } return "Start"; }

    // ---------- VIBRACIÓ ----------

    // low = motor gros (cops sords), high = motor petit (vibració fina), seconds = durada
    public static void Rumble(float low, float high, float seconds)
    {
        if (Instance == null || !Vibration || !UsingGamepad) return;
        Gamepad g = Gamepad.current;
        if (g == null) return;
        g.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high));
        if (Instance.rumbleStop != null) Instance.StopCoroutine(Instance.rumbleStop);
        Instance.rumbleStop = Instance.StartCoroutine(Instance.StopAfter(seconds));
    }

    IEnumerator StopAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        StopRumble();
        rumbleStop = null;
    }

    public static void StopRumble()
    {
        Gamepad g = Gamepad.current;
        if (g != null) g.SetMotorSpeeds(0f, 0f);
    }
}
