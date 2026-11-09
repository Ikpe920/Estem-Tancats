using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Menú de pausa: Esc (teclat) o Start (comandament) durant la partida.
// Es crea sol a l'escena del joc, no cal posar-lo a cap objecte. També guarda el volum.
//
// Només s'obre si el joc no està ja pausat per una altra pantalla (triar plat, presentació del cap,
// canvi d'acte), perquè aquestes miren "timeScale == 0" i tornen el temps a 1 quan acaben.
public class PauseMenu : MonoBehaviour
{
    const string VolumeKey = "volume";
    const float VolumeStep = 0.1f;

    public static bool IsOpen { get; private set; }

    // després de clicar "Continuar", el clic que encara està premut no ha de disparar
    public static bool SuppressMouseFire;

    private GameObject root;
    private Button continueButton;
    private TextMeshProUGUI volumeText;
    private Button tutorialButton;
    private Button vibrationButton;
    private TextMeshProUGUI helpText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        IsOpen = false;
        SuppressMouseFire = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsOpen = false;
        SuppressMouseFire = false;
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f); // el volum guardat, a totes les escenes

        if (scene.name == SceneNames.Game && FindAnyObjectByType<PauseMenu>() == null)
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Start()
    {
        Build();
        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (IsOpen) Time.timeScale = 1f;
        IsOpen = false;
    }

    void Update()
    {
        if (!PausePressed()) return;

        if (IsOpen) Resume();
        else TryOpen();
    }

    // si la finestra perd el focus enmig de la partida, pausem
    void OnApplicationFocus(bool focused)
    {
        if (!focused) TryOpen();
    }

    bool PausePressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        Gamepad pad = Gamepad.current;
        if (pad == null) return false;
        if (pad.startButton.wasPressedThisFrame) return true;
        return IsOpen && pad.buttonEast.wasPressedThisFrame; // B / cercle tanca el menú
    }

    void TryOpen()
    {
        if (IsOpen || root == null) return;
        if (PlayerHealth.IsDying) return;
        if (Time.timeScale < 0.5f) return; // una altra pantalla ja ha pausat el joc (o una pausa d'impacte)

        IsOpen = true;
        Time.timeScale = 0f;
        RefreshVolume();
        RefreshTutorialButton();
        RefreshVibration();
        root.SetActive(true);
        AudioManager.Play("ui_click");

        // perquè es pugui navegar amb el comandament
        if (EventSystem.current != null && continueButton != null)
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    public void Resume()
    {
        if (!IsOpen) return;
        IsOpen = false;
        root.SetActive(false);
        AudioManager.Play("ui_back");
        Time.timeScale = 1f;
        SuppressMouseFire = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Restart()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        RunStats.Reset();
        SceneManager.LoadScene(SceneNames.Game);
    }

    void GoToMenu()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    void EnableTutorial()
    {
        TutorialHints.Done = false;
        RefreshTutorialButton();
    }

    void RefreshTutorialButton()
    {
        if (tutorialButton == null) return;
        TextMeshProUGUI t = tutorialButton.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return;
        t.enableAutoSizing = true; // el text "activat" és més llarg
        t.fontSizeMin = 18f;
        t.fontSizeMax = 30f;
        t.text = TutorialHints.Done ? "Tornar a veure el tutorial" : "Tutorial activat (propera partida)";
    }

    void ToggleVibration()
    {
        GamepadInput.Vibration = !GamepadInput.Vibration;
        RefreshVibration();
        if (GamepadInput.Vibration) GamepadInput.Rumble(0.4f, 0.4f, 0.2f); // perquè es noti que torna a anar
    }

    void RefreshVibration()
    {
        if (vibrationButton == null) return;
        TextMeshProUGUI t = vibrationButton.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = GamepadInput.UsingGamepad && !GamepadInput.CanRumble
            ? "Vibració: no disponible en aquest comandament"
            : "Vibració del comandament: " + (GamepadInput.Vibration ? "Sí" : "No");
        if (helpText != null)
            helpText.text = GamepadInput.UsingGamepad
                ? GamepadInput.StartButton() + " o " + GamepadInput.East() + " per continuar"
                : "Esc per continuar";
    }

    void ChangeVolume(float delta)
    {
        float v = Mathf.Clamp01(Mathf.Round((AudioListener.volume + delta) * 10f) / 10f);
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
        PlayerPrefs.Save();
        RefreshVolume();
    }

    void RefreshVolume()
    {
        if (volumeText != null)
            volumeText.text = "Volum: " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
    }

    // ---------- LA INTERFÍCIE (es crea per codi, com la dels plats) ----------

    void Build()
    {
        // un canvas propi per sobre de tot, que s'escala amb la pantalla
        GameObject canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // fons fosc que tapa el joc (i els clics)
        root = new GameObject("Pausa", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.06f, 0.82f);

        Transform p = root.transform;
        EndScreenUI.CreateText(p, "Titol", "PAUSA", 96f, new Vector2(0.5f, 0.80f), new Vector2(900f, 140f),
            new Color(1f, 0.8f, 0.3f), true);

        Vector2 size = new Vector2(420f, 80f);
        continueButton = EndScreenUI.CreateButton(p, "Continuar", new Vector2(0.5f, 0.64f), size,
            new Color(0.20f, 0.45f, 0.25f), Resume);
        EndScreenUI.CreateButton(p, "Tornar a començar", new Vector2(0.5f, 0.55f), size,
            new Color(0.55f, 0.33f, 0.12f), Restart);
        EndScreenUI.CreateButton(p, "Menú principal", new Vector2(0.5f, 0.46f), size,
            new Color(0.30f, 0.30f, 0.35f), GoToMenu);

        // tornar a veure el tutorial a la propera partida
        tutorialButton = EndScreenUI.CreateButton(p, "Tornar a veure el tutorial", new Vector2(0.5f, 0.37f), size,
            new Color(0.25f, 0.35f, 0.50f), EnableTutorial);

        // volum: [-]  Volum: 80%  [+]
        Vector2 small = new Vector2(80f, 80f);
        EndScreenUI.CreateButton(p, "-", new Vector2(0.40f, 0.26f), small,
            new Color(0.30f, 0.30f, 0.35f), () => ChangeVolume(-VolumeStep));
        volumeText = EndScreenUI.CreateText(p, "Volum", "", 40f, new Vector2(0.5f, 0.26f), new Vector2(260f, 80f),
            Color.white);
        volumeText.raycastTarget = false; // que no tapi els botons del costat
        EndScreenUI.CreateButton(p, "+", new Vector2(0.60f, 0.26f), small,
            new Color(0.30f, 0.30f, 0.35f), () => ChangeVolume(VolumeStep));

        // vibració del comandament: sí / no
        vibrationButton = EndScreenUI.CreateButton(p, "", new Vector2(0.5f, 0.175f), new Vector2(420f, 64f),
            new Color(0.30f, 0.30f, 0.35f), ToggleVibration);

        helpText = EndScreenUI.CreateText(p, "Ajuda", "", 28f, new Vector2(0.5f, 0.08f),
            new Vector2(900f, 60f), new Color(0.85f, 0.78f, 0.65f));
        helpText.raycastTarget = false;
    }
}
