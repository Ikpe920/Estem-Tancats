using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  ANIMACIONS DE DIBUIX ANIMAT: aixafar i estirar, i sortir volant en morir
// =====================================================================
// Squash.Kick(objecte, x, y): deforma l'objecte (x i y són quant s'estira: 0,2 = un 20% més ample)
//   i una molla el torna a la seva mida, rebotant una mica, com als dibuixos dels anys 30.
//   La deformació només es veu en pantalla: es posa just abans de dibuixar i es treu just després,
//   així no toca la física ni els altres scripts que canvien la mida dels objectes.
// CartoonDeath.Launch(client, d'on ve el cop): el client surt volant, girant, i fa "puf" a dalt.
public class Squash : MonoBehaviour
{
    const float Stiffness = 320f; // com de dura és la molla
    const float Damping = 13f;    // com de pressa deixa de rebotar

    private Vector2 offset;   // la deformació ara mateix
    private Vector2 velocity;
    private Vector3 baseScale;
    private bool applied;

    static readonly List<Squash> appliedThisFrame = new List<Squash>();
    static SquashRestorer restorer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        appliedThisFrame.Clear();
        restorer = null;
    }

    public static void Kick(GameObject go, float x, float y)
    {
        if (go == null) return;
        Squash s = go.GetComponent<Squash>();
        if (s == null) s = go.AddComponent<Squash>();
        s.offset += new Vector2(x, y);
        s.offset = Vector2.ClampMagnitude(s.offset, 0.9f);
        s.enabled = true;
    }

    // un cop: s'aixafa cap als costats i rebota
    public static void Hit(GameObject go, float amount = 0.25f) { Kick(go, amount, -amount); }

    // aparèixer amb un "boing": surt petit i creix passant-se una mica de mida
    public static void Pop(GameObject go) { Kick(go, -0.6f, -0.6f); }

    void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f); // amb el temps aturat (pausa d'impacte) també s'atura
        velocity += (-Stiffness * offset - Damping * velocity) * dt;
        offset += velocity * dt;
        if (offset.sqrMagnitude < 0.00002f && velocity.sqrMagnitude < 0.0005f)
        {
            offset = Vector2.zero;
            velocity = Vector2.zero;
            enabled = false;
            return;
        }

        // es deforma només per dibuixar-lo; al final del fotograma es torna a la mida de debò
        baseScale = transform.localScale;
        transform.localScale = new Vector3(baseScale.x * (1f + offset.x), baseScale.y * (1f + offset.y), baseScale.z);
        applied = true;
        appliedThisFrame.Add(this);
        if (restorer == null)
        {
            GameObject g = new GameObject("SquashRestorer");
            DontDestroyOnLoad(g);
            restorer = g.AddComponent<SquashRestorer>();
        }
    }

    public void Restore()
    {
        if (!applied) return;
        applied = false;
        transform.localScale = baseScale;
    }

    void OnDisable() { Restore(); }

    // al final de cada fotograma (ja dibuixat), tothom torna a la seva mida
    public static void RestoreAll()
    {
        for (int i = 0; i < appliedThisFrame.Count; i++)
            if (appliedThisFrame[i] != null) appliedThisFrame[i].Restore();
        appliedThisFrame.Clear();
    }
}

public class SquashRestorer : MonoBehaviour
{
    readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    IEnumerator Start()
    {
        while (true)
        {
            yield return endOfFrame;
            Squash.RestoreAll();
        }
    }
}

// ---------- SORTIR VOLANT EN MORIR ----------
public class CartoonDeath : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector2 velocity;
    private float spin;
    private float gravity;
    private float life, age;
    private Vector3 startScale;
    private bool boss;

    public static void Launch(Enemy e, Vector2 from)
    {
        if (e == null) return;
        SpriteRenderer src = e.GetComponentInChildren<SpriteRenderer>();
        if (src == null || src.sprite == null) return;

        GameObject go = new GameObject("ClientVolant");
        go.transform.position = src.transform.position;
        go.transform.localScale = src.transform.lossyScale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = src.sprite;
        sr.flipX = src.flipX;
        sr.sharedMaterial = src.sharedMaterial;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder + 20; // per sobre dels altres mentre vola
        sr.color = e.BaseTint;

        CartoonDeath d = go.AddComponent<CartoonDeath>();
        d.sr = sr;
        d.boss = e.isBoss;
        // surt disparat cap amunt i cap al costat contrari d'on ve el cop
        Vector2 away = (Vector2)e.transform.position - from;
        float side = Mathf.Abs(away.x) > 0.05f ? Mathf.Sign(away.x) : (Random.value < 0.5f ? -1f : 1f);
        d.velocity = new Vector2(side * Random.Range(2.2f, 3.6f), e.isBoss ? 9f : Random.Range(7f, 9f));
        d.spin = -side * (e.isBoss ? 540f : Random.Range(620f, 900f));
        d.gravity = 26f;
        d.life = e.isBoss ? 1.0f : 0.62f;
        d.startScale = go.transform.localScale;
    }


    void Update()
    {
        float dt = Time.deltaTime; // la pausa d'impacte el congela a l'aire, com un fotograma clau
        age += dt;
        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, spin * dt);

        float k = Mathf.Clamp01(age / life);
        // s'estira mentre surt disparat i s'encongeix al final
        float stretch = Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(age / 0.15f));
        float shrink = k < 0.7f ? 1f : Mathf.Lerp(1f, 0.3f, (k - 0.7f) / 0.3f);
        transform.localScale = new Vector3(startScale.x * shrink / Mathf.Sqrt(stretch), startScale.y * shrink * stretch, startScale.z);

        if (age >= life)
        {
            Fx.Poof(transform.position);
            Fx.Sparkles(transform.position, new Color(1f, 0.92f, 0.5f), boss ? 10 : 4, boss ? 1.4f : 0.6f);
            Destroy(gameObject);
        }
    }
}
