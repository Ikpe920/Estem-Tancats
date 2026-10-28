using UnityEngine;
using UnityEngine.SceneManagement;

public class Story : MonoBehaviour
{
    public void PlayButton()
    {
        Time.timeScale = 1f;
        RunStats.Reset(); // comença una partida nova
        SceneManager.LoadScene("ChefSelect");
    }
}
