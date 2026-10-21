using UnityEngine;

public class SwingEffect : MonoBehaviour
{
    public float duration = 0.15f;

    [Header("Forma del pal")]
    public Transform stick;      // el fill "Stick" (si es deixa buit, agafa el primer fill)
    public float length = 2.2f;  // llargada del pal
    public float thickness = 0.3f;

    private float startAngle;
    private float endAngle;
    private float t;
    private bool initialized;

    public void Init(float centerAngle, float arc)
    {
        if (stick == null && transform.childCount > 0)
            stick = transform.GetChild(0);

        if (stick != null)
        {
            SpriteRenderer sr = stick.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector2 size = sr.sprite.bounds.size;

                // el pal s'estira cap endavant i una punta queda al centre (el pivot)
                stick.localRotation = Quaternion.Euler(0f, 0f, 90f);
                stick.localScale = new Vector3(thickness / size.x, length / size.y, 1f);
                stick.localPosition = new Vector3(length / 2f, 0f, 0f);
            }
        }

        startAngle = centerAngle - arc / 2f;
        endAngle = centerAngle + arc / 2f;
        transform.rotation = Quaternion.Euler(0f, 0f, startAngle);
        initialized = true;
    }

    void Start()
    {
        // si algú s'ha deixat un Swing a l'escena, s'esborra sol
        if (!initialized) Destroy(gameObject);
    }

    void Update()
    {
        if (!initialized) return;

        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float angle = Mathf.Lerp(startAngle, endAngle, k);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (k >= 1f) Destroy(gameObject);
    }
}