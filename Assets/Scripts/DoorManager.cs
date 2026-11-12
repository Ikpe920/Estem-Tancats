using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance { get; private set; }

    private readonly List<Door> doors = new List<Door>();

    private static readonly Color[] FrameColors =
    {
        new Color(0.62f, 0.45f, 0.28f),
        new Color(0.50f, 0.35f, 0.20f),
        new Color(0.75f, 0.80f, 0.82f),
        new Color(0.55f, 0.42f, 0.25f),
        new Color(0.40f, 0.43f, 0.55f),
        new Color(0.36f, 0.32f, 0.30f), // el celler: pedra
        new Color(0.72f, 0.80f, 0.86f), // la cambra: acer
        new Color(0.80f, 0.62f, 0.22f), // el plató: daurat
    };

    private static readonly Color[] PanelColors =
    {
        new Color(0.30f, 0.18f, 0.10f),
        new Color(0.18f, 0.10f, 0.07f),
        new Color(0.40f, 0.45f, 0.50f),
        new Color(0.22f, 0.30f, 0.20f),
        new Color(0.15f, 0.16f, 0.24f),
        new Color(0.26f, 0.12f, 0.08f), // fusta de bóta
        new Color(0.45f, 0.55f, 0.62f), // porta de cambra frigorífica
        new Color(0.10f, 0.08f, 0.10f), // porta negra d'escenari
    };

    void Awake()
    {
        Instance = this;

        Vector2 size = HazardManager.ArenaSize;
        Vector2 c = HazardManager.ArenaCenter;
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float spread = size.x * 0.27f;

        AddDoor(c + new Vector2(-spread, hy), true, Vector2.down);
        AddDoor(c + new Vector2(spread, hy), true, Vector2.down);
        AddDoor(c + new Vector2(-spread, -hy), true, Vector2.up);
        AddDoor(c + new Vector2(spread, -hy), true, Vector2.up);
        AddDoor(c + new Vector2(-hx, 0f), false, Vector2.right);
        AddDoor(c + new Vector2(hx, 0f), false, Vector2.left);

        SetAct(0);
    }

    void AddDoor(Vector2 wallPoint, bool horizontal, Vector2 inward)
    {
        GameObject go = new GameObject("Porta");
        go.transform.SetParent(transform, false);
        go.transform.position = wallPoint - inward * 0.35f; // una mica dins de la paret

        Door d = go.AddComponent<Door>();
        d.Build(horizontal);
        d.spawnPoint = wallPoint + inward * 1.4f;
        d.along = horizontal ? Vector2.right : Vector2.up;
        doors.Add(d);
    }

    public void SetAct(int act)
    {
        act = ((act % 8) + 8) % 8;
        foreach (Door d in doors) d.SetColors(FrameColors[act], PanelColors[act]);
    }

    // tria una porta lluny del jugador, perquè no li surtin a sobre
    public Vector2 GetSpawnPoint(Vector2 playerPos)
    {
        List<Door> far = new List<Door>();
        Door farthest = null;
        float best = -1f;

        foreach (Door d in doors)
        {
            float dist = Vector2.Distance(d.spawnPoint, playerPos);
            if (dist >= 7f) far.Add(d);
            if (dist > best)
            {
                best = dist;
                farthest = d;
            }
        }

        Door chosen = far.Count > 0 ? far[Random.Range(0, far.Count)] : farthest;
        if (chosen == null) return playerPos + Random.insideUnitCircle.normalized * 8f;

        chosen.Open();
        return chosen.spawnPoint + chosen.along * Random.Range(-0.8f, 0.8f);
    }
}
