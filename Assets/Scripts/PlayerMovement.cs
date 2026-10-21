using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Camera cam;
    private Vector2 input;
    private Vector2 knockbackVelocity;
    private float knockbackUntil;

    // estat del dash
    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 dashDirection;
    private Vector2 facing = Vector2.right; // ultima direccio on ha mirat el jugador
    private float afterImageTimer;

    public float dashSpeed = 16f;          // velocitat del dash
    public float dashDuration = 0.18f;     // durada del dash en segons
    public float dashCooldown = 1f;        // temps minim entre dos dashes
    public bool dashTowardsMouse = false;  // el dash va cap al ratoli en lloc de cap on es mou
    public bool showAfterImages = true;    // mostra les esteles del dash
    public Color afterImageColor = new Color(1f, 0.85f, 0.35f, 0.6f);
    public float afterImageInterval = 0.02f; // temps entre estela i estela
    public float afterImageLifetime = 0.25f; // durada de cada estela

    public bool IsDashing => isDashing;
    public bool CanDash => Time.time >= nextDashTime;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input = input.normalized; // perquè en diagonal no vagi més ràpid

        if (input != Vector2.zero) facing = input;

        UpdateDash();
    }
    public void Knockback(Vector2 direction, float force, float duration)
    {
        knockbackVelocity = direction.normalized * force;
        knockbackUntil = Time.time + duration;
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        if (Time.time < knockbackUntil)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }
        rb.linearVelocity = input * speed;
    }

    // ---------- DASH ----------

    void UpdateDash()
    {
        if (isDashing)
        {
            if (Time.time >= dashEndTime) isDashing = false;
            SpawnAfterImages();
            return;
        }

        if (Time.timeScale == 0f) return;     // no fer dash amb la botiga oberta
        if (Time.time < nextDashTime) return; // encara en cooldown
        if (!DashPressed()) return;

        dashDirection = GetDashDirection();
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
        knockbackUntil = 0f;                  // el dash cancel·la el knockback
        afterImageTimer = 0f;
        SpawnAfterImage();
    }

    bool DashPressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) return true;
        return false;
    }

    Vector2 GetDashDirection()
    {
        if (dashTowardsMouse && cam != null && Mouse.current != null)
        {
            Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 toMouse = (mouseWorld - (Vector2)transform.position).normalized;
            if (toMouse != Vector2.zero) return toMouse;
        }

        if (input != Vector2.zero) return input; // cap on es mou el jugador
        return facing;                            // si esta parat, cap on mira
    }

    void SpawnAfterImages()
    {
        afterImageTimer -= Time.deltaTime;
        if (afterImageTimer > 0f) return;

        afterImageTimer = afterImageInterval;
        SpawnAfterImage();
    }

    void SpawnAfterImage()
    {
        if (!showAfterImages || spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject ghost = new GameObject("DashAfterImage");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.lossyScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
        sr.color = afterImageColor;

        ghost.AddComponent<DashAfterImage>().Init(afterImageLifetime, afterImageColor);
    }
}