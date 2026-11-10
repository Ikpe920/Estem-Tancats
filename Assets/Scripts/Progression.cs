using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  PROGRÉS ENTRE PARTIDES: propines, xefs desbloquejats, dificultat i reptes
// =====================================================================
// Tot es guarda al dispositiu (PlayerPrefs).
//  - En acabar una partida guanyes propines segons els punts i l'onada (× la dificultat).
//  - Les propines serveixen per desbloquejar xefs (pantalla de triar xef).
//  - Els reptes donen propines extra la primera vegada que els completes.
public enum Difficulty { Facil, Normal, Dificil }

public static class Progression
{
    const string TipsKey = "tips";
    const string DifficultyKey = "difficulty";
    const string KillsKey = "kills_total";
    const string VipKey = "vip_total";

    // ---------- PROPINES ----------

    public static int Tips
    {
        get { return PlayerPrefs.GetInt(TipsKey, 0); }
        private set { PlayerPrefs.SetInt(TipsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static void AddTips(int n) { if (n > 0) Tips = Tips + n; }

    public static int LastRunTips { get; private set; } // les de l'última partida (per a la pantalla final)

    // ---------- XEFS ----------

    public static int ChefPrice(int index)
    {
        switch (index)
        {
            case 1: return 150; // Carnisser
            case 2: return 250; // Pastisser
            case 3: return 400; // Sushiman
            case 4: return 550; // Pizzer
        }
        return 0;               // el Xef, sempre obert
    }

    public static bool IsChefUnlocked(int index)
    {
        return ChefPrice(index) == 0 || PlayerPrefs.GetInt("chef_unlocked_" + index, 0) == 1;
    }

    // torna true si l'ha pogut comprar
    public static bool BuyChef(int index)
    {
        if (IsChefUnlocked(index)) return true;
        int price = ChefPrice(index);
        if (Tips < price) return false;
        Tips = Tips - price;
        PlayerPrefs.SetInt("chef_unlocked_" + index, 1);
        PlayerPrefs.Save();
        return true;
    }

    // ---------- DIFICULTAT ----------

    public static Difficulty Difficulty
    {
        get { return (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, 1), 0, 2); }
        set { PlayerPrefs.SetInt(DifficultyKey, (int)value); PlayerPrefs.Save(); }
    }

    public static string DifficultyName(Difficulty d)
    {
        return d == Difficulty.Facil ? "FÀCIL" : d == Difficulty.Dificil ? "DIFÍCIL" : "NORMAL";
    }

    public static string DifficultyHint(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Facil: return "+1 cor, clients més fluixos · ×0,75 propines";
            case Difficulty.Dificil: return "Més clients, més forts i més ràpids · ×1,5 propines";
        }
        return "Com sempre";
    }

    public static float EnemyHealthMul => Difficulty == Difficulty.Facil ? 0.75f : Difficulty == Difficulty.Dificil ? 1.35f : 1f;
    public static float EnemySpeedMul => Difficulty == Difficulty.Facil ? 0.9f : Difficulty == Difficulty.Dificil ? 1.12f : 1f;
    public static float EnemyCountMul => Difficulty == Difficulty.Dificil ? 1.25f : 1f;
    public static int ExtraHearts => Difficulty == Difficulty.Facil ? 1 : 0;
    public static float TipsMul => Difficulty == Difficulty.Facil ? 0.75f : Difficulty == Difficulty.Dificil ? 1.5f : 1f;

    // ---------- REPTES ----------

    public class Challenge
    {
        public string id, name, desc;
        public int reward;
        public int goal;                 // per als reptes acumulats (0 = no se'n mostra el progrés)
        public System.Func<int> progress; // quant en portes (per a la llista)
    }

    public static readonly Challenge[] Challenges =
    {
        new Challenge { id = "wave5",      name = "Primer servei",      desc = "Arriba a l'onada 5",                         reward = 50 },
        new Challenge { id = "sala_clean", name = "Sala impecable",     desc = "Acaba la Sala (onada 5) sense rebre cap cop", reward = 120 },
        new Challenge { id = "combo30",    name = "En ratxa",           desc = "Fes una ratxa de 30 clients",                reward = 75 },
        new Challenge { id = "combo60",    name = "Imparable",          desc = "Fes una ratxa de 60 clients",                reward = 150 },
        new Challenge { id = "sets3",      name = "Menú complet",       desc = "Completa 3 conjunts en una partida",         reward = 100 },
        new Challenge { id = "vip3",       name = "Relacions públiques",desc = "Atrapa 3 clients VIP",                       reward = 100, goal = 3, progress = () => PlayerPrefs.GetInt(VipKey, 0) },
        new Challenge { id = "kills1000",  name = "Mil cobraments",     desc = "Despatxa 1000 clients (entre totes les partides)", reward = 150, goal = 1000, progress = () => PlayerPrefs.GetInt(KillsKey, 0) },
        new Challenge { id = "win",        name = "Servei acabat",      desc = "Supera les 25 onades",                       reward = 200 },
        new Challenge { id = "win_hard",   name = "Estrella Michelin",  desc = "Supera les 25 onades en DIFÍCIL",            reward = 400 },
        new Challenge { id = "endless35",  name = "Hores extres",       desc = "Arriba a l'onada 35 en mode infinit",        reward = 250 },
    };

    public static bool IsDone(Challenge c) { return PlayerPrefs.GetInt("challenge_" + c.id, 0) == 1; }
    public static int DoneCount()
    {
        int n = 0;
        foreach (Challenge c in Challenges) if (IsDone(c)) n++;
        return n;
    }

    static void Complete(string id)
    {
        if (PlayerHealth.GodMode) return; // amb el mode invencible de prova no val
        foreach (Challenge c in Challenges)
        {
            if (c.id != id || IsDone(c)) continue;
            PlayerPrefs.SetInt("challenge_" + id, 1);
            AddTips(c.reward);
            PlayerPrefs.Save();

            Transform p = PlayerRef.Transform;
            Vector2 at = p != null ? (Vector2)p.position + Vector2.up * 2.6f : Vector2.zero;
            AudioManager.Play("challenge");
            HazardFx.Popup(at, "REPTE COMPLETAT: " + c.name + "\n<size=70%>+" + c.reward + " propines</size>",
                new Color(0.55f, 1f, 0.6f), 2.8f);
            return;
        }
    }

    // ---------- SEGUIMENT DE LA PARTIDA ----------

    static bool hitThisRun;
    static int setsThisRun;
    static bool inGame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Enemy.OnKilled -= OnEnemyKilled;
        Enemy.OnKilled += OnEnemyKilled;
    }

