using System.Collections;
using Fusion;
using UnityEngine;

public class PSniper : NetworkBehaviour
{

    [Header("Referencias")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;


    [Header("Sniper")]
    [SerializeField]
    private float range = 200f;

    [SerializeField]
    private int damage = 75;


    [Header("Cadencia")]
    [SerializeField]
    private float timeBetweenShots = 1f;


    [Header("Recarga")]
    [SerializeField]
    private float reloadTime = 2.8f;


    [Header("Dispersión")]
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

    [Header("Origen visual del tracer")]
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
            $"INPUT: {Object.HasInputAuthority}"
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


    private void RequestShoot()
    {
        if (!Object.HasInputAuthority)
            return;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PSniper: " +
                "Player Camera no está asignada."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PSniper: " +
                "Fire Point no está asignado."
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
                "SNIPER SIN MUNICIÓN DE RESERVA"
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