using System.Collections;
using UnityEngine;

[System.Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int firstWave = 1;
    public int weight = 10;
}

[System.Serializable]
public class BossInfo
{
    public string name;
    public string subtitle;
    public GameObject prefab;
}

public class WaveSpawner : MonoBehaviour
{
    public EnemyType[] enemyTypes;
    public Transform player;
    public ShopManager shop;

    [Header("Boss")]
    public BossInfo[] bosses;
    public int bossEveryNWaves = 5;
    public BossManager bossManager;

    [Header("Onades")]
    public int startEnemies = 3;
    public int enemiesPerWave = 2;
    public float spawnInterval = 0.5f;
    public float timeBetweenWaves = 3f;
    public float spawnRadius = 10f;

    [Header("Actes")]
    public ActManager actManager; // el canvi d'escenari cada 5 onades

    [Header("Boons")]
    public BoonManager boonManager; // les 3 opcions a triar entre onades

    public int currentWave = 0;

    void Start()
    {
        // si el component Act Manager és al mateix objecte, el trobem sols
        if (actManager == null) actManager = GetComponent<ActManager>();
        if (boonManager == null) boonManager = GetComponent<BoonManager>();

        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;
            Debug.Log("Onada " + currentWave);

            ActData act = actManager != null ? actManager.Current : null;

            int count = startEnemies + (currentWave - 1) * enemiesPerWave;
            if (act != null) count += act.extraEnemies;

            bool isBossWave = bosses != null && bosses.Length > 0
                              && currentWave % bossEveryNWaves == 0;

            if (isBossWave)
            {
                int index = (currentWave / bossEveryNWaves - 1) % bosses.Length;
                BossInfo info = bosses[index];

                if (bossManager != null)
                    yield return StartCoroutine(bossManager.PlayIntro(info.name, info.subtitle));

                SpawnBoss(info);
                count /= 2; // a l'onada del boss surten menys enemics normals
            }

            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            while (FindObjectsByType<Enemy>().Length > 0)
            {
                yield return null;
            }

            // canvi d'escenari: quan l'onada del cap acaba, passem a la sala següent
            if (isBossWave && actManager != null)
                yield return StartCoroutine(actManager.TransitionToNextAct());

            // boons: 3 opcions a triar entre onades (amb els serveis de la botiga a sota)
            if (boonManager != null && boonManager.ShouldOffer(currentWave))
                yield return StartCoroutine(boonManager.OfferBoons());

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    GameObject PickEnemy()
    {
        // si l'acte té la seva pròpia llista de clients, fem servir-la (firstWave = ona dins de l'acte)
        EnemyType[] pool = enemyTypes;
        int wave = currentWave;

        ActData act = actManager != null ? actManager.Current : null;
        if (act != null && act.enemies != null && act.enemies.Length > 0)
        {
            pool = act.enemies;
            wave = currentWave - actManager.currentAct * bossEveryNWaves;
            if (wave < 1) wave = 1;
        }

        int total = 0;
        foreach (EnemyType t in pool)
            if (wave >= t.firstWave) total += t.weight;

        if (total == 0) return null;

        int roll = Random.Range(0, total);
        foreach (EnemyType t in pool)
        {
            if (wave < t.firstWave) continue;
            if (roll < t.weight) return t.prefab;
            roll -= t.weight;
        }
        return null;
    }

    Vector2 RandomSpawnPosition()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
        Vector2 pos = (Vector2)player.position + offset;
        pos.x = Mathf.Clamp(pos.x, -12f, 12f);
        pos.y = Mathf.Clamp(pos.y, -8f, 8f);
        return pos;
    }

    void SpawnEnemy()
    {
        if (player == null) return;

        GameObject prefab = PickEnemy();
        if (prefab == null) return;

        Enemy e = Instantiate(prefab, RandomSpawnPosition(), Quaternion.identity).GetComponent<Enemy>();

        // l'acte endureix els clients (més vida i més pressa)
        ActData act = actManager != null ? actManager.Current : null;
        if (e != null && act != null)
            e.ApplyActDifficulty(act.enemyHealthMultiplier, act.enemySpeedBonus);
    }

    void SpawnBoss(BossInfo info)
    {
        if (player == null || info.prefab == null) return;

        GameObject b = Instantiate(info.prefab, RandomSpawnPosition(), Quaternion.identity);

        if (bossManager != null)
            bossManager.TrackBoss(b.GetComponent<Enemy>(), info.name);
    }
}