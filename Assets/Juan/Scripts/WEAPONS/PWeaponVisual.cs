using Fusion;
using UnityEngine;

public class PWeaponVisuals : NetworkBehaviour
{
    // ============================
    // INVENTORY
    // ============================

    [Header("Inventory")]
    [SerializeField]
    private PWeaponInventory inventory;

    // ============================
    // WEAPON VISUALS
    // ============================

    [Header("Weapon Visuals")]
    [SerializeField]
    private GameObject pistolVisual;

    [SerializeField]
    private GameObject shotgunVisual;

    [SerializeField]
    private GameObject assaultRifleVisual;

    [SerializeField]
    private GameObject sniperVisual;

    [SerializeField]
    private GameObject rocketLauncherVisual;

    [SerializeField]
    private GameObject grenadeVisual;

    // ============================
    // LOCAL
    // ============================

    private PWeaponInventory.WeaponType lastWeapon;

    private bool initialized = false;

    // ============================
    // AWAKE
    // ============================

    private void Awake()
    {
        if (inventory == null)
        {
            inventory =
                GetComponent<PWeaponInventory>();
        }
    }

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (inventory == null)
        {
            inventory =
                GetComponent<PWeaponInventory>();
        }

        if (inventory == null)
        {
            Debug.LogError(
                "PWEAPONVISUALS: PWeaponInventory no encontrado."
            );

            return;
        }

        lastWeapon =
            inventory.CurrentWeapon;

        UpdateWeaponVisuals();

        initialized =
            true;
    }

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        if (inventory == null)
            return;

        if (!initialized ||
            inventory.CurrentWeapon != lastWeapon)
        {
            lastWeapon =
                inventory.CurrentWeapon;

            UpdateWeaponVisuals();

            initialized =
                true;
        }
    }

    // ============================
    // UPDATE VISUALS
    // ============================

    private void UpdateWeaponVisuals()
    {
        if (inventory == null)
            return;

        // Apagar absolutamente todo
        SetWeaponActive(
            pistolVisual,
            false
        );

        SetWeaponActive(
            shotgunVisual,
            false
        );

        SetWeaponActive(
            assaultRifleVisual,
            false
        );

        SetWeaponActive(
            sniperVisual,
            false
        );

        SetWeaponActive(
            rocketLauncherVisual,
            false
        );

        SetWeaponActive(
            grenadeVisual,
            false
        );

        // Prender solamente el arma equipada
        switch (inventory.CurrentWeapon)
        {
            case PWeaponInventory.WeaponType.Pistol:

                SetWeaponActive(
                    pistolVisual,
                    true
                );

                break;

            case PWeaponInventory.WeaponType.Shotgun:

                SetWeaponActive(
                    shotgunVisual,
                    true
                );

                break;

            case PWeaponInventory.WeaponType.AssaultRifle:

                SetWeaponActive(
                    assaultRifleVisual,
                    true
                );

                break;

            case PWeaponInventory.WeaponType.Sniper:

                SetWeaponActive(
                    sniperVisual,
                    true
                );

                break;

            case PWeaponInventory.WeaponType.RocketLauncher:

                SetWeaponActive(
                    rocketLauncherVisual,
                    true
                );

                break;

            case PWeaponInventory.WeaponType.Grenade:

                SetWeaponActive(
                    grenadeVisual,
                    true
                );

                break;
        }
    }

    // ============================
    // SET ACTIVE
    // ============================

    private void SetWeaponActive(
        GameObject weaponObject,
        bool value)
    {
        if (weaponObject == null)
            return;

        if (weaponObject.activeSelf == value)
            return;

        weaponObject.SetActive(
            value
        );
    }
}