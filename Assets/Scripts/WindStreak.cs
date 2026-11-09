using UnityEngine;

// ---------- LA TERRASSA: vent ----------
public class WindStreak : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 1.5f;

    void Start()
    {
        Destroy(gameObject, life);
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }
}
