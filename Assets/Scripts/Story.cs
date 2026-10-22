using UnityEngine;
using UnityEngine.SceneManagement;

public class Story : MonoBehaviour
{
    public void PlayButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SampleScene");
    }
}
