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
    public TextMeshProUGUI dashCooldownText; // opcional: si no el poses, no es mostra

    int damageCost = 40;
    int speedCost = 30;
    int fireRateCost = 40;
    int healCost = 20;
    int dashCooldownCost = 50;

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
        if (health == null) return;
        if (health.CurrentLives >= health.maxLives) return; // ja té tots els cors
        if (!ScoreManager.Instance.TrySpend(healCost)) return;
        health.Heal(1); // cada compra Cura un cor
        RefreshTexts();
    }

    public void BuyDashCooldown()
    {
        if (!ScoreManager.Instance.TrySpend(dashCooldownCost)) return;
        movement.dashCooldown = Mathf.Max(0.25f, movement.dashCooldown - 0.15f);
        dashCooldownCost += 40;
        RefreshTexts();
    }

    void RefreshTexts()
    {
        int pts = ScoreManager.Instance.score;
        damageText.text = "Més de dany (" + damageCost + ")";
        speedText.text = "Més de velocitat (" + speedCost + ")";
        fireRateText.text = "Disparar més ràpid (" + fireRateCost + ")";
        healText.text = "Curar un cor (" + healCost + ")";
        if (dashCooldownText != null)
            dashCooldownText.text = "Dash més ràpid (" + dashCooldownCost + ")";
    }
}