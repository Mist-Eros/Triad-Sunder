using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum StoryMode
{
    Light,
    Dark,
    Chaos,
    Endless
}

[System.Serializable]
public class PlayerStats
{
    [Header("Ground Movement")]
    public float walkSpeed = 8f;
    public float sprintSpeed = 14f;
    public float groundAcceleration = 60f;
    public float groundFriction = 12f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public int maxAirJumps = 1;

    [Header("Stamina (Pill-Based)")]
    public int maxStaminaPills = 3;
    public float staminaRegenTime = 2f;
    public float sprintStaminaDrainTime = 3f;
    public float jumpStaminaCost = 0f;
    public float airJumpStaminaCost = 1f;
    public float wallJumpStaminaCost = 1f;
    public float slideStaminaCost = 1f;

    [Header("Slide")]
    public float slideMinSpeed = 18f;
    public float slideBoost = 5f;
    public float slideFriction = 3f;
    public float slideMaxSpeed = 40f;
    public float slideSteering = 40f;
    public float slopeSlideAccel = 30f;
    public float slideJumpBoostMultiplier = 2.5f;

    [Header("Wall Jump")]
    public float wallJumpUpForce = 7f;
    public float wallJumpAwayForce = 10f;
    public float wallCheckDistance = 1.2f;
    public float wallJumpCooldown = 0.2f;
    public float wallJumpMinVelocity = 3f;

    [Header("Crouch")]
    public float crouchScaleY = 0.5f;
    public float crouchSpeed = 4f;

    [Header("Air Control")]
    public float airAcceleration = 18f;
    public float airMaxSpeed = 16f;

    [Header("Timing")]
    public float coyoteTime = 0.12f;
    public float jumpCooldown = 0.1f;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string SAVE_KEY = "roguelite_save";

    // Weapon defaults per story mode
    private static readonly string[] DefaultWeapons = { "Axe", "Hammer", "Sword", "Longbow", "Katana", "DualCrossbows" };

    [Header("Player Stats")]
    [SerializeField] private PlayerStats stats = new PlayerStats();

    [Header("Weapon Registry")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();

    [Header("Relic Registry")]
    [SerializeField] private List<RelicData> allRelics = new List<RelicData>();

    [Header("Story Mode Registry")]
    [SerializeField] private List<StoryModeData> allStoryModes = new List<StoryModeData>();

    [Header("Levels")]
    [SerializeField] private int totalLevelCount = 5;

    // Runtime state
    private SaveData save;

    public PlayerStats Stats => stats;
    public SaveData Save => save;

    // Cached lookups
    private Dictionary<string, WeaponData> weaponDataLookup;
    private Dictionary<string, RelicData> relicLookup;
    private Dictionary<StoryMode, StoryModeData> storyModeLookup;

    // Events
    public System.Action OnSaveChanged;
    public System.Action OnLoadoutChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookups();
        Load();
    }

    // ============================================================
    // LOOKUPS
    // ============================================================

    void BuildLookups()
    {
        weaponDataLookup = new Dictionary<string, WeaponData>();
        foreach (var w in allWeapons)
            if (w != null && !weaponDataLookup.ContainsKey(w.weaponName))
                weaponDataLookup[w.weaponName] = w;

        relicLookup = new Dictionary<string, RelicData>();
        foreach (var r in allRelics)
            if (r != null && !relicLookup.ContainsKey(r.relicName))
                relicLookup[r.relicName] = r;

        storyModeLookup = new Dictionary<StoryMode, StoryModeData>();
        foreach (var s in allStoryModes)
            if (s != null && !storyModeLookup.ContainsKey(s.mode))
                storyModeLookup[s.mode] = s;
    }

    public StoryModeData GetStoryModeData()
    {
        storyModeLookup.TryGetValue(save.storyMode, out StoryModeData data);
        return data;
    }

    string[] GetStoryModeDefaultWeapons()
    {
        StoryModeData data = GetStoryModeData();
        return data?.defaultWeapons ?? new[] { "Axe", "Hammer" };
    }

    // ============================================================
    // SAVE / LOAD
    // ============================================================

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(save, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        string json = Application.isEditor ? "" : PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            save = new SaveData();

            // Always unlock the 6 default weapons
            foreach (var name in DefaultWeapons)
            {
                if (!save.unlockedWeapons.Contains(name))
                    save.unlockedWeapons.Add(name);
            }

            // Auto-unlock weapons that exist in registry
            foreach (var w in allWeapons)
            {
                if (w != null && !save.unlockedWeapons.Contains(w.weaponName))
                    save.unlockedWeapons.Add(w.weaponName);
            }

            // Auto-equip based on story mode
            string[] defaults = GetStoryModeDefaultWeapons();
            save.equippedWeapon0 = defaults[0];
            save.equippedWeapon1 = defaults[1];

            SaveGame();
        }
        else
        {
            save = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();

            // Migration: remove invalid level indices
            save.unlockedLevels.RemoveAll(i => i < 1);
            if (save.unlockedLevels.Count == 0)
                save.unlockedLevels.Add(1);

            // Migration: ensure 6 default weapons are always unlocked
            foreach (var name in DefaultWeapons)
            {
                if (!save.unlockedWeapons.Contains(name))
                    save.unlockedWeapons.Add(name);
            }

            // Migration: ensure equipped weapons are set for current mode
            if (string.IsNullOrEmpty(save.equippedWeapon0) || string.IsNullOrEmpty(save.equippedWeapon1))
            {
                string[] modeDefaults = GetStoryModeDefaultWeapons();
                save.equippedWeapon0 = modeDefaults[0];
                save.equippedWeapon1 = modeDefaults[1];
            }

            SaveGame();
        }

