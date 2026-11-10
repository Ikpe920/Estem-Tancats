using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// En acabar l'última onada: "Has sobreviscut! Acabes aquí o continues en mode infinit?"
// El joc s'atura mentre es decideix. Enter / A = continuar, Esc / B = acabar.
public class EndlessChoice : MonoBehaviour
{
    private Action<bool> answer;
    private bool wasPaused;
    private bool done;

    // obre la pregunta; quan es respon, crida 'onAnswer' (true = continuar en mode infinit)
    public static System.Collections.IEnumerator Ask(Action<bool> onAnswer)
    {
        bool answered = false;
        EndlessChoice c = new GameObject("PreguntaInfinit").AddComponent<EndlessChoice>();
        c.answer = r => { answered = true; onAnswer?.Invoke(r); };
        while (!answered) yield return null;
    }

    void Awake()
    {
        wasPaused = Time.timeScale == 0f;
        Time.timeScale = 0f;
        GamepadInput.CustomScreens++; // es controla sola: A continuar, B acabar
        Build();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (done) return;
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;
        if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
            (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)))
            Choose(true);
        else if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame))
            Choose(false);
    }

    void OnDestroy() { GamepadInput.CustomScreens = Mathf.Max(0, GamepadInput.CustomScreens - 1); }

    void Choose(bool keepGoing)
    {
        if (done) return;
        done = true;
        if (!wasPaused) Time.timeScale = 1f;
        Destroy(gameObject);
        answer?.Invoke(keepGoing);
    }

    void Build()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bg = new GameObject("Fons", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.06f, 0.88f);

        Transform p = bg.transform;
        EndScreenUI.CreateText(p, "Titol", "HAS SOBREVISCUT AL SERVEI!", 86f, new Vector2(0.5f, 0.74f),
            new Vector2(1700f, 130f), new Color(1f, 0.8f, 0.3f), true);
        TextMeshProUGUI body = EndScreenUI.CreateText(p, "Text",
            "Pots acabar aquí, o fer <b>HORES EXTRES</b> (mode infinit):\ncada onada és una hora, passa la nit i el dia, les sales continuen canviant,\ncada cop és més difícil i els caps tornen enfurismats. Els punts continuen sumant.",
            36f, new Vector2(0.5f, 0.52f), new Vector2(1500f, 220f), Color.white);
        body.raycastTarget = false;

        EndScreenUI.CreateButton(p, "Acabar aquí", new Vector2(0.38f, 0.28f), new Vector2(380f, 84f),
            new Color(0.30f, 0.30f, 0.35f), () => Choose(false));
        EndScreenUI.CreateButton(p, "Hores extres", new Vector2(0.62f, 0.28f), new Vector2(380f, 84f),
            new Color(0.55f, 0.25f, 0.6f), () => Choose(true));
        string helpLine = GamepadInput.UsingGamepad
            ? GamepadInput.South() + " per continuar  ·  " + GamepadInput.East() + " per acabar"
            : "Enter per continuar  ·  Esc per acabar";
        TextMeshProUGUI help = EndScreenUI.CreateText(p, "Ajuda", helpLine, 26f,
            new Vector2(0.5f, 0.17f), new Vector2(900f, 50f), new Color(0.85f, 0.78f, 0.65f));
        help.raycastTarget = false;
    }
}
