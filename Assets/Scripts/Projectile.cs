using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 2f;

    void Start()
    {
        Destroy(gameObject, lifetime); // desapareix sola després de 2 segons
    }
}