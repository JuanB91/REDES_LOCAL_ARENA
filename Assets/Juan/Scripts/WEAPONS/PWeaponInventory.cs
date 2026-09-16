using Fusion;
using UnityEngine;

public class PWeaponInventory : NetworkBehaviour
{
    public enum WeaponType
    {
        Pistol = 0,
        Shotgun = 1,
        AssaultRifle = 2,
        Sniper = 3,
        RocketLauncher = 4,
        Grenade = 5
    }

    // ============================
    // SHOTGUN
    // ============================

    [Header("Shotgun")]
    [SerializeField] private int maxShotgunAmmo = 16;

    [Networked]
    public NetworkBool HasShotgun { get; set; }

    [Networked]
    public int ShotgunLoaded { get; set; }

    [Networked]
    public int ShotgunReserve { get; set; }

    // ============================
    // ARMA ACTUAL
    // ============================

    [Header("Arma actual")]

    [Networked]
    public WeaponType CurrentWeapon { get; set; }

    // Nombre que utiliza el HUD.
    public string CurrentWeaponName
    {
        get
        {
            switch (CurrentWeapon)
            {
                case WeaponType.Pistol:
                    return "PISTOL";

                case WeaponType.Shotgun:
                    return "SHOTGUN";

                case WeaponType.AssaultRifle:
                    return "ASSAULT RIFLE";

                case WeaponType.Sniper:
                    return "SNIPER";

                case WeaponType.RocketLauncher:
                    return "ROCKET LAUNCHER";

                case WeaponType.Grenade:
                    return "GRENADE";

                default:
                    return "UNKNOWN";
            }
        }
    }

    // ============================
    // SPAWN
    // ============================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            HasShotgun = false;

            ShotgunLoaded = 0;
            ShotgunReserve = 0;

            CurrentWeapon =
                WeaponType.Pistol;
        }
    }

    // ============================
    // INPUT CAMBIO DE ARMA
    // ============================

    private void Update()
    {
        if (Object == null ||
            !Object.HasStateAuthority)
        {
            return;
        }

        // -------------------------
        // E = SIGUIENTE ARMA
        // -------------------------

        if (Input.GetKeyDown(KeyCode.E))
        {
            SelectNextWeapon();
        }

        // -------------------------
        // Q = ARMA ANTERIOR
        // -------------------------

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SelectPreviousWeapon();
        }

        // -------------------------
        // RUEDA DEL MOUSE
        // -------------------------

        float scroll =
            Input.GetAxis(
                "Mouse ScrollWheel"
            );

        if (scroll > 0f)
        {
            SelectNextWeapon();
        }
        else if (scroll < 0f)
        {
            SelectPreviousWeapon();
        }
    }

    // ============================
    // SIGUIENTE ARMA
    // ============================

    private void SelectNextWeapon()
    {
        int currentIndex =
            (int)CurrentWeapon;

        int weaponCount =
            System.Enum.GetValues(
                typeof(WeaponType)
            ).Length;

        currentIndex++;

        if (currentIndex >= weaponCount)
        {
            currentIndex = 0;
        }

        CurrentWeapon =
            (WeaponType)currentIndex;

        WeaponChanged();
    }

    // ============================
    // ARMA ANTERIOR
    // ============================

    private void SelectPreviousWeapon()
    {
        int currentIndex =
            (int)CurrentWeapon;

        int weaponCount =
            System.Enum.GetValues(
                typeof(WeaponType)
            ).Length;

        currentIndex--;

        if (currentIndex < 0)
        {
            currentIndex =
                weaponCount - 1;
        }

        CurrentWeapon =
            (WeaponType)currentIndex;

        WeaponChanged();
    }

    // ============================
    // CAMBIO DE ARMA
    // ============================

    private void WeaponChanged()
    {
        Debug.Log(
            $"ARMA EQUIPADA: {CurrentWeaponName}"
        );
    }

    // ============================
    // PICKUP ESCOPETA
    // ============================

    public void AddShotgun(int ammoAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        HasShotgun = true;

        // -------------------------
        // MUNICIÓN TOTAL ACTUAL
        // -------------------------

        int currentTotalAmmo =
            ShotgunLoaded +
            ShotgunReserve;

        // Cuánto espacio queda hasta
        // alcanzar el máximo permitido.
        int availableSpace =
            maxShotgunAmmo -
            currentTotalAmmo;

        // Ya estamos llenos.
        if (availableSpace <= 0)
        {
            Debug.Log(
                $"SHOTGUN AMMO FULL | " +
                $"Cargados: {ShotgunLoaded} | " +
                $"Reserva: {ShotgunReserve} | " +
                $"Total: {currentTotalAmmo}/{maxShotgunAmmo}"
            );

            return;
        }

        // El pickup intenta entregar toda
        // su munición, pero nunca superamos
        // el máximo.
        int ammoToAdd =
            Mathf.Min(
                ammoAmount,
                availableSpace
            );

        int ammoActuallyAdded =
            ammoToAdd;

        // -------------------------
        // 1. COMPLETAMOS LOS 2 CAÑONES
        // -------------------------

        int shellsNeeded =
            2 -
            ShotgunLoaded;

        if (shellsNeeded > 0 &&
            ammoToAdd > 0)
        {
            int shellsToLoad =
                Mathf.Min(
                    shellsNeeded,
                    ammoToAdd
                );

            ShotgunLoaded +=
                shellsToLoad;

            ammoToAdd -=
                shellsToLoad;
        }

        // -------------------------
        // 2. EL RESTO VA A RESERVA
        // -------------------------

        if (ammoToAdd > 0)
        {
            ShotgunReserve +=
                ammoToAdd;
        }

        // -------------------------
        // RESULTADO
        // -------------------------

        int finalTotalAmmo =
            ShotgunLoaded +
            ShotgunReserve;

        Debug.Log(
            $"SHOTGUN PICKUP | " +
            $"+{ammoActuallyAdded} cartuchos | " +
            $"Cargados: {ShotgunLoaded} | " +
            $"Reserva: {ShotgunReserve} | " +
            $"Total: {finalTotalAmmo}/{maxShotgunAmmo}"
        );
    }

    // ============================
    // CONSULTAS DE ARMA
    // ============================

    public bool IsPistolEquipped()
    {
        return CurrentWeapon ==
               WeaponType.Pistol;
    }

    public bool IsShotgunEquipped()
    {
        return CurrentWeapon ==
               WeaponType.Shotgun;
    }

    public bool IsAssaultRifleEquipped()
    {
        return CurrentWeapon ==
               WeaponType.AssaultRifle;
    }

    public bool IsSniperEquipped()
    {
        return CurrentWeapon ==
               WeaponType.Sniper;
    }

    public bool IsRocketLauncherEquipped()
    {
        return CurrentWeapon ==
               WeaponType.RocketLauncher;
    }

    public bool IsGrenadeEquipped()
    {
        return CurrentWeapon ==
               WeaponType.Grenade;
    }
}