        Debug.Log($"[GameManager] Loaded save: {save.unlockedWeapons.Count} weapons, " +
                  $"{save.unlockedRelics.Count} relics, enemy HP x{save.enemyHealthMultiplier:F1}, " +
                  $"story mode: {save.storyMode}");
    }

    public void ResetSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Load();
        OnSaveChanged?.Invoke();
    }

    // ============================================================
    // UNLOCK MANAGEMENT
    // ============================================================

    public void UnlockLevel(int levelIndex)
    {
        if (!save.unlockedLevels.Contains(levelIndex))
        {
            save.unlockedLevels.Add(levelIndex);
            save.unlockedLevels.Sort();
            SaveGame();
            OnSaveChanged?.Invoke();
        }
    }

    public void UnlockWeapon(string weaponName)
    {
        if (!save.unlockedWeapons.Contains(weaponName))
        {
            save.unlockedWeapons.Add(weaponName);
            SaveGame();
            OnSaveChanged?.Invoke();
        }
    }

    public void UnlockRelic(string relicName)
    {
        if (!save.unlockedRelics.Contains(relicName))
        {
            save.unlockedRelics.Add(relicName);
            SaveGame();
            OnSaveChanged?.Invoke();
        }
    }

    // ============================================================
    // EQUIP MANAGEMENT
    // ============================================================

    public void EquipWeapon(int slot, string weaponName)
    {
        if (slot < 0 || slot > 1) return;
        if (!save.IsWeaponUnlocked(weaponName)) return;

        if (slot == 0)
            save.equippedWeapon0 = weaponName;
        else
            save.equippedWeapon1 = weaponName;

        SaveGame();
        OnLoadoutChanged?.Invoke();
    }

    public void UnequipWeapon(int slot)
    {
        if (slot == 0)
            save.equippedWeapon0 = null;
        else
            save.equippedWeapon1 = null;

        SaveGame();
        OnLoadoutChanged?.Invoke();
    }

    public void EquipRelic(string relicName)
    {
        if (!save.IsRelicUnlocked(relicName)) return;
        if (save.equippedRelics.Contains(relicName)) return;

        save.equippedRelics.Add(relicName);
        SaveGame();
        OnLoadoutChanged?.Invoke();
    }

    public void UnequipRelic(string relicName)
    {
        save.equippedRelics.Remove(relicName);
        SaveGame();
        OnLoadoutChanged?.Invoke();
    }

    // ============================================================
    // QUERIES
    // ============================================================

    public List<WeaponData> GetUnlockedWeapons()
    {
        return allWeapons.Where(w => w != null && save.IsWeaponUnlocked(w.weaponName)).ToList();
    }

    /// <summary>
    /// Weapons available for the current story mode.
    /// Story modes return only their 2 defaults. Endless returns all unlocked weapons.
    /// </summary>
    public List<WeaponData> GetAvailableWeapons()
    {
        if (save.storyMode == StoryMode.Endless)
            return allWeapons.Where(w => w != null && save.IsWeaponUnlocked(w.weaponName)).ToList();

        string[] modeDefaults = GetStoryModeDefaultWeapons();
        return allWeapons.Where(w => w != null
            && modeDefaults.Contains(w.weaponName)
            && save.IsWeaponUnlocked(w.weaponName)).ToList();
    }

    public List<RelicData> GetUnlockedRelics()
    {
        return allRelics.Where(r => r != null && save.IsRelicUnlocked(r.relicName)).ToList();
    }

    public List<WeaponData> GetAllWeapons() => allWeapons.Where(w => w != null).ToList();
    public List<RelicData> GetAllRelics() => allRelics.Where(r => r != null).ToList();

    public WeaponData GetEquippedWeaponData(int slot)
    {
        string name = slot == 0 ? save.equippedWeapon0 : save.equippedWeapon1;
        if (string.IsNullOrEmpty(name)) return null;
        weaponDataLookup.TryGetValue(name, out WeaponData data);
        return data;
    }

    public GameObject GetEquippedWeaponPrefab(int slot)
    {
        WeaponData data = GetEquippedWeaponData(slot);
        return data?.prefab;
    }

    public List<RelicData> GetEquippedRelicData()
    {
        return save.equippedRelics
            .Select(n => relicLookup.TryGetValue(n, out RelicData r) ? r : null)
            .Where(r => r != null)
            .ToList();
    }

    public List<int> GetUnlockedLevels() => new List<int>(save.unlockedLevels);

    // ============================================================
    // STAT COMPUTATION (story mode + relics combined)
    // ============================================================

    public float GetEffectiveDamageMultiplier()
    {
        StoryModeData sm = GetStoryModeData();
        float mult = sm?.damageMultiplier ?? 1f;
        foreach (RelicData relic in GetEquippedRelicData())
            mult *= relic.damageMultiplier;
        return mult;
    }

    public float GetEffectiveAttackSpeedMultiplier()
    {
        StoryModeData sm = GetStoryModeData();
        float mult = sm?.attackSpeedMultiplier ?? 1f;
        foreach (RelicData relic in GetEquippedRelicData())
            mult *= relic.attackSpeedMultiplier;
        return mult;
    }

    public float GetEffectiveMoveSpeedMultiplier()
    {
        StoryModeData sm = GetStoryModeData();
        float mult = sm?.moveSpeedMultiplier ?? 1f;
        foreach (RelicData relic in GetEquippedRelicData())
            mult *= relic.moveSpeedMultiplier;
        return mult;
    }

    public float GetEffectiveMaxHealth()
    {
        StoryModeData sm = GetStoryModeData();
        float baseHP = sm?.maxHealth ?? 100f;
        float mult = 1f;
        foreach (RelicData relic in GetEquippedRelicData())
            mult *= relic.maxHealthMultiplier;
        return baseHP * mult;
    }

    public float GetEffectiveHealOnKill()
    {
        float h = 0f;
        foreach (RelicData relic in GetEquippedRelicData())
            h += relic.healOnKill;
        return h;
    }

    public float GetEffectiveDamageReduction()
    {
        float dr = 0f;
        foreach (RelicData relic in GetEquippedRelicData())
            dr = Mathf.Max(dr, relic.damageReduction);
        return dr;
    }

    public int GetEffectiveStaminaPills()
    {
        StoryModeData sm = GetStoryModeData();
        return sm?.maxStaminaPills ?? 3;
    }

    public float GetEffectiveStaminaRegenTime()
    {
        StoryModeData sm = GetStoryModeData();
        float baseVal = sm?.staminaRegenTime ?? 2f;
        float mult = 1f;
        foreach (RelicData relic in GetEquippedRelicData())
            mult *= relic.staminaRegenMultiplier;
        return baseVal / mult;
    }

    // ============================================================
    // PLAYER ALIGNMENT
    // ============================================================

    public StoryMode StoryMode
    {
        get => save.storyMode;
        set
        {
            save.storyMode = value;
            SaveGame();
        }
    }

    // ============================================================
    // ENEMY HEALTH MULTIPLIER
    // ============================================================

    public float EnemyHealthMultiplier
    {
        get => save.enemyHealthMultiplier;
        set
        {
            save.enemyHealthMultiplier = Mathf.Max(0.5f, value);
            SaveGame();
        }
    }

    public float GetEnemyHealth(float baseHealth)
    {
        return baseHealth * save.enemyHealthMultiplier;
    }

    public void IncreaseEnemyHealthMultiplier(float amount)
    {
        EnemyHealthMultiplier += amount;
    }

    public void OnPlayerDeath()
    {
        // Increase difficulty on death
        IncreaseEnemyHealthMultiplier(0.5f);
        Debug.Log($"[GameManager] Player died. Enemy health multiplier: x{save.enemyHealthMultiplier:F1}");
    }

    public void OnPlayerWin()
    {
        // Optionally scale difficulty on win
        IncreaseEnemyHealthMultiplier(0.2f);
        Debug.Log($"[GameManager] Player won. Enemy health multiplier: x{save.enemyHealthMultiplier:F1}");
    }

    // ============================================================
    // LEGACY STAT BOOSTS
    // ============================================================

    public void BoostWalkSpeed(float amount)    => stats.walkSpeed += amount;
    public void BoostSprintSpeed(float amount)  => stats.sprintSpeed += amount;
    public void BoostJumpForce(float amount)    => stats.jumpForce += amount;
    public void BoostAcceleration(float amount) => stats.groundAcceleration += amount;

    public void AddStaminaPill()
    {
        stats.maxStaminaPills = Mathf.Min(stats.maxStaminaPills + 1, 5);
    }
    public void RemoveStaminaPill()
    {
        stats.maxStaminaPills = Mathf.Max(stats.maxStaminaPills - 1, 1);
    }

    public void AddAirJump()
    {
        stats.maxAirJumps = Mathf.Min(stats.maxAirJumps + 1, 3);
    }
    public void RemoveAirJump()
    {
        stats.maxAirJumps = Mathf.Max(stats.maxAirJumps - 1, 0);
    }

    public void BoostSlideJump(float amount)
    {
        stats.slideJumpBoostMultiplier += amount;
    }

    public void BoostStaminaRegen(float amount)
    {
        stats.staminaRegenTime = Mathf.Max(stats.staminaRegenTime - amount, 0.5f);
    }

    public void ResetAllStats()
    {
        stats = new PlayerStats();
    }
}
