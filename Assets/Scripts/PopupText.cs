using UnityEngine;
using TMPro;

public class PopupText : MonoBehaviour
{
    public float life = 0.9f;

    private TextMeshPro tmp;
    private float born;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        born = Time.time;
    }

    void Update()
    {
        float k = (Time.time - born) / life;
        transform.position += Vector3.up * 1.2f * Time.deltaTime;

        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = Mathf.Clamp01(1f - k);
            tmp.color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
