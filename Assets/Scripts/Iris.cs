using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =====================================================================
//  TRANSICIÓ "IRIS": el cercle negre que es tanca, com al final dels dibuixos antics
// =====================================================================
// Iris.LoadScene("Escena") tanca el cercle, carrega l'escena i el torna a obrir.
// Iris.CloseOn(posicióAlMón, segons) tanca el cercle al voltant d'un punt (per exemple, el xef quan mor);
// si després es crida LoadScene, ja no el torna a tancar.
// Mentre es tanca el joc queda aturat (Time.timeScale = 0) i no es pot clicar res.
public class Iris : MonoBehaviour
{
    const float CloseTime = 0.55f;
    const float OpenTime = 0.6f;
    const float HoldTime = 0.12f; // una mica de negre abans d'obrir

    static Iris instance;

    public static bool Busy { get; private set; }

    private Canvas canvas;
    private RectTransform hole;
    private float radius;      // en píxels de pantalla
    private Vector2 focus;     // el centre del cercle, en píxels de pantalla
    private Coroutine closing;
    private bool closed;
    private bool loading;  // ja hi ha un canvi d'escena en marxa

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        instance = null;
        Busy = false;
    }

    // en engegar el joc, el primer menú s'obre amb el cercle
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void FirstScene()
    {
        try { Get().OpenFromBlack(); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    static Iris Get()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("Iris");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Iris>();
            instance.Build();
        }
        return instance;
    }

    // ---------- API ----------

    public static void LoadScene(string scene)
    {
        Iris iris;
        try { iris = Get(); }
        catch (System.Exception e)
        {
            // si la transició falla, que almenys canviï d'escena
            Debug.LogException(e);
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            return;
        }
        if (iris.loading) return; // ja està canviant d'escena (doble clic)
        iris.loading = true;
        iris.StartCoroutine(iris.LoadRoutine(scene));
    }

    public static void CloseOn(Vector3 world, float seconds)
    {
        Iris iris = Get();
        Camera cam = Camera.main;
        Vector2 at = cam != null ? (Vector2)cam.WorldToScreenPoint(world) : new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (iris.closing != null) iris.StopCoroutine(iris.closing);
        Busy = true;
        iris.closing = iris.StartCoroutine(iris.Close(at, seconds));
    }

    // ---------- FUNCIONAMENT ----------

    IEnumerator LoadRoutine(string scene)
    {
        Busy = true;
        // si ja s'està tancant (per exemple, al voltant del xef que mor), l'esperem
        while (closing != null) yield return null;
        if (!closed)
            yield return Close(new Vector2(Screen.width / 2f, Screen.height / 2f), CloseTime);
        Time.timeScale = 0f; // a les fosques no ha de passar res
        yield return new WaitForSecondsRealtime(HoldTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
        yield return null; // que l'escena nova es carregui i es col·loqui
        yield return Open(OpenTime);
        loading = false;
        Busy = false;
    }

    IEnumerator Close(Vector2 at, float seconds)
    {
        canvas.gameObject.SetActive(true);
        focus = at;
        float start = MaxRadius(at);
        Time.timeScale = 0f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            radius = Mathf.Lerp(start, 0f, k * k * (3f - 2f * k));
            Layout();
            yield return null;
        }
        radius = 0f;
        Layout();
        closed = true;
        closing = null;
    }

    IEnumerator Open(float seconds)
    {
        canvas.gameObject.SetActive(true);
        focus = new Vector2(Screen.width / 2f, Screen.height / 2f);
        float end = MaxRadius(focus);
        closed = false;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            radius = Mathf.Lerp(0f, end, k * k); // s'obre a poc a poc i després de cop
            Layout();
            yield return null;
        }
        canvas.gameObject.SetActive(false);
    }

    void OpenFromBlack()
    {
        radius = 0f;
        closed = true;
        canvas.gameObject.SetActive(true);
        Layout();
        Busy = true;
        StartCoroutine(OpenFromBlackRoutine());
    }

    IEnumerator OpenFromBlackRoutine()
    {
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Open(OpenTime + 0.2f);
        Busy = false;
    }

    static float MaxRadius(Vector2 at)
    {
        float dx = Mathf.Max(at.x, Screen.width - at.x);
        float dy = Mathf.Max(at.y, Screen.height - at.y);
        return Mathf.Sqrt(dx * dx + dy * dy) + 20f;
    }

    void Layout()
    {
        if (hole == null) return;
        hole.anchoredPosition = focus;
        hole.sizeDelta = Vector2.one * (radius * 2f);
    }

    // ---------- LA PEÇA ----------

    void Build()
    {
        GameObject cgo = new GameObject("IrisCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000; // per sobre de tot (la pausa és 500, el fos de la mort 1000)

        // el forat: una imatge negra amb un cercle transparent al mig
        GameObject hgo = new GameObject("Forat", typeof(RectTransform), typeof(Image));
        hgo.transform.SetParent(cgo.transform, false);
        hole = hgo.GetComponent<RectTransform>();
        hole.anchorMin = hole.anchorMax = Vector2.zero; // posició en píxels des de baix a l'esquerra
        hole.pivot = new Vector2(0.5f, 0.5f);
        Image img = hgo.GetComponent<Image>();
        img.sprite = HoleSprite();
        img.color = Color.black;
        img.raycastTarget = false;

        // i quatre franges negres enganxades al forat que tapen la resta de la pantalla
        // (aquestes sí que aturen els clics mentre dura la transició)
        Bar("Dalt", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(40000f, 20000f));
        Bar("Baix", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(40000f, 20000f));
        Bar("Esquerra", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20000f, 40000f));
        Bar("Dreta", new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20000f, 40000f));

        canvas.gameObject.SetActive(false);
    }

    void Bar(string name, Vector2 anchor, Vector2 pivot, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(hole, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = Color.black;
    }

    // un cercle transparent amb les cantonades negres; pixelat, com tot el joc
    static Sprite HoleSprite()
    {
        const int n = 160;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                bool inside = dx * dx + dy * dy < r * r;
                px[y * n + x] = inside ? new Color32(0, 0, 0, 0) : new Color32(255, 255, 255, 255);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
