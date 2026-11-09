using UnityEngine;

// ---------- LA BARRA: tolls relliscosos ----------
public class SlipZone : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null) pm.slipUntil = Time.time + 0.15f;
    }
}
