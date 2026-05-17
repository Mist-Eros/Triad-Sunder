using UnityEngine;
using TMPro;
using System;

public class UIController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    private TextMeshProUGUI textMeshProUGUI;

    // Start is called before the first frame update
    void Start()
    {
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        string tmp = "";
        for (int i = 1; i <= Math.Floor(playerController.currentStamina); i++)
            tmp += '=';
        textMeshProUGUI.text = tmp;
    }
}
