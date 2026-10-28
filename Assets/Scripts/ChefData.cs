using UnityEngine;

public class ChefInfo
{
    public string name;
    public string description;      // la passiva, en una frase
    public string stats;            // resum que surt a la carta
    public Color tint = Color.white;  // color provisional, mentre no hi hagi sprites propis
    public string spritePrefix = ""; // si hi ha sprites a Resources/Chefs (Pastisser_Down.png...)

    public int maxLives = 3;
    public float speedBonus = 0f;
    public float fireRateDelta = 0f;     // positiu = dispara més lent, negatiu = més ràpid
    public int damageBonus = 0;
    public float dashCooldownDelta = 0f; // positiu = dash més lent
    public int pierce = 0;
    public float explosionRadius = 0f;
    public int dashDamage = 0;
}

public static class Chefs
{
    public static int Selected = 0; // el xef triat (es manté entre escenes)

    public static ChefInfo Current
    {
        get { return All[Mathf.Clamp(Selected, 0, All.Length - 1)]; }
    }

    public static readonly ChefInfo[] All = new ChefInfo[]
    {
        new ChefInfo
        {
            name = "EL CUINER",
            description = "El xef de tota la vida. Sense trucs: res a favor, res en contra.",
            stats = "Cors: 3\nVelocitat: normal\nCadència: normal",
        },
        new ChefInfo
        {
            name = "EL PASTISSER",
            description = "Les seves llançades exploten en nata i fan mal als clients de prop.",
            stats = "Cors: 3\nVelocitat: normal\nCadència: lenta",
            tint = new Color(1f, 0.75f, 0.85f),
            spritePrefix = "Pastisser",
            fireRateDelta = 0.07f,
            explosionRadius = 1.2f,
        },
        new ChefInfo
        {
            name = "EL CARNISSER",
            description = "Pesat però dur. El seu ganivet travessa un client i fa més mal.",
            stats = "Cors: 4\nVelocitat: lenta\nDash: lent",
            tint = new Color(0.85f, 0.45f, 0.4f),
            spritePrefix = "Carnisser",
            maxLives = 4,
            speedBonus = -0.8f,
            damageBonus = 1,
            dashCooldownDelta = 0.4f,
            pierce = 1,
        },
        new ChefInfo
        {
            name = "EL SUSHIMAN",
            description = "Ràpid i fràgil. El seu dash talla els clients que travessa.",
            stats = "Cors: 2\nVelocitat: alta\nCadència: ràpida",
            tint = new Color(0.55f, 0.85f, 0.95f),
            spritePrefix = "Sushiman",
            maxLives = 2,
            speedBonus = 1.3f,
            fireRateDelta = -0.07f,
            dashCooldownDelta = -0.3f,
            dashDamage = 2,
        },
    };
}