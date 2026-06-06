using UnityEngine;

[CreateAssetMenu(fileName = "NewClass", menuName = "Game/Class Data")]
public class ClassData : ScriptableObject
{
    [Header("Identity")]
    public string className = "Unnamed";
    public Sprite icon;
    [TextArea(2, 5)]
    public string description;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public int maxStaminaPills = 3;
    public float staminaRegenTime = 2f;
}
