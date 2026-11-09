using System.Collections.Generic;
using UnityEngine;

// Animació de caminar: mentre el personatge es mou, alterna el seu sprite amb dos fotogrames de passa
// (Resources/Walk/<nom del sprite>_1 i _2): passa, dret, l'altra passa, dret... Com més ràpid va, més
// de pressa camina. Si un sprite no té fotogrames (els caps, per exemple), no fa res.
// El posen sols l'Enemy i el PlayerMovement: no cal afegir-lo a cap prefab.
public class WalkAnimator : MonoBehaviour
{
    public float minSpeed = 0.25f;     // per sota d'això es considera que està quiet
    public float framesPerUnit = 2.4f; // fotogrames per cada unitat de velocitat
    public float minFps = 5f, maxFps = 14f;

    // sprite dret -> els seus dos fotogrames (null si no en té); i cada fotograma -> el seu sprite dret
    static readonly Dictionary<Sprite, Sprite[]> framesOf = new Dictionary<Sprite, Sprite[]>();
    static readonly Dictionary<Sprite, Sprite> baseOf = new Dictionary<Sprite, Sprite>();

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private float phase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    static Sprite[] FramesFor(Sprite standing)
    {
        if (standing == null) return null;
        if (framesOf.TryGetValue(standing, out Sprite[] cached)) return cached;

        Sprite a = Resources.Load<Sprite>("Walk/" + standing.name + "_1");
        Sprite b = Resources.Load<Sprite>("Walk/" + standing.name + "_2");
        Sprite[] frames = a != null && b != null ? new[] { a, b } : null;
        framesOf[standing] = frames;
        if (frames != null)
        {
            baseOf[a] = standing;
            baseOf[b] = standing;
        }
        return frames;
    }

    void LateUpdate()
    {
        if (sr == null) return;
        Sprite current = sr.sprite;
        if (current == null) return;

        // si ara hi ha un fotograma de passa, el sprite "de debò" és el seu sprite dret
        Sprite standing = baseOf.TryGetValue(current, out Sprite b) ? b : current;
        Sprite[] frames = FramesFor(standing);
        if (frames == null) return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        if (speed < minSpeed || Time.timeScale == 0f)
        {
            phase = 0f;
            if (current != standing) sr.sprite = standing;
            return;
        }

        phase += Time.deltaTime * Mathf.Clamp(speed * framesPerUnit, minFps, maxFps);
        int step = (int)phase % 4; // passa, dret, l'altra passa, dret
        sr.sprite = step == 0 ? frames[0] : step == 2 ? frames[1] : standing;
    }
}
