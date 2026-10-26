using Fusion;
using UnityEngine;

public class PRocketLauncher : NetworkBehaviour
{

    [Header("Referencias")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private PWeaponInventory weaponInventory;

    [SerializeField]
    private PHealth health;


    [Header("Proyectil")]
    [SerializeField]
    private NetworkPrefabRef rocketPrefab;

    [SerializeField]
    private Transform rocketSpawnPoint;


    [Header("Disparo")]
    [SerializeField]
    private float timeBetweenShots = 0.8f;

    [SerializeField]
    private float spawnForwardOffset = 1f;


    [Header("Recarga")]
    [SerializeField]
    private float reloadTime = 1.5f;


    [Header("Debug")]
    [SerializeField]
    private bool showDebug = true;


    [Networked]
    private NetworkBool IsReloadingNetworked
    {
        get;
        set;
    }

    [Networked]
    private TickTimer ReloadTimer
    {
        get;
        set;
    }

    [Networked]
    private TickTimer ShotCooldown
    {
        get;
        set;
    }


    private bool wasRocketLauncherEquipped;

    private NetworkGameManager gameManager;


    public bool IsReloading =>
        IsReloadingNetworked;


    private void Awake()
    {
        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }
    }


    public override void Spawned()
    {
        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }


        if (playerCamera == null &&
            Object.HasInputAuthority)
        {
            playerCamera =
                GetComponentInChildren<Camera>(
                    true
                );
        }

        gameManager =
            FindFirstObjectByType<
                NetworkGameManager
            >();

        wasRocketLauncherEquipped =
            false;


