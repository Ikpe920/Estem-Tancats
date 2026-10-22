using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int firstWave = 1;
    public int weight = 10;
}

public enum BossType
{
    Critic,    // cop de safata i ràfega radial (el de sempre)
    Borratxo,  // càrregues en línia recta i atordiment
    Cuiner,    // invoca clients i llança ventalls de ganivets
    Pallas,    // es teletransporta i deixa bombes al terra
    Inspector  // espiral de trets i fase enfadada
}

[System.Serializable]
public class BossInfo
{
    public string name;
    public string subtitle;
    public GameObject prefab;
    public BossType type = BossType.Critic;
    public int health = 0;               // 0 = la vida que té el prefab
    public float speed = 0f;             // 0 = la velocitat del prefab
    public float scale = 0f;             // 0 = l'escala del prefab
    public Sprite sprite;                // opcional: canvia la imatge del cap
    public Color tint = Color.white;     // blanc = sense tenyir
    public GameObject projectilePrefab;  // per als caps que disparen
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

    [Header("Final del joc")]
    public int finalWave = 25; // en completar aquesta onada, guanyes (0 = sense final)

    public int currentWave = 0;

    public static WaveSpawner Instance; // perquè els caps puguin invocar clients

    void Awake()
    {
        Instance = this;
    }

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
            // final del joc: en acabar l'última onada, guanyes
            if (finalWave > 0 && currentWave >= finalWave)
            {
                // guardem el resultat i anem a la pantalla de victòria
                int points = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
                RunStats.Save(currentWave, points);
                SceneManager.LoadScene("Win");
                yield break; // la partida s'acaba: no hi ha més onades
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
        Enemy e = b.GetComponent<Enemy>();

        if (e != null)
        {
            // vida i velocitat pròpies del cap (si les posa l'escena)
            if (info.health > 0 || info.speed > 0)
                e.OverrideStats(info.health > 0 ? info.health : e.maxHealth,
                                info.speed > 0f ? info.speed : e.speed);

            // imatge i color
            if (info.sprite != null)
            {
                e.spriteFront = info.sprite;
                e.spriteBack = info.sprite;
                SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = info.sprite;
            }
            if (info.tint != Color.white) e.SetTint(info.tint);
        }

        // mida
        if (info.scale > 0f) b.transform.localScale = Vector3.one * info.scale;

        // cada cap té el seu comportament
        ConfigureBoss(b, e, info);

        if (bossManager != null && e != null)
            bossManager.TrackBoss(e, info.name);
    }

    // afegeix el comportament propi de cada cap
    void ConfigureBoss(GameObject b, Enemy e, BossInfo info)
    {
        if (e == null) return;

        BossMelee melee = b.GetComponent<BossMelee>();
        BossAttack attack = b.GetComponent<BossAttack>();

        switch (info.type)
        {
            case BossType.Critic:
                // el Crític fa servir el cop de safata i la ràfega del prefab
                break;

            case BossType.Borratxo:
                if (melee != null) melee.enabled = false;
                if (attack != null) attack.enabled = false;
                b.AddComponent<BossCharger>();
                break;

            case BossType.Cuiner:
                if (melee != null) melee.enabled = false;
                if (attack != null) attack.enabled = false;
                BossSummoner summoner = b.AddComponent<BossSummoner>();
                summoner.projectilePrefab = info.projectilePrefab;
                break;

            case BossType.Pallas:
                if (melee != null) melee.enabled = false;
                if (attack != null) attack.enabled = false;
                b.AddComponent<BossTeleporter>();
                break;

            case BossType.Inspector:
                if (melee != null) melee.enabled = false;
                if (attack != null) attack.enabled = false;
                BossPhases phases = b.AddComponent<BossPhases>();
                phases.projectilePrefab = info.projectilePrefab;
                break;
        }
    }

    // els caps poden fer sortir clients normals (el Xef i la Inspectora)
    public void SpawnMinions(Vector2 around, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickEnemy();
            if (prefab == null) return;

            Vector2 pos = around + Random.insideUnitCircle.normalized * 2.5f;
            Enemy e = Instantiate(prefab, pos, Quaternion.identity).GetComponent<Enemy>();

            ActData act = actManager != null ? actManager.Current : null;
            if (e != null && act != null)
                e.ApplyActDifficulty(act.enemyHealthMultiplier, act.enemySpeedBonus);
        }
    }
}