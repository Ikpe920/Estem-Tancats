using UnityEngine;

public class SlipperyHazard : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = HazardFx.RandomPoint(3f);
            SpriteRenderer sr = HazardFx.MakeCircle(pos, Random.Range(2f, 3f), new Color(0.45f, 0.75f, 1f, 0.35f), -3);
            sr.gameObject.name = "Bassa";
            sr.transform.SetParent(transform, true);

            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            sr.gameObject.AddComponent<SlipZone>();
        }
    }
}
