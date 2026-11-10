using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
//  FONS DE LA HISTÒRIA: el restaurant de nit, animat
// =====================================================================
// Ho crea tot StoryDecor en començar (no cal tocar l'escena). Hi ha:
//  - la paret de paper pintat, el sòcol i el terra de fusta
//  - una finestra amb la lluna, estrelles que fan pampallugues i un núvol que passa
//  - la porta amb el cartell "TANCAT" que es gronxa
//  - dos llums penjats que es balancegen i de tant en tant parpellegen
//  - el xef triat al mig, i els clients que van entrant, l'envolten i es queixen
// Tot fa servir el temps sense escala i cap peça no rep clics.
public class StoryBackdrop : MonoBehaviour
{
    const float FloorLine = 540f; // on s'acaba la paret i comença el terra (des de baix, a 1080 d'alçada)

    static readonly string[] ClientTypes = { "Enemy", "Nen", "EnemyFast", "Iaia", "EnemyShooter", "Turista", "EnemyTank", "Cambrer", "Influencer", "Golafre", "Repartidor", "Olla", "Mim", "Safata" };
    static readonly string[] Complaints = { "TINC GANA!", "EH, XEF!", "ON ÉS EL MEU PLAT?", "UNA ALTRA RACIÓ!", "#@%&!", "!!", "NO MARXAREM!", "LA CARTA!" };
    // llocs on es queden els clients: x i l'alçada dels peus
    static readonly Vector2[] Slots =
    {
        new Vector2(-300f, 420f), new Vector2(300f, 410f), new Vector2(-520f, 380f), new Vector2(520f, 390f),
        new Vector2(-170f, 470f), new Vector2(180f, 475f), new Vector2(-740f, 440f), new Vector2(750f, 430f),
        new Vector2(-420f, 480f), new Vector2(440f, 485f),
    };

    class Person
    {
        public RectTransform root;   // als peus
        public RectTransform body;
        public Image img;
        public Sprite[] frames;
        public Vector2 target;
        public bool arrived;
        public float phase;
        public float shakeUntil;
    }

    private RectTransform scene, crowd;
    private readonly List<Person> clients = new List<Person>();
    private Person chef;
    private Sprite chefDown, chefLeft, chefRight;
    private float chefLookUntil;

    private readonly List<Image> stars = new List<Image>();
    private readonly List<float> starPhase = new List<float>();
    private RectTransform cloud, sign;
    private readonly List<RectTransform> lamps = new List<RectTransform>();
    private readonly List<RawImage> cones = new List<RawImage>();
    private readonly List<Image> pools = new List<Image>();
    private float flickerUntil, nextFlicker;

    private float nextSpawn, nextBubble;
    private int spawned;
    private readonly List<int> freeSlots = new List<int>();

    static Sprite circle;

