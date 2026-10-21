using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public int score;
    public int totalScore;

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(int amount)
    {
        score += amount;
        totalScore += amount;
    }
    public bool TrySpend(int amount)
    {
        if (score < amount) return false;
        score -= amount;
        return true;
    }
}