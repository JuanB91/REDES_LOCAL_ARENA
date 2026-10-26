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


    [Header("Shotgun")]
    [SerializeField]
    private int maxShotgunAmmo = 16;

    [SerializeField]
    private int shotgunMagazineSize = 2;

    [Networked]
    public NetworkBool HasShotgun { get; set; }

    [Networked]
    public int ShotgunLoaded { get; set; }

    [Networked]
    public int ShotgunReserve { get; set; }


    [Header("Assault Rifle")]
    [SerializeField]
    private int maxAssaultRifleAmmo = 120;

    [SerializeField]
    private int assaultRifleMagazineSize = 30;

    [Networked]
    public NetworkBool HasAssaultRifle { get; set; }

    [Networked]
    public int AssaultRifleLoaded { get; set; }

    [Networked]
    public int AssaultRifleReserve { get; set; }


    [Header("Sniper")]
    [SerializeField]
    private int maxSniperAmmo = 20;

    [SerializeField]
    private int sniperMagazineSize = 5;

    [Networked]
    public NetworkBool HasSniper { get; set; }

    [Networked]
    public int SniperLoaded { get; set; }

    [Networked]
    public int SniperReserve { get; set; }


    [Header("Rocket Launcher")]
    [SerializeField]
    private int maxRocketLauncherAmmo = 8;

    [SerializeField]
    private int rocketLauncherMagazineSize = 1;

    [Networked]
    public NetworkBool HasRocketLauncher { get; set; }

    [Networked]
    public int RocketLauncherLoaded { get; set; }

    [Networked]
    public int RocketLauncherReserve { get; set; }


    [Header("Grenade")]
    [SerializeField]
    private int maxGrenadeAmmo = 5;

    [SerializeField]
    private int grenadeMagazineSize = 1;

    [Networked]
    public NetworkBool HasGrenade { get; set; }

    [Networked]
    public int GrenadeLoaded { get; set; }

    [Networked]
    public int GrenadeReserve { get; set; }


    [Header("Arma actual")]

    [Networked]
    public WeaponType CurrentWeapon { get; set; }


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


    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            InitializeInventoryAuthoritative();
        }

        Debug.Log(
            $"WEAPON INVENTORY READY | " +
            $"Player: {Object.InputAuthority.PlayerId} | " +
            $"State: {Object.HasStateAuthority} | " +
            $"Input: {Object.HasInputAuthority}"
        );
    }


    private void InitializeInventoryAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;


        HasShotgun =
            false;

        ShotgunLoaded =
            0;

        ShotgunReserve =
            0;


        HasAssaultRifle =
            false;

        AssaultRifleLoaded =
            0;

        AssaultRifleReserve =
            0;


        HasSniper =
            false;

        SniperLoaded =
            0;

        SniperReserve =
            0;


        HasRocketLauncher =
            false;

        RocketLauncherLoaded =
            0;

        RocketLauncherReserve =
            0;


        HasGrenade =
            false;

        GrenadeLoaded =
            0;

        GrenadeReserve =
            0;


        CurrentWeapon =
            WeaponType.Pistol;
    }


    public void ResetInventory()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        InitializeInventoryAuthoritative();

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} " +
            $"INVENTORY RESET"
        );
    }


    private void Update()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;


        if (!Object.HasInputAuthority)
            return;


        if (Input.GetKeyDown(
            KeyCode.E))
        {
            RequestNextWeapon();

            return;
        }


        if (Input.GetKeyDown(
            KeyCode.Q))
        {
            RequestPreviousWeapon();

            return;
        }


        float scroll =
            Input.GetAxis(
                "Mouse ScrollWheel"
            );

        if (scroll > 0f)
        {
            RequestNextWeapon();
        }
        else if (scroll < 0f)
        {
            RequestPreviousWeapon();
        }
    }


    private void RequestNextWeapon()
    {
        if (!Object.HasInputAuthority)
            return;

        if (Object.HasStateAuthority)
        {
            SelectNextWeaponAuthoritative();

            return;
        }

        RPC_RequestNextWeapon();
    }


    private void RequestPreviousWeapon()
    {
        if (!Object.HasInputAuthority)
            return;

        if (Object.HasStateAuthority)
        {
            SelectPreviousWeaponAuthoritative();

            return;
        }

        RPC_RequestPreviousWeapon();
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestNextWeapon()
    {
        if (!Object.HasStateAuthority)
            return;

        SelectNextWeaponAuthoritative();
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestPreviousWeapon()
    {
        if (!Object.HasStateAuthority)
            return;

        SelectPreviousWeaponAuthoritative();
    }


    private void SelectNextWeaponAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        int currentIndex =
            (int)CurrentWeapon;

        int weaponCount =
            System.Enum.GetValues(
                typeof(WeaponType)
            ).Length;

        currentIndex++;

        if (currentIndex >=
            weaponCount)
        {
            currentIndex =
                0;
        }

        CurrentWeapon =
            (WeaponType)currentIndex;

        WeaponChangedAuthoritative();
    }


    private void SelectPreviousWeaponAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

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

        WeaponChangedAuthoritative();
    }


    private void WeaponChangedAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            $"WEAPON CHANGED | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Weapon: {CurrentWeapon}"
        );
    }


    public void AddShotgun(
        int ammoAmount)
    {
        if (!CanModifyInventory())
            return;

        HasShotgun =
            true;

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
                $"Total: " +
                $"{currentTotalAmmo}/" +
                $"{maxShotgunAmmo}"
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
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"+{ammoActuallyAdded} cartuchos | " +
            $"Loaded: {ShotgunLoaded} | " +
            $"Reserve: {ShotgunReserve} | " +
            $"Total: " +
            $"{finalTotalAmmo}/" +
            $"{maxShotgunAmmo}"
        );
    }


    public void AddAssaultRifle(
        int ammoAmount)
    {
        if (!CanModifyInventory())
            return;

        HasAssaultRifle =
            true;

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
                $"Total: " +
                $"{currentTotalAmmo}/" +
                $"{maxAssaultRifleAmmo}"
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
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"+{ammoActuallyAdded} balas | " +
            $"Loaded: {AssaultRifleLoaded} | " +
            $"Reserve: {AssaultRifleReserve} | " +
            $"Total: " +
            $"{finalTotalAmmo}/" +
            $"{maxAssaultRifleAmmo}"
        );
    }


    public void AddSniper(
        int ammoAmount)
    {
        if (!CanModifyInventory())
            return;

        HasSniper =
            true;

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
                $"Total: " +
                $"{currentTotalAmmo}/" +
                $"{maxSniperAmmo}"
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
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"+{ammoActuallyAdded} balas | " +
            $"Loaded: {SniperLoaded} | " +
            $"Reserve: {SniperReserve} | " +
            $"Total: " +
            $"{finalTotalAmmo}/" +
            $"{maxSniperAmmo}"
        );
    }


    public void AddRocketLauncher(
        int ammoAmount)
    {
        if (!CanModifyInventory())
            return;

        HasRocketLauncher =
            true;

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
                $"Total: " +
                $"{currentTotalAmmo}/" +
                $"{maxRocketLauncherAmmo}"
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
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"+{ammoActuallyAdded} rockets | " +
            $"Loaded: {RocketLauncherLoaded} | " +
            $"Reserve: {RocketLauncherReserve} | " +
            $"Total: " +
            $"{finalTotalAmmo}/" +
            $"{maxRocketLauncherAmmo}"
        );
    }


    public void AddGrenade(
        int ammoAmount)
    {
        if (!CanModifyInventory())
            return;

        HasGrenade =
            true;

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
                $"Total: " +
                $"{currentTotalAmmo}/" +
                $"{maxGrenadeAmmo}"
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
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"+{ammoActuallyAdded} granadas | " +
            $"Loaded: {GrenadeLoaded} | " +
            $"Reserve: {GrenadeReserve} | " +
            $"Total: " +
            $"{finalTotalAmmo}/" +
            $"{maxGrenadeAmmo}"
        );
    }


    private bool CanModifyInventory()
    {
        if (Object == null)
            return false;

        if (!Object.IsValid)
            return false;

        if (!Object.HasStateAuthority)
            return false;

        return true;
    }


    public bool IsPistolEquipped()
    {
        return
            CurrentWeapon ==
            WeaponType.Pistol;
    }

    public bool IsShotgunEquipped()
    {
        return
            CurrentWeapon ==
            WeaponType.Shotgun;
    }

    public bool IsAssaultRifleEquipped()
    {
        return
            CurrentWeapon ==
            WeaponType.AssaultRifle;
    }

    public bool IsSniperEquipped()
    {
        return
            CurrentWeapon ==
            WeaponType.Sniper;
    }

    public bool IsRocketLauncherEquipped()
    {
        return
            CurrentWeapon ==
            WeaponType.RocketLauncher;
    }

    public bool IsGrenadeEquipped()
    {
        return
            CurrentWeapon ==
            WeaponType.Grenade;
    }
}