    public void Build()
    {
        RectTransform canvas = (RectTransform)transform;
        float w = Mathf.Max(1920f, canvas.rect.width);

        // l'antic fons (la imatge fixa) ja no cal
        foreach (Image i in GetComponentsInChildren<Image>(true))
            if (i.gameObject.name == "Background") i.enabled = false;

        scene = MenuScreens.Rect(transform, "EscenaHistoria", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scene.SetAsFirstSibling();

        BuildRoom(w);
        BuildWindow();
        BuildDoor();
        BuildLamps();

        crowd = MenuScreens.Rect(scene, "Gent", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BuildChef();

        // vinyeta: les vores més fosques, per centrar la mirada
        RawImage vig = MenuScreens.Rect(scene, "Vinyeta", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        vig.texture = VignetteTexture();
        vig.raycastTarget = false;

        for (int i = 0; i < Slots.Length; i++) freeSlots.Add(i);
        nextSpawn = Time.unscaledTime + 0.6f;
        nextBubble = Time.unscaledTime + 2f;
        nextFlicker = Time.unscaledTime + Random.Range(2f, 5f);
    }

    // ---------- L'HABITACIÓ ----------

    void BuildRoom(float w)
    {
        // paret: paper pintat de ratlles granates
        RawImage wall = MenuScreens.Rect(scene, "Paret", new Vector2(0f, 0.5f), Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        wall.texture = WallpaperTexture();
        wall.uvRect = new Rect(0f, 0f, w / 96f, 540f / 96f);
        wall.raycastTarget = false;

        // terra: taulons de fusta
        RawImage floor = MenuScreens.Rect(scene, "Terra", Vector2.zero, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        floor.texture = PlanksTexture();
        floor.uvRect = new Rect(0f, 0f, w / 192f, 540f / 96f);
        floor.raycastTarget = false;

        // sòcol de fusta fosca i una motllura
        Stretch(scene, "Socol", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 70f), new Color(0.22f, 0.12f, 0.08f));
        Stretch(scene, "Motllura", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 70f), new Vector2(0f, 80f), new Color(0.45f, 0.28f, 0.14f));
        Stretch(scene, "OmbraSocol", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -14f), new Vector2(0f, 0f), new Color(0f, 0f, 0f, 0.35f));
    }

    // ---------- LA FINESTRA ----------

    void BuildWindow()
    {
        RectTransform win = Box(scene, "Finestra", new Vector2(-500f, FloorLine + 175f), new Vector2(390f, 260f), new Color(0.16f, 0.09f, 0.06f));
        // el cel (amb màscara, perquè el núvol i la lluna no en surtin)
        RectTransform sky = MenuScreens.Rect(win, "Cel", Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));
        RawImage skyImg = sky.gameObject.AddComponent<RawImage>();
        skyImg.texture = GradientTexture(new Color(0.05f, 0.06f, 0.16f), new Color(0.20f, 0.13f, 0.32f));
        skyImg.raycastTarget = false;
        sky.gameObject.AddComponent<RectMask2D>();

        for (int i = 0; i < 16; i++)
        {
            Image s = Img(sky, "Estrella", new Vector2(Random.value, Random.Range(0.3f, 1f)), Vector2.zero, Vector2.zero, Vector2.one * (i % 4 == 0 ? 8f : 5f), Color.white);
            stars.Add(s);
            starPhase.Add(Random.Range(0f, 10f));
        }
        // lluna creixent: un cercle i un altre del color del cel al damunt
        Image moon = Img(sky, "Lluna", new Vector2(0.75f, 0.68f), Vector2.zero, Vector2.zero, new Vector2(80f, 80f), new Color(1f, 0.95f, 0.75f));
        moon.sprite = Circle();
        Image bite = Img(sky, "Lluna2", new Vector2(0.75f, 0.68f), Vector2.zero, new Vector2(-22f, 12f), new Vector2(70f, 70f), new Color(0.10f, 0.08f, 0.22f));
        bite.sprite = Circle();

        cloud = MenuScreens.Rect(sky, "Nuvol", new Vector2(0f, 0.55f), new Vector2(0f, 0.55f), Vector2.zero, Vector2.zero);
        Color cc = new Color(0.30f, 0.27f, 0.42f, 0.9f);
        foreach (Vector4 c in new[] { new Vector4(0f, 0f, 90f, 40f), new Vector4(-30f, 12f, 60f, 50f), new Vector4(25f, 14f, 55f, 45f) })
            Img(cloud, "Bufa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(c.x, c.y), new Vector2(c.z, c.w), cc).sprite = Circle();

        // els barrots i l'ampit
        Img(win, "BarrotV", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 230f), new Color(0.16f, 0.09f, 0.06f));
        Img(win, "BarrotH", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 12f), new Color(0.16f, 0.09f, 0.06f));
        Img(win, "Ampit", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(430f, 18f), new Color(0.45f, 0.28f, 0.14f));
        // cortines vermelles als costats
        foreach (float side in new[] { -1f, 1f })
        {
            Image cur = Img(win, "Cortina", new Vector2(side < 0 ? 0f : 1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(side * 6f, 0f), new Vector2(60f, 272f), new Color(0.55f, 0.10f, 0.14f));
            Img(cur.rectTransform, "Plec", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-side * 10f, 0f), new Vector2(8f, 272f), new Color(0.40f, 0.06f, 0.10f));
        }
    }

    // ---------- LA PORTA ----------

