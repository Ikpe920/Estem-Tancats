using UnityEngine;

// Accés ràpid al jugador des de qualsevol script, sense buscar-lo cada vegada.
// La primera crida el busca i el guarda; si l'escena canvia i el jugador desapareix, es torna a buscar sol.
public static class PlayerRef
{
    static GameObject root;
    static PlayerHealth health;
    static PlayerMovement movement;
    static PlayerShooting shooting;

    // l'objecte del jugador (el que té l'etiqueta "Player")
    public static GameObject Root
    {
        get
        {
            if (root == null)
            {
                root = GameObject.FindGameObjectWithTag("Player");
                if (root == null)
                {
                    PlayerHealth ph = Object.FindAnyObjectByType<PlayerHealth>();
                    if (ph != null) root = ph.gameObject;
                }
            }
            return root;
        }
    }

    public static Transform Transform
    {
        get { GameObject g = Root; return g != null ? g.transform : null; }
    }

    public static PlayerHealth Health
    {
        get { if (health == null) health = Find<PlayerHealth>(); return health; }
    }

    public static PlayerMovement Movement
    {
        get { if (movement == null) movement = Find<PlayerMovement>(); return movement; }
    }

    public static PlayerShooting Shooting
    {
        get { if (shooting == null) shooting = Find<PlayerShooting>(); return shooting; }
    }

    static T Find<T>() where T : Component
    {
        GameObject g = Root;
        T c = g != null ? g.GetComponent<T>() : null;
        if (c == null) c = Object.FindAnyObjectByType<T>(); // per si el component no és a l'objecte amb l'etiqueta
        return c;
    }
}
