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

    [Header("Dificultat progressiva")]
    public float healthPerWave = 0.18f;   // +14% de vida (sobre la base) a cada onada
    public float speedPerWave = 0.03f;    // +0,03 de velocitat a cada onada

    [Header("Actes")]
    public ActManager actManager; // el canvi d'escenari cada 5 onades

    [Header("Boons")]
    public BoonManager boonManager; // les 3 opcions a triar entre onades

    [Header("Final del joc")]
    public int finalWave = 25; // en completar aquesta onada, guanyes (0 = sense final)

    [Header("Avís de sortida")]
    public float spawnWarningTime = 0.6f; // segons que es veu el cercle vermell abans que surti el client (0 = sense avís)

    [Header("Onades encallades")]
    public float stuckCheckInterval = 1f; // cada quant es miren els clients que han sortit de l'arena
    public float stuckWaveTimeout = 45f;  // segons sense cap baixa (amb el cap ja mort) abans de treure els que queden (0 = mai)

    public int currentWave = 0;

    private Enemy currentBoss;
    private int pendingSpawns;   // clients avisats que encara no han sortit (l'onada no s'acaba fins que surtin)
    private int spawnGeneration; // el mode de prova el canvia per anul·lar els avisos pendents // el cap de l'onada actual (no el traiem mai per temps)

    public static WaveSpawner Instance; // perquè els caps puguin invocar clients

    // mode de prova: saltar d'onada
    private bool inWave;        // estem enmig d'una onada (sortint clients o esperant que acabin)
    private bool debugAbort;    // s'ha demanat acabar l'onada ara
    private int debugTarget;    // l'onada on s'ha demanat anar

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
            inWave = true;

            ActData act = actManager != null ? actManager.Current : null;

            int count = startEnemies + (currentWave - 1) * enemiesPerWave;
            if (act != null) count += act.extraEnemies;

            bool isBossWave = bosses != null && bosses.Length > 0
                              && currentWave % bossEveryNWaves == 0;

            // potser és una onada especial (hora punta, tall de llum, client VIP...)
            WaveEvents.Begin(WaveEvents.Roll(currentWave, isBossWave, actManager != null ? actManager.currentAct : 0), this);
            count = WaveEvents.AdjustCount(count);

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
                if (debugAbort) break;
                SpawnEnemy();
                // les onades altes surten en un degoteig més ràpid
                yield return new WaitForSeconds(WaveEvents.AdjustInterval(Mathf.Max(0.15f, spawnInterval - 0.012f * (currentWave - 1))));
            }

            yield return StartCoroutine(WaitForWaveCleared());

            inWave = false;
            WaveEvents.End();

            // mode de prova: s'ha demanat saltar a una altra onada (sense bonus ni plats entremig)
            if (debugAbort)
            {
                debugAbort = false;
                currentWave = debugTarget - 1; // el bucle suma 1 en tornar a començar
                if (actManager != null)
                    actManager.ApplyAct((debugTarget - 1) / Mathf.Max(1, bossEveryNWaves));
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // bonus d'onada (doble si no t'han tocat)
            if (ScoreManager.Instance != null)
            {
                PlayerHealth ph = PlayerRef.Health;
                Vector2 at = ph != null ? (Vector2)ph.transform.position + Vector2.up * 1.2f : Vector2.zero;
                ScoreManager.Instance.EndWave(currentWave, at);
            }

            // petit descans perquè el jugador pugui respirar abans de triar un plat
            if (actManager != null)
                yield return StartCoroutine(actManager.ShowRoundComplete(currentWave));
            yield return new WaitForSeconds(0.4f);

            // final del joc: en acabar l'última onada, guanyes
            if (finalWave > 0 && currentWave >= finalWave)
            {
                // guardem el resultat i anem a la pantalla de victòria
                int points = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
                int combo = ScoreManager.Instance != null ? ScoreManager.Instance.MaxCombo : 0;
                RunStats.Save(currentWave, points, combo);
                SceneManager.LoadScene(SceneNames.Win);
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

    // espera que no quedi cap client. Si algun surt de l'arena el tornem a dins, i si l'onada
    // porta massa estona sense cap baixa (algun client encallat en un moble) traiem els que queden
    IEnumerator WaitForWaveCleared()
    {
        int lastAlive = Enemy.Alive;
        float lastProgress = Time.time;
        float nextCheck = Time.time + stuckCheckInterval;

        while (Enemy.Alive > 0 || pendingSpawns > 0)
        {
            if (Enemy.Alive < lastAlive) lastProgress = Time.time;
            lastAlive = Enemy.Alive;

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + stuckCheckInterval;
                RescueOutOfBounds();

                bool bossAlive = currentBoss != null;
                if (!bossAlive && stuckWaveTimeout > 0f && Time.time - lastProgress >= stuckWaveTimeout)
                {
                    RemoveStragglers();
                    lastProgress = Time.time;
                }
            }

            yield return null; // amb Time.time: amb el joc pausat el compte no avança
        }
    }

    void RescueOutOfBounds()
    {
        if (player == null) return;
        Vector2 half = HazardManager.ArenaSize * 0.5f + Vector2.one * 0.5f;
        Vector2 c = HazardManager.ArenaCenter;

        foreach (Enemy e in FindObjectsByType<Enemy>())
        {
            Vector2 p = e.transform.position;
            if (Mathf.Abs(p.x - c.x) <= half.x && Mathf.Abs(p.y - c.y) <= half.y) continue;

            Rigidbody2D erb = e.GetComponent<Rigidbody2D>();
            Vector2 to = RandomSpawnPosition();
            if (erb != null) { erb.position = to; erb.linearVelocity = Vector2.zero; }
            else e.transform.position = to;
        }
    }

    // treu els clients que queden sense donar punts (el cap de l'onada no es toca mai)
    void RemoveStragglers()
    {
        foreach (Enemy e in FindObjectsByType<Enemy>())
        {
            if (e == currentBoss) continue;
            Destroy(e.gameObject);
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
        if (DoorManager.Instance != null)
            return DoorManager.Instance.GetSpawnPoint(player.position);
        Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
        Vector2 pos = (Vector2)player.position + offset;
        // els límits surten de la mida real de l'arena (abans eren números fixos)
        Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2f;
        Vector2 c = HazardManager.ArenaCenter;
        pos.x = Mathf.Clamp(pos.x, c.x - half.x, c.x + half.x);
        pos.y = Mathf.Clamp(pos.y, c.y - half.y, c.y + half.y);
        return pos;
    }

    void SpawnEnemy()
    {
        if (player == null) return;

        GameObject prefab = PickEnemy();
        if (prefab == null) return;

        SpawnWithWarning(prefab, RandomSpawnPosition());
    }

    // primer surt un cercle vermell a terra i, al cap d'una estona, el client
    void SpawnWithWarning(GameObject prefab, Vector2 pos)
    {
        if (spawnWarningTime <= 0f)
        {
            SpawnNow(prefab, pos);
            return;
        }
        StartCoroutine(SpawnAfterWarning(prefab, pos));
    }

    IEnumerator SpawnAfterWarning(GameObject prefab, Vector2 pos)
    {
        pendingSpawns++;
        int generation = spawnGeneration;
        SpawnWarning.Show(pos, spawnWarningTime);

        yield return new WaitForSeconds(spawnWarningTime);

        pendingSpawns--;
        if (generation != spawnGeneration) yield break; // el mode de prova ha saltat d'onada
        if (PlayerHealth.IsDying) yield break;

        SpawnNow(prefab, pos);
    }

    // el client surt ja: vida i velocitat de l'onada, i potser és un client especial (EnemyVariants)
    void SpawnNow(GameObject prefab, Vector2 pos)
    {
        Enemy e = Instantiate(prefab, pos, Quaternion.identity).GetComponent<Enemy>();
        ApplyWaveDifficulty(e);
        EnemyVariants.MaybeApply(e, prefab, currentWave);
        WaveEvents.OnEnemySpawned(e);
    }

    // un client de més, ara mateix i sense variants ni esdeveniments (el client VIP)
    public Enemy SpawnExtra(GameObject prefab)
    {
        if (prefab == null || player == null) return null;
        Enemy e = Instantiate(prefab, RandomSpawnPosition(), Quaternion.identity).GetComponent<Enemy>();
        ApplyWaveDifficulty(e);
        return e;
    }

    // el client que camina més ràpid (no tirador) de la llista: el VIP en fa servir l'aspecte
    public GameObject FastestWalkerPrefab()
    {
        GameObject best = null;
        float bestSpeed = -1f;
        if (enemyTypes == null) return null;
        foreach (EnemyType t in enemyTypes)
        {
            Enemy e = t.prefab != null ? t.prefab.GetComponent<Enemy>() : null;
            if (e == null || e.isShooter) continue;
            if (e.speed > bestSpeed) { bestSpeed = e.speed; best = t.prefab; }
        }
        return best;
    }

    // vida, velocitat i agressivitat dels clients segons l'onada (i l'acte)
    void ApplyWaveDifficulty(Enemy e)
    {
        if (e == null) return;

        int level = Mathf.Max(0, currentWave - 1);
        float healthMul = 1f + healthPerWave * level;
        float speedBonus = speedPerWave * level;

        ActData act = actManager != null ? actManager.Current : null;
        if (act != null)
        {
            healthMul *= act.enemyHealthMultiplier;
            speedBonus += act.enemySpeedBonus;
        }

        e.ApplyActDifficulty(healthMul, speedBonus);

        // els tiradors disparen més sovint a les onades altes
        if (e.isShooter)
            e.fireInterval = Mathf.Max(0.7f, e.fireInterval * Mathf.Max(0.55f, 1f - 0.015f * currentWave));
    }

    void SpawnBoss(BossInfo info)
    {
        if (player == null || info.prefab == null) return;

        GameObject b = Instantiate(info.prefab, RandomSpawnPosition(), Quaternion.identity);
        Enemy e = b.GetComponent<Enemy>();
        currentBoss = e;

        if (e != null)
        {
            e.isBoss = true; // perquè els impactes als caps tinguin més "suc"

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
                // el Crític fa servir el cop de safata i la ràfega del prefab, però més de pressa
                if (attack != null)
                {
                    attack.fireInterval = 1.8f;
                    attack.phase2Interval = 1.1f;
                }
                if (melee != null)
                {
                    melee.cooldown = 2.4f;
                    melee.windupTime = 0.6f;
                }
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

    // MODE DE PROVA: salta a l'onada 'wave' (la propera que comença). Torna false si ara no es pot.
    public bool DebugJumpToWave(int wave)
    {
        if (!inWave || Time.timeScale == 0f) return false; // enmig d'una pantalla pausada o entre onades

        debugTarget = Mathf.Max(1, wave);
        if (finalWave > 0) debugTarget = Mathf.Min(debugTarget, finalWave);
        debugAbort = true;
        spawnGeneration++; // els clients avisats que encara no han sortit ja no sortiran

        // fora tots els clients (sense punts ni plats: és un salt de prova)
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e != null) Destroy(e.gameObject);
        return true;
    }

    // MODE DE PROVA: fa sortir un client especial de cada tipus al voltant del jugador
    public bool DebugSpawnSpecials()
    {
        if (player == null || enemyTypes == null) return false;

        GameObject prefab = null;
        foreach (EnemyType t in enemyTypes)
        {
            Enemy e = t.prefab != null ? t.prefab.GetComponent<Enemy>() : null;
            if (e != null && !e.isShooter) { prefab = t.prefab; break; }
        }
        if (prefab == null) return false;

        System.Array kinds = System.Enum.GetValues(typeof(EnemyVariants.Kind));
        for (int i = 0; i < kinds.Length; i++)
        {
            float a = i * Mathf.PI * 2f / kinds.Length;
            Vector2 pos = (Vector2)player.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 5f;
            Vector2 c = HazardManager.ArenaCenter;
            Vector2 half = HazardManager.ArenaSize * 0.5f - Vector2.one * 2f;
            pos.x = Mathf.Clamp(pos.x, c.x - half.x, c.x + half.x);
            pos.y = Mathf.Clamp(pos.y, c.y - half.y, c.y + half.y);

            Enemy e = Instantiate(prefab, pos, Quaternion.identity).GetComponent<Enemy>();
            ApplyWaveDifficulty(e);
            EnemyVariants.Force(e, prefab, (EnemyVariants.Kind)kinds.GetValue(i));
        }
        return true;
    }

    // els caps poden fer sortir clients normals (el Xef i la Inspectora)
    public void SpawnMinions(Vector2 around, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickEnemy();
            if (prefab == null) return;

            Vector2 pos = around + Random.insideUnitCircle.normalized * 2.5f;
            SpawnWithWarning(prefab, pos);
        }
    }
}