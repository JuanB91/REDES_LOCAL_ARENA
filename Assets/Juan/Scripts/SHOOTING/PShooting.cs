using System.Collections;
using Fusion;
using UnityEngine;

public class PShooting : NetworkBehaviour
{
    
    [Header("Referencias")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;


    [Header("Arma")]
    [SerializeField]
    private string weaponName =
        "PISTOL";


    [Header("Disparo")]
    [SerializeField]
    private float range =
        100f;

    [SerializeField]
    private int damage =
        25;


    [Header("Munición")]
    [SerializeField]
    private int magazineSize =
        6;

    [SerializeField]
    private float reloadTime =
        2f;


    [Header("Dispersión")]
    [SerializeField]
    private float spread =
        0.03f;


    [Header("Tracer")]
    [SerializeField]
    private Color tracerColor =
        Color.yellow;

    [SerializeField]
    private float tracerDuration =
        0.5f;

    [SerializeField]
    private float tracerWidth =
        0.06f;


    [Header("Origen visual del tracer")]
    [SerializeField]
    private float tracerForwardOffset =
        0.5f;

    [SerializeField]
    private float tracerDownOffset =
        0.15f;


    [Header("Efecto de disparo")]
    [SerializeField]
    private ParticleSystem muzzleFlash;


    [Networked]
    private int CurrentAmmoNetworked
    {
        get;
        set;
    }

    [Networked]
    private NetworkBool IsReloadingNetworked
    {
        get;
        set;
    }


    private PWeaponInventory weaponInventory;

    private PHealth health;

    private NetworkGameManager gameManager;


    public string WeaponName =>
        weaponName;

    public int CurrentAmmo =>
        CurrentAmmoNetworked;

    public int MagazineSize =>
        magazineSize;

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
            CurrentAmmoNetworked =
                magazineSize;

            IsReloadingNetworked =
                false;
        }

        Debug.Log(
            $"PSHOOTING READY | " +
            $"PLAYER: " +
            $"{Object.InputAuthority.PlayerId} | " +
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

        if (weaponInventory != null &&
            !weaponInventory.IsPistolEquipped())
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


    private void RequestShoot()
    {
        if (!Object.HasInputAuthority)
            return;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PShooting: " +
                "Player Camera no está asignada."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PShooting: " +
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

        if (weaponInventory != null &&
            !weaponInventory.IsPistolEquipped())
        {
            return;
        }


        if (IsReloadingNetworked)
            return;


        if (CurrentAmmoNetworked <= 0)
        {
            StartReloadAuthoritative();

            return;
        }

        CurrentAmmoNetworked--;


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
                $"SHOT HIT | " +
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
                shotOrigin +
                shotDirection.normalized *
                range;

            Debug.Log(
                $"SHOT MISS | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId}"
            );
        }


        RPC_PlayShotEffects(
            shotOrigin,
            tracerEndPoint
        );

        Debug.Log(
            $"DISPARO | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Balas: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );


        if (CurrentAmmoNetworked <= 0)
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

        if (IsReloadingNetworked)
            return;

        if (CurrentAmmoNetworked >=
            magazineSize)
        {
            return;
        }

        StartCoroutine(
            ReloadRoutine()
        );
    }


    private IEnumerator ReloadRoutine()
    {
        if (!Object.HasStateAuthority)
            yield break;

        if (IsReloadingNetworked)
            yield break;

        IsReloadingNetworked =
            true;

        Debug.Log(
            $"RECARGANDO... | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Balas actuales: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );

        yield return
            new WaitForSeconds(
                reloadTime
            );

        if (Object == null)
            yield break;

        if (!Object.IsValid)
            yield break;

        if (!Object.HasStateAuthority)
            yield break;

        CurrentAmmoNetworked =
            magazineSize;

        IsReloadingNetworked =
            false;

        Debug.Log(
            $"RECARGA COMPLETA | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Balas: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );
    }


    private IEnumerator ShowTracer(
        Vector3 start,
        Vector3 end)
    {
        GameObject tracerObject =
            new GameObject(
                "BulletTracer"
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


    public void ResetPistolAmmo()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        StopAllCoroutines();

        CurrentAmmoNetworked =
            magazineSize;

        IsReloadingNetworked =
            false;

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} " +
            $"PISTOL RESET | " +
            $"Balas: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );
    }
}