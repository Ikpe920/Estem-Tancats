using TMPro;
using UnityEngine;

// Número de dany que surt del client en rebre un cop.
// Blanc i petit als cops normals; groc, gran i amb "!" als crítics.
public class DamageNumber : MonoBehaviour
{
    // posa-ho a false si només vols veure els números dels crítics
    public static bool showNormalHits = true;

    static readonly Color NormalColor = new Color(1f, 0.97f, 0.9f, 1f);
    static readonly Color CritColor = new Color(1f, 0.85f, 0.15f, 1f);

    private TextMeshPro tmp;
    private Vector3 start;
    private float born;
    private float life;
    private float rise;
    private bool crit;
    private Color color;

    public static void Spawn(Vector2 pos, int amount, bool isCrit)
    {
        if (amount <= 0) return;
        if (!isCrit && !showNormalHits) return;

        GameObject go = new GameObject("Dany");
        // una mica d'atzar perquè els números d'una explosió no es tapin entre ells
        Vector2 p = pos + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0f, 0.25f));
        go.transform.position = p;

        TextMeshPro t = go.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = isCrit ? amount + "!" : amount.ToString();
        t.fontSize = isCrit ? 6f : 3.2f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = isCrit ? CritColor : NormalColor;
        t.outlineWidth = isCrit ? 0.3f : 0.25f;
        t.outlineColor = new Color32(45, 26, 30, 255);
        t.sortingOrder = 61;
        t.rectTransform.sizeDelta = new Vector2(4f, 1f);
        t.textWrappingMode = TextWrappingModes.NoWrap;

        DamageNumber dn = go.AddComponent<DamageNumber>();
        dn.tmp = t;
        dn.start = p;
        dn.born = Time.time;
        dn.crit = isCrit;
        dn.life = isCrit ? 0.85f : 0.55f;
        dn.rise = isCrit ? 1.8f : 1.2f;
        dn.color = isCrit ? CritColor : NormalColor;
    }

    void Update()
    {
        float k = (Time.time - born) / life;
        if (k >= 1f) { Destroy(gameObject); return; }

        // puja ràpid i frena
        float ease = 1f - (1f - k) * (1f - k);
        transform.position = start + Vector3.up * rise * ease;

        // "pop": entra gran i s'encongeix (més als crítics)
        float pop = crit
            ? Mathf.Lerp(1.7f, 1f, Mathf.Clamp01(k / 0.18f))
            : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(k / 0.12f));
        transform.localScale = Vector3.one * pop;

        // s'esvaeix al final
        float alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        Color c = color;
        c.a = alpha;
        tmp.color = c;
    }
}
