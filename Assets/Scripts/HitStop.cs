using System.Collections;
using UnityEngine;

// Una pausa minúscula en el moment de l'impacte: el joc gairebé s'atura uns centèsimes de segon.
// Es nota molt als crítics i en matar un cap, i no toca el balanç (no canvia cap xifra).
//
// No fa servir timeScale = 0 sinó un valor molt baix, perquè la resta del codi (triar plat, introducció
// del cap, canvi d'acte) mira "timeScale == 0" per saber si el joc ja estava pausat. Si una d'aquestes
// pantalles s'obre mentre dura la pausa, aquí no tornem a tocar el temps.
public class HitStop : MonoBehaviour
{
    const float StopScale = 0.02f;

    static HitStop instance;
    static float nextAllowed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        nextAllowed = 0f;
    }

    // force = true: ignora el temps mínim entre pauses (per a coses grans, com matar un cap)
    public static void Do(float duration, bool force = false)
    {
        if (duration <= 0f) return;
        if (Time.timeScale < 0.5f) return;                       // pausat (menú de plats...) o ja en pausa d'impacte
        if (!force && Time.unscaledTime < nextAllowed) return;   // evita que mil impactes seguits ho arrosseguin

        if (instance == null)
        {
            GameObject go = new GameObject("HitStop");
            instance = go.AddComponent<HitStop>();
        }

        nextAllowed = Time.unscaledTime + duration + 0.12f;
        instance.StartCoroutine(instance.Run(duration));
    }

    IEnumerator Run(float duration)
    {
        float previous = Time.timeScale;
        Time.timeScale = StopScale;

        yield return new WaitForSecondsRealtime(duration);

        // només restaurem si ningú més ha tocat el temps entremig
        if (Mathf.Approximately(Time.timeScale, StopScale)) Time.timeScale = previous;
    }

    void OnDestroy()
    {
        if (Mathf.Approximately(Time.timeScale, StopScale)) Time.timeScale = 1f;
        if (instance == this) instance = null;
    }
}
