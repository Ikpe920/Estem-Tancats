using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;                 // total de cors (vida) del jugador
    public float invulnerabilityTime = 1.2f; // temps invulnerable després de rebre un cop

    private int lives;
    private float nextDamageTime;
    private SpriteRenderer spriteRenderer;
    private Color baseColor = Color.white;

    public int CurrentLives => lives;
    public event Action<int> OnLivesChanged; // s'avisa cada cop que els cors canvien

    void Awake()
    {
        lives = maxLives;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    void Start()
    {
        // assegurar CameraShaker
        if (CameraShaker.Instance == null)
        {
            GameObject g = new GameObject("CameraShaker");
            g.AddComponent<CameraShaker>();
        }
        CameraShaker.Instance.Shake(0.01f, 0.01f);
    }

    // cada cop que rep mal, perdi un cor (la quantitat de dany ja no compta)
    public void TakeDamage()
    {
        if (lives <= 0) return;
        if (Time.time < nextDamageTime) return; // encara és invulnerable

        lives--;
        nextDamageTime = Time.time + invulnerabilityTime;
        OnLivesChanged?.Invoke(lives);
        if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(0.18f, 0.16f);
        else if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.18f, 0.16f);
        else if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.18f, 0.16f);
        Debug.Log("Cors restants: " + lives);

        if (lives <= 0)
        {
            // guardem el resultat per a la pantalla de Game Over
            WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
            int wave = spawner != null ? spawner.currentWave : 0;
            int points = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
            RunStats.Save(wave, points);
            SceneManager.LoadScene("GameOver");
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
