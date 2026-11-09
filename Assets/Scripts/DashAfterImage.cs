using System.Collections.Generic;
using UnityEngine;

// Estela del dash: una còpia del sprite del jugador que s'esvaeix.
// Se'n fan moltes seguides, així que es reaprofiten en lloc de crear-les i destruir-les.
public class DashAfterImage : MonoBehaviour
{
    static readonly Stack<DashAfterImage> pool = new Stack<DashAfterImage>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPool() { pool.Clear(); }

    private SpriteRenderer sr;
    private float life;
    private float maxLife = 1f;
    private float startAlpha;

    // crea (o reaprofita) una estela amb l'aspecte actual de 'source'
    public static void Spawn(SpriteRenderer source, Color color, float lifetime)
    {
        if (source == null || source.sprite == null) return;

        DashAfterImage ghost = null;
        while (pool.Count > 0 && ghost == null) ghost = pool.Pop(); // els de l'escena anterior ja no existeixen
        if (ghost == null)
        {
            GameObject go = new GameObject("DashAfterImage");
            go.AddComponent<SpriteRenderer>();
            ghost = go.AddComponent<DashAfterImage>();
        }

        Transform t = ghost.transform;
        Transform from = source.transform;
        t.position = from.position;
        t.rotation = from.rotation;
        t.localScale = from.lossyScale;

        SpriteRenderer gsr = ghost.GetComponent<SpriteRenderer>();
        gsr.sprite = source.sprite;
        gsr.flipX = source.flipX;
        gsr.sortingLayerID = source.sortingLayerID;
        gsr.sortingOrder = source.sortingOrder - 1;

        ghost.Init(lifetime, color);
        ghost.gameObject.SetActive(true);
    }

    public void Init(float lifetime, Color color)
    {
        maxLife = Mathf.Max(0.01f, lifetime);
        life = maxLife;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
            startAlpha = color.a;
        }
    }

    void Update()
    {
        life -= Time.deltaTime;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = startAlpha * Mathf.Clamp01(life / maxLife);
            sr.color = c;
        }

        if (life <= 0f)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}
