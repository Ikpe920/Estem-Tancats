using UnityEngine;

// =====================================================================
//  LA TEULADA: foscor amb un cercle de llum al voltant del xef
// =====================================================================

public class DarknessHazard : MonoBehaviour
{
    private static DarknessHazard instance;

    public float lightRadius = 6f;  // com de gran és el cercle de llum
    public float darkness = 0.82f;  // 0 = res de foscor, 1 = negre total

    private SpriteRenderer overlay;
    private Transform player;
    private float level = 1f;
    private float flashUntil;

    // el llamp il·lumina l'escena un instant
    public static void Flash()
    {
        if (instance != null) instance.flashUntil = Time.time + 0.2f;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        PlayerMovement pm = PlayerRef.Movement;
        if (pm != null) player = pm.transform;

        int size = 512;
        float worldSize = 70f; // unitats que cobreix la capa
        float unitsPerPixel = worldSize / size;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) * unitsPerPixel;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lightRadius * 0.45f, lightRadius, dist));
                px[y * size + x] = new Color32(4, 6, 18, (byte)(a * darkness * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        GameObject go = new GameObject("Foscor");
        go.transform.SetParent(transform, false);
        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / worldSize);
        overlay.sortingOrder = 90; // per sobre de tot el món, per sota de la interfície
    }

    void LateUpdate()
    {
        if (overlay == null) return;

        if (player != null)
            overlay.transform.position = new Vector3(player.position.x, player.position.y, 0f);

        float target = Time.time < flashUntil ? 0.15f : 1f;
        level = Mathf.MoveTowards(level, target, 8f * Time.unscaledDeltaTime);
        overlay.color = new Color(1f, 1f, 1f, level);
    }
}
