using UnityEngine;

// punt únic per fer tremolar la càmera: tothom crida CameraFX.Shake(durada, força)
// (abans hi havia tres sistemes i el CameraShaker congelava la càmera mentre durava el tremolor)
public static class CameraFX
{
    public static void Shake(float duration, float magnitude)
    {
        if (CameraFollow.Instance != null)
        {
            if (Accessibility.ShakeMul <= 0f) return; // accessibilitat: sense sacseig
            CameraFollow.Instance.Shake(duration, magnitude * Accessibility.ShakeMul);
        }
    }
}
