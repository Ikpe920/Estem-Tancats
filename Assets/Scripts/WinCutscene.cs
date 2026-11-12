using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// =====================================================================
//  L'ESCENA FINAL (quan guanyes): per fi, el restaurant pot TANCAR
// =====================================================================
// La façana a trenc d'alba (Resources/Backgrounds/WinPorta). Els últims clients surten rodolant per la
// porta, plens com un ou; el xef surt cansat però content, gira el cartell de la porta d'OBERT a TANCAT,
// fa un salt d'alegria i... a la pantalla de victòria. Uns 7 segons; un clic o una tecla la salten.
public class WinCutscene : MonoBehaviour
{
    static readonly string[] Clients = { "Enemy", "Nen", "Iaia", "Turista", "EnemyTank", "Golafre" };

    // posicions a la pantalla (1920x1080, des de baix a l'esquerra); la porta és al mig
    static readonly Vector2 Door = new Vector2(960f, 170f);
    static readonly Vector2 SignPos = new Vector2(960f, 520f);

    RectTransform root;
    bool skip;

    public static IEnumerator Play(Canvas canvas)
    {
        Sprite bg = Resources.Load<Sprite>("Backgrounds/WinPorta");
        if (canvas == null || bg == null) yield break;
        WinCutscene c = canvas.gameObject.AddComponent<WinCutscene>();
        yield return c.Run(bg);
        if (c.root != null) Destroy(c.root.gameObject);
        Destroy(c);
    }

