using UnityEngine;

// =====================================================================
//  TAULES: es poden bolcar amb la tecla E i fan de barricada
// =====================================================================

// els trets dels clients s'aturen contra això (si té durabilitat, es gasta)
public class BlocksShots : MonoBehaviour
{
    public int durability = 0; // 0 = indestructible

    public void OnShotBlocked()
    {
        if (durability <= 0) return;

        durability--;
        if (durability == 0)
        {
            Table table = GetComponent<Table>();
            if (table != null) table.Collapse();
        }
    }
}
