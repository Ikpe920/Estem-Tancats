using System.Collections;
using UnityEngine;

// El Xef renegat: es queda a distància, llança ventalls de ganivets i invoca clients
public class BossSummoner : MonoBehaviour
{
    public GameObject projectilePrefab;      // el projectil dels clients (EnemyShot)
    public float fanInterval = 3.2f;         // cada quant llança el ventall
    public int fanShots = 5;                 // ganivets per ventall
    public float fanAngle = 45f;             // obertura del ventall
    public float shotSpeed = 6f;
    public float summonInterval = 8f;        // cada quant invoca clients
    public int minionsPerSummon = 2;
    public float summonTelegraph = 0.9f;     // avís abans que surtin els clients
    public float summonRadius = 2.9f;

    private Enemy enemy;
    private Transform player;
    private float nextFan;
    private float nextSummon;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // es queda a distància en lloc de tirar-se a sobre del jugador
        enemy.isShooter = true;
        enemy.stopDistance = 6f;

        nextFan = Time.time + 1.2f;
        nextSummon = Time.time + 4f;
    }

    void Update()
    {
        if (player == null) return;

        if (Time.time >= nextFan)
        {
            nextFan = Time.time + fanInterval;
            ThrowFan();
        }

        if (Time.time >= nextSummon)
        {
            nextSummon = Time.time + summonInterval;
            StartCoroutine(SummonRoutine());
        }
    }

    void ThrowFan()
    {
        if (projectilePrefab == null) return;

        Vector2 origin = transform.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < fanShots; i++)
        {
            float offset = fanShots > 1
                ? Mathf.Lerp(-fanAngle / 2f, fanAngle / 2f, (float)i / (fanShots - 1))
                : 0f;
            float a = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            GameObject s = Instantiate(projectilePrefab, origin + d * 1.2f, Quaternion.identity);
            s.GetComponent<Rigidbody2D>().linearVelocity = d * shotSpeed;
        }
    }

    IEnumerator SummonRoutine()
    {
        Vector2 pos = transform.position;
        Color green = new Color(0.4f, 0.9f, 0.5f, 1f);

        // avís al terra: un cercle verd que es va omplint
        GameObject warn = BossFx.Telegraph(pos, summonRadius, new Color(green.r, green.g, green.b, 0.1f), "BossSummonAviso");
        Destroy(warn, summonTelegraph + 1f);

        SpriteRenderer sr = warn.GetComponent<SpriteRenderer>();
        float t = 0f;
        while (t < summonTelegraph)
        {
            t += Time.deltaTime;
            sr.color = new Color(green.r, green.g, green.b, Mathf.Lerp(0.1f, 0.4f, t / summonTelegraph));
            yield return null;
        }
        Destroy(warn);

        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.SpawnMinions(pos, minionsPerSummon);
    }
}
