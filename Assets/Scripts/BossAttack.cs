using UnityEngine;

public class BossAttack : MonoBehaviour
{
    public GameObject shotPrefab;
    public float fireInterval = 2.5f;
    public int shotsPerBurst = 10;
    public float shotSpeed = 5f;

    [Header("Fase 2 (menys de la meitat de vida)")]
    public int phase2Shots = 16;
    public float phase2Interval = 1.6f;

    private Enemy enemy;
    private float nextShot;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        nextShot = Time.time + 1.5f;
    }

    void Update()
    {
        if (shotPrefab == null || Time.time < nextShot) return;

        bool phase2 = enemy.CurrentHealth <= enemy.maxHealth / 2;
        int shots = phase2 ? phase2Shots : shotsPerBurst;
        nextShot = Time.time + (phase2 ? phase2Interval : fireInterval);

        CameraFX.Shake(0.18f, 0.16f);

        float step = 360f / shots;
        float startAngle = Random.Range(0f, step); // perquè no sempre surti igual

        for (int i = 0; i < shots; i++)
        {
            float angle = (startAngle + i * step) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 pos = (Vector2)transform.position + dir * transform.localScale.x * 0.6f;

            GameObject s = Instantiate(shotPrefab, pos, Quaternion.identity);
            s.GetComponent<Rigidbody2D>().linearVelocity = dir * shotSpeed;
        }
    }
}