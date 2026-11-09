using System.Collections;
using UnityEngine;

// ---------- LA TEULADA: llamps ----------
public class LightningHazard : MonoBehaviour
{
    public float radius = 1.5f;
    public float warningTime = 1.1f;
    public int enemyDamage = 6;

    private Transform player;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            StartCoroutine(Strike(PickTarget()));

            // a les últimes onades cauen més sovint
            int wave = WaveSpawner.Instance != null ? WaveSpawner.Instance.currentWave : 21;
            float k = Mathf.InverseLerp(21f, 25f, wave);
            yield return new WaitForSeconds(Mathf.Lerp(3.2f, 1.8f, k));
        }
    }

    // dos de cada tres llamps van cap a on ets; l'altre cau a l'atzar
    Vector2 PickTarget()
    {
        if (player == null)
        {
            PlayerMovement pm = PlayerRef.Movement;
            if (pm != null) player = pm.transform;
        }

        if (player != null && Random.value < 0.66f) return player.position;
        return HazardFx.RandomPoint(1.5f);
    }

    IEnumerator Strike(Vector2 pos)
    {
        // avís: cercle groc amb un cercle interior que creix
        SpriteRenderer warn = HazardFx.MakeCircle(pos, radius, new Color(1f, 0.9f, 0.3f, 0.12f), -3);
        warn.transform.SetParent(transform, true);
        SpriteRenderer fill = HazardFx.MakeCircle(pos, radius, new Color(1f, 0.95f, 0.5f, 0.35f), -2);
        fill.transform.SetParent(transform, true);
        fill.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < warningTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / warningTime);
            fill.transform.localScale = Vector3.one * radius * 2f * k;
            warn.color = new Color(1f, 0.9f, 0.3f, Mathf.Lerp(0.12f, 0.4f, k));
            yield return null;
        }

        Destroy(warn.gameObject);
        Destroy(fill.gameObject);

        // el llamp: flaix al terra i un raig que cau del cel
        SpriteRenderer flash = HazardFx.MakeCircle(pos, radius, new Color(1f, 1f, 1f, 0.9f), 20);
        Destroy(flash.gameObject, 0.12f);

        GameObject beam = new GameObject("Raig");
        beam.transform.position = pos + Vector2.up * 7f;
        beam.transform.localScale = new Vector3(0.6f, 14f, 1f);
        SpriteRenderer bsr = beam.AddComponent<SpriteRenderer>();
        bsr.sprite = HazardFx.Square();
        bsr.color = new Color(1f, 1f, 0.85f, 0.95f);
        bsr.sortingOrder = 21;
        Destroy(beam, 0.12f);

        HazardFx.Shake(0.2f, 0.22f);

        DarknessHazard.Flash(); // a la Teulada, el llamp il·lumina un instant

        foreach (Collider2D h in Physics2D.OverlapCircleAll(pos, radius))
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage();
                continue;
            }

            Enemy e = h.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(enemyDamage);
        }
    }
}
