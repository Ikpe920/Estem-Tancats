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

    void Awake()
    {
        Instance = this;
        baseZ = transform.position.z;
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
        if (target == null)
        {
            if (shakeOffset != Vector3.zero)
                transform.position += shakeOffset * 0f; // no fer res si no hi ha target
            // aplicar shake igualment si volem
            if (shakeOffset != Vector3.zero)
            {
                transform.position = new Vector3(transform.position.x + shakeOffset.x, transform.position.y + shakeOffset.y, transform.position.z);
                // reset visual offset next frame? no, millor mantenir
                return;
            }
            return;
        }
        if (baseZ == 0f) baseZ = transform.position.z;

        Vector3 desired = new Vector3(target.position.x, target.position.y, baseZ);
        Vector3 currentNoShake = transform.position - shakeOffset;
        Vector3 basePos = Vector3.Lerp(currentNoShake, desired, smooth * Time.deltaTime);
        transform.position = new Vector3(basePos.x + shakeOffset.x, basePos.y + shakeOffset.y, baseZ);
    }
}


