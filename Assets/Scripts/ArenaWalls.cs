using System.Collections.Generic;
using UnityEngine;

public class ArenaWalls : MonoBehaviour
{
    public static ArenaWalls Instance { get; private set; }

    [Header("Mida de la sala (la part de dins)")]
    public Vector2 size = new Vector2(30f, 20f);
    public float thickness = 2f;

    [Header("Aspecte")]
    public bool showWalls = false;
    public Color wallColor = new Color(0.25f, 0.15f, 0.1f, 1f);
    public int sortingOrder = -5;

    private static Sprite squareSprite;
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly List<Vector2> sizes = new List<Vector2>();

    void Awake()
    {
        Instance = this;

        float w = size.x + thickness * 2f; // les parets de dalt i baix cobreixen les cantonades

        BuildWall("Paret Dalt", new Vector2(0f, size.y / 2f + thickness / 2f), new Vector2(w, thickness));
        BuildWall("Paret Baix", new Vector2(0f, -size.y / 2f - thickness / 2f), new Vector2(w, thickness));
        BuildWall("Paret Esquerra", new Vector2(-size.x / 2f - thickness / 2f, 0f), new Vector2(thickness, size.y));
        BuildWall("Paret Dreta", new Vector2(size.x / 2f + thickness / 2f, 0f), new Vector2(thickness, size.y));
    }

    void BuildWall(string wallName, Vector2 localPos, Vector2 wallSize)
    {
        GameObject go = new GameObject(wallName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = wallSize;

        if (showWalls)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = wallSize;
            sr.color = wallColor;
            sr.sortingOrder = sortingOrder;

            renderers.Add(sr);
            sizes.Add(wallSize);
        }
    }

    // posa la textura d'un acte a totes les parets (només si showWalls està marcat)
    public void SetSkin(Sprite tile, Color tint)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].sprite = tile;
            renderers[i].drawMode = SpriteDrawMode.Tiled;
            renderers[i].size = sizes[i];
            renderers[i].color = tint;
        }
    }

    static Sprite GetSquareSprite()
    {
        if (squareSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        }
        return squareSprite;
    }

    // rectangle groc a la finestra Scene perquè vegis on queda el límit
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}