using UnityEngine;

// Puntuació de la partida: cada client que despatxes suma punts, i si els encadenes sense
// parar (ratxa) o sense rebre mal el multiplicador puja. Rebre un cop talla la ratxa.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public static event System.Action<bool> OnWaveEnded; // true = onada acabada sense cops

    public int totalScore;
    public float scoreMultiplier = 1f; // el multiplicador dels plats (Safrà, Torrons...)

    [Header("Ratxa (combo)")]
    public float comboWindow = 4f;        // segons que tens per despatxar el següent client
    public int killsPerTier = 5;          // clients seguits per pujar un graó
    public float tierBonus = 0.25f;       // el que puja el multiplicador a cada graó
    public float maxComboMultiplier = 3f; // topall del multiplicador de ratxa

    [Header("Bonus d'onada")]
    public int waveBonusPerWave = 40;     // punts per número d'onada en acabar-la
    public int flawlessFactor = 2;        // ×2 si l'onada s'acaba sense rebre cap cop

    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public float ComboTimeLeft { get; private set; }

    public float ComboMultiplier
    {
        get { return Mathf.Min(maxComboMultiplier, 1f + (Combo / Mathf.Max(1, killsPerTier)) * tierBonus); }
    }

    private bool hitThisWave;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Combo <= 0) return;

        ComboTimeLeft -= Time.deltaTime; // amb el joc pausat (triar plat) la ratxa no corre
        if (ComboTimeLeft <= 0f) EndCombo();
    }

    // punts "plans" (recollectables, plats): només compta el multiplicador dels plats
    public void AddScore(int amount)
    {
        totalScore += Mathf.RoundToInt(amount * scoreMultiplier);
    }

    // un client despatxat: puja la ratxa i els punts depenen del multiplicador actual
    public void AddKill(int basePoints, Vector2 at)
    {
        int before = Mathf.FloorToInt(ComboMultiplier * 100f);

        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
        Progression.OnCombo(Combo);
        ComboTimeLeft = comboWindow;

        totalScore += Mathf.RoundToInt(basePoints * ComboMultiplier * scoreMultiplier);

        // avís quan puges de graó
        if (Combo >= killsPerTier && Mathf.FloorToInt(ComboMultiplier * 100f) > before)
            HazardFx.Popup(at, "RATXA ×" + ComboMultiplier.ToString("0.00").Replace('.', ','), new Color(1f, 0.8f, 0.3f));
    }

    // el jugador ha rebut un cop: es talla la ratxa i l'onada ja no és "perfecta"
    public void NotifyPlayerHit()
    {
        hitThisWave = true;
        EndCombo();
    }

    void EndCombo()
    {
        Combo = 0;
        ComboTimeLeft = 0f;
    }

    // l'onada s'ha acabat: bonus per onada (doble si no t'han tocat). Torna el que ha sumat.
    public int EndWave(int wave, Vector2 at)
    {
        int bonus = waveBonusPerWave * Mathf.Max(1, wave);
        bool flawless = !hitThisWave;
        if (flawless) bonus *= Mathf.Max(1, flawlessFactor);
        hitThisWave = false;
        OnWaveEnded?.Invoke(flawless);

        int gained = Mathf.RoundToInt(bonus * scoreMultiplier);
        totalScore += gained;
        HazardFx.Popup(at, (flawless ? "SERVEI PERFECTE +" : "ONADA +") + gained,
            flawless ? new Color(0.55f, 0.95f, 0.6f) : new Color(1f, 0.9f, 0.4f));
        return gained;
    }
}
