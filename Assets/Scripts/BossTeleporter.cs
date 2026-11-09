using UnityEngine;

// El Client VIP: es teletransporta quan se li acosta el jugador i deixa bombes amb avís al terra
public class BossTeleporter : MonoBehaviour
{
    public float bombInterval = 1.6f;     // cada quant deixa una bomba
    public int bombsPerVolley = 3;
    public float bombRadius = 1.8f;
    public float bombDelay = 1.1f;
    public float teleportCooldown = 3.2f;
    public float keepDistance = 6f;       // distància a què reapareix del jugador
    public float fleeDistance = 3.5f;     // si el jugador se li acosta, s'escapa

    private Rigidbody2D rb;
    private Transform player;
    private float nextBomb;
    private float nextTeleport;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        nextBomb = Time.time + 1.5f;
        nextTeleport = Time.time + 3f;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (Time.time >= nextTeleport || dist < fleeDistance)
        {
            nextTeleport = Time.time + teleportCooldown;
            Teleport();
        }

        if (Time.time >= nextBomb)
        {
            nextBomb = Time.time + bombInterval;
            DropBombs();
        }
    }

    void Teleport()
    {
        Color smoke = new Color(0.8f, 0.6f, 1f, 0.5f);
        BossFx.Puff(transform.position, 1.5f, smoke);

        // reapareix a prop del jugador però sense quedar-se-li a sobre
        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        Vector2 target = (Vector2)player.position + dir * keepDistance;
        target.x = Mathf.Clamp(target.x, -11f, 11f);
        target.y = Mathf.Clamp(target.y, -7f, 7f);

        rb.position = target;
        rb.linearVelocity = Vector2.zero;

        BossFx.Puff(target, 1.5f, smoke);
    }

    void DropBombs()
    {
        CameraFX.Shake(0.18f, 0.14f);
        for (int i = 0; i < bombsPerVolley; i++)
        {
            // la primera cau als peus del jugador; les altres, a prop del cap
            Vector2 pos = (i == 0 && player != null)
                ? (Vector2)player.position
                : (Vector2)transform.position + Random.insideUnitCircle * 3f;
            BossBomb.Spawn(pos, bombRadius, bombDelay);
        }
    }
}
