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

    public int currentWave = 0;

    void Start()
    {
        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;
            Debug.Log("Onada " + currentWave);

            int count = startEnemies + (currentWave - 1) * enemiesPerWave;

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

            while (FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0)
            {
                yield return null;
            }

            if (shop != null)
            {
                shop.Open();
                yield return new WaitUntil(() => !shop.isOpen);
            }
            else
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
    }

    GameObject PickEnemy()
    {
        int total = 0;
        foreach (EnemyType t in enemyTypes)
            if (currentWave >= t.firstWave) total += t.weight;

        if (total == 0) return null;

        int roll = Random.Range(0, total);
        foreach (EnemyType t in enemyTypes)
        {
            if (currentWave < t.firstWave) continue;
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

        Instantiate(prefab, RandomSpawnPosition(), Quaternion.identity);
    }

    void SpawnBoss(BossInfo info)
    {
        if (player == null || info.prefab == null) return;

        GameObject b = Instantiate(info.prefab, RandomSpawnPosition(), Quaternion.identity);

        if (bossManager != null)
            bossManager.TrackBoss(b.GetComponent<Enemy>(), info.name);
    }
}