using UnityEngine;

[CreateAssetMenu(fileName = "NewRelic", menuName = "Game/Relic Data")]
public class RelicData : ScriptableObject
{
    [Header("Identity")]
    public string relicName = "Unnamed";
    public Sprite icon;
    [TextArea(2, 5)]
    public string description;

    [Header("Stat Modifiers")]
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float maxHealthMultiplier = 1f;
    public float staminaRegenMultiplier = 1f;
    public float healOnKill;
    public float damageReduction; // 0-1
}
