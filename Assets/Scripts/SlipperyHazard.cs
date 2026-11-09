using UnityEngine;

public class SlipperyHazard : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = HazardFx.RandomPoint(3f);
            SpriteRenderer sr = HazardFx.MakeCircle(pos, Random.Range(2f, 3f), Color.white, -3);
            sr.gameObject.name = "Bassa";
            sr.sprite = Fx.OilPuddle(); // bassal d'oli irregular, amb reflexos
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            sr.gameObject.AddComponent<PuddleShine>();
            sr.transform.SetParent(transform, true);

            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            sr.gameObject.AddComponent<SlipZone>();
        }
    }
}
