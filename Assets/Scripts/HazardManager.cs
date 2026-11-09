using UnityEngine;
using UnityEngine.SceneManagement;

// crea i canvia els perills i l'ambient de cada acte (no cal posar-lo a cap escena)
public class HazardManager : MonoBehaviour
{
    public static Vector2 ArenaSize = new Vector2(30f, 20f);
    public static Vector2 ArenaCenter = Vector2.zero;

    private int applied = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name != SceneNames.Game) return;
            EnsureExists();
        };
    }

    // crea els perills (taules, caixes...) i les portes si encara no hi són. El WaveSpawner també ho
    // demana en començar: així no depèn que l'avís de l'escena carregada arribi fins aquí
    public static void EnsureExists()
    {
        if (FindAnyObjectByType<HazardManager>() == null) new GameObject("Perills").AddComponent<HazardManager>();
        if (DoorManager.Instance == null) new GameObject("Portes").AddComponent<DoorManager>();
    }

    void Awake()
    {
        ArenaWalls walls = FindAnyObjectByType<ArenaWalls>();
        if (walls != null)
        {
            ArenaSize = walls.size;
            ArenaCenter = walls.transform.position;
        }
    }

    void OnEnable()
    {
        ActManager.OnActChanged += Apply;
    }

    void OnDisable()
    {
        ActManager.OnActChanged -= Apply;
    }

    void Start()
    {
        ActManager am = FindAnyObjectByType<ActManager>();
        Apply(am != null ? am.currentAct : 0);
    }

    void Apply(int act)
    {
        act = ((act % 5) + 5) % 5;
        if (act == applied) return;
        applied = act;

        Clear();

        switch (act)
        {
            case 0: Add<TableHazard>(); break;
            case 1: Add<SlipperyHazard>(); Add<BottleHazard>(); break;
            case 2: Add<FireHazard>(); break;
            case 3: Add<WindHazard>(); break;
            case 4: Add<LightningHazard>(); Add<DarknessHazard>(); break;
        }

        // les caixes destructibles surten a tots els actes
        Add<PropsHazard>().act = act;

        // l'aspecte de l'acte: parets i portes
        if (ArenaWalls.Instance != null) ArenaWalls.Instance.SetSkin(WallSkin.Get(act), Color.white);
        if (DoorManager.Instance != null) DoorManager.Instance.SetAct(act);
    }

    T Add<T>() where T : Component
    {
        GameObject go = new GameObject(typeof(T).Name);
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        PlayerMovement pm = PlayerRef.Movement;
        if (pm != null)
        {
            pm.externalPush = Vector2.zero;
            pm.slipUntil = 0f;
        }
    }
}