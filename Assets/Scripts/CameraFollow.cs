using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; set; }

    public Transform target;
    public float smooth = 10f;

    private Vector3 shakeOffset = Vector3.zero;
    private Coroutine shakeRoutine;
    private float baseZ;
    private Camera cam;

    [Header("Límits")]
    public bool clampToArena = true; // la càmera no surt de la sala (no es veu el buit de fora)

    void Awake()
    {
        Instance = this;
        baseZ = transform.position.z;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // en començar, la càmera ja està sobre el jugador (sense lliscament inicial)
        if (target != null)
            transform.position = ClampToArena(new Vector3(target.position.x, target.position.y, transform.position.z));
    }

    public void Shake(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        Vector3 startOffset = shakeOffset;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            float x = Random.Range(-1f, 1f) * magnitude * damper;
            float y = Random.Range(-1f, 1f) * magnitude * damper;
            shakeOffset = new Vector3(x, y, 0f);
            yield return null;
        }
        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (baseZ == 0f) baseZ = transform.position.z;

        Vector3 desired = ClampToArena(new Vector3(target.position.x, target.position.y, baseZ));
        Vector3 currentNoShake = transform.position - shakeOffset;
        Vector3 basePos = Vector3.Lerp(currentNoShake, desired, smooth * Time.deltaTime);
        transform.position = new Vector3(basePos.x + shakeOffset.x, basePos.y + shakeOffset.y, baseZ);
    }

    // manté la vista dins de la sala (fins a la vora de fora de les parets)
    Vector3 ClampToArena(Vector3 p)
    {
        if (!clampToArena || cam == null || !cam.orthographic) return p;
        ArenaWalls a = ArenaWalls.Instance;
        if (a == null) return p;

        Vector2 half = a.size * 0.5f + Vector2.one * a.thickness;
        float viewH = cam.orthographicSize;
        float viewW = viewH * cam.aspect;
        Vector2 center = a.transform.position;

        // si la sala fos més petita que la vista, la càmera es queda al centre
        float rangeX = Mathf.Max(0f, half.x - viewW);
        float rangeY = Mathf.Max(0f, half.y - viewH);

        p.x = Mathf.Clamp(p.x, center.x - rangeX, center.x + rangeX);
        p.y = Mathf.Clamp(p.y, center.y - rangeY, center.y + rangeY);
        return p;
    }
}