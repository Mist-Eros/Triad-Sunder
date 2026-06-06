using UnityEngine;
using System.Collections.Generic;

public class LoadoutApplier : MonoBehaviour
{
    [Header("Weapon Parent")]
    [SerializeField] private Transform weaponHolder;

    private CombatController combatController;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LoadoutApplier] No GameManager instance — skipping loadout.");
            return;
        }

        combatController = GetComponent<CombatController>();
        if (combatController == null)
        {
            combatController = gameObject.AddComponent<CombatController>();
        }

        ApplyWeapons();
        ApplyClassStats();
        ApplyRelicStats();
        ApplyHealth();
        HookPlayerDeath();
    }

    void ApplyWeapons()
    {
        // Remove existing weapon children
        Transform holder = weaponHolder ?? transform;
        List<Transform> existing = new List<Transform>();
        foreach (Transform child in holder)
        {
            if (child.GetComponent<WeaponBase>() != null)
                existing.Add(child);
        }
        foreach (Transform ex in existing)
            Destroy(ex.gameObject);

        // Instantiate equipped weapons
        List<WeaponBase> equipped = new List<WeaponBase>();

        for (int slot = 0; slot < 2; slot++)
        {
            string weaponName = slot == 0
                ? GameManager.Instance.Save.equippedWeapon0
                : GameManager.Instance.Save.equippedWeapon1;

            if (string.IsNullOrEmpty(weaponName)) continue;

            GameObject prefab = GameManager.Instance.GetEquippedWeaponPrefab(slot);
            if (prefab == null)
            {
                Debug.LogWarning($"[LoadoutApplier] No prefab found for weapon: {weaponName}");
                continue;
            }

            GameObject instance = Instantiate(prefab, holder);
            instance.name = weaponName;

            // Position weapons slightly offset from each other
            instance.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            instance.transform.localRotation = Quaternion.identity;

            WeaponBase wb = instance.GetComponent<WeaponBase>();
            if (wb != null)
            {
                instance.SetActive(slot == 0); // Activate first weapon by default
                equipped.Add(wb);
            }
        }

        // Set CombatController weapons array
        if (equipped.Count > 0)
        {
            SerializeWeaponsToController(equipped.ToArray());
        }

        Debug.Log($"[LoadoutApplier] Equipped {equipped.Count} weapon(s)");
    }

    void SerializeWeaponsToController(WeaponBase[] weapons)
    {
        combatController.SetWeapons(weapons, 0);
    }

    void ApplyClassStats()
    {
        ClassData cls = GameManager.Instance.GetEquippedClassData();
        if (cls == null) return;

        PlayerStats stats = GameManager.Instance.Stats;

        stats.walkSpeed *= cls.moveSpeedMultiplier;
        stats.sprintSpeed *= cls.moveSpeedMultiplier;
        stats.maxStaminaPills = cls.maxStaminaPills;
        stats.staminaRegenTime = cls.staminaRegenTime;

        Debug.Log($"[LoadoutApplier] Applied class: {cls.className}");
    }

    void ApplyRelicStats()
    {
        List<RelicData> relics = GameManager.Instance.GetEquippedRelicData();
        if (relics.Count == 0) return;

        PlayerStats stats = GameManager.Instance.Stats;

        foreach (RelicData relic in relics)
        {
            stats.walkSpeed *= relic.moveSpeedMultiplier;
            stats.sprintSpeed *= relic.moveSpeedMultiplier;
            stats.staminaRegenTime /= Mathf.Max(relic.staminaRegenMultiplier, 0.01f);
        }

        Debug.Log($"[LoadoutApplier] Applied {relics.Count} relic(s)");
    }

    void ApplyHealth()
    {
        HealthComponent health = GetComponent<HealthComponent>();
        if (health == null) return;

        float effectiveHP = GameManager.Instance.GetEffectiveMaxHealth();
        health.SetMaxHealth(effectiveHP, true);
    }

    void HookPlayerDeath()
    {
        HealthComponent health = GetComponent<HealthComponent>();
        if (health == null) return;

        health.OnDeath.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerDeath();
        });
    }
}
