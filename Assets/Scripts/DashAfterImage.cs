using UnityEngine;

// Estela del dash: una copia del sprite del jugador que es dessfadeix i es destrueix sola.
public class DashAfterImage : MonoBehaviour
{
    private SpriteRenderer sr;
    private float life;
    private float maxLife = 1f;
    private float startAlpha;

    public void Init(float lifetime, Color color)
    {
        maxLife = Mathf.Max(0.01f, lifetime);
        life = maxLife;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
            startAlpha = color.a;
        }
    }

    void Update()
    {
        life -= Time.deltaTime;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = startAlpha * Mathf.Clamp01(life / maxLife);
            sr.color = c;
        }

        if (life <= 0f) Destroy(gameObject);
    }
}