    // si alguna cosa falla aquí, que no aturi els altres scripts que esperen l'escena (Unity els crida
    // un darrere l'altre i un error talla la resta: per exemple, el que crea les taules i les caixes)
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try { OnSceneLoadedSafe(scene, mode); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    static void OnSceneLoadedSafe(Scene scene, LoadSceneMode mode)
    {
        inGame = scene.name == SceneNames.Game;
        if (!inGame) return;
        hitThisRun = false;
        setsThisRun = 0;
        LastRunTips = 0;
    }

    static void OnEnemyKilled(Enemy e)
    {
        if (!inGame) return;
        int total = PlayerPrefs.GetInt(KillsKey, 0) + 1;
        PlayerPrefs.SetInt(KillsKey, total); // es desa amb el proper PlayerPrefs.Save()
        if (total >= 1000) Complete("kills1000");
    }

    public static void OnPlayerHit() { hitThisRun = true; }

    public static void OnCombo(int combo)
    {
        if (combo >= 30) Complete("combo30");
        if (combo >= 60) Complete("combo60");
    }

    public static void OnSetCompleted()
    {
        setsThisRun++;
        if (setsThisRun >= 3) Complete("sets3");
    }

    public static void OnVipCaught()
    {
        int n = PlayerPrefs.GetInt(VipKey, 0) + 1;
        PlayerPrefs.SetInt(VipKey, n);
        PlayerPrefs.Save();
        if (n >= 3) Complete("vip3");
    }

    public static void OnWaveCleared(int wave, int finalWave, bool endless)
    {
        if (wave >= 5) Complete("wave5");
        if (wave == 5 && !hitThisRun) Complete("sala_clean");
        if (finalWave > 0 && wave >= finalWave)
        {
            Complete("win");
            if (Difficulty == Difficulty.Dificil) Complete("win_hard");
        }
        if (endless && wave >= 35) Complete("endless35");
        PlayerPrefs.Save();
    }

    // la partida s'acaba (mort o victòria): les propines guanyades
    public static int EndRun(int points, int wave)
    {
        int tips = Mathf.RoundToInt((points / 25f + wave * 4f) * TipsMul);
        LastRunTips = tips;
        AddTips(tips);
        return tips;
    }
}
