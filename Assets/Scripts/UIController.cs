using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private HealthComponent playerHealth;
    private TextMeshProUGUI staminaText;

    void Start()
    {
        staminaText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (playerController == null) return;

        int pills = playerController.CurrentStaminaPills;
        int maxPills = playerController.MaxStaminaPills;

        string display = "";
        for (int i = 0; i < maxPills; i++)
            display += i < pills ? "=" : "-";

        staminaText.text = display + $" player: {playerHealth.CurrentHealth}";
    }
}
