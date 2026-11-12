using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  ESTADÍSTIQUES GLOBALS (es guarden al dispositiu; es veuen al Receptari)
// =====================================================================
// Comptadors que només pugen: partides, clients despatxats, caps vençuts, plats triats, morts,
// receptes, temps jugat... Es van sumant sols (aquí mateix i des d'altres scripts amb Stats.Add).
public static class Stats
{
    public static int Get(string key) { return PlayerPrefs.GetInt("stat_" + key, 0); }
    public static void Add(string key, int n = 1) { PlayerPrefs.SetInt("stat_" + key, Get(key) + n); }
    public static void Max(string key, int v) { if (v > Get(key)) PlayerPrefs.SetInt("stat_" + key, v); }

    public static float Seconds => PlayerPrefs.GetFloat("stat_seconds", 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Enemy.OnKilled -= OnKilled;
        Enemy.OnKilled += OnKilled;
        SceneManager.sceneLoaded -= OnScene;
        SceneManager.sceneLoaded += OnScene;
        if (Object.FindAnyObjectByType<StatsClock>() == null)
            Object.DontDestroyOnLoad(new GameObject("Estadistiques").AddComponent<StatsClock>().gameObject);
    }

    static void OnKilled(Enemy e)
    {
        if (e == null || SceneManager.GetActiveScene().name != SceneNames.Game) return;
        Add(e.isBoss ? "bosses" : "kills");
    }

    static void OnScene(Scene s, LoadSceneMode m)
    {
        if (s.name == SceneNames.Game) Add("runs");
        else if (s.name == SceneNames.GameOver) Add("deaths");
        else if (s.name == SceneNames.Win) Add("wins");
        PlayerPrefs.Save();
    }

    // les línies que surten al Receptari
    public static string Text()
    {
        int secs = Mathf.FloorToInt(Seconds);
        string time = (secs / 3600) + " h " + (secs / 60 % 60).ToString("00") + " min";
        return
            "<color=#FFCC4D>Partides jugades:</color> " + Get("runs") + "\n" +
            "<color=#FFCC4D>Victòries:</color> " + Get("wins") + "    <color=#FFCC4D>Derrotes:</color> " + Get("deaths") + "\n" +
            "<color=#FFCC4D>Clients despatxats:</color> " + Get("kills") + "\n" +
            "<color=#FFCC4D>Caps vençuts:</color> " + Get("bosses") + "\n" +
            "<color=#FFCC4D>Plats triats:</color> " + Get("dishes") + "\n" +
            "<color=#FFCC4D>Plats estrella cuinats:</color> " + Get("evos") + "\n" +
            "<color=#FFCC4D>Millor onada:</color> " + RunStats.BestWave + "    <color=#FFCC4D>Millor nivell de xef:</color> " + Get("best_level") + "\n" +
            "<color=#FFCC4D>Habilitats fetes servir:</color> " + Get("abilities") + "\n" +
            "<color=#FFCC4D>Receptes perdudes trobades:</color> " + Get("recipes") + "  (" + LostRecipes.FoundCount() + "/" + LostRecipes.All.Length + " diferents)\n" +
            "<color=#FFCC4D>Fantasmes vençuts:</color> " + Get("ghosts") + "\n" +
            "<color=#FFCC4D>Temps jugat:</color> " + time;
    }
}

// compta el temps jugat (només dins la partida i amb el joc en marxa)
public class StatsClock : MonoBehaviour
{
    float pending;

    void Update()
    {
        if (Time.timeScale <= 0f || SceneManager.GetActiveScene().name != SceneNames.Game) return;
        pending += Time.unscaledDeltaTime;
        if (pending < 5f) return;
        PlayerPrefs.SetFloat("stat_seconds", Stats.Seconds + pending);
        pending = 0f;
    }
}
