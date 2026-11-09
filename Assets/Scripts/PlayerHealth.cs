using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class PlayerHealth : MonoBehaviour
{
    public const int MaxLivesCap = 5;        // topall de cors (boons i botiga el comparteixen)
    public int maxLives = 3;                 // total de cors (vida) del jugador
    public bool invulnerableWhileDashing = true; // el dash esquiva els cops (es pot desactivar a l'Inspector)
    public float invulnerabilityTime = 1.2f; // temps invulnerable després de rebre un cop

    public static bool GodMode;              // només el posa el mode de prova (DebugMode)

    private int lives;
    private float nextDamageTime;
    private SpriteRenderer spriteRenderer;
    private PlayerMovement movement;
    private Color baseColor = Color.white;

    public int CurrentLives => lives;
    public event Action<int> OnLivesChanged; // s'avisa cada cop que els cors canvien

    void Awake()
    {
        lives = maxLives;
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    // cada cop que rep mal, perdi un cor (la quantitat de dany ja no compta)
    public void TakeDamage()
    {
        if (lives <= 0) return;
        if (GodMode) return;
        if (Time.time < nextDamageTime) return; // encara és invulnerable
        if (invulnerableWhileDashing && movement != null && movement.IsDashing) return;

        lives--;
        if (ScoreManager.Instance != null) ScoreManager.Instance.NotifyPlayerHit(); // talla la ratxa
        nextDamageTime = Time.time + invulnerabilityTime;
        OnLivesChanged?.Invoke(lives);
        CameraFX.Shake(0.18f, 0.16f);
        if (lives > 0) HitStop.Do(0.07f); // una mica de pausa quan et toquen

        if (lives <= 0)
        {
            // guardem el resultat per a la pantalla de Game Over
            WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
            int wave = spawner != null ? spawner.currentWave : 0;
            int points = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
            int combo = ScoreManager.Instance != null ? ScoreManager.Instance.MaxCombo : 0;
            RunStats.Save(wave, points, combo);
            SceneManager.LoadScene(SceneNames.GameOver);
        }
    }

    // cura un cor (la botiga en dona un per cop)
    public void Heal(int amount)
    {
        int newLives = Mathf.Min(lives + amount, maxLives);
        if (newLives == lives) return;
        lives = newLives;
        OnLivesChanged?.Invoke(lives);
    }

    void Update()
    {
        // parpelleja mentre és invulnerable, així es veu que el cop ja ha comptat
        if (spriteRenderer == null) return;

        bool invulnerable = lives > 0 && Time.time < nextDamageTime;
        if (!invulnerable)
        {
            spriteRenderer.color = baseColor;
            return;
        }

        float blink = Mathf.PingPong(Time.time * 8f, 1f);
        spriteRenderer.color = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.45f, baseColor.a), blink);
    }
}
