using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 input;
    private Vector2 knockbackVelocity;
    private float knockbackUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input = input.normalized; // perquè en diagonal no vagi més ràpid
    }
    public void Knockback(Vector2 direction, float force, float duration)
    {
        knockbackVelocity = direction.normalized * force;
        knockbackUntil = Time.time + duration;
    }

    void FixedUpdate()
    {
        if (Time.time < knockbackUntil)
    {
        rb.linearVelocity = knockbackVelocity;
        return;
    }
        rb.linearVelocity = input * speed;
    }
}