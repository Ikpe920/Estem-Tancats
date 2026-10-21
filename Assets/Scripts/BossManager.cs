using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossManager : MonoBehaviour
{
    [Header("Introducció")]
    public GameObject introPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI subtitleText;
    public float introDuration = 3f;

    [Header("Barra de vida")]
    public GameObject healthBarRoot;
    public Slider healthSlider;
    public TextMeshProUGUI healthBarName;

    private Enemy currentBoss;
    private int bossMaxHealth;

    void Start()
    {
        introPanel.SetActive(false);
        healthBarRoot.SetActive(false);
    }

    public IEnumerator PlayIntro(string bossName, string subtitle)
    {
        nameText.text = bossName;
        subtitleText.text = subtitle;

        CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = introPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        introPanel.SetActive(true);
        Time.timeScale = 0f; // pausa el joc

        float fade = 0.5f;
        float t = 0f;
        while (t < introDuration)
        {
            t += Time.unscaledDeltaTime; // el temps normal està aturat

            float alpha = 1f;
            if (t < fade) alpha = t / fade;
            else if (t > introDuration - fade) alpha = (introDuration - t) / fade;
            cg.alpha = alpha;

            // el nom comença gran i es redueix
            float scale = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(t / fade));
            nameText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        introPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void TrackBoss(Enemy boss, string bossName)
    {
        if (boss == null) return;
        currentBoss = boss;
        bossMaxHealth = boss.maxHealth;
        healthBarName.text = bossName;
        healthSlider.value = 1f;
        healthBarRoot.SetActive(true);
    }

    void Update()
    {
        if (currentBoss == null)
        {
            if (healthBarRoot.activeSelf) healthBarRoot.SetActive(false);
            return;
        }

        healthSlider.value = (float)currentBoss.CurrentHealth / bossMaxHealth;
    }
}