    void BuildDoor()
    {
        RectTransform frame = Box(scene, "MarcPorta", new Vector2(480f, FloorLine + 175f), new Vector2(260f, 350f), new Color(0.16f, 0.09f, 0.06f));
        RectTransform door = Box(frame, "Porta", new Vector2(0f, -8f), new Vector2(226f, 326f), new Color(0.42f, 0.24f, 0.14f));
        Box(door, "Plafo1", new Vector2(0f, -85f), new Vector2(170f, 110f), new Color(0.34f, 0.19f, 0.11f));
        Box(door, "Vidre", new Vector2(0f, 95f), new Vector2(150f, 90f), new Color(0.95f, 0.78f, 0.40f)); // llum de dins
        Img(door, "Pom", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(82f, -10f), new Vector2(20f, 20f), new Color(0.95f, 0.78f, 0.30f)).sprite = Circle();

        // el cartell penjat d'un clau, que es gronxa
        Img(door, "Clau", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(10f, 10f), new Color(0.6f, 0.6f, 0.65f)).sprite = Circle();
        sign = MenuScreens.Rect(door, "Cartell", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        sign.anchoredPosition = new Vector2(0f, 40f);
        foreach (float side in new[] { -1f, 1f })
        {
            Image str = Img(sign, "Cordill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(4f, 64f), new Color(0.85f, 0.8f, 0.65f));
            str.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * 58f);
        }
        Box(sign, "Vora", new Vector2(0f, -70f), new Vector2(200f, 78f), new Color(0.50f, 0.08f, 0.08f));
        Box(sign, "Fusta", new Vector2(0f, -70f), new Vector2(186f, 64f), new Color(0.96f, 0.90f, 0.76f));
        MenuScreens.Text(sign, "TANCAT", 46f, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(180f, 60f), new Color(0.80f, 0.12f, 0.12f), true);
    }

    // ---------- ELS LLUMS PENJATS ----------

    void BuildLamps()
    {
        foreach (float x in new[] { -760f, 760f })
        {
            RectTransform pivot = MenuScreens.Rect(scene, "Llum", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            pivot.anchoredPosition = new Vector2(x, 0f);
            Img(pivot, "Cable", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(4f, 130f), new Color(0.1f, 0.08f, 0.08f));

            // el con de llum (per sota de la pantalla del llum)
            RectTransform coneRt = MenuScreens.Rect(pivot, "Con", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            coneRt.pivot = new Vector2(0.5f, 1f);
            coneRt.sizeDelta = new Vector2(560f, 720f);
            coneRt.anchoredPosition = new Vector2(0f, -160f);
            RawImage cone = coneRt.gameObject.AddComponent<RawImage>();
            cone.texture = ConeTexture();
            cone.color = new Color(1f, 0.88f, 0.55f, 0.20f);
            cone.raycastTarget = false;
            cones.Add(cone);

            Image bulb = Img(pivot, "Bombeta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(34f, 34f), new Color(1f, 0.95f, 0.7f));
            bulb.sprite = Circle();
            Img(pivot, "Pantalla", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(110f, 34f), new Color(0.15f, 0.35f, 0.25f));
            Img(pivot, "Pantalla2", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(60f, 16f), new Color(0.12f, 0.28f, 0.20f));
            Img(pivot, "Vora", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -157f), new Vector2(116f, 6f), new Color(0.85f, 0.70f, 0.30f));
            lamps.Add(pivot);

            // la taca de llum al terra
            Image pool = Img(scene, "TacaLlum", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(x, 420f), new Vector2(480f, 120f), new Color(1f, 0.85f, 0.5f, 0.12f));
            pool.sprite = Circle();
            pools.Add(pool);
        }
    }

    // ---------- ELS PERSONATGES ----------

    void BuildChef()
    {
        int sel = Progression.IsChefUnlocked(ChefRoster.Selected) ? ChefRoster.Selected : 0;
        string n = ChefRoster.All[sel].sprites;
        chefDown = Resources.Load<Sprite>("Chefs/" + n + "_Down");
        chefLeft = Resources.Load<Sprite>("Chefs/" + n + "_Left");
        chefRight = Resources.Load<Sprite>("Chefs/" + n + "_Right");
        chef = MakePerson("Xef", new[] { chefDown, chefDown }, new Vector2(0f, 430f), 220f);
        chef.arrived = true;
        chef.target = chef.root.anchoredPosition;
    }

    Person MakePerson(string name, Sprite[] frames, Vector2 feet, float size)
    {
        Person p = new Person();
        p.root = MenuScreens.Rect(crowd, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        p.root.anchorMin = p.root.anchorMax = new Vector2(0.5f, 0f);
        p.root.pivot = new Vector2(0.5f, 0f);
        p.root.sizeDelta = new Vector2(size, size);
        p.root.anchoredPosition = feet;

        Image shadow = Img(p.root, "Ombra", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.04f), new Vector2(size * 0.55f, size * 0.13f), new Color(0f, 0f, 0f, 0.35f));
        shadow.sprite = Circle();

        p.body = MenuScreens.Rect(p.root, "Cos", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        p.img = p.body.gameObject.AddComponent<Image>();
        p.img.sprite = frames[0];
        p.img.preserveAspect = true;
        p.img.raycastTarget = false;
        p.img.enabled = frames[0] != null;
        p.frames = frames;
        p.phase = Random.Range(0f, 10f);
        return p;
    }

    void SpawnClient()
    {
        if (freeSlots.Count == 0) return;
        int si = freeSlots[Random.Range(0, freeSlots.Count)];
        freeSlots.Remove(si);
        string type = ClientTypes[spawned % ClientTypes.Length];
        spawned++;
        Sprite a = Resources.Load<Sprite>("Walk/" + type + "_Front_1");
        Sprite b = Resources.Load<Sprite>("Walk/" + type + "_Front_2");
        if (a == null || b == null) return;

        Vector2 target = Slots[si];
        float startX = target.x < 0f ? -1150f : 1150f;
        float size = Mathf.Lerp(200f, 170f, Mathf.InverseLerp(370f, 490f, target.y)); // els del fons, una mica més petits
        Person p = MakePerson(type, new[] { a, b }, new Vector2(startX, target.y), size);
        p.target = target;
        clients.Add(p);
    }

    void Bubble(Person p)
    {
        string text = Complaints[Random.Range(0, Complaints.Length)];
        TextMeshProUGUI t = MenuScreens.Text(p.root, text, 30f, new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(320f, 44f), new Color(1f, 0.95f, 0.6f), true);
        StartCoroutine(BubbleLife(t));
        p.shakeUntil = Time.unscaledTime + 0.6f;

        // el xef mira cap a qui crida
        chef.img.sprite = p.root.anchoredPosition.x < 0f ? chefLeft : chefRight;
        if (chef.img.sprite == null) chef.img.sprite = chefDown;
        chefLookUntil = Time.unscaledTime + 1.2f;
    }

    System.Collections.IEnumerator BubbleLife(TextMeshProUGUI t)
    {
        float start = Time.unscaledTime;
        const float Life = 1.5f;
        while (t != null && Time.unscaledTime - start < Life)
        {
            float k = Time.unscaledTime - start;
            float s = k < 0.12f ? k / 0.12f * 1.2f : k < 0.2f ? Mathf.Lerp(1.2f, 1f, (k - 0.12f) / 0.08f) : 1f;
            t.rectTransform.localScale = Vector3.one * s;
            t.rectTransform.anchoredPosition = new Vector2(0f, 10f + k * 12f);
            t.alpha = k > Life - 0.35f ? (Life - k) / 0.35f : 1f;
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    // ---------- ANIMACIÓ ----------

    void Update()
    {
        if (scene == null) return;
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // estrelles, núvol, cartell i llums
        for (int i = 0; i < stars.Count; i++)
        {
            Color c = stars[i].color;
            c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 1.3f + starPhase[i]));
            stars[i].color = c;
        }
        if (cloud != null) cloud.anchoredPosition = new Vector2(Mathf.Repeat(t * 18f, 560f) - 100f, 0f);
        if (sign != null) sign.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f) * 7f + Mathf.Sin(t * 4.3f) * 1.5f);

        if (t > nextFlicker)
        {
            flickerUntil = t + Random.Range(0.15f, 0.45f);
            nextFlicker = t + Random.Range(3f, 7f);
        }
        bool flick = t < flickerUntil && Mathf.FloorToInt(t * 30f) % 3 != 0;
        for (int i = 0; i < lamps.Count; i++)
        {
            float rot = Mathf.Sin(t * 1.1f + i * 1.7f) * 4f;
            lamps[i].localRotation = Quaternion.Euler(0f, 0f, rot);
            float a = flick && i == 0 ? 0.05f : 0.20f; // només parpelleja el de l'esquerra
            cones[i].color = new Color(1f, 0.88f, 0.55f, a);
            Color pc = pools[i].color; pc.a = a * 0.6f; pools[i].color = pc;
            pools[i].rectTransform.anchoredPosition = new Vector2(lamps[i].anchoredPosition.x - rot * 12f, 420f);
        }

        // nous clients i queixes
        if (t > nextSpawn && freeSlots.Count > 0)
        {
            SpawnClient();
            nextSpawn = t + Random.Range(1.0f, 1.8f);
        }
        if (t > nextBubble)
        {
            List<Person> ready = clients.FindAll(c => c.arrived);
            if (ready.Count > 0) Bubble(ready[Random.Range(0, ready.Count)]);
            nextBubble = t + Random.Range(1.1f, 2.0f);
        }

        // el xef: respira i, si ningú no crida, torna a mirar endavant
        chef.body.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 2.2f)) * 4f);
        if (t > chefLookUntil && chefDown != null) chef.img.sprite = chefDown;

