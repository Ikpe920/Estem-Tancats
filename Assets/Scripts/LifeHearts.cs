using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LifeHearts : MonoBehaviour
{
    public PlayerHealth playerHealth;    // d'on agafem els cors que queden
    public Sprite heartSprite;            // cor ple
    public Sprite emptyHeartSprite;       // cor buit (opcional)
    public int total = 3;                 // total de cors que es mostren
    public float size = 40f;              // mida de cada cor en pixels
    public float spacing = 44f;           // separació entre cors
    public Vector2 offset = Vector2.zero; // ajust del grupo respecte al centre

    private Image[] hearts;

    void Awake()
    {
        if (heartSprite == null) Debug.LogWarning("LifeHearts: assigna un sprite de cor!");

        BuildHearts();

        // si en aquest objecte hi ha el text antic "Vida: 100", amaguem-lo
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

    // crea els cors com a fills d'aquest objecte (neteja el canvas automàticament)
    void BuildHearts()
    {
        hearts = new Image[total];

        for (int i = 0; i < total; i++)
        {
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
            rt.anchoredPosition = offset + new Vector2((i - (total - 1) * 0.5f) * spacing, 0f);

            hearts[i] = img;
        }
    }

    // posa els cors que queden: els perduts es veuen buits o desapareixen
    public void Refresh(int lives)
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
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
