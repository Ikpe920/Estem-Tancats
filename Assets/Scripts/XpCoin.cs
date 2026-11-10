using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  EXPERIÈNCIA: les monedes de propina que deixen els clients
// =====================================================================
// Com les gemmes de Vampire Survivors: cada client despatxat deixa monedes a terra. Si passes a prop,
// l'imant les atrau i, en arribar-te, sumen experiència (BoonManager.AddXp). Quan la barra s'omple,
// puges de nivell i tries un plat. En acabar cada onada, les que queden a terra venen totes soles.
public class XpCoin : MonoBehaviour
{
    public static float MagnetMul = 1f;          // Coca de llardons i el Rebost
    const float BaseMagnet = 1.8f;
    const int MaxCoins = 250;                    // si n'hi ha massa a terra, les més velles se sumen soles

    static readonly List<XpCoin> all = new List<XpCoin>();
    static Sprite sprite;

    private int value;
    private bool attracted;
    private float speed;
    private float bornAt;
    private Vector3 baseScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        all.Clear();
        MagnetMul = 1f;
        Enemy.OnKilled -= OnKilled;
        Enemy.OnKilled += OnKilled;
        ScoreManager.OnWaveEnded -= OnWaveEnded;
        ScoreManager.OnWaveEnded += OnWaveEnded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) { all.Clear(); MagnetMul = 1f + 0.15f * PowerUps.Level("imant"); }

    // quanta experiència dona cada client
    static void OnKilled(Enemy e)
    {
        if (e == null || BoonManager.Instance == null) return;
        string key = Codex.ClientKey(e);
        int total;
        if (e.isBoss) total = 30;
        else if (key == "EnemyTank") total = 3;
        else if (key == "Enemy" || key == "EnemyFast" || key == "EnemyShooter") total = 1;
        else total = 2; // els especials

        Vector2 pos = e.transform.position;
        // els caps escampen moltes monedes (de 3 en 3)
        while (total > 0)
        {
            int v = Mathf.Min(total, e.isBoss ? 3 : total);
            Spawn(pos + (e.isBoss ? Random.insideUnitCircle * 1.5f : Random.insideUnitCircle * 0.3f), v);
            total -= v;
        }
    }

    static void OnWaveEnded(bool flawless)
    {
        foreach (XpCoin c in all) if (c != null) c.attracted = true;
    }

    public static void Spawn(Vector2 pos, int value)
    {
        if (sprite == null) sprite = PropArt.Coin();
        if (all.Count >= MaxCoins && all[0] != null) all[0].Collect(); // la més vella se suma sola

        GameObject go = new GameObject("Moneda");
        go.transform.position = pos;
        float size = value >= 3 ? 0.42f : value == 2 ? 0.34f : 0.28f;
        go.transform.localScale = Vector3.one * size;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2;
        sr.color = value >= 3 ? new Color(1f, 0.75f, 0.35f) : Color.white;
        XpCoin c = go.AddComponent<XpCoin>();
        c.value = value;
        c.bornAt = Time.time;
        c.baseScale = go.transform.localScale;
        all.Add(c);
    }

    void Update()
    {
        Transform player = PlayerRef.Transform;
        if (player == null) return;
        Vector2 to = (Vector2)player.position - (Vector2)transform.position;
        float d = to.magnitude;

        if (!attracted && d < BaseMagnet * MagnetMul) attracted = true;
        if (attracted)
        {
            speed = Mathf.Min(speed + 30f * Time.deltaTime, 18f);
            transform.position += (Vector3)(to.normalized * Mathf.Min(d, speed * Time.deltaTime));
            if (d < 0.45f) { Collect(); return; }
        }
        else
        {
            // brilla i bota una mica a terra
            float k = 1f + 0.08f * Mathf.Sin((Time.time - bornAt) * 6f);
            transform.localScale = baseScale * k;
        }
    }

    void Collect()
    {
        all.Remove(this);
        if (BoonManager.Instance != null) BoonManager.Instance.AddXp(value);
        AudioManager.Play("coin", 0.35f, 1f + Mathf.Min(0.5f, value * 0.08f));
        Destroy(gameObject);
    }

    void OnDestroy() { all.Remove(this); }
}
