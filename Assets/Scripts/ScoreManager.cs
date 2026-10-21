using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public int score;

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(int amount)
    {
        score += amount;
    }
    public bool TrySpend(int amount)
    {
        if (score < amount) return false;
        score -= amount;
        return true;
    }
}