using System.Collections.Generic;
using UnityEngine;

// Cercle vermell a terra on està a punt de sortir un client: un disc fosc de la mida final
// i un altre de més viu que creix a dins fins a omplir-lo. Quan s'omple, surt el client.
public class SpawnWarning : MonoBehaviour
{
    const float Size = 1.5f; // diàmetre en unitats del món
    static readonly Color OuterColor = new Color(0.9f, 0.15f, 0.1f, 0.22f);
    static readonly Color InnerColor = new Color(1f, 0.3f, 0.2f, 0.55f);

    static readonly Stack<SpawnWarning> pool = new Stack<SpawnWarning>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPool() { pool.Clear(); }

    private SpriteRenderer outer;
    private SpriteRenderer inner;
    private float born;
    private float duration;

    public static void Show(Vector2 pos, float seconds)
    {
        SpawnWarning w = null;
        while (pool.Count > 0 && w == null) w = pool.Pop(); // els de l'escena anterior ja no existeixen
        if (w == null) w = Create();

        w.transform.position = pos;
        w.born = Time.time;
        w.duration = Mathf.Max(0.05f, seconds);
        w.inner.transform.localScale = Vector3.zero;
        w.gameObject.SetActive(true);
    }

    static SpawnWarning Create()
    {
        GameObject go = new GameObject("AvisClient");
        SpawnWarning w = go.AddComponent<SpawnWarning>();
        w.outer = Disc(go.transform, "Fora", OuterColor, 1f, -2); // per sobre del terra, per sota dels personatges
        w.inner = Disc(go.transform, "Dins", InnerColor, 0f, -1);
        return w;
    }

    static SpriteRenderer Disc(Transform parent, string name, Color color, float scale, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * Size * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void Update()
    {
        float k = (Time.time - born) / duration;
        if (k >= 1f)
        {
            gameObject.SetActive(false);
            pool.Push(this);
            return;
        }

        // el disc de dins creix, i tot plegat batega cada cop més de pressa
        inner.transform.localScale = Vector3.one * Size * Mathf.SmoothStep(0f, 1f, k);
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * Mathf.Lerp(10f, 30f, k));
        Color o = OuterColor; o.a *= pulse; outer.color = o;
        Color i = InnerColor; i.a *= pulse; inner.color = i;
    }
}
