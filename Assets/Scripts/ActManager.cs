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
    public AudioSource musicSource;         // opcional: per posar la música de cada acte

    public GameObject actPanel;             // opcional: panell on es mostra el títol de l'acte
    public TextMeshProUGUI actTitleText;
    public TextMeshProUGUI actSubtitleText;

    public GameObject legacyBackground;     // opcional: el panell vell del fons (s'amaga sol)
    public bool autoHideLegacyBackground = true;

    public float fadeDuration = 0.6f;       // durada de la transició
    public float titleDuration = 2f;        // temps que dura el títol de l'acte

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

    void Reset()
    {
        acts = new ActData[]
        {
            MakeAct("LA SALA", "Les taules ja no serveixen per a ningú", "Sala", new Color(1f, 0.97f, 0.93f), 0, 0f, 1f),
            MakeAct("LA BARRA", "Els copes s'han buidat... i ells no", "Barra", new Color(1f, 0.93f, 0.82f), 2, 0.15f, 1.2f),
            MakeAct("LA CUINA", "A la cuina hi ha foc, i ells han vingut a apagar-lo", "Cuina", new Color(0.88f, 0.95f, 1f), 4, 0.3f, 1.4f),
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
            if (cam != null)
            {
                background.transform.SetParent(cam.transform, false);
                background.transform.localPosition = new Vector3(0f, 0f, 10f);
                background.transform.localScale = Vector3.one;
            }
        }
        background.sortingOrder = sortingOrder;
        background.drawMode = SpriteDrawMode.Tiled;
        UpdateBackgroundSize();
    }

    void UpdateBackgroundSize()
    {
        if (background == null || cam == null) return;
        float h = cam.orthographic ? cam.orthographicSize * 2f : 20f;
        float w = h * cam.aspect;
        background.size = new Vector2(w + 0.5f, h + 0.5f);
    }

    void HideLegacyBackground()
    {
        if (!autoHideLegacyBackground) return;
        if (legacyBackground == null) legacyBackground = GameObject.Find("Background");
        if (legacyBackground != null) legacyBackground.SetActive(false);
    }

    void LateUpdate()
    {
        if (background == null || cam == null) return;
        if (background.transform.parent != cam.transform)
        {
            Vector3 p = cam.transform.position;
            background.transform.position = new Vector3(p.x, p.y, background.transform.position.z);
        }
        UpdateBackgroundSize();
    }

    // ---------- ELS ACTES ----------

    public void ApplyAct(int index)
    {
        if (acts == null || acts.Length == 0) return;
        currentAct = ((index % acts.Length) + acts.Length) % acts.Length;

        ActData act = Current;
        if (act == null) return;

        if (background != null)
        {
            Sprite sprite = GetActSprite(act);
            if (sprite != null) background.sprite = sprite;
            background.color = act.tint;
        }
        if (cam != null) cam.backgroundColor = act.tint * 0.4f;
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

        yield return StartCoroutine(FadeTo(0f, Color.black));

        ApplyAct(currentAct + 1);

        ActData act = Current;
        Color tint = act != null ? act.tint : Color.white;
        if (background != null) background.color = new Color(tint.r, tint.g, tint.b, 0f);
        if (cam != null) cam.backgroundColor = Color.black;

        yield return StartCoroutine(FadeTo(1f, tint * 0.4f));

        yield return StartCoroutine(ShowTitle());
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
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            background.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, targetAlpha, k));
            if (cam != null) cam.backgroundColor = Color.Lerp(startCam, targetCamColor, k);
            yield return null;
        }
    }

    IEnumerator ShowTitle()
    {
        if (actPanel == null || actTitleText == null) yield break;

        ActData act = Current;
        if (act == null) yield break;

        actTitleText.text = "ACTE " + (currentAct + 1) + " - " + act.name;
        if (actSubtitleText != null) actSubtitleText.text = act.subtitle;

        CanvasGroup cg = actPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = actPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        actPanel.SetActive(true);

        yield return FadeCanvas(cg, 0f, 1f, 0.35f);
        yield return new WaitForSeconds(titleDuration);
        yield return FadeCanvas(cg, 1f, 0f, 0.35f);

        actPanel.SetActive(false);
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}
