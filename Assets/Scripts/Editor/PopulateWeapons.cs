using UnityEditor;
using UnityEngine;

public static class PopulateWeapons
{
    public static void Execute()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("[PopulateWeapons] No GameManager in scene.");
            return;
        }

        // Access via SerializedObject to modify private list
        SerializedObject so = new SerializedObject(gm);
        SerializedProperty allWeaponsProp = so.FindProperty("allWeapons");

        string[] paths =
        {
            "Assets/Data/Weapons/AxeData.asset",
            "Assets/Data/Weapons/HammerData.asset",
            "Assets/Data/Weapons/SwordData.asset",
            "Assets/Data/Weapons/LongbowData.asset",
            "Assets/Data/Weapons/KatanaData.asset",
            "Assets/Data/Weapons/DualCrossbowData.asset",
            "Assets/Data/Weapons/BowData.asset",
            "Assets/Data/Weapons/CutlassData.asset",
            "Assets/Data/Weapons/GlaiveData.asset",
            "Assets/Data/Weapons/MaceData.asset",
        };

        allWeaponsProp.ClearArray();

        foreach (string path in paths)
        {
            WeaponData wd = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (wd == null)
            {
                Debug.LogWarning($"[PopulateWeapons] Not found: {path}");
                continue;
            }

            allWeaponsProp.InsertArrayElementAtIndex(allWeaponsProp.arraySize);
            SerializedProperty elem = allWeaponsProp.GetArrayElementAtIndex(allWeaponsProp.arraySize - 1);
            elem.objectReferenceValue = wd;
        }

        so.ApplyModifiedProperties();
        Debug.Log($"[PopulateWeapons] Added {allWeaponsProp.arraySize} weapons to GameManager.");
    }

    public static void Verify()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("[PopulateWeapons] No GameManager in scene.");
            return;
        }

        SerializedObject so = new SerializedObject(gm);
        SerializedProperty allWeaponsProp = so.FindProperty("allWeapons");
        Debug.Log($"[Verify] allWeapons count: {allWeaponsProp.arraySize}");
        for (int i = 0; i < allWeaponsProp.arraySize; i++)
        {
            SerializedProperty elem = allWeaponsProp.GetArrayElementAtIndex(i);
            WeaponData wd = elem.objectReferenceValue as WeaponData;
            Debug.Log($"[Verify]   [{i}] {wd?.weaponName ?? "NULL"}");
        }
    }
}
