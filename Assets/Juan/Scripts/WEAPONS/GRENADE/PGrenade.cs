using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PGrenade : NetworkBehaviour
{
    // ============================
    // REFERENCES
    // ============================

    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private PWeaponInventory weaponInventory;

    [SerializeField]
    private PHealth health;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // PROJECTILE
    // ============================

    [Header("Projectile")]
    [SerializeField]
    private NetworkPrefabRef grenadePrefab;

    [SerializeField]
    private Transform grenadeSpawnPoint;

    // ============================
    // THROW
    // ============================

    [Header("Throw")]
    [SerializeField]
    private float throwForce =
        15f;

    [SerializeField]
    private float upwardForce =
        4f;

    [SerializeField]
    private float timeBetweenThrows =
        0.8f;

    [SerializeField]
    private float spawnForwardOffset =
        1f;

    // ============================
    // RELOAD
    // ============================

    [Header("Reload")]
    [SerializeField]
    private float reloadTime =
        1f;

    // ============================
    // DEBUG
    // ============================

    [Header("Debug")]
    [SerializeField]
    private bool showDebug =
        true;

    // ============================
    // NETWORK STATE
    // ============================

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

    // ============================
    // RUNTIME
    // ============================

    private bool wasGrenadeEquipped;

    // ============================
    // MANAGERS
    // ============================

    private NetworkGameManager gameManager;

    private DeathmatchGameManager
        deathmatchGameManager;

    // ============================
    // PUBLIC
    // ============================

    public bool IsReloading =>
        IsReloadingNetworked;

    // ============================
    // AWAKE
    // ============================

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

    // ============================
    // SPAWNED
    // ============================

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

        deathmatchGameManager =
            FindFirstObjectByType<
                DeathmatchGameManager
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
                $"{Object.HasInputAuthority} | " +
                $"SCENE: " +
                $"{SceneManager.GetActiveScene().name}"
            );
        }
    }

    // ============================
    // UPDATE
    // ============================

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

        if (!CanUseWeapon())
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

    // ============================
    // FIXED UPDATE NETWORK
    // ============================

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

    // ============================
    // CAN USE WEAPON
    // ============================

    private bool CanUseWeapon()
    {
        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        // ============================
        // DEATHMATCH
        // ============================

        if (sceneName ==
            deathmatchSceneName)
        {
            if (deathmatchGameManager == null)
            {
                deathmatchGameManager =
                    FindFirstObjectByType<
                        DeathmatchGameManager
                    >();
            }

            if (deathmatchGameManager == null)
            {
                return false;
            }

            return
                deathmatchGameManager
                    .CanPlay;
        }

        // ============================
        // TEAM VS TEAM
        // ============================

        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

        if (gameManager == null)
        {
            return false;
        }

        if (!gameManager.CanPlay)
        {
            return false;
        }

        if (gameManager.IsReady &&
            gameManager.GameOver)
        {
            return false;
        }

        return true;
    }

    // ============================
    // GRENADE EQUIPPED
    // ============================

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

    // ============================
    // REQUEST THROW
    // ============================

    private void RequestThrowGrenade()
    {
        if (!Object.HasInputAuthority)
            return;

        if (!CanUseWeapon())
            return;

        if (playerCamera == null)
        {
            Debug.LogError(
                "GRENADE: " +
                "Player Camera is not assigned."
            );

            return;
        }

        if (!grenadePrefab.IsValid)
        {
            Debug.LogError(
                "GRENADE: " +
                "Grenade Prefab is not assigned."
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

    // ============================
    // THROW RPC
    // ============================

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

    // ============================
    // AUTHORITATIVE THROW
    // ============================

    private void ProcessThrowAuthoritative(
        Vector3 requestedSpawnPosition,
        Vector3 forwardDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!CanUseWeapon())
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
                    "GRENADE: still on cooldown."
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
                    $"GRENADE EMPTY | " +
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
                "Grenade Prefab is not assigned."
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
                "Runner.Spawn returned NULL."
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
                "Prefab does not contain " +
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

    // ============================
    // REQUEST RELOAD
    // ============================

    private void RequestReload()
    {
        if (!Object.HasInputAuthority)
            return;

        if (!CanUseWeapon())
            return;

        if (Object.HasStateAuthority)
        {
            StartReloadAuthoritative();

            return;
        }

        RPC_RequestReload();
    }

    // ============================
    // RELOAD RPC
    // ============================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestReload()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!CanUseWeapon())
            return;

        StartReloadAuthoritative();
    }

    // ============================
    // START RELOAD
    // ============================

    private void StartReloadAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!CanUseWeapon())
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
                    "NO GRENADES IN RESERVE."
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

    // ============================
    // COMPLETE RELOAD
    // ============================

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