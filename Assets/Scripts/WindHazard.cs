using System.Collections;
using UnityEngine;

public class WindHazard : MonoBehaviour
{
    public float force = 3.2f;       // velocitat extra que t'empeny
    public float warningTime = 1.5f; // només es veuen les línies de vent
    public float gustTime = 3.5f;    // la ràfega empeny de debò

    private PlayerMovement movement;
    private Camera cam;
    private float nextStreak;

    private static readonly Vector2[] Directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

    IEnumerator Start()
    {
        movement = PlayerRef.Movement;
        cam = Camera.main;

        yield return new WaitForSeconds(4f);

        while (true)
        {
            Vector2 dir = Directions[Random.Range(0, Directions.Length)];

            // 1. avís
            float t = 0f;
            while (t < warningTime)
            {
                t += Time.deltaTime;
                Streaks(dir, 0.12f);
                yield return null;
            }

            // 2. ràfega
            t = 0f;
            while (t < gustTime)
            {
                t += Time.deltaTime;
                float ramp = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((gustTime - t) / 0.5f);
                if (movement != null) movement.externalPush = dir * force * ramp;
                Streaks(dir, 0.04f);
                yield return null;
            }

            if (movement != null) movement.externalPush = Vector2.zero;

            yield return new WaitForSeconds(Random.Range(5f, 9f));
        }
    }

    void Streaks(Vector2 dir, float interval)
    {
        if (cam == null || Time.time < nextStreak) return;
        nextStreak = Time.time + interval;

        float hh = cam.orthographicSize;
        float hw = hh * cam.aspect;
        bool horizontal = Mathf.Abs(dir.x) > 0.5f;
        float reach = horizontal ? hw : hh;
        float span = horizontal ? hh : hw;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2 pos = (Vector2)cam.transform.position - dir * (reach + 1f) + perp * Random.Range(-span, span);

        GameObject go = new GameObject("Vent");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        go.transform.localScale = new Vector3(Random.Range(1f, 2.2f), 0.05f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HazardFx.Square();
        sr.color = new Color(1f, 1f, 1f, 0.3f);
        sr.sortingOrder = 50;

        WindStreak s = go.AddComponent<WindStreak>();
        s.velocity = dir * 22f;
        s.life = (reach * 2f + 3f) / 22f;
    }

    void OnDestroy()
    {
        if (movement != null) movement.externalPush = Vector2.zero;
    }
}
