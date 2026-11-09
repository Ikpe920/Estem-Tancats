using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    [HideInInspector] public Vector2 externalPush; // el vent
    [HideInInspector] public float slipUntil;      // terra relliscós fins aquest moment
    public float slipAcceleration = 7f;            // com més baix, més llisca

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
    private Vector2 facing = Vector2.down; // ultima direccio on ha mirat el jugador
    private float afterImageTimer;

    public float dashSpeed = 16f;          // velocitat del dash
    public float dashDuration = 0.18f;     // durada del dash en segons
    public float dashCooldown = 1f;        // temps minim entre dos dashes
    public bool dashTowardsMouse = false;  // el dash va cap al ratoli en lloc de cap on es mou
    public bool showAfterImages = true;    // mostra les esteles del dash
    public Color afterImageColor = new Color(1f, 0.85f, 0.35f, 0.6f);
    public float afterImageInterval = 0.02f; // temps entre estela i estela
    public float afterImageLifetime = 0.25f; // durada de cada estela
    public Sprite spriteUp;    // sprite del xef mirant amunt
    public Sprite spriteDown;  // sprite del xef mirant avall
    public Sprite spriteLeft;  // sprite del xef mirant a l'esquerra
    public Sprite spriteRight; // sprite del xef mirant a la dreta

    // Dash picant (conjunt): recupera una mica de recàrrega del dash
    public void ReduceDashCooldown(float seconds)
    {
        nextDashTime = Mathf.Max(Time.time, nextDashTime - seconds);
    }

    public bool IsDashing => isDashing;
    public bool CanDash => Time.time >= nextDashTime;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);

    void Awake()
    {
        if (GetComponent<WalkAnimator>() == null) gameObject.AddComponent<WalkAnimator>(); // animació de caminar
        rb = GetComponent<Rigidbody2D>();
        // la física va a 50 Hz i la pantalla a més: sense interpolació el jugador va a tirones
        if (rb != null) rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        spriteRenderer = GetComponent<SpriteRenderer>();
        cam = Camera.main;
        UpdateFacingSprite();
    }

    void Update()
    {
        input = ReadMoveInput().normalized; // perquè en diagonal no vagi més ràpid

        if (input != Vector2.zero && input != facing)
        {
            facing = input;
            UpdateFacingSprite();
        }

        UpdateDash();
    }
    // moviment amb el nou Input System: WASD, fletxes i joystick esquerre
    Vector2 ReadMoveInput()
    {
        Vector2 v = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v.y += 1f;
        }

        if (v == Vector2.zero && Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.magnitude > 0.2f) v = stick; // zona morta
        }

        return v;
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
        Vector2 target = input * speed + externalPush;

        if (Time.time < slipUntil)
        {
            // terra relliscós: la velocitat canvia a poc a poc (inèrcia)
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, slipAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = target;
        }
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
        AudioManager.Play("dash");
        Fx.Dust(transform.position);
        GamepadInput.Rumble(0.0f, 0.25f, 0.07f);
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

    // ---------- SPRITE SEGONS LA DIRECCIO ----------

    void UpdateFacingSprite()
    {
        if (spriteRenderer == null) return;

        // l'eix dominant decideix si mira amunt/avall o a l'esquerra/dreta
        Sprite target = null;
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
        {
            target = facing.x > 0f ? spriteRight : spriteLeft;
        }
        else
        {
            target = facing.y > 0f ? spriteUp : spriteDown;
        }

        if (target != null) spriteRenderer.sprite = target; // si no hi ha sprite assignat, deixa el que hi ha
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

        DashAfterImage.Spawn(spriteRenderer, afterImageColor, afterImageLifetime);
    }
}