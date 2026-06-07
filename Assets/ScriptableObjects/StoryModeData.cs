using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryMode", menuName = "Game/Story Mode Data")]
public class StoryModeData : ScriptableObject
{
    [Header("Identity")]
    public StoryMode mode = StoryMode.Light;
    public string displayName = "Light";
    public Sprite icon;
    [TextArea(2, 5)]
    public string description;

    [Header("Default Weapons")]
    public string[] defaultWeapons = { "Axe", "Hammer" };

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public int maxStaminaPills = 3;
    public float staminaRegenTime = 2f;
}
