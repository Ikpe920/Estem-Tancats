using System;
using UnityEngine;

// =====================================================================
//  EL REPTE DEL DIA
// =====================================================================
// Una partida curta d'El Servei (15 onades: 3 sales i 3 caps) amb una regla especial que canvia cada
// dia (la mateixa per a tothom: surt de la data). Té el seu rècord de punts del dia.
// Superar-lo dona propines (només el primer cop de cada dia) i compta per a les medalles.
public static class DailyChallenge
{
    public enum Rule { NomesRars, UnSolCor, ClientsRapids, TotEspecial, SenseDash, PaellaCalenta, FestaElit }
    public const int Waves = 15;

    public static bool Active;

    public static int DateKey { get { DateTime d = DateTime.Now; return d.Year * 10000 + d.Month * 100 + d.Day; } }
    public static Rule Today { get { return (Rule)(Mathf.Abs(DateKey * 7919) % Enum.GetValues(typeof(Rule)).Length); } }
    public static int Seed => DateKey;

    public static string Name(Rule r)
    {
        switch (r)
        {
            case Rule.NomesRars: return "NOMÉS PLATS RARS";
            case Rule.UnSolCor: return "UN SOL COR";
            case Rule.ClientsRapids: return "CLIENTS AMB PRESSA";
            case Rule.TotEspecial: return "TOT ÉS ESPECIAL";
            case Rule.SenseDash: return "SENSE DASH";
            case Rule.PaellaCalenta: return "PAELLA CALENTA";
            case Rule.FestaElit: return "FESTA D'ELIT";
        }
        return "";
    }

    public static string Desc(Rule r)
    {
        switch (r)
        {
            case Rule.NomesRars: return "Només surten plats rars i llegendaris";
            case Rule.UnSolCor: return "Tens un sol cor (però el premi és el doble)";
            case Rule.ClientsRapids: return "Els clients van un 30% més ràpids";
            case Rule.TotEspecial: return "Cada onada té un esdeveniment";
            case Rule.SenseDash: return "No pots fer el dash";
            case Rule.PaellaCalenta: return "La paella s'escalfa el doble de ràpid";
            case Rule.FestaElit: return "Molts més clients d'elit";
        }
        return "";
    }

    public static bool Is(Rule r) { return Active && Today == r; }

    static string BestKey => "daily_best_" + DateKey;
    public static int BestToday => PlayerPrefs.GetInt(BestKey, 0);
    public static bool DoneToday => PlayerPrefs.GetInt("daily_done_" + DateKey, 0) == 1;

    public static void Begin() { Active = true; }

    // en començar la partida (des del WaveSpawner): la llavor del dia i la regla
    public static void Apply(WaveSpawner spawner)
    {
        UnityEngine.Random.InitState(Seed);
        if (spawner != null) spawner.finalWave = Waves;
        PlayerHealth h = PlayerRef.Health;
        PlayerMovement m = PlayerRef.Movement;
        PlayerShooting s = PlayerRef.Shooting;
        switch (Today)
        {
            case Rule.UnSolCor: if (h != null) h.SetMaxLives(1); break;
            case Rule.ClientsRapids: Enemy.speedMultiplier *= 1.3f; break;
            case Rule.SenseDash: if (m != null) m.dashCooldown = 99999f; break;
            case Rule.PaellaCalenta: if (s != null) s.heatPerSecond *= 2f; break;
        }
    }

    // en acabar (guanyant o perdent): el rècord del dia, i les propines si l'has superat
    public static void Finish(bool won, int points)
    {
        if (points > BestToday) PlayerPrefs.SetInt(BestKey, points);
        if (won && !DoneToday)
        {
            PlayerPrefs.SetInt("daily_done_" + DateKey, 1);
            Progression.AddTips(Today == Rule.UnSolCor ? 120 : 60);
            Achievements.Unlock("daily1");
            Achievements.Add("daily", 1, 5, "daily5");
        }
        PlayerPrefs.Save();
    }

    public static string RecordLine()
    {
        string s = DoneToday ? "Superat avui!" : "Encara no l'has superat avui";
        if (BestToday > 0) s += " · rècord: " + BestToday + " punts";
        return s;
    }
}
