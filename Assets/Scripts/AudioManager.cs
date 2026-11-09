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

    static readonly Dictionary<string, float> MusicGain = new Dictionary<string, float>
    {
        { "win", 0.6f }, { "gameover", 0.85f },
    };

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
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
        lastPlayed[name] = now;

        AudioSource s = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        s.pitch = pitch > 0f ? pitch : Random.Range(0.95f, 1.05f);
        float v = Volumes.TryGetValue(name, out float vol) ? vol : 0.5f;
        s.PlayOneShot(clip, v * volume * SfxVolume);
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
        PlayMusic("act" + Mathf.Clamp(act, 0, 4), true);
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
        if (name == musicName && musicNow != null && musicNow.isPlaying) return;
        musicName = name;

        AudioSource from = musicNow;
        AudioSource to = musicNow == musicA ? musicB : musicA;
        to.clip = clip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();
        musicNow = to;

        float target = MusicVolume * (MusicGain.TryGetValue(name, out float g) ? g : 1f);
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(Crossfade(from, to, target, loop ? 0.8f : 0.2f));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float target, float seconds)
    {
        float t = 0f;
        float fromStart = from != null ? from.volume : 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // també amb el joc en pausa
            float k = Mathf.Clamp01(t / seconds);
            to.volume = target * k;
            if (from != null) from.volume = fromStart * (1f - k);
            yield return null;
        }
        to.volume = target;
        if (from != null) { from.Stop(); from.volume = 0f; }
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
