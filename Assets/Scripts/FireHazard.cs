using System.Collections;
using UnityEngine;

public class FireHazard : MonoBehaviour
{
    public int maxFires = 3;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            if (transform.childCount < maxFires)
            {
                SpriteRenderer sr = HazardFx.MakeCircle(HazardFx.RandomPoint(2.5f), 1.6f, new Color(1f, 0.2f, 0.1f, 0.12f), -3);
                sr.gameObject.name = "Foc";
                sr.transform.SetParent(transform, true);
                sr.gameObject.AddComponent<FireZone>();
            }

            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }
    }
}
