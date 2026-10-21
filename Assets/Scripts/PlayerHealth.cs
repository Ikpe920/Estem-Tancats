using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int health;
    public int CurrentHealth => health;

    void Awake()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        health = Mathf.Max(health - amount, 0);
        Debug.Log("Vida: " + health);

        if (health <= 0)
        {
            FindFirstObjectByType<GameOverManager>().ShowGameOver();
        }
    }

    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }   
}