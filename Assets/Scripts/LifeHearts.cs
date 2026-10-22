using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LifeHearts : MonoBehaviour
{
    public PlayerHealth playerHealth;     // d'on agafem els cors que queden
    public Sprite heartSprite;            // cor ple
    public Sprite emptyHeartSprite;       // cor buit (opcional)
    public int baseHearts = 3;            // cors inicials: queden on són, els extra s'afegeixen a la dreta
    public float size = 40f;              // mida de cada cor en pixels
    public float spacing = 44f;           // separació entre cors
    public Vector2 offset = Vector2.zero; // ajust del grup respecte al centre

    private readonly List<Image> hearts = new List<Image>();

    void Awake()
    {
        if (heartSprite == null) Debug.LogWarning("LifeHearts: assigna un sprite de cor!");

        int start = playerHealth != null ? playerHealth.maxLives : baseHearts;
        EnsureHearts(start, false);

        // si en aquest objecte hi ha el text antic "Vida: 100", l'amaguem
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.enabled = false;
    }

    void OnEnable()
    {
        if (playerHealth == null) return;
        playerHealth.OnLivesChanged += Refresh;
        Refresh(playerHealth.CurrentLives);
    }

    void OnDisable()
    {
        if (playerHealth == null) return;
        playerHealth.OnLivesChanged -= Refresh;
    }

    // crea els cors que falten (si el màxim ha pujat, n'apareix un de nou a la dreta)
    void EnsureHearts(int count, bool animate)
    {
        while (hearts.Count < count)
        {
            int i = hearts.Count;

            GameObject go = new GameObject("Cor" + (i + 1), typeof(RectTransform));
            go.transform.SetParent(transform, false);

            Image img = go.AddComponent<Image>();
            img.sprite = heartSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = offset + new Vector2((i - (baseHearts - 1) * 0.5f) * spacing, 0f);

            hearts.Add(img);

            if (animate) StartCoroutine(PopIn(rt));
        }
    }

    // el cor nou creix una mica de més i s'assenta
    IEnumerator PopIn(RectTransform rt)
    {
        float t = 0f;
        float duration = 0.35f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // el joc està en pausa mentre es tria el plat
            float k = Mathf.Clamp01(t / duration);
            float scale = k < 0.7f
                ? Mathf.Lerp(0f, 1.3f, k / 0.7f)
                : Mathf.Lerp(1.3f, 1f, (k - 0.7f) / 0.3f);
            rt.localScale = Vector3.one * scale;
            yield return null;
        }

        rt.localScale = Vector3.one;
    }

    // posa els cors que queden: els perduts es veuen buits o desapareixen
    public void Refresh(int lives)
    {
        int max = playerHealth != null ? playerHealth.maxLives : hearts.Count;
        EnsureHearts(max, true);

        for (int i = 0; i < hearts.Count; i++)
        {
            if (i >= max)
            {
                hearts[i].enabled = false; // aquest cor encara no existeix
                continue;
            }

            bool alive = i < lives;

            if (emptyHeartSprite != null)
            {
                hearts[i].sprite = alive ? heartSprite : emptyHeartSprite;
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = alive; // sense cor buit, els cors perduts desapareixen
            }
        }
    }
}