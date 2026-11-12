using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  CONSULTES DE FÍSICA SENSE ESCOMBRARIES
// =====================================================================
// Physics2D.OverlapCircleAll crea una llista nova a cada crida (i n'hi ha centenars per segon amb
// explosions, flames i tolls). Aquí es reaprofiten les llistes. Com que un cop pot matar un client
// i fer esclatar-ne un altre (una consulta dins d'una altra), cada consulta en té una de pròpia:
//   List<Collider2D> hits = Phys.Circle(pos, r);
//   foreach (...) { ... }
//   Phys.Release(hits);
public static class Phys
{
    static readonly Stack<List<Collider2D>> pool = new Stack<List<Collider2D>>();
    static ContactFilter2D filter;
    static bool filterReady;

    public static List<Collider2D> Circle(Vector2 pos, float radius)
    {
        if (!filterReady)
        {
            filter = new ContactFilter2D();
            filter.NoFilter();          // tot, com OverlapCircleAll (també els triggers)
            filterReady = true;
        }
        List<Collider2D> list = pool.Count > 0 ? pool.Pop() : new List<Collider2D>(32);
        list.Clear();
        Physics2D.OverlapCircle(pos, radius, filter, list);
        return list;
    }

    public static void Release(List<Collider2D> list)
    {
        if (list == null) return;
        list.Clear();
        pool.Push(list);
    }
}
