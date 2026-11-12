using UnityEngine;

public static class HazardFx
{

    public static Sprite Circle()
    {
        return SpriteFactory.Circle();
    }

    public static Sprite Square()
    {
        return SpriteFactory.Square();
    }

    // cercle de radi 'radius' (en unitats del món)
    public static SpriteRenderer MakeCircle(Vector2 pos, float radius, Color color, int order = -3)
    {
        GameObject go = new GameObject("Cercle");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // un punt a l'atzar dins de la sala
    public static Vector2 RandomPoint(float margin)
    {
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * margin;
        return HazardManager.ArenaCenter +
               new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
    }

    public static void Shake(float duration, float magnitude)
    {
        CameraFX.Shake(duration, magnitude);
    }

        // text que puja i s'esvaeix (per als drops)
    public static void Popup(Vector2 pos, string text, Color color, float life = 0.9f)
    {
        GameObject go = new GameObject("Avis");
        go.transform.position = pos;

        TMPro.TextMeshPro tmp = go.AddComponent<TMPro.TextMeshPro>();
        if (TMPro.TMP_Settings.defaultFontAsset != null) tmp.font = TMPro.TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = 4f * Accessibility.TextMul;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 60;
        tmp.rectTransform.sizeDelta = new Vector2(8f, 1f);
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        PopupText popup = go.AddComponent<PopupText>();
        popup.life = life;
    }
}