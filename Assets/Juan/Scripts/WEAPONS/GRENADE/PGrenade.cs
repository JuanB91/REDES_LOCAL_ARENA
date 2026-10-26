using Fusion;
using UnityEngine;

public class PGrenade : NetworkBehaviour
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
    private NetworkPrefabRef grenadePrefab;

    [SerializeField]
    private Transform grenadeSpawnPoint;


    [Header("Lanzamiento")]
    [SerializeField]
    private float throwForce = 15f;

    [SerializeField]
    private float upwardForce = 4f;

    [SerializeField]
    private float timeBetweenThrows = 0.8f;

    [SerializeField]
    private float spawnForwardOffset = 1f;


    [Header("Recarga")]
    [SerializeField]
    private float reloadTime = 1f;


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
    private TickTimer ThrowCooldown
    {
        get;
        set;
    }


    private bool wasGrenadeEquipped;

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

        wasGrenadeEquipped =
            false;


        if (Object.HasStateAuthority)
        {
            IsReloadingNetworked =
                false;

            ReloadTimer =
                TickTimer.None;

            ThrowCooldown =
                TickTimer.None;
        }

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE WEAPON READY | " +
                $"PLAYER: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"STATE: " +
                $"{Object.HasStateAuthority} | " +
                $"INPUT: " +
                $"{Object.HasInputAuthority}"
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

        bool grenadeEquipped =
            weaponInventory
                .IsGrenadeEquipped();


        if (grenadeEquipped &&
            !wasGrenadeEquipped)
        {
            OnGrenadeEquipped();
        }

        wasGrenadeEquipped =
            grenadeEquipped;

        if (!grenadeEquipped)
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
            RequestThrowGrenade();
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


    private void OnGrenadeEquipped()
    {
        if (weaponInventory == null)
            return;

        if (weaponInventory
                .GrenadeLoaded <= 0 &&
            weaponInventory
                .GrenadeReserve > 0)
        {
            RequestReload();
        }
    }


    private void RequestThrowGrenade()
    {
        if (!Object.HasInputAuthority)
            return;


        if (playerCamera == null)
        {
            Debug.LogError(
                "GRENADE: " +
                "Player Camera no está asignada."
            );

            return;
        }


        if (!grenadePrefab.IsValid)
        {
            Debug.LogError(
                "GRENADE: " +
                "Grenade Prefab no está asignado."
            );

            return;
        }


        Vector3 forwardDirection =
            playerCamera
                .transform
                .forward
                .normalized;


        Vector3 spawnPosition;

        if (grenadeSpawnPoint != null)
        {
            spawnPosition =
                grenadeSpawnPoint.position;
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
            ProcessThrowAuthoritative(
                spawnPosition,
                forwardDirection
            );

            return;
        }


        RPC_RequestThrowGrenade(
            spawnPosition,
            forwardDirection
        );
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestThrowGrenade(
        Vector3 spawnPosition,
        Vector3 forwardDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        ProcessThrowAuthoritative(
            spawnPosition,
            forwardDirection
        );
    }


    private void ProcessThrowAuthoritative(
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
            .IsGrenadeEquipped())
        {
            return;
        }


        if (IsReloadingNetworked)
            return;


        if (ThrowCooldown.IsRunning &&
            !ThrowCooldown.Expired(
                Runner))
        {
            if (showDebug)
            {
                Debug.Log(
                    "GRENADE: " +
                    "todavía está en cooldown."
                );
            }

            return;
        }


        if (weaponInventory
            .GrenadeLoaded <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    $"GRENADE VACÍA | " +
                    $"Loaded: " +
                    $"{weaponInventory.GrenadeLoaded} | " +
                    $"Reserve: " +
                    $"{weaponInventory.GrenadeReserve}"
                );
            }

            StartReloadAuthoritative();

            return;
        }


        if (!grenadePrefab.IsValid)
        {
            Debug.LogError(
                "GRENADE: " +
                "Grenade Prefab no está asignado."
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


        Vector3 launchVelocity =
            forwardDirection *
            throwForce;

        launchVelocity +=
            Vector3.up *
            upwardForce;

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                forwardDirection,
                Vector3.up
            );


        NetworkObject spawnedGrenade =
            Runner.Spawn(
                grenadePrefab,
                requestedSpawnPosition,
                spawnRotation,
                Object.InputAuthority
            );

        if (spawnedGrenade == null)
        {
            Debug.LogError(
                "GRENADE: " +
                "Runner.Spawn devolvió NULL."
            );

            return;
        }


        GrenadeProjectile grenade =
            spawnedGrenade
                .GetComponent<
                    GrenadeProjectile
                >();

        if (grenade == null)
        {
            Debug.LogError(
                "GRENADE: " +
                "El prefab no tiene " +
                "GrenadeProjectile."
            );

            Runner.Despawn(
                spawnedGrenade
            );

            return;
        }

        grenade.Initialize(
            Object,
            launchVelocity
        );


        weaponInventory
            .GrenadeLoaded--;


        ThrowCooldown =
            TickTimer.CreateFromSeconds(
                Runner,
                timeBetweenThrows
            );

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE THROWN BY HOST | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Loaded: " +
                $"{weaponInventory.GrenadeLoaded} | " +
                $"Reserve: " +
                $"{weaponInventory.GrenadeReserve} | " +
                $"Position: " +
                $"{requestedSpawnPosition} | " +
                $"Velocity: " +
                $"{launchVelocity}"
            );
        }


        if (weaponInventory
                .GrenadeLoaded <= 0 &&
            weaponInventory
                .GrenadeReserve > 0)
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
                .GrenadeLoaded >=
            weaponInventory
                .GrenadeMagazineSize)
        {
            return;
        }


        if (weaponInventory
            .GrenadeReserve <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    "GRENADE: " +
                    "SIN GRANADAS EN RESERVA."
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
                $"GRENADE RELOADING... | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Loaded: " +
                $"{weaponInventory.GrenadeLoaded} | " +
                $"Reserve: " +
                $"{weaponInventory.GrenadeReserve}"
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


        int grenadesNeeded =
            weaponInventory
                .GrenadeMagazineSize -
            weaponInventory
                .GrenadeLoaded;


        int grenadesToLoad =
            Mathf.Min(
                grenadesNeeded,
                weaponInventory
                    .GrenadeReserve
            );


        weaponInventory
            .GrenadeLoaded +=
            grenadesToLoad;

        weaponInventory
            .GrenadeReserve -=
            grenadesToLoad;

        IsReloadingNetworked =
            false;

        ReloadTimer =
            TickTimer.None;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE RELOAD COMPLETE | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Loaded: " +
                $"{weaponInventory.GrenadeLoaded} | " +
                $"Reserve: " +
                $"{weaponInventory.GrenadeReserve}"
            );
        }
    }
}