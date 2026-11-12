using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  MEDALLES (assoliments): es guanyen jugant i donen propines per al Rebost
// =====================================================================
// Es veuen al Receptari (pestanya MEDALLES). Quan en guanyes una, surt un avís a la partida.
// Les altres parts del joc les desbloquegen amb Achievements.Unlock("id") o amb els comptadors d'aquí.
public static class Achievements
{
    public class Medal
    {
        public string id, name, desc;
        public int reward;
        public bool secret; // no es diu què cal fer fins que la tens
        public Medal(string i, string n, string d, int r, bool s = false) { id = i; name = n; desc = d; reward = r; secret = s; }
    }

    public static readonly Medal[] All =
    {
        // ---- clients ----
        new Medal("kill1", "Primer client", "Despatxa el primer client", 10),
        new Medal("kill500", "Cinc-cents cobraments", "Despatxa 500 clients (entre totes les partides)", 60),
        new Medal("kill5000", "La cuina no para", "Despatxa 5.000 clients (entre totes les partides)", 200),
        new Medal("elite10", "Clientela d'elit", "Despatxa 10 clients d'elit", 80),
        // ---- caps ----
        new Medal("boss_Critic", "Crític criticat", "Venç el Crític gastronòmic", 40),
        new Medal("boss_Borratxo", "Cap a casa", "Venç el Borratxo de la barra", 40),
        new Medal("boss_XefRenegat", "Xef destronat", "Venç el Xef renegat", 50),
        new Medal("boss_ClientVIP", "Sense reserva", "Venç el Client VIP", 50),
        new Medal("boss_Inspectora", "Inspecció superada", "Venç la Inspectora", 60),
        new Medal("boss_Sommelier", "Ja n'hi ha prou de vi", "Venç el Sommelier", 70),
        new Medal("boss_Gelater", "Fos", "Venç el Gelater boig", 80),
        new Medal("boss_CriticaTV", "Fora de l'aire", "Venç la Crítica de la tele", 100),
        new Medal("boss_Fantasma", "Descansa en pau", "Venç el Fantasma del Restaurant", 200, true),
        new Medal("weak", "Al punt feble", "Remata un cap amb el seu punt feble", 60),
        new Medal("nohit_boss", "Ni un rascada", "Venç un cap sense que et toqui", 120),
        // ---- partides i modes ----
        new Medal("win", "Servei acabat", "Guanya El Servei (40 onades)", 150),
        new Medal("rush_Classic", "Menú clàssic", "Acaba el Menú degustació clàssic", 100),
        new Medal("rush_Chef", "Menú del xef", "Acaba el Menú del xef", 150),
        new Medal("rush_Surprise", "Menú sorpresa", "Acaba el Menú sorpresa", 120),
        new Medal("rush_Infernal", "Menú infernal", "Acaba el Menú infernal", 200),
        new Medal("rush_Ghost", "Menú fantasma", "Acaba el Menú fantasma", 250, true),
        new Medal("rush_fast", "Servei exprés", "Acaba el Menú clàssic en menys de 10 minuts", 200),
        new Medal("daily1", "Plat del dia", "Supera un Repte del dia", 60),
        new Medal("daily5", "Client fidel", "Supera 5 Reptes del dia", 150),
        // ---- plats ----
        new Medal("evo1", "Plat estrella", "Cuina el teu primer plat estrella", 40),
        new Medal("evo5", "Cinc estrelles", "Descobreix 5 plats estrella diferents", 120),
        new Medal("legend", "Llegendari", "Tria un plat llegendari", 30),
        new Medal("shiny", "Brillant!", "Tria una carta brillant", 30),
        new Medal("fulltable", "Taula plena", "Omple les 6 especialitats en una partida", 80),
        // ---- el xef ----
        new Medal("tier4", "Mestre de l'habilitat", "Porta l'habilitat del xef al nivell IV", 100),
        new Medal("abil100", "Cent cops d'habilitat", "Fes servir l'habilitat 100 vegades", 60),
        new Medal("overheat", "Massa calent!", "Encalla la paella per primer cop", 10),
        new Medal("outfit", "De gala", "Compra un vestit", 30),
        new Medal("allchefs", "Brigada completa", "Desbloqueja tots els xefs", 150),
        // ---- secrets i esdeveniments ----
        new Medal("recipe1", "Arqueòleg de fogons", "Troba una recepta perduda", 30),
        new Medal("recipe8", "El llibre complet", "Troba les 8 receptes perdudes", 200),
        new Medal("review", "Cinc estrelles a la tele", "Supera una Ressenya en directe sense que et toquin", 50),
        new Medal("cake", "Per molts anys!", "Agafa un pastís a la Festa d'aniversari", 20),
    };

    public static float LastPlayerHit = -999f; // quan t'han tocat per últim cop (per a "Ni un rascada")

    public static bool Has(string id) { return PlayerPrefs.GetInt("medal_" + id, 0) == 1; }

    public static int Count()
    {
        int n = 0;
        foreach (Medal m in All) if (Has(m.id)) n++;
        return n;
    }

    public static void Unlock(string id)
    {
        if (PlayerHealth.GodMode || Has(id)) return; // amb el mode invencible de prova no val
        foreach (Medal m in All)
        {
            if (m.id != id) continue;
            PlayerPrefs.SetInt("medal_" + id, 1);
            Progression.AddTips(m.reward);
            PlayerPrefs.Save();
            Transform p = PlayerRef.Transform;
            Vector2 at = p != null ? (Vector2)p.position + Vector2.up * 2.8f : Vector2.zero;
            if (p != null)
                HazardFx.Popup(at, "MEDALLA: " + m.name + "\n<size=70%>+" + m.reward + " propines</size>", new Color(1f, 0.85f, 0.3f), 2.8f);
            AudioManager.Play("challenge");
            return;
        }
    }

    // ---------- comptadors ----------
    public static void Add(string key, int n, int goal, string medal)
    {
        int v = PlayerPrefs.GetInt("medalc_" + key, 0) + n;
        PlayerPrefs.SetInt("medalc_" + key, v);
        if (v >= goal) Unlock(medal);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Enemy.OnKilled -= OnKilled;
        Enemy.OnKilled += OnKilled;
    }

    static void OnKilled(Enemy e)
    {
        if (e == null || SceneManager.GetActiveScene().name != SceneNames.Game || e.isBoss) return;
        Unlock("kill1");
        int total = Stats.Get("kills");
        if (total >= 500) Unlock("kill500");
        if (total >= 5000) Unlock("kill5000");
    }

    // un cap ha mort: la seva medalla, i si ha estat amb el punt feble o sense que et toquin
    public static void OnBossKilled(Enemy e, bool noHit)
    {
        if (e == null) return;
        Unlock("boss_" + Codex.ClientKey(e));
        if (e.weakness != DmgKind.None && (DamageKind.Current == e.weakness)) Unlock("weak");
        if (noHit) Unlock("nohit_boss");
    }
}
