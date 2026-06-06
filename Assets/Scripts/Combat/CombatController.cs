using UnityEngine;
using System.Collections;

public class CombatController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private int defaultWeaponIndex = 0;

    private int currentIndex;

    public WeaponBase CurrentWeapon =>
        (weapons != null && weapons.Length > 0 && currentIndex < weapons.Length)
            ? weapons[currentIndex] : null;
    public int CurrentWeaponIndex => currentIndex;

    void Start()
    {
        if (weapons == null || weapons.Length == 0)
            weapons = new WeaponBase[0];

        currentIndex = Mathf.Clamp(defaultWeaponIndex, 0, Mathf.Max(0, weapons.Length - 1));

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].gameObject.SetActive(i == currentIndex);
        }
    }

    public void SetWeapons(WeaponBase[] newWeapons, int defaultIndex = 0)
    {
        // Deactivate old weapons
        if (weapons != null)
        {
            foreach (var w in weapons)
                if (w != null) w.gameObject.SetActive(false);
        }

        weapons = newWeapons ?? new WeaponBase[0];
        defaultWeaponIndex = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, weapons.Length - 1));
        currentIndex = defaultWeaponIndex;

        // Activate default weapon
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].gameObject.SetActive(i == currentIndex);
        }
    }

    void Update()
    {
        HandleWeaponSwitch();
        HandleAttack();
    }

    void HandleWeaponSwitch()
    {
        if (weapons == null || weapons.Length < 2) return;

        for (int i = 0; i < Mathf.Min(weapons.Length, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchWeapon(i);
                return;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            SwitchWeapon((currentIndex + 1) % weapons.Length);
        else if (scroll < 0f)
            SwitchWeapon((currentIndex - 1 + weapons.Length) % weapons.Length);
    }

    void HandleAttack()
    {
        if (CurrentWeapon == null) return;

        // Primary attack (left click / Fire1)
        if (Input.GetButtonDown("Fire1"))
        {
            CurrentWeapon.PrimaryAttack(Team.Player);
        }

        // Secondary attack (right click / Fire2)
        if (Input.GetButtonDown("Fire2"))
        {
            CurrentWeapon.SecondaryAttack(Team.Player);
        }
    }

    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length || index == currentIndex) return;

        if (weapons[currentIndex] != null)
            weapons[currentIndex].gameObject.SetActive(false);

        currentIndex = index;

        if (weapons[currentIndex] != null)
            weapons[currentIndex].gameObject.SetActive(true);
    }
}
