using UnityEngine;

// =====================================================================
//  LES RECEPTES PERDUDES (una a cada sala)
// =====================================================================
// A cada sala, en una onada a l'atzar, apareix un pergamí en algun racó durant uns segons.
// - Si l'agafes: un plat de regal, i la recepta queda guardada per sempre al Receptari.
//   La primera vegada que en trobes una, també propines.
// - Si en una partida d'El Servei en trobes almenys 6, després de la Crítica de la tele (onada 40)
//   surt el cap secret: EL FANTASMA DEL RESTAURANT.
public static class LostRecipes
{
    public const int GhostNeeds = 6;

    public class Recipe
    {
        public string name, lore;
        public Recipe(string n, string l) { name = n; lore = l; }
    }

    // una per sala, en el mateix ordre que les sales
    public static readonly Recipe[] All =
    {
        new Recipe("La crema de la besàvia", "«Remenar sempre cap a la dreta, i mai, MAI, de nit de lluna plena.»"),
        new Recipe("El còctel del primer client", "«El primer client que va entrar mai no va sortir. Encara demana el mateix.»"),
        new Recipe("El brou de les set olles", "«Set olles, set hores, set crits. Si el brou xiuxiueja, no l'escoltis.»"),
        new Recipe("La coca del migdia etern", "«A la terrassa, el sol no es pon mai mentre la coca és al forn.»"),
        new Recipe("Les castanyes del terrat", "«Torrar-les a la teulada. El fum avisa els que són a l'altre costat.»"),
        new Recipe("El vi que no s'acaba", "«La bóta del fons no té fons. El sommelier ho sabia.»"),
        new Recipe("El sorbet de l'hivern sense fi", "«Servir-lo gelat. Si es fon, alguna cosa ha despertat.»"),
        new Recipe("El plat que va tancar el restaurant", "«L'últim plat del xef fundador. Qui el tasta, ja no se'n va mai.»"),
    };

    public static int RunFound;                 // trobades en aquesta partida
    static bool[] spawnedThisRun = new bool[8];

    public static bool Found(int i) { return PlayerPrefs.GetInt("recipe_found_" + i, 0) == 1; }

    public static int FoundCount()
    {
        int n = 0;
        for (int i = 0; i < All.Length; i++) if (Found(i)) n++;
        return n;
    }

    public static void ResetRun()
    {
        RunFound = 0;
        spawnedThisRun = new bool[All.Length];
    }

    // el WaveSpawner ho crida a cada onada normal: potser surt la recepta d'aquesta sala
    public static void MaybeSpawn(int room, int waveInRoom)
    {
        if (room < 0 || room >= All.Length || spawnedThisRun[room]) return;
        // a la 4a onada de la sala segur; abans, una mica a l'atzar
        if (waveInRoom < 4 && Random.value > 0.3f) return;
        spawnedThisRun[room] = true;
        RecipePickup.Spawn(HazardFx.RandomPoint(3f), room);
    }

    public static void OnFound(int room)
    {
        RunFound++;
        bool first = !Found(room);
        PlayerPrefs.SetInt("recipe_found_" + room, 1);
        PlayerPrefs.Save();
        Stats.Add("recipes");
        if (first) Progression.AddTips(20);
    }
}

// el pergamí que brilla a terra: s'esvaeix si no l'agafes a temps
public class RecipePickup : MonoBehaviour
{
    const float Life = 22f;
    private int room;
    private float born;
    private SpriteRenderer sr;

    public static void Spawn(Vector2 pos, int room)
    {
        Sprite sp = Resources.Load<Sprite>("DishFx/Recepta");
        if (sp == null) return;
        GameObject go = new GameObject("ReceptaPerduda");
        go.transform.position = pos;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;
        RecipePickup p = go.AddComponent<RecipePickup>();
        p.room = room;
        p.born = Time.time;
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = sp;
        p.sr.sortingOrder = 4;
        go.transform.localScale = Vector3.one * 1.1f;
        HazardFx.Popup(pos + Vector2.up * 1.2f, "UNA RECEPTA PERDUDA!", new Color(1f, 0.85f, 0.4f), 1.6f);
        AudioManager.Play("pickup", 0.6f, 0.7f);
    }

    void Update()
    {
        float age = Time.time - born;
        if (age > Life) { Fx.Poof(transform.position); Destroy(gameObject); return; }
        // flota, i parpelleja quan està a punt de desaparèixer
        transform.position += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.004f;
        if (sr != null) sr.enabled = age < Life - 4f || Mathf.Repeat(Time.time * 6f, 1f) < 0.6f;
        if (Random.value < Time.deltaTime * 4f) Fx.Sparkle((Vector2)transform.position + Random.insideUnitCircle * 0.6f, new Color(1f, 0.85f, 0.4f));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerHealth>() == null) return;
        LostRecipes.OnFound(room);
        Fx.Sparkles(transform.position, new Color(1f, 0.85f, 0.4f), 14, 1.2f);
        AudioManager.Play("recipe");
        HazardFx.Popup((Vector2)transform.position + Vector2.up * 1.4f,
            LostRecipes.All[room].name.ToUpper() + "  (" + LostRecipes.RunFound + ")", new Color(1f, 0.9f, 0.5f), 2.2f);
        if (BoonManager.Instance != null && BoonManager.Instance.xpMode)
            BoonManager.Instance.StartCoroutine(BoonManager.Instance.OpenChest("RECEPTA PERDUDA!"));
        Destroy(gameObject);
    }
}
