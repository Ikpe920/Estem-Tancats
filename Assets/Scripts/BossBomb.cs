using System.Collections;
using UnityEngine;

// Bomba d'àrea telegrafiada: al cap d'un temps explota i fa mal si ets a dins
public class BossBomb : MonoBehaviour
{
    public float radius = 1.8f;
    public float delay = 1.3f;

    private SpriteRenderer sr;
    private Transform player;

    // crea una bomba en un punt del terra
    public static BossBomb Spawn(Vector2 pos, float radius, float delay)
    {
        GameObject go = BossFx.Telegraph(pos, radius, new Color(1f, 0.25f, 0.2f, 0.12f), "BossBomba");
        BossBomb bomb = go.AddComponent<BossBomb>();
        bomb.radius = radius;
        bomb.delay = delay;
        return bomb;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        Color warn = new Color(1f, 0.25f, 0.2f, 1f);

        // 1. avís: el cercle es va omplint
        float t = 0f;
        while (t < delay)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = new Color(warn.r, warn.g, warn.b, Mathf.Lerp(0.12f, 0.55f, t / delay));
            yield return null;
        }

        // 2. explosió
        if (sr != null) sr.color = new Color(1f, 0.85f, 0.2f, 0.85f);

        if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(0.26f, 0.22f);
        else if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.26f, 0.22f);
        else if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.26f, 0.22f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D h in hits)
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(); // cada cop que toca el jugador, perdi un cor
        }

        yield return new WaitForSeconds(0.12f);
        Destroy(gameObject);
    }
}
