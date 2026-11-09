using System.Collections;
using UnityEngine;

// cada acte té les seves caixes: cadires, barrils, caixes de verdura, testos i caixes
public class PropsHazard : MonoBehaviour
{
    public int act;
    public int maxProps = 4;

    private static readonly Color[] Colors =
    {
        new Color(0.60f, 0.42f, 0.25f),
        new Color(0.55f, 0.30f, 0.12f),
        new Color(0.45f, 0.65f, 0.30f),
        new Color(0.75f, 0.38f, 0.25f),
        new Color(0.50f, 0.45f, 0.40f),
    };

    private static readonly Vector2[] Sizes =
    {
        new Vector2(0.9f, 0.9f),
        new Vector2(1.0f, 1.0f),
        new Vector2(1.2f, 0.9f),
        new Vector2(0.9f, 0.9f),
        new Vector2(1.1f, 1.1f),
    };

    private Transform player;

    IEnumerator Start()
    {
        PlayerMovement pm = PlayerRef.Movement;
        if (pm != null) player = pm.transform;

        yield return null; // deixem que els altres perills es creïn primer

        for (int i = 0; i < maxProps; i++) TrySpawn();

        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (transform.childCount < maxProps) TrySpawn();
        }
    }

    void TrySpawn()
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector2 pos = HazardFx.RandomPoint(3f);

            if (player != null && Vector2.Distance(pos, player.position) < 4f) continue;
            if (!IsFree(pos)) continue;

            Build(pos);
            return;
        }
    }

    // no posem una caixa a sobre de res sòlid (taules, parets, clients, el jugador)
    bool IsFree(Vector2 pos)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 1.6f))
        {
            if (!c.isTrigger) return false;
        }
        return true;
    }

    void Build(Vector2 pos)
    {
        int a = Mathf.Clamp(act, 0, 4);

        GameObject root = new GameObject("Caixa");
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size = Sizes[a];

        GameObject body = new GameObject("Cos");
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(Sizes[a].x, Sizes[a].y, 1f);
        SpriteRenderer sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = Colors[a];
        sr.sortingOrder = -1;

        root.AddComponent<Breakable>();
    }
}
