using UnityEngine;

// L'Inspectora: llança una espiral de trets i, a meitat de vida, s'enfada i invoca reforços
public class BossPhases : MonoBehaviour
{
    public GameObject projectilePrefab;       // el projectil dels clients (EnemyShot)
    public float spiralInterval = 0.12f;      // cada quant surt un projectil
    public float spiralStep = 24f;            // graus que gira l'espiral
    public float shotSpeed = 6.5f;
    public float rageHealth = 0.55f;           // percentatge de vida en què s'enfada
    public int minionsOnRage = 3;
    public float rageSpeedMultiplier = 1.5f;
    public float rageSpiralInterval = 0.075f;

    private Enemy enemy;
    private float angle;
    private float nextShot;
    private bool raging;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        angle = Random.Range(0f, 360f);
        nextShot = Time.time + 1.2f;
    }

    void Update()
    {
        if (projectilePrefab == null) return;

        // fase 2: quan baixa de meitat de vida, s'accelera i crida clients
        if (!raging && enemy.CurrentHealth <= Mathf.RoundToInt(enemy.maxHealth * rageHealth))
            Enrage();

        if (Time.time < nextShot) return;
        nextShot = Time.time + (raging ? rageSpiralInterval : spiralInterval);

        CameraFX.Shake(0.14f, 0.12f);

        // espiral de trets
        angle += spiralStep;
        float a = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        Vector2 pos = (Vector2)transform.position + dir * transform.localScale.x * 0.5f;

        GameObject s = Instantiate(projectilePrefab, pos, Quaternion.identity);
        s.GetComponent<Rigidbody2D>().linearVelocity = dir * shotSpeed;
    }

    void Enrage()
    {
        raging = true;
        enemy.speed *= rageSpeedMultiplier;

        BossFx.Puff(transform.position, 4f, new Color(1f, 0.5f, 0.9f, 0.45f));

        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.SpawnMinions(transform.position, minionsOnRage);
    }
}
