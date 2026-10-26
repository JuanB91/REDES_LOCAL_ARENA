using System.Collections;
using Fusion;
using UnityEngine;

public class PShotgun : NetworkBehaviour
{

    [Header("Referencias")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;


    [Header("Escopeta")]
    [SerializeField]
    private float range = 30f;

    [SerializeField]
    private int pelletsPerShot = 8;

    [SerializeField]
    private int damagePerPellet = 8;


    [Header("Munición")]
    [SerializeField]
    private int maxLoadedShells = 2;

    [SerializeField]
    private float reloadTime = 2f;


    [Header("Dispersión")]
    [SerializeField]
    private float spread = 0.08f;


    [Header("Falloff")]
    [SerializeField]
    private float fullDamageDistance = 5f;

    [SerializeField]
    private float minimumDamageDistance = 12f;

    [SerializeField]
    private float minimumDamageMultiplier = 0.3f;


    [Header("Tracer")]
    [SerializeField]
    private Material tracerMaterial;

    [SerializeField]
    private Color tracerColor = Color.red;

    [SerializeField]
    private float tracerDuration = 0.5f;

    [SerializeField]
    private float tracerWidth = 0.035f;


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
        }

        Debug.Log(
            $"SHOTGUN READY | " +
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
            .IsShotgunEquipped())
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
                "PShotgun: " +
                "Player Camera no está asignada."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PShotgun: " +
                "Fire Point no está asignado."
            );

            return;
        }

        Vector3 cameraForward =
            playerCamera
                .transform
                .forward;

        Vector3 cameraRight =
            playerCamera
                .transform
                .right;

        Vector3 cameraUp =
            playerCamera
                .transform
                .up;

        Vector3 shotOrigin =
            firePoint.position;


        if (Object.HasStateAuthority)
        {
            ProcessShootAuthoritative(
                shotOrigin,
                cameraForward,
                cameraRight,
                cameraUp
            );

            return;
        }


        RPC_RequestShoot(
            shotOrigin,
            cameraForward,
            cameraRight,
            cameraUp
        );
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestShoot(
        Vector3 shotOrigin,
        Vector3 cameraForward,
        Vector3 cameraRight,
        Vector3 cameraUp)
    {
        if (!Object.HasStateAuthority)
            return;

        ProcessShootAuthoritative(
            shotOrigin,
            cameraForward,
            cameraRight,
            cameraUp
        );
    }


    private void ProcessShootAuthoritative(
        Vector3 shotOrigin,
        Vector3 cameraForward,
        Vector3 cameraRight,
        Vector3 cameraUp)
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
            .IsShotgunEquipped())
        {
            return;
        }


        if (IsReloadingNetworked)
            return;


        if (weaponInventory
            .ShotgunLoaded <= 0)
        {
            Debug.Log(
                "SHOTGUN VACÍA"
            );

            StartReloadAuthoritative();

            return;
        }


        weaponInventory
            .ShotgunLoaded--;

        Debug.Log(
            $"SHOTGUN FIRE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Loaded: " +
            $"{weaponInventory.ShotgunLoaded} | " +
            $"Reserve: " +
            $"{weaponInventory.ShotgunReserve}"
        );


        for (
            int i = 0;
            i < pelletsPerShot;
            i++)
        {
            FireSinglePelletAuthoritative(
                shotOrigin,
                cameraForward,
                cameraRight,
                cameraUp
            );
        }


        RPC_PlayMuzzleFlash();


        if (weaponInventory
            .ShotgunLoaded <= 0)
        {
            StartReloadAuthoritative();
        }
    }


    private void FireSinglePelletAuthoritative(
        Vector3 shotOrigin,
        Vector3 cameraForward,
        Vector3 cameraRight,
        Vector3 cameraUp)
    {
        if (!Object.HasStateAuthority)
            return;


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

        Vector3 shotDirection =
            cameraForward;

        shotDirection +=
            cameraRight *
            randomHorizontal;

        shotDirection +=
            cameraUp *
            randomVertical;

        shotDirection.Normalize();


        Ray ray =
            new Ray(
                shotOrigin,
                shotDirection
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
                float damageMultiplier =
                    CalculateDamageFalloff(
                        hit.distance
                    );

                int finalDamage =
                    Mathf.RoundToInt(
                        damagePerPellet *
                        damageMultiplier
                    );

                finalDamage =
                    Mathf.Max(
                        0,
                        finalDamage
                    );

                Debug.Log(
                    $"SHOTGUN HIT | " +
                    $"Distance: {hit.distance:F1}m | " +
                    $"Damage pellet: {finalDamage}"
                );

                targetHealth
                    .RPC_TakeDamage(
                        finalDamage,
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


        RPC_PlayTracer(
            shotOrigin,
            tracerEndPoint
        );
    }


    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
    }


    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayTracer(
        Vector3 start,
        Vector3 end)
    {
        StartCoroutine(
            ShowTracer(
                start,
                end
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
            .ShotgunLoaded >=
            maxLoadedShells)
        {
            return;
        }

        if (weaponInventory
            .ShotgunReserve <= 0)
        {
            Debug.Log(
                "SHOTGUN SIN MUNICIÓN DE RESERVA"
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
            $"SHOTGUN RELOADING... | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Loaded: " +
            $"{weaponInventory.ShotgunLoaded} | " +
            $"Reserve: " +
            $"{weaponInventory.ShotgunReserve}"
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

        int shellsNeeded =
            maxLoadedShells -
            weaponInventory
                .ShotgunLoaded;

        int shellsToLoad =
            Mathf.Min(
                shellsNeeded,
                weaponInventory
                    .ShotgunReserve
            );

        weaponInventory
            .ShotgunLoaded +=
            shellsToLoad;

        weaponInventory
            .ShotgunReserve -=
            shellsToLoad;

        IsReloadingNetworked =
            false;

        ReloadTimer =
            TickTimer.None;

        Debug.Log(
            $"SHOTGUN RELOAD COMPLETE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Loaded: " +
            $"{weaponInventory.ShotgunLoaded} | " +
            $"Reserve: " +
            $"{weaponInventory.ShotgunReserve}"
        );
    }


    private float CalculateDamageFalloff(
        float distance)
    {
        if (distance <=
            fullDamageDistance)
        {
            return 1f;
        }

        if (distance >=
            minimumDamageDistance)
        {
            return
                minimumDamageMultiplier;
        }

        float t =
            Mathf.InverseLerp(
                fullDamageDistance,
                minimumDamageDistance,
                distance
            );

        return
            Mathf.Lerp(
                1f,
                minimumDamageMultiplier,
                t
            );
    }


    private IEnumerator ShowTracer(
        Vector3 start,
        Vector3 end)
    {
        GameObject tracerObject =
            new GameObject(
                "ShotgunPelletTracer"
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