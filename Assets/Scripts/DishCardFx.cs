using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// animació de les cartes dels plats: entren des de baix, el plat floteja una mica
// i la carta s'il·lumina i puja en passar-hi el ratolí.
// Fa servir el temps "sense escala" perquè el joc està en pausa (timeScale = 0) mentre es tria.
public class DishCardFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image border;              // el marc fosc de la carta
    public RectTransform icon;        // el plat (flota)
    public Color borderNormal = new Color(0.16f, 0.11f, 0.13f, 1f);
    public Color borderHover = new Color(1f, 0.8f, 0.3f, 1f);
    public Vector2 basePos;           // posició final de la carta
    public float delay = 0f;          // retard d'entrada (perquè no surtin totes alhora)
    public float lift = 12f;          // quant puja en passar-hi el ratolí
    public float bob = 3f;            // amplitud del flotar del plat
    [HideInInspector] public bool focused; // marcada amb el comandament: es veu com si hi passessis el ratolí

    private RectTransform rt;
    private CanvasGroup group;
    private Vector2 iconBase;
    private float t;
    private float hover;
    private bool hovering;
    private Texture2D ownedTexture;

    // la textura del gingham es crea per carta; així la podem destruir amb ella
    public void Own(Texture2D tex) { ownedTexture = tex; }

    // s'ha de cridar just després de crear-la, perquè el primer fotograma no es vegi sencera
    public void Init()
    {
        rt = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (icon != null) iconBase = icon.anchoredPosition;
        Apply(0f);
    }

    void Update()
    {
        if (rt == null) Init();

        t += Time.unscaledDeltaTime;
        hover = Mathf.MoveTowards(hover, hovering || focused ? 1f : 0f, Time.unscaledDeltaTime * 9f);
        Apply(Mathf.Clamp01((t - delay) / 0.32f));
    }

    void Apply(float p)
    {
        float e = EaseOutBack(p);

        rt.localScale = Vector3.one * (Mathf.Lerp(0.86f, 1f, e) * (1f + 0.05f * hover));
        rt.anchoredPosition = basePos + new Vector2(0f, (1f - e) * -lift * 4f + hover * lift);
        group.alpha = Mathf.Clamp01(p * 1.6f);

        if (border != null) border.color = Color.Lerp(borderNormal, borderHover, hover);

        if (icon != null)
        {
            float amp = bob * (0.6f + hover * 1.2f);
            float wave = Mathf.Sin((t + delay) * 2.6f) * amp;
            icon.anchoredPosition = iconBase + new Vector2(0f, wave);
        }
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float k = x - 1f;
        return 1f + c3 * k * k * k + c1 * k * k;
    }

    public void OnPointerEnter(PointerEventData e) { hovering = true; }
    public void OnPointerExit(PointerEventData e) { hovering = false; }

    void OnDestroy()
    {
        if (ownedTexture != null) Destroy(ownedTexture);
    }
}
