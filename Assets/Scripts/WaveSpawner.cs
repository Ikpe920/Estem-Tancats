using System.Collections.Generic;
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
    Inspector, // espiral de trets i fase enfadada
    TvCritic,  // la Crítica de la tele (onada 40): focus que enlluernen i públic que aplaudeix
    Sommelier, // el Sommelier (onada 30): ampolles que cauen on ets, tolls de vi i cercles de taps
    Gelater,   // el Gelater boig (onada 35): ventalls de gelat, cercles de gel i espiral
    Ghost      // el Fantasma del Restaurant (secret): surt després de l'onada 40 si trobes prou receptes perdudes
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

    // la versió de les hores extres: mateix cap, però enfurismat (nom, frase i color)
    public BossInfo Furious()
    {
        BossInfo b = (BossInfo)MemberwiseClone();
        bool fem = name != null && name.StartsWith("LA ");
        b.name = (name ?? "") + (fem ? " ENFURISMADA" : " ENFURISMAT");
        b.subtitle = "Ha tornat. I ara sí que està de mal humor.";
        b.tint = new Color(tint.r * 1f, tint.g * 0.62f, tint.b * 0.58f, tint.a);
        return b;
    }
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
    public BoonManager boonManager; // els plats (per experiència) i el cofre dels caps

    [Header("Final del joc")]
    public int finalWave = 40; // en completar aquesta onada, guanyes (0 = sense final)

    [Header("Avís de sortida")]
    public float spawnWarningTime = 0.6f; // segons que es veu el cercle vermell abans que surti el client (0 = sense avís)

    [Header("Onades encallades")]
    public float stuckCheckInterval = 1f; // cada quant es miren els clients que han sortit de l'arena
    public float stuckWaveTimeout = 45f;  // segons sense cap baixa (amb el cap ja mort) abans de treure els que queden (0 = mai)

    public int currentWave = 0;

    // mode infinit: després de l'última onada, el jugador ha triat continuar
    public bool endless;

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
        AddNewBosses();
    }

    // els caps de l'escena són 5 (onades 5 a 25); aquí s'afegeixen els de les onades 30, 35 i 40
    void AddNewBosses()
    {
        if (bosses == null || bosses.Length == 0 || bosses.Length >= 8) return;
        BossInfo template = bosses[0];
        GameObject shot = null;
        foreach (BossInfo b in bosses) if (b != null && b.projectilePrefab != null) { shot = b.projectilePrefab; break; }

        List<BossInfo> list = new List<BossInfo>(bosses);
        list.Add(new BossInfo
        {
            name = "EL SOMMELIER", subtitle = "Té un vi per a cada plat. I un plat per a cada xef.",
            prefab = template.prefab, type = BossType.Sommelier, health = 2200, speed = 1.7f, scale = 3f,
            sprite = Resources.Load<Sprite>("Bosses/Sommelier"), tint = Color.white, projectilePrefab = shot,
        });
        list.Add(new BossInfo
        {
            name = "EL GELATER BOIG", subtitle = "Ve a servir-te una bola de gel. De les grosses.",
            prefab = template.prefab, type = BossType.Gelater, health = 2800, speed = 1.8f, scale = 3f,
            sprite = Resources.Load<Sprite>("Bosses/Gelater"), tint = Color.white, projectilePrefab = shot,
        });
        list.Add(TvCriticInfo(template));
        bosses = list.ToArray();
    }

    void Start()
    {
        HazardManager.EnsureExists(); // taules, caixes i portes, segur (abans de fer sortir cap client)

        // si el component Act Manager és al mateix objecte, el trobem sols
        if (actManager == null) actManager = GetComponent<ActManager>();
        if (boonManager == null) boonManager = GetComponent<BoonManager>();

        LostRecipes.ResetRun();

        // caps seguits: només els caps, contra rellotge
        if (BossRush.Active)
        {
            finalWave = BossRush.Bosses;
            BossRushHud.Create();
        }

        StartCoroutine(WaveLoop());
    }

    // l'onada que compta per a la vida i la velocitat dels clients (als caps seguits, la del cap de debò)
    int DifficultyWave => BossRush.Active ? currentWave * bossEveryNWaves : currentWave;

    IEnumerator WaveLoop()
    {
        // caps seguits: comences amb dos cofres (4 plats) per poder plantar cara al primer cap
        if (BossRush.Active && boonManager != null && boonManager.xpMode)
        {
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(boonManager.OpenChest("APERITIU!"));
            yield return StartCoroutine(boonManager.OpenChest("APERITIU!"));
        }

        while (true)
        {
            currentWave++;
            inWave = true;
            if (actManager != null) actManager.OnWaveStarted(currentWave); // el rellotge de les hores extres


            int count = startEnemies + (currentWave - 1) * enemiesPerWave;
            if (actManager != null) count += actManager.ExtraEnemies; // fent hores extres no torna a baixar

            bool isBossWave = bosses != null && bosses.Length > 0
                              && (BossRush.Active || currentWave % bossEveryNWaves == 0);

            // potser és una onada especial (hora punta, tall de llum, client VIP...)
            WaveEvents.Begin(WaveEvents.Roll(currentWave, isBossWave, actManager != null ? actManager.currentAct : 0), this);
            count = WaveEvents.AdjustCount(count);
            count = Mathf.Max(1, Mathf.RoundToInt(count * Progression.EnemyCountMul)); // dificultat

            // les receptes perdudes: una a cada sala, en alguna onada normal (El Servei)
            if (!isBossWave && !BossRush.Active && actManager != null)
                LostRecipes.MaybeSpawn(actManager.currentAct % LostRecipes.All.Length, (currentWave - 1) % bossEveryNWaves + 1);

            if (isBossWave)
            {
                AudioManager.SetBossMusic(true); // la música dels caps, fins que acabi l'onada
                // quins caps surten (al menú infernal, dos alhora)
                List<BossInfo> infos = new List<BossInfo>();
                if (BossRush.Active)
                    foreach (int bi in BossRush.BossesForPlate(currentWave))
                        infos.Add(bi == BossRush.GhostIndex ? GhostInfo() : bosses[bi % bosses.Length]);
                else infos.Add(bosses[(currentWave / bossEveryNWaves - 1) % bosses.Length]);

                int actNumber = (actManager != null ? actManager.currentAct : 0) + 1;
                string bossTag = BossRush.Active ? "— " + BossRush.Name(BossRush.Variant) + " · PLAT " + currentWave + " DE " + BossRush.Bosses + " —"
                               : endless && actManager != null ? "— HORES EXTRES · " + ActManager.Clock(actManager.Hour) + " · ONADA " + currentWave + " —" : null;
                for (int k = 0; k < infos.Count; k++)
                {
                    // fent hores extres (o al menú del xef) tornen enfurismats
                    if (endless || (BossRush.Active && BossRush.Furious)) infos[k] = infos[k].Furious();
                    // la presentació del cap (estil "versus"): retrat, nom, frase i un consell
                    yield return StartCoroutine(BossIntroScreen.Play(infos[k], PortraitOf(infos[k]), actNumber, currentWave, bossTag));
                }
                foreach (BossInfo info in infos)
                {
                    SpawnBoss(info);
                    // dos caps alhora: cadascun amb menys vida
                    if (infos.Count > 1 && currentBoss != null)
                        currentBoss.OverrideStats(Mathf.RoundToInt(currentBoss.maxHealth * 0.65f), currentBoss.speed);
                }
                count = 0; // a l'onada del cap només hi ha el cap (i els clients que faci sortir ell)
            }

            for (int i = 0; i < count; i++)
            {
                if (debugAbort) break;
                SpawnEnemy();
                // les onades altes surten en un degoteig més ràpid
                yield return new WaitForSeconds(WaveEvents.AdjustInterval(Mathf.Max(0.15f, spawnInterval - 0.012f * (currentWave - 1))));
            }

            yield return StartCoroutine(WaitForWaveCleared());
            BossBrain.ClearBattlefield(); // cap tret ni toll de l'onada passa a la següent

            inWave = false;
            WaveEvents.End();
            AudioManager.SetBossMusic(false);

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
            if (BossRush.Active)
            {
                BossRush.Beaten++;
                if (PlayerRef.Health != null) PlayerRef.Health.Heal(1); // cada cap vençut, un cor
            }
            else Progression.OnWaveCleared(currentWave, finalWave, endless);

            // petit descans perquè el jugador pugui respirar abans de triar un plat
            if (actManager != null)
                yield return StartCoroutine(actManager.ShowRoundComplete(currentWave));
            yield return new WaitForSeconds(0.4f);

            // final del joc: en acabar l'última onada, guanyes... o continues en mode infinit
            if (BossRush.Active && currentWave >= finalWave)
            {
                BossRush.Finish(true);
                int rp = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
                int rc = ScoreManager.Instance != null ? ScoreManager.Instance.MaxCombo : 0;
                RunStats.Save(currentWave, rp, rc);
                Iris.LoadScene(SceneNames.Win);
                yield break;
            }
            // el cap secret: si en aquesta partida has trobat prou receptes perdudes, el Fantasma desperta
            if (!BossRush.Active && finalWave > 0 && currentWave >= finalWave && !endless && !ghostDone
                && LostRecipes.RunFound >= LostRecipes.GhostNeeds)
            {
                ghostDone = true;
                yield return StartCoroutine(GhostFight());
            }
            if (finalWave > 0 && currentWave >= finalWave && !endless)
            {
                bool keepGoing = false;
                yield return StartCoroutine(EndlessChoice.Ask(r => keepGoing = r));

                if (!keepGoing)
                {
                    // guardem el resultat i anem a la pantalla de victòria
                    int points = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
                    int combo = ScoreManager.Instance != null ? ScoreManager.Instance.MaxCombo : 0;
                    RunStats.Save(currentWave, points, combo);
                    Iris.LoadScene(SceneNames.Win);
                    yield break; // la partida s'acaba: no hi ha més onades
                }

                endless = true;
                RunStats.endless = true;
                if (actManager != null) actManager.StartOvertime(currentWave + 1); // comencen les hores extres
            }

            // el cap deixa un cofre: dos plats de regal (abans de canviar de sala)
            if (isBossWave && boonManager != null && boonManager.xpMode)
                yield return StartCoroutine(boonManager.OpenChest());

            // canvi d'escenari: quan l'onada del cap acaba, passem a la sala següent
            if (isBossWave && actManager != null)
                yield return StartCoroutine(actManager.TransitionToNextAct());

            // (si els plats no van per experiència: 3 opcions a triar entre onades, com abans)
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

                bool bossAlive = AnyBossAlive();
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
            if (e == currentBoss || e.isBoss) continue;
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
            wave = BossRush.Active ? bossEveryNWaves : currentWave - actManager.currentAct * bossEveryNWaves;
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
        EnemyVariants.MaybeApply(e, prefab, DifficultyWave);
        if (actManager != null) RoomClient.Apply(e, actManager.currentAct % 8); // els clients del celler, la cambra i el plató
        EliteClient.MaybeApply(e, currentWave); // de tant en tant, un client d'elit amb corona
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

        int level = Mathf.Max(0, DifficultyWave - 1);
        float healthMul = 1f + healthPerWave * level;
        float speedBonus = speedPerWave * level;

        if (actManager != null)
        {
            healthMul *= actManager.HealthMultiplier;
            speedBonus += actManager.SpeedBonus;
        }

        healthMul *= Progression.EnemyHealthMul; // dificultat triada
        e.ApplyActDifficulty(healthMul, speedBonus);
        e.speed *= Progression.EnemySpeedMul;

        // els tiradors disparen més sovint a les onades altes
        if (e.isShooter)
            e.fireInterval = Mathf.Max(0.7f, e.fireInterval * Mathf.Max(0.55f, 1f - 0.015f * DifficultyWave));
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

        // dificultat triada
        if (e != null && !Mathf.Approximately(Progression.EnemyHealthMul, 1f))
            e.OverrideStats(Mathf.RoundToInt(e.maxHealth * Progression.EnemyHealthMul), e.speed);

        // mode infinit: a cada volta (cada 25 onades) els caps tornen més forts
        int loop = finalWave > 0 ? (currentWave - 1) / finalWave : 0;
        if (e != null && loop > 0)
            e.OverrideStats(Mathf.RoundToInt(e.maxHealth * (1f + 0.75f * loop)), e.speed * (1f + 0.08f * loop));

        // ara els caps van sols (sense clients): són més durs
        if (e != null) e.OverrideStats(Mathf.RoundToInt(e.maxHealth * 1.4f), e.speed);

        // cada cap té el seu comportament
        ConfigureBoss(b, e, info);

        if (bossManager != null && e != null)
            bossManager.TrackBoss(e, info.name, BossIntroScreen.ColorFor(info.type));
    }

    // cada cap té el seu cervell (BossBrain.cs): atacs propis, 2a fase i punt feble
    void ConfigureBoss(GameObject b, Enemy e, BossInfo info)
    {
        if (e == null) return;

        BossMelee melee = b.GetComponent<BossMelee>();
        BossAttack attack = b.GetComponent<BossAttack>();
        if (melee != null) melee.enabled = false;
        if (attack != null) attack.enabled = false;

        BossBrain brain = null;
        switch (info.type)
        {
            case BossType.Critic: brain = b.AddComponent<BossCriticBrain>(); break;
            case BossType.Borratxo: brain = b.AddComponent<BossDrunkBrain>(); break;
            case BossType.Cuiner: brain = b.AddComponent<BossChefBrain>(); break;
            case BossType.Pallas: brain = b.AddComponent<BossVipBrain>(); break;
            case BossType.Inspector: brain = b.AddComponent<BossInspectorBrain>(); break;
            case BossType.Sommelier: brain = b.AddComponent<BossSommelierBrain>(); break;
            case BossType.Gelater: brain = b.AddComponent<BossGelaterBrain>(); break;
            case BossType.TvCritic: brain = b.AddComponent<BossTvBrain>(); break;
            case BossType.Ghost: brain = b.AddComponent<BossGhostBrain>(); break;
        }
        if (brain != null)
        {
            brain.shotPrefab = info.projectilePrefab != null ? info.projectilePrefab : ShotPrefab();
            brain.spawner = this;
        }
        e.weakness = DamageKind.WeaknessOf(info.type);
    }

    // el tret dels caps (el primer que tingui algun cap de l'escena)
    public GameObject ShotPrefab()
    {
        if (bosses != null) foreach (BossInfo b in bosses) if (b != null && b.projectilePrefab != null) return b.projectilePrefab;
        if (enemyTypes != null) foreach (EnemyType t in enemyTypes)
            {
                Enemy en = t.prefab != null ? t.prefab.GetComponent<Enemy>() : null;
                if (en != null && en.shotPrefab != null) return en.shotPrefab;
            }
        return null;
    }
    // MODE DE PROVA: salta a l'onada 'wave' (la propera que comença). Torna false si ara no es pot.
    public bool DebugJumpToWave(int wave)
    {
        if (!inWave || Time.timeScale == 0f) return false; // enmig d'una pantalla pausada o entre onades

        debugTarget = Mathf.Max(1, wave);
        if (finalWave > 0 && !endless) debugTarget = Mathf.Min(debugTarget, finalWave);
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

    bool ghostDone;

    static bool AnyBossAlive()
    {
        foreach (Enemy e in Enemy.All) if (e != null && e.isBoss && !e.IsDead) return true;
        return false;
    }

    Sprite PortraitOf(BossInfo info)
    {
        Sprite portrait = info.sprite;
        if (portrait == null && info.prefab != null)
        {
            SpriteRenderer psr = info.prefab.GetComponentInChildren<SpriteRenderer>();
            if (psr != null) portrait = psr.sprite;
        }
        return portrait;
    }

    // EL FANTASMA DEL RESTAURANT: el cap secret
    BossInfo GhostInfo()
    {
        return new BossInfo
        {
            name = "EL FANTASMA DEL RESTAURANT",
            subtitle = "Les receptes perdudes l'han despertat. I té gana de servei.",
            prefab = bosses[0].prefab,
            type = BossType.Ghost,
            health = 4000,
            speed = 2f,
            scale = 3f,
            sprite = Resources.Load<Sprite>("Bosses/Fantasma"),
            tint = Color.white,
            projectilePrefab = bosses[0].projectilePrefab,
        };
    }

    // el combat secret, després de l'onada 40 (abans de triar si continues)
    IEnumerator GhostFight()
    {
        yield return new WaitForSeconds(1f);
        inWave = true;
        AudioManager.SetBossMusic(true);
        BossInfo ghost = GhostInfo();
        int actNumber = (actManager != null ? actManager.currentAct : 0) + 1;
        yield return StartCoroutine(BossIntroScreen.Play(ghost, ghost.sprite, actNumber, currentWave, "— CAP SECRET · LES RECEPTES L'HAN DESPERTAT —"));
        SpawnBoss(ghost);
        yield return StartCoroutine(WaitForWaveCleared());
        inWave = false;
        AudioManager.SetBossMusic(false);
        if (!PlayerHealth.IsDying)
        {
            PlayerPrefs.SetInt("ghost_beaten", 1);
            PlayerPrefs.Save();
            Stats.Add("ghosts");
            if (boonManager != null && boonManager.xpMode) yield return StartCoroutine(boonManager.OpenChest());
        }
    }

    // la Crítica de la tele: es fa amb el prefab d'un altre cap, amb el seu dibuix i el seu comportament
    BossInfo TvCriticInfo(BossInfo template)
    {
        return new BossInfo
        {
            name = "LA CRÍTICA DE LA TELE",
            subtitle = "En directe des del teu restaurant... i en directe del teu final.",
            prefab = template.prefab,
            type = BossType.TvCritic,
            health = 3400, // és el cap final de la partida
            speed = 1.8f,
            scale = 3f,
            sprite = Resources.Load<Sprite>("Bosses/CriticaTV"),
            tint = Color.white,
            projectilePrefab = template.projectilePrefab,
        };
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