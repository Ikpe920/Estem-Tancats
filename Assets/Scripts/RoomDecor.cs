using UnityEngine;

// =====================================================================
//  DECORACIÓ ANIMADA DE CADA SALA (Tools/SpriteGen/decor.py -> Resources/Decor)
// =====================================================================
// Dues peces a les cantonades de dalt de la sala, que es mouen i fan partícules:
//   Sala      la gramola: el disc gira i en surten notes musicals
//   Barra     el rètol de neó BAR, que parpelleja
//   Cuina     l'olla al foc: la tapa balla i treu vapor
//   Terrassa  els testos: el vent s'emporta alguna fulla
//   Teulada   la xemeneia: fum
//   Celler    el barril: degota vi
//   Cambra    el bloc de gel: neu i boira
//   Plató     el focus: un con de llum que escombra la sala
// No té col·lisions: és només decorat. L'ActManager la canvia a cada sala.
public class RoomDecor : MonoBehaviour
{
    static GameObject root;

    string kind;
    SpriteRenderer sr, extra;
    float next, phase;
    Vector3 baseScale;

    public static void Show(string room)
    {
        if (root != null) Destroy(root);
        string sprite = SpriteFor(room);
        if (sprite == null) return;
        Sprite sp = Resources.Load<Sprite>("Decor/" + sprite);
        if (sp == null) return;

        root = new GameObject("DecoracioSala");
        Vector2 c = HazardManager.ArenaCenter;
        Vector2 half = HazardManager.ArenaSize * 0.5f;
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            GameObject go = new GameObject(sprite);
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector2(c.x + side * (half.x - 1.3f), c.y + half.y - 1.3f);
            go.transform.localScale = Vector3.one * 1.7f;
            RoomDecor d = go.AddComponent<RoomDecor>();
            d.kind = sprite;
            d.phase = i * 1.7f;
            d.sr = go.AddComponent<SpriteRenderer>();
            d.sr.sprite = sp;
            d.sr.flipX = side > 0 && sprite != "CartellBar";
            d.sr.sortingOrder = -1;
            d.baseScale = go.transform.localScale;
            if (sprite == "Focus")
            {
                Sprite light = Resources.Load<Sprite>("Decor/Llum");
                if (light != null)
                {
                    GameObject l = new GameObject("Llum");
                    l.transform.SetParent(go.transform, false);
                    l.transform.localPosition = new Vector3(0f, -0.2f, 0f);
                    d.extra = l.AddComponent<SpriteRenderer>();
                    d.extra.sprite = light;
                    d.extra.sortingOrder = -2;
                    d.extra.color = new Color(1f, 0.95f, 0.7f, 0.14f);
                }
            }
        }
    }

    static string SpriteFor(string room)
    {
        switch (room)
        {
            case "Sala": return "Gramola";
            case "Barra": return "CartellBar";
            case "Cuina": return "Olla";
            case "Terrassa": return "Testos";
            case "Teulada": return "Xemeneia";
            case "Celler": return "Barril";
            case "Cambra": return "BlocGel";
            case "Plato": return "Focus";
        }
        return null;
    }

    static Sprite note, leaf;

    void Update()
    {
        float t = Time.time + phase;
        Vector2 p = transform.position;
        switch (kind)
        {
            case "Gramola":
                transform.localScale = baseScale * (1f + 0.04f * Mathf.Abs(Mathf.Sin(t * 4f))); // balla amb la música
                if (Time.time >= next)
                {
                    next = Time.time + Random.Range(0.6f, 1.1f);
                    if (note == null) note = Resources.Load<Sprite>("Decor/Nota");
                    Fx.EmitSprite(note, p + new Vector2(sr.flipX ? 0.4f : -0.4f, 0.9f), new Vector2(Random.Range(-0.3f, 0.3f), 0.8f), 1.6f, 0.35f, 0.25f,
                                  new Color(0.15f, 0.1f, 0.1f, 1f), 3, Random.Range(-60f, 60f));
                }
                break;
            case "CartellBar":
                // el neó: quasi sempre encès, de tant en tant fa pampallugues
                bool on = Mathf.PerlinNoise(t * 3f, phase) > 0.25f;
                sr.color = on ? Color.white : new Color(0.55f, 0.5f, 0.55f);
                break;
            case "Olla":
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * (Mathf.Repeat(t, 2.5f) < 0.6f ? 3f : 0f)); // la tapa balla
                if (Time.time >= next)
                {
                    next = Time.time + 0.25f;
                    Fx.Steam(p + Vector2.up * 0.9f);
                }
                break;
            case "Testos":
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 3f);
                if (Time.time >= next)
                {
                    next = Time.time + Random.Range(1.5f, 3f);
                    if (leaf == null) leaf = Resources.Load<Sprite>("Decor/Fulla");
                    Fx.EmitSprite(leaf, p + Vector2.up * 0.5f, new Vector2(Random.Range(0.8f, 1.6f) * (sr.flipX ? -1f : 1f), -0.4f), 3f, 0.4f, 0.35f,
                                  Color.white, 3, Random.Range(-200f, 200f));
                }
                break;
            case "Xemeneia":
                if (Time.time >= next)
                {
                    next = Time.time + 0.3f;
                    Fx.Steam(p + Vector2.up * 1.1f, new Color(0.35f, 0.33f, 0.33f, 0.7f));
                }
                break;
            case "Barril":
                if (Time.time >= next)
                {
                    next = Time.time + Random.Range(0.9f, 1.6f);
                    Fx.Ripple(p + Vector2.down * 1.1f, 0.6f, new Color(0.6f, 0.1f, 0.25f));
                }
                break;
            case "BlocGel":
                if (Time.time >= next)
                {
                    next = Time.time + 0.35f;
                    Fx.Snowflake(p + Random.insideUnitCircle * 1.2f + Vector2.up * 0.6f);
                    if (Random.value < 0.4f) Fx.Steam(p + Vector2.down * 0.4f, new Color(0.85f, 0.95f, 1f, 0.5f));
                }
                break;
            case "Focus":
                if (extra != null)
                {
                    // el con de llum escombra la sala d'un costat a l'altre
                    float a = Mathf.Sin(t * 0.6f) * 35f + (sr.flipX ? 20f : -20f);
                    extra.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                    extra.transform.localScale = new Vector3(1.8f, 4f, 1f);
                    extra.transform.localPosition = extra.transform.localRotation * new Vector3(0f, -2.1f, 0f);
                }
                break;
        }
    }
}
