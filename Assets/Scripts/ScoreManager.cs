using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public int score;
    public int totalScore;
    public float scoreMultiplier = 1f; // els boons de "boa fortuna" l'augmenten

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(int amount)
    {
        int points = Mathf.RoundToInt(amount * scoreMultiplier);
        score += points;
        totalScore += points;
    }
    public bool TrySpend(int amount)
    {
        if (score < amount) return false;
        score -= amount;
        return true;
    }
}