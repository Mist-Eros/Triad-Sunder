using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuHandler : MonoBehaviour
{
    [SerializeField] private Button playBtn; 

    // Start is called before the first frame update
    void Start()
    {
        playBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(2);
        });
    }
}
