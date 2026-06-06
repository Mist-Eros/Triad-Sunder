using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LoadoutMenu : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Transform weaponContentContainer;
    [SerializeField] private GameObject weaponTogglePrefab;

    [Header("Relics")]
    [SerializeField] private Transform relicContentContainer;
    [SerializeField] private GameObject relicTogglePrefab;

    private List<Toggle> weaponToggles = new List<Toggle>();
    private List<Toggle> relicToggles = new List<Toggle>();
    private List<string> weaponNames = new List<string>();
    private List<string> relicNames = new List<string>();

    private bool isRefreshing;

    // Colours for equipped / unequipped toggle backgrounds
    private static readonly Color EquippedColor = new Color(0.2f, 0.5f, 0.2f, 1f);
    private static readonly Color UnequippedColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    void OnEnable()
    {
        Populate();
    }

    void OnDisable()
    {
        ClearEntries();
    }

    public void Populate()
    {
        ClearEntries();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LoadoutMenu] GameManager not found.");
            return;
        }

        PopulateWeapons();
        PopulateRelics();
    }

    // ============================================================
    // WEAPONS
    // ============================================================

    void PopulateWeapons()
    {
        var unlockedWeapons = GameManager.Instance.GetAvailableWeapons();
        string equipped0 = GameManager.Instance.Save.equippedWeapon0;
        string equipped1 = GameManager.Instance.Save.equippedWeapon1;

        foreach (var wpn in unlockedWeapons)
        {
            if (wpn == null) continue;

            GameObject entry = Instantiate(weaponTogglePrefab, weaponContentContainer);
            entry.SetActive(true);
            entry.name = $"Weapon_{wpn.weaponName}";

            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{wpn.weaponName}\n<size=80%>{wpn.weaponType}</size>";

            // Tint background based on equipped state
            bool isEquipped = wpn.weaponName == equipped0 || wpn.weaponName == equipped1;
            var bg = entry.GetComponent<Image>();
            if (bg != null)
                bg.color = isEquipped ? EquippedColor : UnequippedColor;

            var toggle = entry.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = isEquipped;
                toggle.onValueChanged.AddListener((val) => OnWeaponToggled(wpn.weaponName, val));
                weaponToggles.Add(toggle);
                weaponNames.Add(wpn.weaponName);
            }
        }
    }

    void OnWeaponToggled(string weaponName, bool isOn)
    {
        if (isRefreshing || GameManager.Instance == null) return;

        if (isOn)
        {
            // Already equipped — nothing to do
            if (GameManager.Instance.Save.equippedWeapon0 == weaponName ||
                GameManager.Instance.Save.equippedWeapon1 == weaponName)
                return;

            // Find free slot
            if (string.IsNullOrEmpty(GameManager.Instance.Save.equippedWeapon0))
                GameManager.Instance.EquipWeapon(0, weaponName);
            else if (string.IsNullOrEmpty(GameManager.Instance.Save.equippedWeapon1))
                GameManager.Instance.EquipWeapon(1, weaponName);
            else
            {
                // Both slots full — unequip slot 0, equip new there
                GameManager.Instance.UnequipWeapon(0);
                GameManager.Instance.EquipWeapon(0, weaponName);
            }
        }
        else
        {
            // Unequip from whichever slot holds this weapon
            if (GameManager.Instance.Save.equippedWeapon0 == weaponName)
                GameManager.Instance.UnequipWeapon(0);
            else if (GameManager.Instance.Save.equippedWeapon1 == weaponName)
                GameManager.Instance.UnequipWeapon(1);
        }

        // Sync all toggle visuals
        RefreshWeaponToggles();
    }

    void RefreshWeaponToggles()
    {
        isRefreshing = true;

        string eq0 = GameManager.Instance.Save.equippedWeapon0;
        string eq1 = GameManager.Instance.Save.equippedWeapon1;

        for (int i = 0; i < weaponToggles.Count && i < weaponNames.Count; i++)
        {
            if (weaponToggles[i] == null) continue;

            bool equipped = weaponNames[i] == eq0 || weaponNames[i] == eq1;

            // Update toggle without firing callbacks
            weaponToggles[i].SetIsOnWithoutNotify(equipped);

            // Update background colour
            var bg = weaponToggles[i].GetComponent<Image>();
            if (bg != null)
                bg.color = equipped ? EquippedColor : UnequippedColor;
        }

        isRefreshing = false;
    }

    // ============================================================
    // RELICS
    // ============================================================

    void PopulateRelics()
    {
        var unlockedRelics = GameManager.Instance.GetUnlockedRelics();
        var equippedRelics = GameManager.Instance.Save.equippedRelics;

        foreach (var relic in unlockedRelics)
        {
            if (relic == null) continue;

            GameObject entry = Instantiate(relicTogglePrefab, relicContentContainer);
            entry.SetActive(true);
            entry.name = $"Relic_{relic.relicName}";

            // Build compact tooltip
            string effects = BuildRelicEffectsString(relic);

            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{relic.relicName}\n<size=65%>{effects}</size>";

            bool isEquipped = equippedRelics.Contains(relic.relicName);

            var bg = entry.GetComponent<Image>();
            if (bg != null)
                bg.color = isEquipped ? EquippedColor : UnequippedColor;

            var toggle = entry.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = isEquipped;
                toggle.onValueChanged.AddListener((val) => OnRelicToggled(relic.relicName, val));
                relicToggles.Add(toggle);
                relicNames.Add(relic.relicName);
            }
        }
    }

    string BuildRelicEffectsString(RelicData relic)
    {
        var parts = new List<string>();

        if (!Mathf.Approximately(relic.damageMultiplier, 1f))
            parts.Add($"DMG x{relic.damageMultiplier:F1}");
        if (!Mathf.Approximately(relic.attackSpeedMultiplier, 1f))
            parts.Add($"AS x{relic.attackSpeedMultiplier:F1}");
        if (!Mathf.Approximately(relic.moveSpeedMultiplier, 1f))
            parts.Add($"SPD x{relic.moveSpeedMultiplier:F1}");
        if (!Mathf.Approximately(relic.maxHealthMultiplier, 1f))
            parts.Add($"HP x{relic.maxHealthMultiplier:F1}");
        if (relic.healOnKill > 0)
            parts.Add($"+{relic.healOnKill}HP/kill");
        if (relic.damageReduction > 0)
            parts.Add($"-{relic.damageReduction * 100:F0}%dmg");
        if (!Mathf.Approximately(relic.staminaRegenMultiplier, 1f))
            parts.Add($"STA x{relic.staminaRegenMultiplier:F1}");

        return parts.Count > 0 ? string.Join("  ", parts) : "No effects";
    }

    void OnRelicToggled(string relicName, bool isOn)
    {
        if (isRefreshing || GameManager.Instance == null) return;

        if (isOn)
            GameManager.Instance.EquipRelic(relicName);
        else
            GameManager.Instance.UnequipRelic(relicName);

        RefreshRelicToggles();
    }

    void RefreshRelicToggles()
    {
        isRefreshing = true;

        var equippedRelics = GameManager.Instance.Save.equippedRelics;

        for (int i = 0; i < relicToggles.Count && i < relicNames.Count; i++)
        {
            if (relicToggles[i] == null) continue;

            bool equipped = equippedRelics.Contains(relicNames[i]);

            relicToggles[i].SetIsOnWithoutNotify(equipped);

            var bg = relicToggles[i].GetComponent<Image>();
            if (bg != null)
                bg.color = equipped ? EquippedColor : UnequippedColor;
        }

        isRefreshing = false;
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    void ClearEntries()
    {
        foreach (var t in weaponToggles)
            if (t != null) Destroy(t.gameObject);
        foreach (var t in relicToggles)
            if (t != null) Destroy(t.gameObject);

        weaponToggles.Clear();
        relicToggles.Clear();
        weaponNames.Clear();
        relicNames.Clear();
    }
}
