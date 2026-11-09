using System.Collections;
using UnityEngine;

// cada acte té les seves caixes: cadires, barrils, caixes de verdura, testos i caixes
public class PropsHazard : MonoBehaviour
{
    public int act;
    public int maxProps = 4;

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

        // el dibuix de cada acte (cadira, bóta, verdura, test, caixa) amb una ombra a sota
        Table.AddVisual(root.transform, PropArt.Prop(a, Sizes[a]), Sizes[a]);

        root.AddComponent<Breakable>();
    }
}
