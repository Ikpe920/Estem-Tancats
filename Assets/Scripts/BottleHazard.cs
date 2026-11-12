using System.Collections;
using UnityEngine;

public class BottleHazard : MonoBehaviour
{
    public int maxBottles = 3;

    IEnumerator Start()
    {
        for (int i = 0; i < maxBottles; i++) SpawnBottle();

        while (true)
        {
            yield return new WaitForSeconds(6f);
            if (transform.childCount < maxBottles) SpawnBottle();
        }
    }

    void SpawnBottle()
    {
        Vector2 pos = HazardFx.RandomPoint(2.5f);

        GameObject root = new GameObject("Ampolla");
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.7f, 1.1f);

        Bottle bottle = root.AddComponent<Bottle>();

        // l'ampolla de licor de contraban que bull (Tools/SpriteGen/bottles.py); si no hi és, el rectangle d'abans
        GameObject body = new GameObject("Cos");
        body.transform.SetParent(root.transform, false);
        SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
        bsr.sortingOrder = -1;
        if (Bottle.Frames() != null)
        {
            body.transform.localScale = Vector3.one * 1.3f;
            bsr.sprite = Bottle.Frames()[0];
            bottle.body = bsr;
        }
        else
        {
            body.transform.localScale = new Vector3(0.3f, 0.7f, 1f);
            bsr.sprite = HazardFx.Square();
            bsr.color = new Color(0.25f, 0.65f, 0.35f, 1f);
        }

        // el radi de l'explosió, molt fluix, perquè se sàpiga on fa mal
        SpriteRenderer radius = HazardFx.MakeCircle(pos, bottle.blastRadius, new Color(1f, 0.3f, 0.1f, 0.07f), -3);
        radius.transform.SetParent(root.transform, true);
    }
}
