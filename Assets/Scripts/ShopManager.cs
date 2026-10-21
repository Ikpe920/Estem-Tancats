using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public GameObject panel;
    public bool isOpen;

    public PlayerMovement movement;
    public PlayerShooting shooting;
    public PlayerHealth health;

    public TextMeshProUGUI damageText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI fireRateText;
    public TextMeshProUGUI healText;

    int damageCost = 40;
    int speedCost = 30;
    int fireRateCost = 40;
    int healCost = 20;

    void Start()
    {
        panel.SetActive(false);
    }

    public void Open()
    {
        isOpen = true;
        panel.SetActive(true);
        Time.timeScale = 0f; // pausa el joc
        RefreshTexts();
    }

    public void Close()
    {
        isOpen = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void BuyDamage()
    {
        if (!ScoreManager.Instance.TrySpend(damageCost)) return;
        shooting.damage += 1;
        damageCost += 30;
        RefreshTexts();
    }

    public void BuySpeed()
    {
        if (!ScoreManager.Instance.TrySpend(speedCost)) return;
        movement.speed += 0.5f;
        speedCost += 20;
        RefreshTexts();
    }

    public void BuyFireRate()
    {
        if (!ScoreManager.Instance.TrySpend(fireRateCost)) return;
        shooting.fireRate = Mathf.Max(0.1f, shooting.fireRate * 0.85f);
        fireRateCost += 30;
        RefreshTexts();
    }

    public void BuyHeal()
    {
        if (!ScoreManager.Instance.TrySpend(healCost)) return;
        health.Heal(30);
        RefreshTexts();
    }

    void RefreshTexts()
    {
        int pts = ScoreManager.Instance.score;
        damageText.text = "Més dany (" + damageCost + ")";
        speedText.text = "Més velocitat (" + speedCost + ")";
        fireRateText.text = "Disparar més ràpid (" + fireRateCost + ")";
        healText.text = "Curar +30 (" + healCost + ")";
    }
}