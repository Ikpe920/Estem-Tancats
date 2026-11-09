using UnityEngine;

// dades de la partida que han de sobreviure al canvi d'escena (Game Over i victòria),
// i el rècord, que es guarda al dispositiu
public static class RunStats
{
    const string BestPointsKey = "best_points";
    const string BestWaveKey = "best_wave";

    public static int wave;        // l'onada on s'ha acabat la partida
    public static int points;      // els punts totals de la partida
    public static int maxCombo;    // la ratxa més llarga de la partida
    public static bool newRecord;  // aquesta partida ha batut el rècord de punts
    public static bool endless;    // la partida ha continuat en mode infinit

    public static int BestPoints { get { return PlayerPrefs.GetInt(BestPointsKey, 0); } }
    public static int BestWave { get { return PlayerPrefs.GetInt(BestWaveKey, 0); } }

    // es crida just abans de carregar la pantalla final
    public static void Save(int finalWave, int totalPoints, int longestCombo = 0)
    {
        wave = finalWave;
        points = totalPoints;
        maxCombo = longestCombo;

        Progression.EndRun(totalPoints, finalWave); // propines guanyades en aquesta partida

        newRecord = totalPoints > BestPoints;
        if (newRecord) PlayerPrefs.SetInt(BestPointsKey, totalPoints);
        if (finalWave > BestWave) PlayerPrefs.SetInt(BestWaveKey, finalWave);
        PlayerPrefs.Save();
    }

    // es crida en començar una partida nova
    public static void Reset()
    {
        wave = 0;
        points = 0;
        maxCombo = 0;
        newRecord = false;
        endless = false;
    }

    // les línies de sota els punts a les pantalles finals
    public static string ExtraLines()
    {
        string s = "\nMillor ratxa: " + maxCombo;
        s += "\n<color=#8FE39B>+" + Progression.LastRunTips + " propines</color> (en tens " + Progression.Tips + ")";
        s += newRecord
            ? "\n<color=#FFD24D>NOU RÈCORD!</color>"
            : "\nRècord: " + BestPoints;
        return s;
    }
}
