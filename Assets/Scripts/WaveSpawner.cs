using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;

    public int startEnemies = 3;      // enemics a l'onada 1
    public int enemiesPerWave = 2;    // enemics extra a cada onada nova
    public float spawnInterval = 0.5f;
    public float timeBetweenWaves = 3f;
    public float spawnRadius = 10f;   // distància al jugador
    public ShopManager shop;

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

            // espera que no quedi cap enemic viu
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

    void SpawnEnemy()
    {
        if (player == null) return;

        Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
        Vector2 pos = (Vector2)player.position + offset;
        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}