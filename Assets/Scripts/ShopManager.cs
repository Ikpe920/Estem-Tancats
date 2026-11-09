using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public GameObject panel;
    public bool isOpen;

    public PlayerMovement movement;
    public PlayerShooting shooting;
    public PlayerHealth health;

    // textos de la botiga antiga (els millores Ara els donen els boons)
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI fireRateText;
    public TextMeshProUGUI healText;
    public TextMeshProUGUI dashCooldownText;

    int damageCost = 40;
    int speedCost = 30;
    int fireRateCost = 40;
    int dashCooldownCost = 50;

    public int healCost = 20;     // servei: curar un cor
    public int maxLifeCost = 60;  // servei: +1 cor màxim
    public int rerollCost = 30;   // servei: canviar les cartes dels boons

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        isOpen = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f; // pausa el joc
        RefreshTexts();
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---------- SERVEIS (es compren des de la pantalla dels boons) ----------

    // curar un cor
    public bool TryBuyHeal()
    {
        if (health == null) return false;
        if (health.CurrentLives >= health.maxLives) return false; // ja té tots els cors
        if (!TrySpend(healCost)) return false;
        health.Heal(1);
        return true;
    }

    // +1 cor màxim (i el deixes ple)
    public bool TryBuyMaxLife()
    {
        if (health == null) return false;
        if (health.maxLives >= PlayerHealth.MaxLivesCap) return false; // ja té el màxim de cors
        if (!TrySpend(maxLifeCost)) return false;
        health.maxLives += 1;
        health.Heal(1);
        return true;
    }

    // canviar les tres cartes de boó que s'estan oferint
    public bool TryBuyReroll(BoonManager boonManager)
    {
        if (boonManager == null) return false;
        if (!boonManager.CanReroll()) return false; // no hi ha altres boons per oferir
        if (!TrySpend(rerollCost)) return false;
        boonManager.RerollCards();
        return true;
    }

    bool TrySpend(int amount)
    {
        if (ScoreManager.Instance == null) return false;
        return ScoreManager.Instance.TrySpend(amount);
    }

    // ---------- MÈTODES ANTICS (es mantenen per no trencar els botons de l'escena) ----------

    public void BuyDamage()
    {
        if (shooting == null) return;
        if (!TrySpend(damageCost)) return;
        shooting.damage += 1;
        damageCost += 30;
        RefreshTexts();
    }

    public void BuySpeed()
    {
        if (movement == null) return;
        if (!TrySpend(speedCost)) return;
        movement.speed += 0.5f;
        speedCost += 20;
        RefreshTexts();
    }

    public void BuyFireRate()
    {
        if (shooting == null) return;
        if (!TrySpend(fireRateCost)) return;
        shooting.fireRate = Mathf.Max(0.1f, shooting.fireRate * 0.85f);
        fireRateCost += 30;
        RefreshTexts();
    }

    public void BuyHeal()
    {
        if (TryBuyHeal()) RefreshTexts();
    }

    public void BuyDashCooldown()
    {
        if (movement == null) return;
        if (!TrySpend(dashCooldownCost)) return;
        movement.dashCooldown = Mathf.Max(0.25f, movement.dashCooldown - 0.15f);
        dashCooldownCost += 40;
        RefreshTexts();
    }

    void RefreshTexts()
    {
        int pts = ScoreManager.Instance != null ? ScoreManager.Instance.score : 0;
        if (damageText != null) damageText.text = "Més de dany (" + damageCost + ")";
        if (speedText != null) speedText.text = "Més de velocitat (" + speedCost + ")";
        if (fireRateText != null) fireRateText.text = "Disparar més ràpid (" + fireRateCost + ")";
        if (healText != null) healText.text = "Curar un cor (" + healCost + ")";
        if (dashCooldownText != null) dashCooldownText.text = "Dash més ràpid (" + dashCooldownCost + ")";
    }
}
