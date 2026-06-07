using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // Unlocks
    public List<int> unlockedLevels = new List<int> { 1 };
    public List<string> unlockedWeapons = new List<string>();
    public List<string> unlockedRelics = new List<string>();

    // Equipped loadout
    public string equippedWeapon0;
    public string equippedWeapon1;
    public List<string> equippedRelics = new List<string>();

    // Alignment
    public StoryMode storyMode = StoryMode.Light;

    // Difficulty
    public float enemyHealthMultiplier = 1f;

    public bool IsWeaponUnlocked(string name) => unlockedWeapons.Contains(name);
    public bool IsRelicUnlocked(string name) => unlockedRelics.Contains(name);
    public bool IsLevelUnlocked(int index) => unlockedLevels.Contains(index);

    public bool HasBothWeaponsEquipped =>
        !string.IsNullOrEmpty(equippedWeapon0) && !string.IsNullOrEmpty(equippedWeapon1);
}
