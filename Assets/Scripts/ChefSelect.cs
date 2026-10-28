using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChefSelect : MonoBehaviour
{
    public Sprite defaultSprite; // el sprite del xef (Chef_Down) per a la vista prèvia

    private readonly List<Image> cardBackgrounds = new List<Image>();
    private int selected;

    void Start()
    {
        Time.timeScale = 1f;

        Camera cam = Camera.main;
        if (cam != null) cam.backgroundColor = new Color(0.08f, 0.07f, 0.10f);

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        selected = Mathf.Clamp(Chefs.Selected, 0, Chefs.All.Length - 1);

        EndScreenUI.CreateText(canvas.transform, "TitleText", "TRIA EL TEU XEF", 72f,
            new Vector2(0.5f, 0.90f), new Vector2(1500f, 110f), Color.white, true);

        int n = Chefs.All.Length;
        float gap = 40f;
        float cardW = Mathf.Min(380f, (1800f - (n - 1) * gap) / n);
        float cardH = 600f;
        float totalW = n * cardW + (n - 1) * gap;

        for (int i = 0; i < n; i++)
        {
            BuildCard(canvas.transform, i, -totalW / 2f + cardW / 2f + i * (cardW + gap), cardW, cardH);
        }

        EndScreenUI.CreateButton(canvas.transform, "Començar", new Vector2(0.5f, 0.07f),
            new Vector2(360f, 80f), new Color(0.20f, 0.45f, 0.25f), StartGame);

        Highlight();
    }

    void BuildCard(Transform parent, int index, float x, float w, float h)
    {
        ChefInfo chef = Chefs.All[index];

        GameObject card = new GameObject("Carta " + chef.name, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -20f);

        Image bg = card.GetComponent<Image>();
        cardBackgrounds.Add(bg);

        Button btn = card.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None; // el ressaltat el fem nosaltres
        int captured = index;
        btn.onClick.AddListener(() => Select(captured));

        // vista prèvia del xef
        Sprite preview = null;
        bool custom = false;
        if (!string.IsNullOrEmpty(chef.spritePrefix))
        {
            preview = Resources.Load<Sprite>("Chefs/" + chef.spritePrefix + "_Down");
            custom = preview != null;
        }
        if (preview == null) preview = defaultSprite;

        GameObject portrait = new GameObject("Retrat", typeof(RectTransform), typeof(Image));
        portrait.transform.SetParent(card.transform, false);
        RectTransform prt = portrait.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.sizeDelta = new Vector2(220f, 220f);
        prt.anchoredPosition = new Vector2(0f, -30f);
        Image pimg = portrait.GetComponent<Image>();
        pimg.sprite = preview;
        pimg.preserveAspect = true;
        pimg.color = custom ? Color.white : chef.tint;
        pimg.raycastTarget = false;

        // nom, passiva i estadístiques (els anchors són fraccions de la carta)
        TMPro.TextMeshProUGUI nameText = EndScreenUI.CreateText(card.transform, "Nom", chef.name, 38f,
            new Vector2(0.5f, 0.57f), new Vector2(w - 30f, 60f), Color.white, true);
        nameText.raycastTarget = false;

        TMPro.TextMeshProUGUI descText = EndScreenUI.CreateText(card.transform, "Passiva", chef.description, 25f,
            new Vector2(0.5f, 0.38f), new Vector2(w - 40f, 170f), new Color(0.92f, 0.92f, 0.92f));
        descText.raycastTarget = false;

        TMPro.TextMeshProUGUI statsText = EndScreenUI.CreateText(card.transform, "Estadistiques", chef.stats, 24f,
            new Vector2(0.5f, 0.13f), new Vector2(w - 40f, 130f), new Color(1f, 0.88f, 0.55f));
        statsText.raycastTarget = false;
    }

    void Select(int index)
    {
        selected = index;
        Chefs.Selected = index;
        Highlight();
    }

    void Highlight()
    {
        for (int i = 0; i < cardBackgrounds.Count; i++)
        {
            bool on = i == selected;
            cardBackgrounds[i].color = on ? new Color(0.32f, 0.27f, 0.15f, 1f) : new Color(0.13f, 0.12f, 0.15f, 0.98f);
            cardBackgrounds[i].transform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        int n = Chefs.All.Length;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            Select((selected + n - 1) % n);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            Select((selected + 1) % n);

        for (int i = 0; i < n && i < 9; i++)
        {
            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Select(i);
        }

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            StartGame();
    }

    public void StartGame()
    {
        RunStats.Reset();
        SceneManager.LoadScene("SampleScene");
    }
}