    Image Img(string name, Sprite sp, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.sprite = sp;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse ms = Mouse.current;
        Gamepad pad = Gamepad.current;
        if ((kb != null && kb.anyKey.wasPressedThisFrame) || (ms != null && ms.leftButton.wasPressedThisFrame) ||
            (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)))
            skip = true;
    }

    IEnumerator Wait(float s)
    {
        for (float t = 0f; t < s && !skip; t += Time.unscaledDeltaTime) yield return null;
    }

    IEnumerator Run(Sprite bgSprite)
    {
        GameObject r = new GameObject("EscenaFinal", typeof(RectTransform), typeof(Image));
        root = r.GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.SetAsLastSibling();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        Image bg = r.GetComponent<Image>();
        bg.sprite = bgSprite;
        bg.raycastTarget = true; // que els botons de sota no rebin clics mentre dura

        // el cartell de la porta: de moment diu OBERT
        Sprite open = Resources.Load<Sprite>("Backgrounds/CartellObert");
        Sprite closed = Resources.Load<Sprite>("Backgrounds/CartellTancat");
        Image sign = Img("Cartell", open, SignPos - new Vector2(0f, 38f), new Vector2(192f, 76f));

        // el xef (amb el vestit que porta)
        ChefRoster.Chef chef = ChefRoster.All[Progression.IsChefUnlocked(ChefRoster.Selected) ? ChefRoster.Selected : 0];
        Sprite down = ChefRoster.LoadSprite(chef, "Down");
        Sprite right = ChefRoster.LoadSprite(chef, "Right");
        Sprite walkA = down != null ? Resources.Load<Sprite>("Walk/" + down.name + "_1") : null;
        Sprite walkB = down != null ? Resources.Load<Sprite>("Walk/" + down.name + "_2") : null;

        AudioManager.Play("wave_clear", 0.8f);

        // 1. els clients surten rodolant per la porta, plens
        List<Image> clients = new List<Image>();
        List<Vector2> vel = new List<Vector2>();
        List<float> spin = new List<float>();
        float nextClient = 0f;
        int spawned = 0;
        for (float t = 0f; t < 2.6f && !skip; t += Time.unscaledDeltaTime)
        {
            if (t >= nextClient && spawned < 5)
            {
                nextClient = t + 0.35f;
                string n = Clients[Random.Range(0, Clients.Length)];
                Sprite sp = Resources.Load<Sprite>("Walk/" + n + "_Front_1");
                if (sp != null)
                {
                    Image ci = Img("Client", sp, Door, new Vector2(170f, 170f));
                    ci.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    float side = spawned % 2 == 0 ? -1f : 1f;
                    clients.Add(ci);
                    vel.Add(new Vector2(side * Random.Range(420f, 620f), Random.Range(380f, 520f)));
                    spin.Add(-side * Random.Range(300f, 520f));
                    AudioManager.Play("enemy_die", 0.7f);
                }
                spawned++;
            }
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] == null) continue;
                Vector2 v = vel[i];
                v.y -= 900f * dt;
                vel[i] = v;
                RectTransform rt = clients[i].rectTransform;
                rt.anchoredPosition += v * dt;
                rt.localRotation = Quaternion.Euler(0f, 0f, rt.localRotation.eulerAngles.z + spin[i] * dt);
                if (rt.anchoredPosition.y < 60f && v.y < 0f) { vel[i] = new Vector2(v.x * 0.8f, -v.y * 0.5f); } // rebot a terra
            }
            yield return null;
        }
        foreach (Image ci in clients) if (ci != null) Destroy(ci.gameObject);

        // 2. el xef surt per la porta, caminant
        Image cf = Img("Xef", down, Door, new Vector2(220f, 220f));
        Vector2 stand = Door + new Vector2(-230f, -20f);
        for (float t = 0f; t < 1.2f && !skip; t += Time.unscaledDeltaTime)
        {
            float k = t / 1.2f;
            cf.rectTransform.anchoredPosition = Vector2.Lerp(Door, stand, k);
            cf.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1f, k);
            if (walkA != null && walkB != null) cf.sprite = Mathf.Repeat(t * 6f, 2f) < 1f ? walkA : walkB;
            yield return null;
        }
        cf.rectTransform.anchoredPosition = stand;
        cf.rectTransform.localScale = Vector3.one;
        cf.sprite = right != null ? right : down;
        yield return Wait(0.35f);

        // 3. gira el cartell: OBERT -> TANCAT
        if (!skip)
        {
            yield return Squash(cf.rectTransform, 0.25f);
            for (float t = 0f; t < 0.18f && !skip; t += Time.unscaledDeltaTime)
            {
                sign.rectTransform.localScale = new Vector3(1f - t / 0.18f, 1f, 1f);
                yield return null;
            }
        }
        sign.sprite = closed != null ? closed : open;
        if (!skip)
            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                sign.rectTransform.localScale = new Vector3(t / 0.18f, 1f, 1f);
                yield return null;
            }
        sign.rectTransform.localScale = Vector3.one;
        AudioManager.Play("set", 0.9f);
        Sparkles(SignPos, 10);

        // 4. per fi! un salt d'alegria
        cf.sprite = down;
        if (!skip)
        {
            TMPro.TextMeshProUGUI say = MenuScreens.Text(root, "PER FI... TANCAT!", 46f, Vector2.zero, stand + new Vector2(0f, 270f), new Vector2(700f, 70f), UIStyle.Gold, true);
            UIStyle.Outline(say, 0.25f);
            for (int j = 0; j < 2 && !skip; j++)
            {
                for (float t = 0f; t < 0.4f && !skip; t += Time.unscaledDeltaTime)
                {
                    float h = Mathf.Sin(t / 0.4f * Mathf.PI) * 70f;
                    cf.rectTransform.anchoredPosition = stand + new Vector2(0f, h);
                    yield return null;
                }
                AudioManager.Play("bounce", 0.6f);
                Sparkles(stand + new Vector2(0f, 120f), 6);
            }
            cf.rectTransform.anchoredPosition = stand;
        }
        yield return Wait(1.2f);

        // 5. fos a negre i cap a la pantalla de victòria
        Image black = Img("Negre", null, Vector2.zero, Vector2.zero);
        RectTransform brt = black.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
        brt.pivot = new Vector2(0.5f, 0.5f);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            black.color = new Color(0f, 0f, 0f, t / 0.4f);
            yield return null;
        }
    }

    IEnumerator Squash(RectTransform rt, float d)
    {
        for (float t = 0f; t < d; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Sin(t / d * Mathf.PI);
            rt.localScale = new Vector3(1f + 0.2f * k, 1f - 0.2f * k, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // estrelletes que surten i s'esvaeixen (fetes d'imatges de la interfície)
    void Sparkles(Vector2 at, int n)
    {
        Sprite star = Resources.Load<Sprite>("Fx/Ember");
        if (star == null) return;
        for (int i = 0; i < n; i++) StartCoroutine(Star(star, at));
    }

    IEnumerator Star(Sprite sp, Vector2 at)
    {
        Image s = Img("Estrella", sp, at, new Vector2(40f, 40f));
        RectTransform rt = s.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        Vector2 v = Random.insideUnitCircle.normalized * Random.Range(200f, 420f);
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            if (s == null) yield break;
            rt.anchoredPosition += v * Time.unscaledDeltaTime;
            v *= 0.94f;
            rt.localRotation = Quaternion.Euler(0f, 0f, t * 600f);
            s.color = new Color(1f, 1f, 1f, 1f - t / 0.6f);
            yield return null;
        }
        if (s != null) Destroy(s.gameObject);
    }
}
