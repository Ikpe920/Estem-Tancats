using UnityEngine;
using TMPro;

public class GameOver : MonoBehaviour
{
    public TextMeshProUGUI resultText;

    void Start()
    {
        resultText.text = "Has arribat a l'onada: \nPunts: ";
    }

    void Update()
    {
        
    }
}
