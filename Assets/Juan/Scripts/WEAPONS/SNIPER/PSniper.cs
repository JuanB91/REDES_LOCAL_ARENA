using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PSniper : NetworkBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    [Header("Sniper")]
    [SerializeField]
    private float range = 200f;

    [SerializeField]
    private int damage = 75;

    [Header("Fire Rate")]
    [SerializeField]
    private float timeBetweenShots = 1f;

    [Header("Reload")]
    [SerializeField]
    private float reloadTime = 2.8f;

    [Header("Spread")]
    [SerializeField]
    private float spread = 0.002f;

    [Header("Tracer")]
    [SerializeField]
    private Material tracerMaterial;

    [SerializeField]
    private Color tracerColor = Color.white;

    [SerializeField]
    private float tracerDuration = 0.35f;

    [SerializeField]
    private float tracerWidth = 0.02f;

    [Header("Tracer Visual Origin")]
    [SerializeField]
    private float tracerForwardOffset = 0.5f;

    [SerializeField]
    private float tracerDownOffset = 0.15f;

    [SerializeField]
    private ParticleSystem muzzleFlash;

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

    private PWeaponInventory weaponInventory;

    private PHealth health;

    private NetworkGameManager gameManager;

    private DeathmatchGameManager
        deathmatchGameManager;

    public bool IsReloading =>
        IsReloadingNetworked;

    public float TimeBetweenShots =>
        timeBetweenShots;

    public int Damage =>
        damage;

    public float Range =>
        range;

    private void Awake()
    {
        weaponInventory =
            GetComponent<PWeaponInventory>();

        health =
            GetComponent<PHealth>();
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

        deathmatchGameManager =
            FindFirstObjectByType<
                DeathmatchGameManager
            >();

        if (Object.HasStateAuthority)
        {
            IsReloadingNetworked =
                false;

            ReloadTimer =
                TickTimer.None;

            ShotCooldown =
                TickTimer.None;
        }

        Debug.Log(
            $"SNIPER READY | " +
            $"PLAYER: {Object.InputAuthority.PlayerId} | " +
            $"STATE: {Object.HasStateAuthority} | " +
            $"INPUT: {Object.HasInputAuthority} | " +
            $"SCENE: {SceneManager.GetActiveScene().name}"
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

        if (!weaponInventory
            .IsSniperEquipped())
        {
            return;
        }

        if (Input.GetKeyDown(
            KeyCode.R))
        {
            RequestReload();
        }

        if (IsReloadingNetworked)
            return;

        if (Input.GetMouseButtonDown(
            0))
        {
            RequestShoot();
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

    private void RequestShoot()
    {
        if (!Object.HasInputAuthority)
            return;

        if (!CanUseWeapon())
            return;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PSniper: " +
                "Player Camera is not assigned."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PSniper: " +
                "Fire Point is not assigned."
            );

            return;
        }

        Vector3 shotDirection =
            playerCamera
                .transform
                .forward;

        float randomHorizontal =
            Random.Range(
                -spread,
                spread
            );

        float randomVertical =
            Random.Range(
                -spread,
                spread
            );

        shotDirection +=
            playerCamera
                .transform
                .right *
            randomHorizontal;

        shotDirection +=
            playerCamera
                .transform
                .up *
            randomVertical;

        shotDirection.Normalize();

        Vector3 shotOrigin =
            firePoint.position;

        if (Object.HasStateAuthority)
        {
            ProcessShootAuthoritative(
                shotOrigin,
                shotDirection
            );

            return;
        }

        RPC_RequestShoot(
            shotOrigin,
            shotDirection
        );
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestShoot(
        Vector3 shotOrigin,
        Vector3 shotDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        ProcessShootAuthoritative(
            shotOrigin,
            shotDirection
        );
    }

    private void ProcessShootAuthoritative(
        Vector3 shotOrigin,
        Vector3 shotDirection)
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
            .IsSniperEquipped())
        {
            return;
        }

        if (IsReloadingNetworked)
            return;

        if (ShotCooldown.IsRunning &&
            !ShotCooldown.Expired(
                Runner))
        {
            return;
        }

        if (weaponInventory
            .SniperLoaded <= 0)
        {
            StartReloadAuthoritative();

            return;
        }

        weaponInventory
            .SniperLoaded--;

        ShotCooldown =
            TickTimer.CreateFromSeconds(
                Runner,
                timeBetweenShots
            );

        Ray ray =
            new Ray(
                shotOrigin,
                shotDirection.normalized
            );

        Vector3 tracerEndPoint;

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            range))
        {
            tracerEndPoint =
                hit.point;

            Debug.Log(
                $"SNIPER HIT | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Target: {hit.collider.name}"
            );

            PHealth targetHealth =
                hit.collider
                    .GetComponentInParent<
                        PHealth
                    >();

            if (targetHealth != null)
            {
                targetHealth
                    .RPC_TakeDamage(
                        damage,
                        Object
                    );
            }
        }
        else
        {
            tracerEndPoint =
                ray.origin +
                ray.direction *
                range;
        }

        RPC_PlayShotEffects(
            shotOrigin,
            tracerEndPoint
        );

        Debug.Log(
            $"SNIPER FIRE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Loaded: " +
            $"{weaponInventory.SniperLoaded} | " +
            $"Reserve: " +
            $"{weaponInventory.SniperReserve}"
        );

        if (weaponInventory
            .SniperLoaded <= 0)
        {
            StartReloadAuthoritative();
        }
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayShotEffects(
        Vector3 tracerStart,
        Vector3 tracerEnd)
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        StartCoroutine(
            ShowTracer(
                tracerStart,
                tracerEnd
            )
        );
    }

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
                .SniperLoaded >=
            weaponInventory
                .SniperMagazineSize)
        {
            return;
        }

        if (weaponInventory
            .SniperReserve <= 0)
        {
            Debug.Log(
                "SNIPER NO RESERVE AMMO"
            );

            return;
        }

        IsReloadingNetworked =
            true;

        ReloadTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                reloadTime
            );

        Debug.Log(
            $"SNIPER RELOADING... | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Loaded: " +
            $"{weaponInventory.SniperLoaded} | " +
            $"Reserve: " +
            $"{weaponInventory.SniperReserve}"
        );
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

        int bulletsNeeded =
            weaponInventory
                .SniperMagazineSize -
            weaponInventory
                .SniperLoaded;

        int bulletsToLoad =
            Mathf.Min(
                bulletsNeeded,
                weaponInventory
                    .SniperReserve
            );

        weaponInventory
            .SniperLoaded +=
            bulletsToLoad;

        weaponInventory
            .SniperReserve -=
            bulletsToLoad;

        IsReloadingNetworked =
            false;

        ReloadTimer =
            TickTimer.None;

        Debug.Log(
            $"SNIPER RELOAD COMPLETE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Loaded: " +
            $"{weaponInventory.SniperLoaded} | " +
            $"Reserve: " +
            $"{weaponInventory.SniperReserve}"
        );
    }

    private IEnumerator ShowTracer(
        Vector3 start,
        Vector3 end)
    {
        GameObject tracerObject =
            new GameObject(
                "SniperTracer"
            );

        LineRenderer lineRenderer =
            tracerObject
                .AddComponent<
                    LineRenderer
                >();

        lineRenderer.positionCount =
            2;

        lineRenderer.SetPosition(
            0,
            start
        );

        lineRenderer.SetPosition(
            1,
            end
        );

        lineRenderer.startWidth =
            tracerWidth;

        lineRenderer.endWidth =
            tracerWidth;

        lineRenderer.startColor =
            tracerColor;

        lineRenderer.endColor =
            tracerColor;

        if (tracerMaterial != null)
        {
            lineRenderer.material =
                tracerMaterial;
        }
        else
        {
            Material fallbackMaterial =
                new Material(
                    Shader.Find(
                        "Sprites/Default"
                    )
                );

            fallbackMaterial.color =
                tracerColor;

            lineRenderer.material =
                fallbackMaterial;
        }

        yield return
            new WaitForSeconds(
                tracerDuration
            );

        Destroy(
            tracerObject
        );
    }
}