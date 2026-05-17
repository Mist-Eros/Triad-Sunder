using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager INSTANCE { get; private set; }

    void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            Destroy(gameObject);
        }
        INSTANCE = this;
        DontDestroyOnLoad(gameObject);
    }

    
    public int Level = 0;
}
