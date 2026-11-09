using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
//  SO I MÚSICA
// =====================================================================
// Es crea sol en començar el joc i viu entre escenes. No cal posar-lo enlloc.
//  - Efectes:  AudioManager.Play("shoot")  ->  Resources/Audio/Sfx/shoot.wav
//  - Música:   cada escena i cada acte tenen la seva (Resources/Audio/Music/*.ogg), amb fos entre temes.
//              Si un acte té música assignada a l'ActManager, es fa servir aquella.
// El volum general és el del menú de pausa (AudioListener.volume).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const float MusicVolume = 0.45f;
    const float SfxVolume = 0.8f;
    const int Voices = 16;

    // volum propi de cada so (els que sonen molt sovint, més fluixos)
    static readonly Dictionary<string, float> Volumes = new Dictionary<string, float>
    {
        { "shoot", 0.16f }, { "hit", 0.45f }, { "crit", 0.55f }, { "enemy_die", 0.32f },
        { "hurt", 0.6f }, { "death", 0.6f }, { "dash", 0.45f }, { "heal", 0.4f },
        { "pickup", 0.35f }, { "coin", 0.32f }, { "coin_big", 0.4f },
        { "boon", 0.35f }, { "set", 0.45f }, { "combo", 0.22f }, { "challenge", 0.45f },
        { "wave_clear", 0.35f }, { "wave_perfect", 0.4f }, { "event", 0.45f },
        { "ui_click", 0.22f }, { "ui_back", 0.22f },
        { "boss_intro", 0.9f }, { "explosion", 0.75f }, { "table", 0.7f }, { "box_break", 0.65f },
        { "bounce", 0.3f }, { "fuse", 0.22f },
    };

    // temps mínim entre dues vegades el mateix so (perquè 20 impactes alhora no siguin un soroll)
    static readonly Dictionary<string, float> MinGap = new Dictionary<string, float>
    {
        { "shoot", 0.05f }, { "hit", 0.035f }, { "crit", 0.05f }, { "enemy_die", 0.04f },
        { "bounce", 0.05f }, { "explosion", 0.06f }, { "coin", 0.05f }, { "pickup", 0.05f },
    };

    // quantes còpies del mateix so poden sonar alhora (si no hi és, 3)
    static readonly Dictionary<string, int> MaxSame = new Dictionary<string, int>
    {
        { "shoot", 2 }, { "hit", 3 }, { "crit", 2 }, { "enemy_die", 3 }, { "explosion", 2 },
        { "bounce", 2 }, { "coin", 2 }, { "pickup", 2 }, { "combo", 1 }, { "fuse", 2 },
        { "boss_intro", 1 }, { "death", 1 }, { "hurt", 1 }, { "set", 1 }, { "challenge", 1 }, { "event", 1 },
    };

    // els sons que es poden saltar quan ja n'hi ha molts sonant alhora
    static readonly HashSet<string> LowPriority = new HashSet<string>
    {
        "shoot", "hit", "crit", "enemy_die", "bounce", "coin", "pickup", "explosion", "combo",
    };
    const int MaxBusy = 10; // a partir d'aquí, els sons poc importants no sonen

    static readonly Dictionary<string, float> MusicGain = new Dictionary<string, float>
    {
        { "win", 0.6f }, { "gameover", 0.85f },
    };

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private readonly List<KeyValuePair<string, float>> playing = new List<KeyValuePair<string, float>>(); // so i quan acaba
    private AudioSource[] voices;
    private int nextVoice;

    private AudioSource musicA, musicB;  // dues fonts per fer el fos entre temes
    private AudioSource musicNow;
    private string musicName;
    private bool bossMusic;
    private Coroutine fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        Instance = this;
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++) voices[i] = NewSource(false);
        musicA = NewSource(true);
        musicB = NewSource(true);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ActManager.OnActChanged += OnActChanged;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ActManager.OnActChanged -= OnActChanged;
        if (Instance == this) Instance = null;
    }

    AudioSource NewSource(bool loop)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f; // 2D
        return s;
    }

    // ---------- EFECTES ----------

    // fa sonar un efecte; 'pitch' > 0 el força, si no, una mica a l'atzar perquè no soni sempre igual
    public static void Play(string name, float volume = 1f, float pitch = 0f)
    {
        if (Instance != null) Instance.PlayInternal(name, volume, pitch);
    }

    void PlayInternal(string name, float volume, float pitch)
    {
        AudioClip clip = Clip("Audio/Sfx/" + name);
        if (clip == null) return;

        float now = Time.unscaledTime;
        if (MinGap.TryGetValue(name, out float gap) && lastPlayed.TryGetValue(name, out float last) && now - last < gap) return;

        // quants sons sonen ara (i quants d'aquest mateix)
        int same = 0;
        for (int i = playing.Count - 1; i >= 0; i--)
        {
            if (playing[i].Value <= now) { playing.RemoveAt(i); continue; }
            if (playing[i].Key == name) same++;
        }
        int maxSame = MaxSame.TryGetValue(name, out int m) ? m : 3;
        if (same >= maxSame) return;
        if (playing.Count >= MaxBusy && LowPriority.Contains(name)) return;

        lastPlayed[name] = now;

        AudioSource s = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        s.pitch = pitch > 0f ? pitch : Random.Range(0.95f, 1.05f);
        float v = Volumes.TryGetValue(name, out float vol) ? vol : 0.5f;
        s.PlayOneShot(clip, v * volume * SfxVolume);
        playing.Add(new KeyValuePair<string, float>(name, now + clip.length / Mathf.Max(0.5f, s.pitch)));
    }

    AudioClip Clip(string path)
    {
        if (clips.TryGetValue(path, out AudioClip c)) return c;
        c = Resources.Load<AudioClip>(path);
        clips[path] = c;
        return c;
    }

    // ---------- MÚSICA ----------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bossMusic = false;
        if (scene.name == SceneNames.Game) PlayActMusic();
        else if (scene.name == SceneNames.GameOver) PlayMusic("gameover", false);
        else if (scene.name == SceneNames.Win) PlayMusic("win", false);
        else PlayMusic("menu", true); // menú principal i història
    }

    void OnActChanged(int act)
    {
        if (!bossMusic) PlayActMusic();
    }

    void PlayActMusic()
    {
        ActManager am = FindAnyObjectByType<ActManager>();
        int act = am != null ? am.currentAct : 0;
        ActData data = am != null ? am.Current : null;
        if (data != null && data.music != null) { PlayClip("act_custom_" + act, data.music, true); return; }
        // al torn de nit, la versió de nit de la sala (si no hi és, la normal)
        string actName = "act" + Mathf.Clamp(act, 0, 4);
        if (am != null && am.NightShift && Clip("Audio/Music/night" + Mathf.Clamp(act, 0, 4)) != null)
            actName = "night" + Mathf.Clamp(act, 0, 4);
        PlayMusic(actName, true);
    }

    // la música dels caps: sona mentre dura l'onada del cap
    public static void SetBossMusic(bool on)
    {
        if (Instance == null || Instance.bossMusic == on) return;
        Instance.bossMusic = on;
        if (on) Instance.PlayMusic("boss", true);
        else Instance.PlayActMusic();
    }

    public void PlayMusic(string name, bool loop)
    {
        PlayClip(name, Clip("Audio/Music/" + name), loop);
    }

    void PlayClip(string name, AudioClip clip, bool loop)
    {
        if (clip == null) return;
        // ja sona (o ja està entrant) aquest mateix tema
        if (name == musicName && musicNow != null && (musicNow.isPlaying || fade != null)) return;
        musicName = name;

        if (fade != null) StopCoroutine(fade);

        // les dues fonts: la que sona i la que entrarà. Si un fos anterior s'havia quedat a mitges,
        // totes dues poden estar sonant: ens assegurem que la nova comenci aturada.
        AudioSource from = musicNow;
        AudioSource to = musicNow == musicA ? musicB : musicA;
        to.Stop();
        to.clip = clip;
        to.loop = loop;
        to.volume = 0f;
        musicNow = to;

        float target = MusicVolume * (MusicGain.TryGetValue(name, out float g) ? g : 1f);
        fade = StartCoroutine(FadeOutThenIn(from, to, target, loop ? 0.35f : 0.15f, loop ? 0.5f : 0.1f));
    }

    // primer s'apaga la música vella i després entra la nova: mai sonen dues músiques alhora
    IEnumerator FadeOutThenIn(AudioSource from, AudioSource to, float target, float outTime, float inTime)
    {
        if (from != null && from.isPlaying)
        {
            float start = from.volume;
            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime) // també amb el joc en pausa
            {
                from.volume = start * (1f - t / outTime);
                yield return null;
            }
        }
        if (from != null) { from.Stop(); from.volume = 0f; }

        to.Play();
        for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
        {
            to.volume = target * (t / inTime);
            yield return null;
        }
        to.volume = target;
        fade = null;
    }

    void Update()
    {
        // amb el menú de pausa obert, la música baixa una mica
        if (musicNow == null || fade != null) return;
        float target = MusicVolume * (MusicGain.TryGetValue(musicName ?? "", out float g) ? g : 1f);
        if (PauseMenu.IsOpen) target *= 0.4f;
        musicNow.volume = Mathf.MoveTowards(musicNow.volume, target, Time.unscaledDeltaTime);
    }
}
