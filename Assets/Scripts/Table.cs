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

    private static readonly Vector3 FlippedScale = new Vector3(1.2f, 2.6f, 1f);
    private static readonly Color UprightColor = new Color(0.45f, 0.28f, 0.15f);
    private static readonly Color FlippedColor = new Color(0.30f, 0.18f, 0.10f);

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

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

        Vector3 from = transform.localScale;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.2f);
            transform.localScale = Vector3.Lerp(from, FlippedScale, k);
            if (sr != null) sr.color = Color.Lerp(UprightColor, FlippedColor, k);
            yield return null;
        }

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
