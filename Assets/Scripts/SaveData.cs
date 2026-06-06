using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // Unlocks
    public List<int> unlockedLevels = new List<int> { 2 }; // First real level at build index 2 (skip Bootstrap=0, MainMenu=1)
    public List<string> unlockedWeapons = new List<string>();
    public List<string> unlockedRelics = new List<string>();
    public List<string> unlockedClasses = new List<string>();

    // Equipped loadout
    public string equippedWeapon0;
    public string equippedWeapon1;
    public List<string> equippedRelics = new List<string>();
    public string equippedClass;

    // Alignment
    public StoryMode storyMode = StoryMode.Light;

    // Difficulty
    public float enemyHealthMultiplier = 1f;

    // Proxy properties for array safety
    public string[] EquippedWeapons
    {
        get => new[] { equippedWeapon0, equippedWeapon1 };
        set
        {
            equippedWeapon0 = (value != null && value.Length > 0) ? value[0] : null;
            equippedWeapon1 = (value != null && value.Length > 1) ? value[1] : null;
        }
    }

    public bool IsWeaponUnlocked(string name) => unlockedWeapons.Contains(name);
    public bool IsRelicUnlocked(string name) => unlockedRelics.Contains(name);
    public bool IsClassUnlocked(string name) => unlockedClasses.Contains(name);
    public bool IsLevelUnlocked(int index) => unlockedLevels.Contains(index);

    public bool HasBothWeaponsEquipped =>
        !string.IsNullOrEmpty(equippedWeapon0) && !string.IsNullOrEmpty(equippedWeapon1);

    public bool HasClassEquipped => !string.IsNullOrEmpty(equippedClass);
}
