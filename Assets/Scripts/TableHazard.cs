using UnityEngine;

// LA SALA: 4 taules; si se'n trenca una, torna a sortir als 15 segons
public class TableHazard : MonoBehaviour
{
    private Vector2[] spots;
    private GameObject[] tables;
    private float[] respawnAt;

    void Start()
    {
        Vector2 size = HazardManager.ArenaSize;
        Vector2 c = HazardManager.ArenaCenter;
        float dx = size.x * 0.27f;
        float dy = size.y * 0.22f;

        spots = new Vector2[]
        {
            c + new Vector2(-dx, -dy), c + new Vector2(dx, -dy),
            c + new Vector2(-dx, dy),  c + new Vector2(dx, dy)
        };

        tables = new GameObject[spots.Length];
        respawnAt = new float[spots.Length];

        for (int i = 0; i < spots.Length; i++) tables[i] = BuildTable(spots[i]);
    }

    void Update()
    {
        for (int i = 0; i < spots.Length; i++)
        {
            if (tables[i] != null)
            {
                respawnAt[i] = 0f;
                continue;
            }

            if (respawnAt[i] == 0f) respawnAt[i] = Time.time + 15f;
            else if (Time.time >= respawnAt[i])
            {
                tables[i] = BuildTable(spots[i]);
                respawnAt[i] = 0f;
            }
        }
    }

    GameObject BuildTable(Vector2 pos)
    {
        GameObject go = new GameObject("Taula");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2.4f, 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = new Color(0.45f, 0.28f, 0.15f, 1f);
        sr.sortingOrder = -1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        go.AddComponent<Table>();
        return go;
    }
}
