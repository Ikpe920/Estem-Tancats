using UnityEngine;

// Petits sprites generats per codi per als avisos dels caps (cercles i línies)
public static class BossFx
{

    // cercle blanc d'1 unitat de diàmetre
    public static Sprite Circle()
    {
        return SpriteFactory.Circle();
    }

    // quadrat blanc d'1x1 unitats (per fer línies d'avís)
    public static Sprite Square()
    {
        return SpriteFactory.Square();
    }

    // cercle d'avís al terra (el diàmetre és 2 * radius)
    public static GameObject Telegraph(Vector2 pos, float radius, Color color, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = color;
        sr.sortingOrder = -2;
        return go;
    }

    // línia d'avís que surt d'un punt i va en una direcció
    public static SpriteRenderer Line(Vector2 from, Vector2 dir, float length, float width, Color color, string name)
    {
        GameObject go = new GameObject(name);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.position = from + dir * (length * 0.5f);
        go.transform.localScale = new Vector3(length, width, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square();
        sr.color = color;
        sr.sortingOrder = -2;
        return sr;
    }

    // fum curt (teletransports, canvis de fase)
    public static void Puff(Vector2 pos, float radius, Color color)
    {
        GameObject go = Telegraph(pos, radius, color, "BossFum");
        Object.Destroy(go, 0.25f);
    }
}
