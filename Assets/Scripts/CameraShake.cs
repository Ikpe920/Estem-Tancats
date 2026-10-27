using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; set; }

    public void Shake(float duration, float magnitude)
    {
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Shake(duration, magnitude);
            return;
        }
        if (duration <= 0f || magnitude <= 0f) return;
    }
}

