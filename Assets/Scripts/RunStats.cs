using UnityEngine;

// dades de la partida que han de sobreviure al canvi d'escena (Game Over i victòria)
public static class RunStats
{
    public static int wave;   // l'onada on s'ha acabat la partida
    public static int points; // els punts totals de la partida

    // es crida just abans de carregar la pantalla final
    public static void Save(int finalWave, int totalPoints)
    {
        wave = finalWave;
        points = totalPoints;
    }

    // es crida en començar una partida nova
    public static void Reset()
    {
        wave = 0;
        points = 0;
    }
}
