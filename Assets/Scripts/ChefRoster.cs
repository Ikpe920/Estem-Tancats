using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Els xefs que es poden triar (pantalla ChefSelectScreen) i el que fa cadascun.
// El xef triat es guarda al dispositiu, i en començar la partida se li posen els sprites i els valors.
// Els sprites són a Resources/Chefs/<nom>_Down, _Up, _Left i _Right.
public static class ChefRoster
{
    const string SelectedKey = "chef";

    public class Chef
    {
        public string name;       // el que surt a la pantalla
        public string sprites;    // el prefix dels sprites a Resources/Chefs
        public string strong;     // punt fort
        public string weak;       // punt feble
        public Color color;       // color de la carta
        public Action<PlayerMovement, PlayerShooting, PlayerHealth> apply;
    }

    public static readonly Chef[] All =
    {
        new Chef
        {
            name = "EL XEF", sprites = "Chef", color = new Color(0.95f, 0.93f, 0.88f),
            strong = "Equilibrat en tot", weak = "Cap",
            apply = (m, s, h) => { }
        },
        new Chef
        {
            name = "EL CARNISSER", sprites = "Carnisser", color = new Color(0.85f, 0.3f, 0.3f),
            strong = "4 cors i +1 de dany", weak = "Més lent i dispara més a poc a poc",
            apply = (m, s, h) =>
            {
                if (h != null) h.SetMaxLives(h.maxLives + 1);
                if (s != null) { s.damage += 1; s.fireRate *= 1.25f; }
                if (m != null) m.speed *= 0.85f;
            }
        },
        new Chef
        {
            name = "EL PASTISSER", sprites = "Pastisser", color = new Color(0.95f, 0.55f, 0.72f),
            strong = "Dispara molt ràpid i el dash es recarrega abans", weak = "Només 2 cors",
            apply = (m, s, h) =>
            {
                if (h != null) h.SetMaxLives(h.maxLives - 1);
                if (s != null) s.fireRate *= 0.6f;
                if (m != null) m.dashCooldown *= 0.6f;
            }
        },
        new Chef
        {
            name = "EL SUSHIMAN", sprites = "Sushiman", color = new Color(0.35f, 0.45f, 0.85f),
            strong = "3 trets en ventall des del principi", weak = "Dispara més a poc a poc, i els trets dels costats fan menys mal",
            apply = (m, s, h) =>
            {
                if (s != null) { s.extraShots += 2; s.sideShotDamage = 0.5f; s.fireRate *= 1.35f; }
            }
        },
    };

    public static int Selected
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, All.Length - 1); }
        set { PlayerPrefs.SetInt(SelectedKey, Mathf.Clamp(value, 0, All.Length - 1)); PlayerPrefs.Save(); }
    }

    public static Sprite LoadSprite(Chef c, string dir)
    {
        return Resources.Load<Sprite>("Chefs/" + c.sprites + "_" + dir);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // en carregar l'escena del joc (abans del Start de ningú), el jugador es converteix en el xef triat
    // si alguna cosa falla aquí, que no aturi els altres scripts que esperen l'escena (Unity els crida
    // un darrere l'altre i un error talla la resta: per exemple, el que crea les taules i les caixes)
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try { OnSceneLoadedSafe(scene, mode); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    static void OnSceneLoadedSafe(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.Game) return;

        // un xef bloquejat no es pot fer servir (per si de cas): el Xef
        int index = Progression.IsChefUnlocked(Selected) ? Selected : 0;
        Chef c = All[index];
        PlayerMovement m = PlayerRef.Movement;
        PlayerShooting s = PlayerRef.Shooting;
        PlayerHealth h = PlayerRef.Health;

        if (m != null)
        {
            Sprite down = LoadSprite(c, "Down");
            if (down != null)
            {
                m.spriteDown = down;
                m.spriteUp = LoadSprite(c, "Up") ?? down;
                m.spriteLeft = LoadSprite(c, "Left") ?? down;
                m.spriteRight = LoadSprite(c, "Right") ?? down;
                SpriteRenderer sr = m.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = down;
            }
        }

        c.apply?.Invoke(m, s, h);

        // en FÀCIL, un cor més
        if (h != null && Progression.ExtraHearts > 0) h.SetMaxLives(h.maxLives + Progression.ExtraHearts);
    }
}
