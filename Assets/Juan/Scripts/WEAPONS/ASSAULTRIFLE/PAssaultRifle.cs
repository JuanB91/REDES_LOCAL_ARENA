using System.Collections;
using Fusion;
using UnityEngine;

public class PAssaultRifle : NetworkBehaviour
{

    [Header("Referencias")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;


    [Header("Assault Rifle")]
    [SerializeField]
    private float range = 100f;

    [SerializeField]
    private int damage = 11;


    [Header("Cadencia")]
    [SerializeField]
    private float shotsPerSecond = 8f;


    [Header("Recarga")]
    [SerializeField]
    private float reloadTime = 2.2f;


    [Header("Dispersión")]
    [SerializeField]
    private float spread = 0.02f;


    [Header("Tracer")]
    [SerializeField]
    private Color tracerColor = Color.cyan;

    [SerializeField]
    private float tracerDuration = 0.15f;

    [SerializeField]
    private float tracerWidth = 0.025f;

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
    private TickTimer ShotCooldown
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


    private PWeaponInventory weaponInventory;

    private PHealth health;

    private NetworkGameManager gameManager;


    public bool IsReloading =>
        IsReloadingNetworked;


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

        gameManager =
            FindFirstObjectByType<
                NetworkGameManager
            >();

        if (Object.HasStateAuthority)
        {
            IsReloadingNetworked =
                false;

            ShotCooldown =
                TickTimer.None;

            ReloadTimer =
                TickTimer.None;
        }

        Debug.Log(
            $"ASSAULT RIFLE READY | " +
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
            .IsAssaultRifleEquipped())
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


        if (Input.GetMouseButton(
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
                "PAssaultRifle: " +
                "Player Camera no está asignada."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PAssaultRifle: " +
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
            .IsAssaultRifleEquipped())
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
            .AssaultRifleLoaded <= 0)
        {
            StartReloadAuthoritative();

            return;
        }


        weaponInventory
            .AssaultRifleLoaded--;


        float shotInterval =
            shotsPerSecond > 0f
                ? 1f / shotsPerSecond
                : 0.125f;

        ShotCooldown =
            TickTimer.CreateFromSeconds(
                Runner,
                shotInterval
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

            Debug.Log(
                $"ASSAULT RIFLE HIT | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Target: {hit.collider.name}"
            );
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
            $"ASSAULT RIFLE FIRE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Cargadas: " +
            $"{weaponInventory.AssaultRifleLoaded} | " +
            $"Reserva: " +
            $"{weaponInventory.AssaultRifleReserve}"
        );


        if (weaponInventory
            .AssaultRifleLoaded <= 0)
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
            .AssaultRifleLoaded >=
            weaponInventory
                .AssaultRifleMagazineSize)
        {
            return;
        }

        if (weaponInventory
            .AssaultRifleReserve <= 0)
        {
            Debug.Log(
                "ASSAULT RIFLE SIN MUNICIÓN DE RESERVA"
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
            $"ASSAULT RIFLE RELOADING... | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Cargadas: " +
            $"{weaponInventory.AssaultRifleLoaded} | " +
            $"Reserva: " +
            $"{weaponInventory.AssaultRifleReserve}"
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
                .AssaultRifleMagazineSize -
            weaponInventory
                .AssaultRifleLoaded;

        int bulletsToLoad =
            Mathf.Min(
                bulletsNeeded,
                weaponInventory
                    .AssaultRifleReserve
            );

        weaponInventory
            .AssaultRifleLoaded +=
            bulletsToLoad;

        weaponInventory
            .AssaultRifleReserve -=
            bulletsToLoad;

        IsReloadingNetworked =
            false;

        ReloadTimer =
            TickTimer.None;

        Debug.Log(
            $"ASSAULT RIFLE RELOAD COMPLETE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Cargadas: " +
            $"{weaponInventory.AssaultRifleLoaded} | " +
            $"Reserva: " +
            $"{weaponInventory.AssaultRifleReserve}"
        );
    }


    private IEnumerator ShowTracer(
        Vector3 start,
        Vector3 end)
    {
        GameObject tracerObject =
            new GameObject(
                "AssaultRifleTracer"
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

        Material tracerMaterial =
            new Material(
                Shader.Find(
                    "Sprites/Default"
                )
            );

        tracerMaterial.color =
            tracerColor;

        lineRenderer.material =
            tracerMaterial;

        yield return
            new WaitForSeconds(
                tracerDuration
            );

        if (tracerMaterial != null)
        {
            Destroy(
                tracerMaterial
            );
        }

        if (tracerObject != null)
        {
            Destroy(
                tracerObject
            );
        }
    }
}