        foreach (Person p in clients)
        {
            Vector2 pos = p.root.anchoredPosition;
            if (!p.arrived)
            {
                float dir = Mathf.Sign(p.target.x - pos.x);
                pos.x += dir * 240f * dt;
                if ((dir > 0f && pos.x >= p.target.x) || (dir < 0f && pos.x <= p.target.x)) { pos.x = p.target.x; p.arrived = true; }
                p.root.anchoredPosition = pos;
                int f = Mathf.FloorToInt(t * 8f + p.phase) % 2;
                p.img.sprite = p.frames[f];
                p.body.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 12f + p.phase)) * 10f);
            }
            else
            {
                // impacients: piquen de peus, i quan criden tremolen
                int f = Mathf.FloorToInt(t * 2.5f + p.phase) % 2;
                p.img.sprite = p.frames[f];
                float shake = t < p.shakeUntil ? Mathf.Sin(t * 60f) * 4f : 0f;
                p.body.anchoredPosition = new Vector2(shake, Mathf.Abs(Mathf.Sin(t * 5f + p.phase)) * 4f);
            }
        }

        // els de més avall (més a prop) es dibuixen al davant
        List<Person> all = new List<Person>(clients) { chef };
        all.Sort((a, b) => b.root.anchoredPosition.y.CompareTo(a.root.anchoredPosition.y));
        for (int i = 0; i < all.Count; i++) all[i].root.SetSiblingIndex(i);
    }

    // ---------- PECES I TEXTURES ----------

    static Image Img(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = MenuScreens.Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image i = rt.gameObject.AddComponent<Image>();
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    // una peça amb ancoratges que s'estiren (de vora a vora): offMin/offMax en lloc de mida
    static Image Stretch(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color color)
    {
        RectTransform rt = MenuScreens.Rect(parent, name, aMin, aMax, offMin, offMax);
        Image i = rt.gameObject.AddComponent<Image>();
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    // una capsa de color, centrada (des de baix al mig si el pare és l'escena)
    RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        Vector2 anchor = parent == scene ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        return Img(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, size, color).rectTransform;
    }

    static Texture2D NewTex(int w, int h, bool point)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        t.wrapMode = point ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        return t;
    }

    static Sprite Circle()
    {
        if (circle != null) return circle;
        const int n = 32;
        Texture2D t = NewTex(n, n, true);
        t.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - n / 2f, dy = y + 0.5f - n / 2f;
                px[y * n + x] = dx * dx + dy * dy <= (n / 2f) * (n / 2f) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        t.SetPixels32(px);
        t.Apply();
        circle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }

    static Texture2D WallpaperTexture()
    {
        Texture2D t = NewTex(16, 16, true);
        Color a = new Color(0.33f, 0.10f, 0.14f), b = new Color(0.27f, 0.08f, 0.12f), line = new Color(0.46f, 0.30f, 0.16f);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                Color c = x < 8 ? a : b;
                if (x == 0) c = line;
                if (x == 12 && y % 8 == 4) c = line; // uns puntets daurats
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return t;
    }

    static Texture2D PlanksTexture()
    {
        Texture2D t = NewTex(32, 16, true);
        Color[] tones = { new Color(0.40f, 0.25f, 0.15f), new Color(0.36f, 0.22f, 0.13f) };
        Color seam = new Color(0.20f, 0.12f, 0.07f), grain = new Color(0.33f, 0.20f, 0.12f);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 32; x++)
            {
                int row = y / 8;
                Color c = tones[row];
                int joint = row == 0 ? 6 : 22;
                if (y % 8 == 0 || x == joint) c = seam;
                else if ((x * 7 + y * 3) % 23 == 0) c = grain;
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return t;
    }

    static Texture2D GradientTexture(Color top, Color bottom)
    {
        Texture2D t = NewTex(2, 32, false);
        for (int y = 0; y < 32; y++)
        {
            Color c = Color.Lerp(bottom, top, y / 31f);
            t.SetPixel(0, y, c); t.SetPixel(1, y, c);
        }
        t.Apply();
        return t;
    }

    static Texture2D ConeTexture()
    {
        const int n = 64;
        Texture2D t = NewTex(n, n, false);
        for (int y = 0; y < n; y++)
        {
            float down = 1f - y / (float)(n - 1);       // 0 a dalt, 1 a baix
            float half = 3f + down * 29f;
            for (int x = 0; x < n; x++)
            {
                float e = Mathf.Clamp01(1f - Mathf.Abs(x + 0.5f - n / 2f) / half);
                float a = Mathf.SmoothStep(0f, 1f, e) * (1f - down * 0.7f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        return t;
    }

    static Texture2D VignetteTexture()
    {
        const int n = 64;
        Texture2D t = NewTex(n, n, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.707f;
                t.SetPixel(x, y, new Color(0.03f, 0.01f, 0.03f, Mathf.SmoothStep(0f, 0.75f, (d - 0.45f) / 0.55f)));
            }
        t.Apply();
        return t;
    }
}
