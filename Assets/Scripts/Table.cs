using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Table : MonoBehaviour
{
    public int flippedDurability = 8;

    private bool flipped;
    private Transform player;
    private SpriteRenderer sr;
    private TextMeshPro prompt;

    public static readonly Vector2 UprightSize = new Vector2(2.4f, 1.2f);
    public static readonly Vector2 FlippedSize = new Vector2(1.2f, 2.6f); // bolcada fa de paret

    private Transform shadow;

    // posa el dibuix (i l'ombra a sota) com a fills de l'objecte; l'objecte no s'escala
    public static void AddVisual(Transform parent, Sprite sprite, Vector2 size)
    {
        GameObject sh = new GameObject("Ombra");
        sh.transform.SetParent(parent, false);
        SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
        ssr.sprite = PropArt.Shadow();
        ssr.color = new Color(1f, 1f, 1f, 0.35f);
        ssr.sortingOrder = -3;
        FitShadow(sh.transform, size);

        GameObject body = new GameObject("Dibuix");
        body.transform.SetParent(parent, false);
        SpriteRenderer sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -1;
    }

    // l'ombra és una mica més ampla que l'objecte i surt per baix (el sprite de l'ombra fa 1 x 0,5)
    static void FitShadow(Transform sh, Vector2 size)
    {
        sh.localPosition = new Vector3(0f, -size.y * 0.42f, 0f);
        sh.localScale = new Vector3(size.x * 1.15f, size.y * 0.9f, 1f);
    }

    void Start()
    {
        Transform body = transform.Find("Dibuix");
        sr = body != null ? body.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        shadow = transform.Find("Ombra");

        PlayerMovement pm = PlayerRef.Movement;
        if (pm != null) player = pm.transform;

        // l'avís "E · Bolcar" (no és fill de la taula perquè l'escala el deformaria)
        GameObject pg = new GameObject("AvisTaula");
        pg.transform.position = transform.position + Vector3.up * 1.3f;
        prompt = pg.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) prompt.font = TMP_Settings.defaultFontAsset;
        prompt.text = "E · Bolcar";
        prompt.fontSize = 3.5f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = Color.white;
        prompt.sortingOrder = 60;
        prompt.rectTransform.sizeDelta = new Vector2(6f, 1f);
        prompt.textWrappingMode = TextWrappingModes.NoWrap;
        pg.SetActive(false);
    }

    void Update()
    {
        if (prompt == null || flipped || player == null || Time.timeScale == 0f)
        {
            if (prompt != null) prompt.gameObject.SetActive(false);
            return;
        }

        bool near = Vector2.Distance(player.position, transform.position) < 2.6f;
        prompt.gameObject.SetActive(near);
        if (!near) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame) StartCoroutine(Flip());
    }

    IEnumerator Flip()
    {
        flipped = true;
        if (prompt != null) prompt.gameObject.SetActive(false);

        HazardFx.Shake(0.1f, 0.08f);

        // el dibuix s'aixafa, canvia per la taula bolcada i torna a créixer
        Transform body = sr != null ? sr.transform : null;
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            if (body != null) body.localScale = new Vector3(1f + 0.15f * (t / 0.1f), Mathf.Lerp(1f, 0.15f, t / 0.1f), 1f);
            yield return null;
        }

        if (sr != null) sr.sprite = PropArt.TableFlipped();
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.size = FlippedSize;
        if (shadow != null) FitShadow(shadow, FlippedSize);

        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.12f);
            if (body != null) body.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, k), Mathf.Lerp(0.6f, 1f, k) + Mathf.Sin(k * Mathf.PI) * 0.12f, 1f);
            yield return null;
        }
        if (body != null) body.localScale = Vector3.one;

        // ara atura els trets, però es trenca després d'uns quants
        BlocksShots b = gameObject.AddComponent<BlocksShots>();
        b.durability = flippedDurability;
    }

    public void Collapse()
    {
        SpriteRenderer dust = HazardFx.MakeCircle(transform.position, 1.4f, new Color(0.7f, 0.6f, 0.45f, 0.6f), 4);
        Destroy(dust.gameObject, 0.2f);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }
}
