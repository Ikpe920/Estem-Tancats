using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =====================================================================
//  FILTRE DE PEL·LÍCULA ANTIGA (opcional, al menú de pausa)
// =====================================================================
// Dues capes:
//  - el món del joc es veu en sepia i menys saturat (postprocessat d'URP, només la càmera)
//  - per sobre de tot (també els menús): gra, ratlles verticals, pols, parpelleig, una vinyeta i un vel càlid
// Es crea sol, no cal posar-lo a cap escena. FilmFilter.Enabled l'encén o l'apaga (i es guarda).
public class FilmFilter : MonoBehaviour
{
    const string Key = "film_filter";

    static FilmFilter instance;

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt(Key, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(Key, value ? 1 : 0);
            PlayerPrefs.Save();
            if (instance != null) instance.Apply();
        }
    }

    private GameObject overlay;
    private RawImage grain;
    private Image flicker;
    private readonly List<Image> scratches = new List<Image>();
    private readonly List<float> scratchUntil = new List<float>();
    private readonly List<float> scratchDrift = new List<float>();
    private readonly List<Image> dust = new List<Image>();
    private readonly List<float> dustUntil = new List<float>();
    private float nextGrain, nextFlicker;

    private Volume volume;
    private VolumeProfile profile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // si alguna cosa falla aquí, que no aturi els altres scripts que esperen l'escena
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            if (instance == null)
            {
                GameObject go = new GameObject("FiltrePelicula");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<FilmFilter>();
                instance.Build();
            }
            instance.volume = null; // el de l'escena anterior ja no hi és
            instance.Apply();
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    // ---------- ENCENDRE I APAGAR ----------

    void Apply()
    {
        bool on = Enabled;
        if (overlay != null) overlay.SetActive(on);

        // la part de la càmera: sepia
        try
        {
            if (on && volume == null) volume = CreateVolume();
            if (volume != null) volume.weight = on ? 1f : 0f;
            Camera cam = Camera.main;
            if (cam != null)
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = on;
            }
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    Volume CreateVolume()
    {
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            ColorAdjustments ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(-65f);
            ca.contrast.Override(14f);
            ca.colorFilter.Override(new Color(1f, 0.9f, 0.74f));
            ca.postExposure.Override(0.1f);
        }
        GameObject go = new GameObject("VolumPelicula");
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 50f;
        v.sharedProfile = profile;
        return v;
    }

    // ---------- LA CAPA DE DAMUNT ----------

    void Build()
    {
        overlay = new GameObject("CapaPelicula", typeof(Canvas));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500; // per sobre de la pausa (500), per sota del cercle de les transicions (2000)
        Transform root = overlay.transform;

        // vel càlid i vinyeta
        Full(root, "Vel").color = new Color(0.55f, 0.40f, 0.20f, 0.09f);
        RawImage vig = FullRaw(root, "Vinyeta");
        vig.texture = VignetteTexture();

        // gra
        grain = FullRaw(root, "Gra");
        grain.texture = NoiseTexture();
        grain.color = new Color(1f, 1f, 1f, 0.55f);

        // ratlles verticals
        for (int i = 0; i < 3; i++)
        {
            Image s = Piece(root, "Ratlla");
            s.rectTransform.anchorMin = new Vector2(0f, 0f);
            s.rectTransform.anchorMax = new Vector2(0f, 1f);
            s.rectTransform.sizeDelta = new Vector2(2f, 0f);
            s.enabled = false;
            scratches.Add(s); scratchUntil.Add(0f); scratchDrift.Add(0f);
        }
        // pols
        for (int i = 0; i < 6; i++)
        {
            Image d = Piece(root, "Pols");
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = Vector2.zero;
            d.enabled = false;
            dust.Add(d); dustUntil.Add(0f);
        }
        // parpelleig (el llum del projector)
        flicker = Full(root, "Parpelleig");
        flicker.color = new Color(0f, 0f, 0f, 0f);
    }

    void Update()
    {
        if (overlay == null || !overlay.activeSelf) return;
        float t = Time.unscaledTime;
        float w = Screen.width, h = Screen.height;

        // el gra salta 18 vegades per segon
        if (t > nextGrain)
        {
            nextGrain = t + 1f / 18f;
            grain.uvRect = new Rect(Random.value, Random.value, w / 192f, h / 192f);
        }
        if (t > nextFlicker)
        {
            nextFlicker = t + 1f / 12f;
            flicker.color = new Color(0f, 0f, 0f, Random.Range(0f, 0.055f));
        }

        for (int i = 0; i < scratches.Count; i++)
        {
            Image s = scratches[i];
            if (t > scratchUntil[i])
            {
                // de tant en tant n'apareix una de nova
                bool show = Random.value < 0.02f;
                s.enabled = show;
                if (show)
                {
                    scratchUntil[i] = t + Random.Range(0.15f, 0.9f);
                    scratchDrift[i] = Random.Range(-30f, 30f);
                    s.rectTransform.anchoredPosition = new Vector2(Random.Range(0.05f, 0.95f) * w, 0f);
                    s.rectTransform.sizeDelta = new Vector2(Random.value < 0.7f ? 2f : 3f, 0f);
                    s.color = Random.value < 0.6f ? new Color(0.12f, 0.09f, 0.06f, 0.35f) : new Color(1f, 0.97f, 0.88f, 0.28f);
                }
                else scratchUntil[i] = t + 0.05f;
            }
            else if (s.enabled)
            {
                Vector2 p = s.rectTransform.anchoredPosition;
                p.x += scratchDrift[i] * Time.unscaledDeltaTime + Random.Range(-1f, 1f);
                s.rectTransform.anchoredPosition = p;
            }
        }

        for (int i = 0; i < dust.Count; i++)
        {
            Image d = dust[i];
            if (t > dustUntil[i])
            {
                bool show = Random.value < 0.015f;
                d.enabled = show;
                dustUntil[i] = t + (show ? Random.Range(0.04f, 0.12f) : 0.05f);
                if (show)
                {
                    float size = Random.Range(3f, 9f);
                    d.rectTransform.sizeDelta = Random.value < 0.3f ? new Vector2(size * 3f, 2f) : new Vector2(size, size);
                    d.rectTransform.anchoredPosition = new Vector2(Random.value * w, Random.value * h);
                    d.color = Random.value < 0.75f ? new Color(0.1f, 0.07f, 0.05f, 0.55f) : new Color(1f, 0.97f, 0.9f, 0.45f);
                }
            }
        }
    }

    // ---------- PECES ----------

    static Image Piece(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image i = go.GetComponent<Image>();
        i.raycastTarget = false;
        return i;
    }

    static Image Full(Transform parent, string name)
    {
        Image i = Piece(parent, name);
        Stretch(i.rectTransform);
        return i;
    }

    static RawImage FullRaw(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage r = go.GetComponent<RawImage>();
        r.raycastTarget = false;
        Stretch(r.rectTransform);
        return r;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // puntets clars i foscos escampats, gairebé tot transparent
    static Texture2D NoiseTexture()
    {
        const int n = 96;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        Color32[] px = new Color32[n * n];
        System.Random rnd = new System.Random(7);
        for (int i = 0; i < px.Length; i++)
        {
            double r = rnd.NextDouble();
            if (r < 0.10) px[i] = new Color32(20, 14, 10, (byte)rnd.Next(40, 110));
            else if (r < 0.16) px[i] = new Color32(255, 248, 230, (byte)rnd.Next(30, 80));
            else px[i] = new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static Texture2D VignetteTexture()
    {
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.707f;
                float a = Mathf.SmoothStep(0f, 0.55f, (d - 0.55f) / 0.45f);
                px[y * n + x] = new Color32(30, 18, 10, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
