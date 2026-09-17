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
    [SerializeField] private int shotgunMagazineSize = 2;

    [Networked]
    public NetworkBool HasShotgun { get; set; }

    [Networked]
    public int ShotgunLoaded { get; set; }

    [Networked]
    public int ShotgunReserve { get; set; }

    // ============================
    // ASSAULT RIFLE
    // ============================

    [Header("Assault Rifle")]
    [SerializeField] private int maxAssaultRifleAmmo = 120;
    [SerializeField] private int assaultRifleMagazineSize = 30;

    [Networked]
    public NetworkBool HasAssaultRifle { get; set; }

    [Networked]
    public int AssaultRifleLoaded { get; set; }

    [Networked]
    public int AssaultRifleReserve { get; set; }

    // ============================
    // SNIPER
    // ============================

    [Header("Sniper")]
    [SerializeField] private int maxSniperAmmo = 20;
    [SerializeField] private int sniperMagazineSize = 5;

    [Networked]
    public NetworkBool HasSniper { get; set; }

    [Networked]
    public int SniperLoaded { get; set; }

    [Networked]
    public int SniperReserve { get; set; }

    // ============================
    // ROCKET LAUNCHER
    // ============================

    [Header("Rocket Launcher")]
    [SerializeField] private int maxRocketLauncherAmmo = 8;
    [SerializeField] private int rocketLauncherMagazineSize = 1;

    [Networked]
    public NetworkBool HasRocketLauncher { get; set; }

    [Networked]
    public int RocketLauncherLoaded { get; set; }

    [Networked]
    public int RocketLauncherReserve { get; set; }

    // ============================
    // GRENADE
    // ============================

    [Header("Grenade")]
    [SerializeField] private int maxGrenadeAmmo = 5;
    [SerializeField] private int grenadeMagazineSize = 1;

    [Networked]
    public NetworkBool HasGrenade { get; set; }

    [Networked]
    public int GrenadeLoaded { get; set; }

    [Networked]
    public int GrenadeReserve { get; set; }

    // ============================
    // ARMA ACTUAL
    // ============================

    [Header("Arma actual")]

    [Networked]
    public WeaponType CurrentWeapon { get; set; }

    // ============================
    // PROPIEDADES PÚBLICAS
    // ============================

    public int MaxShotgunAmmo =>
        maxShotgunAmmo;

    public int ShotgunMagazineSize =>
        shotgunMagazineSize;

    public int MaxAssaultRifleAmmo =>
        maxAssaultRifleAmmo;

    public int AssaultRifleMagazineSize =>
        assaultRifleMagazineSize;

    public int MaxSniperAmmo =>
        maxSniperAmmo;

    public int SniperMagazineSize =>
        sniperMagazineSize;

    public int MaxRocketLauncherAmmo =>
        maxRocketLauncherAmmo;

    public int RocketLauncherMagazineSize =>
        rocketLauncherMagazineSize;

    public int MaxGrenadeAmmo =>
        maxGrenadeAmmo;

    public int GrenadeMagazineSize =>
        grenadeMagazineSize;

    // ============================
    // NOMBRE DEL ARMA
    // ============================

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
        if (!Object.HasStateAuthority)
            return;

        // Shotgun
        HasShotgun = false;
        ShotgunLoaded = 0;
        ShotgunReserve = 0;

        // Assault Rifle
        HasAssaultRifle = false;
        AssaultRifleLoaded = 0;
        AssaultRifleReserve = 0;

        // Sniper
        HasSniper = false;
        SniperLoaded = 0;
        SniperReserve = 0;

        // Rocket Launcher
        HasRocketLauncher = false;
        RocketLauncherLoaded = 0;
        RocketLauncherReserve = 0;

        // Grenade
        HasGrenade = false;
        GrenadeLoaded = 0;
        GrenadeReserve = 0;

        // Arma inicial
        CurrentWeapon =
            WeaponType.Pistol;
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

        // E = siguiente arma
        if (Input.GetKeyDown(KeyCode.E))
        {
            SelectNextWeapon();
        }

        // Q = arma anterior
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SelectPreviousWeapon();
        }

        // Rueda del mouse
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
    // PICKUP SHOTGUN
    // ============================

    public void AddShotgun(
        int ammoAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        HasShotgun = true;

        int currentTotalAmmo =
            ShotgunLoaded +
            ShotgunReserve;

        int availableSpace =
            maxShotgunAmmo -
            currentTotalAmmo;

        if (availableSpace <= 0)
        {
            Debug.Log(
                $"SHOTGUN AMMO FULL | " +
                $"Total: {currentTotalAmmo}/{maxShotgunAmmo}"
            );

            return;
        }

        int ammoToAdd =
            Mathf.Min(
                ammoAmount,
                availableSpace
            );

        int ammoActuallyAdded =
            ammoToAdd;

        int bulletsNeeded =
            shotgunMagazineSize -
            ShotgunLoaded;

        if (bulletsNeeded > 0 &&
            ammoToAdd > 0)
        {
            int bulletsToLoad =
                Mathf.Min(
                    bulletsNeeded,
                    ammoToAdd
                );

            ShotgunLoaded +=
                bulletsToLoad;

            ammoToAdd -=
                bulletsToLoad;
        }

        if (ammoToAdd > 0)
        {
            ShotgunReserve +=
                ammoToAdd;
        }

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
    // PICKUP ASSAULT RIFLE
    // ============================

    public void AddAssaultRifle(
        int ammoAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        HasAssaultRifle = true;

        int currentTotalAmmo =
            AssaultRifleLoaded +
            AssaultRifleReserve;

        int availableSpace =
            maxAssaultRifleAmmo -
            currentTotalAmmo;

        if (availableSpace <= 0)
        {
            Debug.Log(
                $"ASSAULT RIFLE AMMO FULL | " +
                $"Total: {currentTotalAmmo}/{maxAssaultRifleAmmo}"
            );

            return;
        }

        int ammoToAdd =
            Mathf.Min(
                ammoAmount,
                availableSpace
            );

        int ammoActuallyAdded =
            ammoToAdd;

        int bulletsNeeded =
            assaultRifleMagazineSize -
            AssaultRifleLoaded;

        if (bulletsNeeded > 0 &&
            ammoToAdd > 0)
        {
            int bulletsToLoad =
                Mathf.Min(
                    bulletsNeeded,
                    ammoToAdd
                );

            AssaultRifleLoaded +=
                bulletsToLoad;

            ammoToAdd -=
                bulletsToLoad;
        }

        if (ammoToAdd > 0)
        {
            AssaultRifleReserve +=
                ammoToAdd;
        }

        int finalTotalAmmo =
            AssaultRifleLoaded +
            AssaultRifleReserve;

        Debug.Log(
            $"ASSAULT RIFLE PICKUP | " +
            $"+{ammoActuallyAdded} balas | " +
            $"Cargadas: {AssaultRifleLoaded} | " +
            $"Reserva: {AssaultRifleReserve} | " +
            $"Total: {finalTotalAmmo}/{maxAssaultRifleAmmo}"
        );
    }

    // ============================
    // PICKUP SNIPER
    // ============================

    public void AddSniper(
        int ammoAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        HasSniper = true;

        int currentTotalAmmo =
            SniperLoaded +
            SniperReserve;

        int availableSpace =
            maxSniperAmmo -
            currentTotalAmmo;

        if (availableSpace <= 0)
        {
            Debug.Log(
                $"SNIPER AMMO FULL | " +
                $"Total: {currentTotalAmmo}/{maxSniperAmmo}"
            );

            return;
        }

        int ammoToAdd =
            Mathf.Min(
                ammoAmount,
                availableSpace
            );

        int ammoActuallyAdded =
            ammoToAdd;

        int bulletsNeeded =
            sniperMagazineSize -
            SniperLoaded;

        if (bulletsNeeded > 0 &&
            ammoToAdd > 0)
        {
            int bulletsToLoad =
                Mathf.Min(
                    bulletsNeeded,
                    ammoToAdd
                );

            SniperLoaded +=
                bulletsToLoad;

            ammoToAdd -=
                bulletsToLoad;
        }

        if (ammoToAdd > 0)
        {
            SniperReserve +=
                ammoToAdd;
        }

        int finalTotalAmmo =
            SniperLoaded +
            SniperReserve;

        Debug.Log(
            $"SNIPER PICKUP | " +
            $"+{ammoActuallyAdded} balas | " +
            $"Cargadas: {SniperLoaded} | " +
            $"Reserva: {SniperReserve} | " +
            $"Total: {finalTotalAmmo}/{maxSniperAmmo}"
        );
    }

    // ============================
    // PICKUP ROCKET LAUNCHER
    // ============================

    public void AddRocketLauncher(
        int ammoAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        HasRocketLauncher = true;

        int currentTotalAmmo =
            RocketLauncherLoaded +
            RocketLauncherReserve;

        int availableSpace =
            maxRocketLauncherAmmo -
            currentTotalAmmo;

        if (availableSpace <= 0)
        {
            Debug.Log(
                $"ROCKET LAUNCHER AMMO FULL | " +
                $"Total: {currentTotalAmmo}/{maxRocketLauncherAmmo}"
            );

            return;
        }

        int ammoToAdd =
            Mathf.Min(
                ammoAmount,
                availableSpace
            );

        int ammoActuallyAdded =
            ammoToAdd;

        int rocketsNeeded =
            rocketLauncherMagazineSize -
            RocketLauncherLoaded;

        if (rocketsNeeded > 0 &&
            ammoToAdd > 0)
        {
            int rocketsToLoad =
                Mathf.Min(
                    rocketsNeeded,
                    ammoToAdd
                );

            RocketLauncherLoaded +=
                rocketsToLoad;

            ammoToAdd -=
                rocketsToLoad;
        }

        if (ammoToAdd > 0)
        {
            RocketLauncherReserve +=
                ammoToAdd;
        }

        int finalTotalAmmo =
            RocketLauncherLoaded +
            RocketLauncherReserve;

        Debug.Log(
            $"ROCKET LAUNCHER PICKUP | " +
            $"+{ammoActuallyAdded} rockets | " +
            $"Cargado: {RocketLauncherLoaded} | " +
            $"Reserva: {RocketLauncherReserve} | " +
            $"Total: {finalTotalAmmo}/{maxRocketLauncherAmmo}"
        );
    }

    // ============================
    // PICKUP GRENADE
    // ============================

    public void AddGrenade(
        int ammoAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        HasGrenade = true;

        int currentTotalAmmo =
            GrenadeLoaded +
            GrenadeReserve;

        int availableSpace =
            maxGrenadeAmmo -
            currentTotalAmmo;

        if (availableSpace <= 0)
        {
            Debug.Log(
                $"GRENADE AMMO FULL | " +
                $"Total: {currentTotalAmmo}/{maxGrenadeAmmo}"
            );

            return;
        }

        int ammoToAdd =
            Mathf.Min(
                ammoAmount,
                availableSpace
            );

        int ammoActuallyAdded =
            ammoToAdd;

        // ============================
        // PRIMERO CARGAR UNA GRANADA
        // ============================

        int grenadesNeeded =
            grenadeMagazineSize -
            GrenadeLoaded;

        if (grenadesNeeded > 0 &&
            ammoToAdd > 0)
        {
            int grenadesToLoad =
                Mathf.Min(
                    grenadesNeeded,
                    ammoToAdd
                );

            GrenadeLoaded +=
                grenadesToLoad;

            ammoToAdd -=
                grenadesToLoad;
        }

        // ============================
        // RESTO A RESERVA
        // ============================

        if (ammoToAdd > 0)
        {
            GrenadeReserve +=
                ammoToAdd;
        }

        int finalTotalAmmo =
            GrenadeLoaded +
            GrenadeReserve;

        Debug.Log(
            $"GRENADE PICKUP | " +
            $"+{ammoActuallyAdded} granadas | " +
            $"Cargada: {GrenadeLoaded} | " +
            $"Reserva: {GrenadeReserve} | " +
            $"Total: {finalTotalAmmo}/{maxGrenadeAmmo}"
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