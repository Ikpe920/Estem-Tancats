using System.Collections;
using UnityEngine;

[System.Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int firstWave = 1;  // a partir de quina onada apareix
    public int weight = 10;    // com de freqüent és (més alt = més sovint)
}

public class WaveSpawner : MonoBehaviour
{
    public EnemyType[] enemyTypes;
    public Transform player;
    public ShopManager shop;

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

    void SpawnEnemy()
    {
        if (player == null) return;

        GameObject prefab = PickEnemy();
        if (prefab == null) return;

        Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
        Vector2 pos = (Vector2)player.position + offset;

        // mantenir-los dins de la sala (ajusta-ho a la mida del teu terra)
        pos.x = Mathf.Clamp(pos.x, -14f, 14f);
        pos.y = Mathf.Clamp(pos.y, -9f, 9f);

        Instantiate(prefab, pos, Quaternion.identity);
    }
}