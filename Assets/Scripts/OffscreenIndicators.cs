using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fletxes a la vora de la pantalla que assenyalen els clients que són fora de la càmera.
// Els caps tenen una fletxa daurada i més gran. Es crea sol a l'escena del joc.
public class OffscreenIndicators : MonoBehaviour
{
    const int MaxArrows = 16;          // més fletxes que això només fan soroll
    const float EdgeMargin = 36f;      // píxels de separació de la vora (a 1080p)
    const float ArrowSize = 30f;       // mida de la fletxa normal (a 1080p)
    const float BossArrowSize = 46f;

    static readonly Color EnemyColor = new Color(1f, 0.35f, 0.3f, 0.85f);
    static readonly Color BossColor = new Color(1f, 0.8f, 0.25f, 0.95f);

    private readonly List<Image> arrows = new List<Image>();
    private RectTransform canvasRect;
    private Camera cam;

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
        if (scene.name == SceneNames.Game && FindAnyObjectByType<OffscreenIndicators>() == null)
            new GameObject("OffscreenIndicators").AddComponent<OffscreenIndicators>();
    }

    void Awake()
    {
        GameObject canvasGo = new GameObject("FletxesCanvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // per sota de la pausa i del tutorial
        canvasRect = canvasGo.GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;

        // amagades amb el joc aturat (triar plat, pausa...) i mentre el xef cau
        bool hide = cam == null || Time.timeScale == 0f || PlayerHealth.IsDying;

        int used = 0;
        if (!hide)
        {
            float scale = Screen.height / 1080f;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Vector2 center = screen * 0.5f;
            Vector2 half = center - Vector2.one * EdgeMargin * scale;

            // primer els caps, perquè no es quedin sense fletxa si n'hi ha moltes
            for (int pass = 0; pass < 2 && used < MaxArrows; pass++)
            {
                foreach (Enemy e in Enemy.All)
                {
                    if (used >= MaxArrows) break;
                    if (e == null || e.isBoss != (pass == 0)) continue;

                    Vector3 sp = cam.WorldToScreenPoint(e.transform.position);
                    if (sp.x >= 0f && sp.x <= screen.x && sp.y >= 0f && sp.y <= screen.y) continue; // es veu

                    Vector2 dir = (Vector2)sp - center;
                    if (dir.sqrMagnitude < 0.01f) continue;

                    // el punt on la línia des del centre talla el rectangle de la vora
                    float t = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)),
                                        half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
                    Vector2 pos = center + dir * t;

                    Image img = GetArrow(used++);
                    RectTransform rt = img.rectTransform;
                    rt.position = pos;
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    float size = (e.isBoss ? BossArrowSize : ArrowSize) * scale;
                    rt.sizeDelta = new Vector2(size, size);
                    img.color = e.isBoss ? BossColor : EnemyColor;
                }
            }
        }

        for (int i = used; i < arrows.Count; i++)
            if (arrows[i].enabled) arrows[i].enabled = false;
    }

    Image GetArrow(int index)
    {
        while (arrows.Count <= index)
        {
            GameObject go = new GameObject("Fletxa", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            Image img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.Arrow();
            img.raycastTarget = false;
            arrows.Add(img);
        }
        Image a = arrows[index];
        if (!a.enabled) a.enabled = true;
        return a;
    }
}
