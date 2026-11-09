using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public const int MaxLivesCap = 5;        // topall de cors (boons i botiga el comparteixen)
    public int maxLives = 3;                 // total de cors (vida) del jugador
    public bool invulnerableWhileDashing = true; // el dash esquiva els cops (es pot desactivar a l'Inspector)
    public float invulnerabilityTime = 1.2f; // temps invulnerable després de rebre un cop

    public static bool GodMode;              // només el posa el mode de prova (DebugMode)
    public static bool IsDying { get; private set; } // la seqüència de mort està en marxa

    [Header("Mort")]
    public float deathSlowMotion = 0.25f;    // el temps del joc va a aquesta velocitat mentre caus
    public float deathHoldTime = 1.0f;       // segons (reals) de càmera lenta abans del fos
    public float deathFadeTime = 0.8f;       // segons (reals) del fos a negre

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
        IsDying = false;
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
            WaveSpawner spawner = WaveSpawner.Instance != null ? WaveSpawner.Instance : FindAnyObjectByType<WaveSpawner>();
            int wave = spawner != null ? spawner.currentWave : 0;
            int points = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
            int combo = ScoreManager.Instance != null ? ScoreManager.Instance.MaxCombo : 0;
            RunStats.Save(wave, points, combo);
            StartCoroutine(DeathSequence(spawner));
        }
    }

    // en lloc de saltar de cop a Game Over: càmera lenta, el xef cau i fos a negre
    IEnumerator DeathSequence(WaveSpawner spawner)
    {
        IsDying = true;

        // ja no hi ha més onades, ni plats, ni victòria
        if (spawner != null) spawner.StopAllCoroutines();

        // el xef deixa de moure's i de disparar, i ja no xoca amb res
        if (movement != null) movement.enabled = false;
        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        CameraFX.Shake(0.4f, 0.3f);
        Time.timeScale = deathSlowMotion;

        Image fade = CreateFadeOverlay();
        Vector3 startScale = transform.localScale;
        Color deathColor = new Color(1f, 0.35f, 0.35f, baseColor.a);

        float total = deathHoldTime + deathFadeTime;
        float t = 0f;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            // el xef es torna vermell, gira i s'encongeix
            float k = Mathf.Clamp01(t / total);
            if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(baseColor, deathColor, Mathf.Clamp01(t * 4f));
            transform.localScale = startScale * Mathf.Lerp(1f, 0.6f, k);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 90f, Mathf.SmoothStep(0f, 1f, k)));

            // fos a negre al final
            if (fade != null)
                fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01((t - deathHoldTime) / Mathf.Max(0.01f, deathFadeTime)));

            yield return null;
        }

        Time.timeScale = 1f;
        IsDying = false;
        SceneManager.LoadScene(SceneNames.GameOver);
    }

    // una imatge negra transparent per sobre de tot, que anirem enfosquint
    Image CreateFadeOverlay()
    {
        GameObject canvasGo = new GameObject("FosMort", typeof(Canvas));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        GameObject imgGo = new GameObject("Negre", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = imgGo.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        return img;
    }

    // cura un cor (la botiga en dona un per cop)
    public void Heal(int amount)
    {
        if (IsDying) return; // un plat o un conjunt no et pot reviure enmig de la caiguda
        int newLives = Mathf.Min(lives + amount, maxLives);
        if (newLives == lives) return;
        lives = newLives;
        OnLivesChanged?.Invoke(lives);
    }

    void OnDestroy()
    {
        if (IsDying) { IsDying = false; Time.timeScale = 1f; } // per si se surt de l'escena enmig de la mort
    }

    void Update()
    {
        // parpelleja mentre és invulnerable, així es veu que el cop ja ha comptat
        if (spriteRenderer == null) return;
        if (IsDying) return; // el color el porta la seqüència de mort

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