        if (Object.HasStateAuthority)
        {
            IsReloadingNetworked =
                false;

            ReloadTimer =
                TickTimer.None;

            ShotCooldown =
                TickTimer.None;
        }

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET LAUNCHER READY | " +
                $"PLAYER: {Object.InputAuthority.PlayerId} | " +
                $"STATE: {Object.HasStateAuthority} | " +
                $"INPUT: {Object.HasInputAuthority}"
            );
        }
    }


    private void Update()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;


        if (!Object.HasInputAuthority)
            return;


        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }

        if (health != null &&
            health.IsDead)
        {
            return;
        }


        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }


        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (weaponInventory == null)
            return;

        bool rocketEquipped =
            weaponInventory
                .IsRocketLauncherEquipped();


        if (rocketEquipped &&
            !wasRocketLauncherEquipped)
        {
            OnRocketLauncherEquipped();
        }

        wasRocketLauncherEquipped =
            rocketEquipped;

        if (!rocketEquipped)
            return;


        if (IsReloadingNetworked)
            return;


        if (Input.GetKeyDown(
            KeyCode.R))
        {
            RequestReload();

            return;
        }


        if (Input.GetMouseButtonDown(
            0))
        {
            RequestFire();
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;


        if (IsReloadingNetworked &&
            ReloadTimer.IsRunning &&
            ReloadTimer.Expired(
                Runner))
        {
            CompleteReloadAuthoritative();
        }
    }


    private void OnRocketLauncherEquipped()
    {
        if (weaponInventory == null)
            return;

        if (weaponInventory
                .RocketLauncherLoaded <= 0 &&
            weaponInventory
                .RocketLauncherReserve > 0)
        {
            RequestReload();
        }
    }


    private void RequestFire()
    {
        if (!Object.HasInputAuthority)
            return;


        if (playerCamera == null)
        {
            Debug.LogError(
                "ROCKET LAUNCHER: " +
                "Player Camera no está asignada."
            );

            return;
        }


        if (!rocketPrefab.IsValid)
        {
            Debug.LogError(
                "ROCKET LAUNCHER: " +
                "Rocket Prefab no está asignado."
            );

            return;
        }


        Vector3 forwardDirection =
            playerCamera
                .transform
                .forward
                .normalized;


        Vector3 spawnPosition;

        if (rocketSpawnPoint != null)
        {
            spawnPosition =
                rocketSpawnPoint.position;
        }
        else
        {
            spawnPosition =
                playerCamera
                    .transform
                    .position +
                forwardDirection *
                spawnForwardOffset;
        }


        if (Object.HasStateAuthority)
        {
            ProcessFireAuthoritative(
                spawnPosition,
                forwardDirection
            );

            return;
        }


        RPC_RequestFire(
            spawnPosition,
            forwardDirection
        );
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestFire(
        Vector3 spawnPosition,
        Vector3 forwardDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        ProcessFireAuthoritative(
            spawnPosition,
            forwardDirection
        );
    }


    private void ProcessFireAuthoritative(
        Vector3 requestedSpawnPosition,
        Vector3 forwardDirection)
    {
        if (!Object.HasStateAuthority)
            return;


        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }


        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }

        if (health != null &&
            health.IsDead)
        {
            return;
        }


        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (weaponInventory == null)
            return;

        if (!weaponInventory
            .IsRocketLauncherEquipped())
        {
            return;
        }


        if (IsReloadingNetworked)
            return;


        if (ShotCooldown.IsRunning &&
            !ShotCooldown.Expired(
                Runner))
        {
            if (showDebug)
            {
                Debug.Log(
                    "ROCKET: todavía está en cooldown."
                );
            }

            return;
        }


        if (weaponInventory
            .RocketLauncherLoaded <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    $"ROCKET VACÍO | " +
                    $"Loaded: " +
                    $"{weaponInventory.RocketLauncherLoaded} | " +
                    $"Reserve: " +
                    $"{weaponInventory.RocketLauncherReserve}"
                );
            }

            StartReloadAuthoritative();

            return;
        }


        if (!rocketPrefab.IsValid)
        {
            Debug.LogError(
                "ROCKET LAUNCHER: " +
                "Rocket Prefab no está asignado."
            );

            return;
        }


        if (forwardDirection.sqrMagnitude <
            0.001f)
        {
            forwardDirection =
                transform.forward;
        }

        forwardDirection.Normalize();

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                forwardDirection,
                Vector3.up
            );


        NetworkObject spawnedRocket =
            Runner.Spawn(
                rocketPrefab,
                requestedSpawnPosition,
                spawnRotation,
                Object.InputAuthority
            );

        if (spawnedRocket == null)
        {
            Debug.LogError(
                "ROCKET: " +
                "Runner.Spawn devolvió NULL."
            );

            return;
        }


        RocketProjectile projectile =
            spawnedRocket
                .GetComponent<
                    RocketProjectile
                >();

        if (projectile == null)
        {
            Debug.LogError(
                "ROCKET: " +
                "El prefab no tiene " +
                "RocketProjectile."
            );

            Runner.Despawn(
                spawnedRocket
            );

            return;
        }

        projectile.Initialize(
            Object
        );


        weaponInventory
            .RocketLauncherLoaded--;


        ShotCooldown =
            TickTimer.CreateFromSeconds(
                Runner,
                timeBetweenShots
            );

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET FIRED BY HOST | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Loaded: " +
                $"{weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: " +
                $"{weaponInventory.RocketLauncherReserve} | " +
                $"Position: " +
                $"{requestedSpawnPosition}"
            );
        }


        if (weaponInventory
                .RocketLauncherLoaded <= 0 &&
            weaponInventory
                .RocketLauncherReserve > 0)
        {
            StartReloadAuthoritative();
        }
    }


    private void RequestReload()
    {
        if (!Object.HasInputAuthority)
            return;


        if (Object.HasStateAuthority)
        {
            StartReloadAuthoritative();

            return;
        }


        RPC_RequestReload();
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestReload()
    {
        if (!Object.HasStateAuthority)
            return;

        StartReloadAuthoritative();
    }


    private void StartReloadAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (weaponInventory == null)
            return;

        if (IsReloadingNetworked)
            return;


        if (weaponInventory
                .RocketLauncherLoaded >=
            weaponInventory
                .RocketLauncherMagazineSize)
        {
            return;
        }


        if (weaponInventory
            .RocketLauncherReserve <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    "ROCKET: " +
                    "sin munición de reserva."
                );
            }

            return;
        }


        IsReloadingNetworked =
            true;

        ReloadTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                reloadTime
            );

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET RELOADING... | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Loaded: " +
                $"{weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: " +
                $"{weaponInventory.RocketLauncherReserve}"
            );
        }
    }


    private void CompleteReloadAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!IsReloadingNetworked)
            return;

        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (weaponInventory == null)
        {
            IsReloadingNetworked =
                false;

            ReloadTimer =
                TickTimer.None;

            return;
        }


        int rocketsNeeded =
            weaponInventory
                .RocketLauncherMagazineSize -
            weaponInventory
                .RocketLauncherLoaded;


        int rocketsToLoad =
            Mathf.Min(
                rocketsNeeded,
                weaponInventory
                    .RocketLauncherReserve
            );


        weaponInventory
            .RocketLauncherLoaded +=
            rocketsToLoad;

        weaponInventory
            .RocketLauncherReserve -=
            rocketsToLoad;

        IsReloadingNetworked =
            false;

        ReloadTimer =
            TickTimer.None;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET RELOAD COMPLETE | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Loaded: " +
                $"{weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: " +
                $"{weaponInventory.RocketLauncherReserve}"
            );
        }
    }
}