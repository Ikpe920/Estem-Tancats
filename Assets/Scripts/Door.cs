using UnityEngine;

// =====================================================================
//  PORTES PER ON SURTEN ELS CLIENTS
// =====================================================================

public class Door : MonoBehaviour
{
    public Vector2 spawnPoint; // on apareix el client
    public Vector2 along;      // direcció al llarg de la porta

    private SpriteRenderer frame;
    private SpriteRenderer panel;
    private SpriteRenderer glow;
    private float flash;

    public void Build(bool horizontal)
    {
        Vector2 frameSize = horizontal ? new Vector2(3.4f, 1.0f) : new Vector2(1.0f, 3.4f);
        Vector2 panelSize = horizontal ? new Vector2(3.0f, 0.7f) : new Vector2(0.7f, 3.0f);

        frame = Part("Marc", frameSize, -4);
        panel = Part("Porta", panelSize, -3);
        glow = Part("Llum", panelSize, -2);
        glow.color = new Color(1f, 0.8f, 0.3f, 0f);
    }

    SpriteRenderer Part(string partName, Vector2 size, int order)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.sortingOrder = order;
        return sr;
    }

    public void SetColors(Color frameColor, Color panelColor)
    {
        if (frame != null) frame.color = frameColor;
        if (panel != null) panel.color = panelColor;
    }

    public void Open()
    {
        flash = 1f;
    }

    void Update()
    {
        flash = Mathf.MoveTowards(flash, 0f, 2.2f * Time.deltaTime);
        if (glow != null) glow.color = new Color(1f, 0.8f, 0.3f, flash * 0.85f);
    }
}
