using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMainMenu : MonoBehaviour
{
    void Awake()
    {
        SceneManager.LoadScene(1);
    }
}
