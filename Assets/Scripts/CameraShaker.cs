using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    private Transform camTransform;
    private Vector3 originalLocalPos;
    private Coroutine shakeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("CameraShaker");
        DontDestroyOnLoad(go);
        go.AddComponent<CameraShaker>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CacheCamera();
    }

    void OnEnable()
    {
        CacheCamera();
    }

    void CacheCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindObjectOfType<Camera>();
        }
        if (cam != null)
        {
            camTransform = cam.transform;
            originalLocalPos = camTransform.localPosition;
        }
    }

    void LateUpdate()
    {
        if (camTransform == null)
        {
            CacheCamera();
            return;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f) return;
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;
        camTransform = cam.transform;
        if (camTransform == null) return;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        if (camTransform == null) yield break;
        Vector3 basePos = camTransform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            float x = Random.Range(-1f, 1f) * magnitude * damper;
            float y = Random.Range(-1f, 1f) * magnitude * damper;
            camTransform.position = basePos + new Vector3(x, y, 0f);
            yield return null;
        }

        camTransform.position = basePos;
        shakeRoutine = null;
    }
}
