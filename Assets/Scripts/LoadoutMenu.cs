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

    private static readonly Color EquippedColor   = new Color(0.2f, 0.5f, 0.2f, 1f);
    private static readonly Color UnequippedColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    void OnEnable()  => Populate();
    void OnDisable() => ClearEntries();

    // ============================================================
    // POPULATE
    // ============================================================

    public void Populate()
    {
        ClearEntries();
        if (GameManager.Instance == null) return;

        PopulateWeapons();
        PopulateRelics();
    }

    // ============================================================
    // WEAPONS
    // ============================================================

    void PopulateWeapons()
    {
        var unlockedWeapons = GameManager.Instance.GetAvailableWeapons();
        string eq0 = GameManager.Instance.Save.equippedWeapon0;
        string eq1 = GameManager.Instance.Save.equippedWeapon1;

        foreach (var wpn in unlockedWeapons)
        {
            if (wpn == null) continue;

            bool isEquipped = wpn.weaponName == eq0 || wpn.weaponName == eq1;
            GameObject entry = CreateEntry(weaponTogglePrefab, weaponContentContainer,
                $"Weapon_{wpn.weaponName}", $"{wpn.weaponName}\n<size=80%>{wpn.weaponType}</size>", isEquipped);

            var toggle = entry.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(val => OnWeaponToggled(wpn.weaponName, val));
            weaponToggles.Add(toggle);
            weaponNames.Add(wpn.weaponName);
        }
    }

    void OnWeaponToggled(string weaponName, bool isOn)
    {
        if (isRefreshing || GameManager.Instance == null) return;
        var save = GameManager.Instance.Save;

        if (isOn)
        {
            if (save.equippedWeapon0 == weaponName || save.equippedWeapon1 == weaponName) return;

            if (string.IsNullOrEmpty(save.equippedWeapon0))
                GameManager.Instance.EquipWeapon(0, weaponName);
            else if (string.IsNullOrEmpty(save.equippedWeapon1))
                GameManager.Instance.EquipWeapon(1, weaponName);
            else
            {
                GameManager.Instance.UnequipWeapon(0);
                GameManager.Instance.EquipWeapon(0, weaponName);
            }
        }
        else
        {
            if (save.equippedWeapon0 == weaponName)
                GameManager.Instance.UnequipWeapon(0);
            else if (save.equippedWeapon1 == weaponName)
                GameManager.Instance.UnequipWeapon(1);
        }

        RefreshToggles(weaponToggles, weaponNames, name =>
            GameManager.Instance.Save.equippedWeapon0 == name
            || GameManager.Instance.Save.equippedWeapon1 == name);
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

            string effects = BuildRelicEffectsString(relic);
            bool isEquipped = equippedRelics.Contains(relic.relicName);

            GameObject entry = CreateEntry(relicTogglePrefab, relicContentContainer,
                $"Relic_{relic.relicName}", $"{relic.relicName}\n<size=65%>{effects}</size>", isEquipped);

            var toggle = entry.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(val => OnRelicToggled(relic.relicName, val));
            relicToggles.Add(toggle);
            relicNames.Add(relic.relicName);
        }
    }

    void OnRelicToggled(string relicName, bool isOn)
    {
        if (isRefreshing || GameManager.Instance == null) return;

        if (isOn)
            GameManager.Instance.EquipRelic(relicName);
        else
            GameManager.Instance.UnequipRelic(relicName);

        RefreshToggles(relicToggles, relicNames,
            name => GameManager.Instance.Save.equippedRelics.Contains(name));
    }

    // ============================================================
    // SHARED HELPERS
    // ============================================================

    GameObject CreateEntry(GameObject prefab, Transform parent,
        string entryName, string labelText, bool isEquipped)
    {
        GameObject entry = Instantiate(prefab, parent);
        entry.SetActive(true);
        entry.name = entryName;

        var label = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = labelText;

        var bg = entry.GetComponent<Image>();
        if (bg != null) bg.color = isEquipped ? EquippedColor : UnequippedColor;

        var toggle = entry.GetComponent<Toggle>();
        if (toggle != null) toggle.isOn = isEquipped;

        return entry;
    }

    void RefreshToggles(List<Toggle> toggles, List<string> names,
        System.Func<string, bool> isEquipped)
    {
        isRefreshing = true;
        for (int i = 0; i < toggles.Count && i < names.Count; i++)
        {
            if (toggles[i] == null) continue;
            bool equipped = isEquipped(names[i]);
            toggles[i].SetIsOnWithoutNotify(equipped);
            var bg = toggles[i].GetComponent<Image>();
            if (bg != null) bg.color = equipped ? EquippedColor : UnequippedColor;
        }
        isRefreshing = false;
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
