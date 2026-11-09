using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PShooting : NetworkBehaviour
{
    // ============================
    // REFERENCES
    // ============================

    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // WEAPON
    // ============================

    [Header("Weapon")]
    [SerializeField]
    private string weaponName =
        "PISTOL";

    // ============================
    // SHOOTING
    // ============================

    [Header("Shooting")]
    [SerializeField]
    private float range =
        100f;

    [SerializeField]
    private int damage =
        25;

    // ============================
    // AMMO
    // ============================

    [Header("Ammo")]
    [SerializeField]
    private int magazineSize =
        6;

    [SerializeField]
    private float reloadTime =
        2f;

    // ============================
    // SPREAD
    // ============================

    [Header("Spread")]
    [SerializeField]
    private float spread =
        0.03f;

    // ============================
    // TRACER
    // ============================

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

    // ============================
    // TRACER VISUAL ORIGIN
    // ============================

    [Header("Tracer Visual Origin")]
    [SerializeField]
    private float tracerForwardOffset =
        0.5f;

    [SerializeField]
    private float tracerDownOffset =
        0.15f;

    // ============================
    // MUZZLE FLASH
    // ============================

    [Header("Shoot Effect")]
    [SerializeField]
    private ParticleSystem muzzleFlash;

    // ============================
    // NETWORK STATE
    // ============================

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

    // ============================
    // REFERENCES
    // ============================

    private PWeaponInventory weaponInventory;

    private PHealth health;

    private NetworkGameManager gameManager;

    private DeathmatchGameManager
        deathmatchGameManager;

    // ============================
    // PUBLIC PROPERTIES
    // ============================

    public string WeaponName =>
        weaponName;

    public int CurrentAmmo =>
        CurrentAmmoNetworked;

    public int MagazineSize =>
        magazineSize;

    public bool IsReloading =>
        IsReloadingNetworked;

    // ============================
    // AWAKE
    // ============================

    private void Awake()
    {
        weaponInventory =
            GetComponent<PWeaponInventory>();

        health =
            GetComponent<PHealth>();
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
            $"INPUT: {Object.HasInputAuthority} | " +
            $"SCENE: {SceneManager.GetActiveScene().name}"
        );
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

        // ============================
        // HEALTH
        // ============================

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

        // ============================
        // CAN USE WEAPON?
        // ============================

        if (!CanUseWeapon())
        {
            return;
        }

        // ============================
        // WEAPON INVENTORY
        // ============================

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

        // ============================
        // RELOAD
        // ============================

        if (Input.GetKeyDown(
            KeyCode.R))
        {
            RequestReload();
        }

        if (IsReloadingNetworked)
            return;

        // ============================
        // SHOOT
        // ============================

        if (Input.GetMouseButtonDown(
            0))
        {
            RequestShoot();
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
    // REQUEST SHOOT
    // ============================

    private void RequestShoot()
    {
        if (!Object.HasInputAuthority)
            return;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PShooting: " +
                "Player Camera is not assigned."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PShooting: " +
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

    // ============================
    // SHOOT RPC
    // ============================

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

    // ============================
    // AUTHORITATIVE SHOOT
    // ============================

    private void ProcessShootAuthoritative(
        Vector3 shotOrigin,
        Vector3 shotDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        // ============================
        // CAN USE WEAPON?
        // ============================

        if (!CanUseWeapon())
        {
            return;
        }

        // ============================
        // HEALTH
        // ============================

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

        // ============================
        // WEAPON INVENTORY
        // ============================

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

        // ============================
        // RELOAD CHECK
        // ============================

        if (IsReloadingNetworked)
            return;

        // ============================
        // AMMO CHECK
        // ============================

        if (CurrentAmmoNetworked <= 0)
        {
            StartReloadAuthoritative();

            return;
        }

        CurrentAmmoNetworked--;

        // ============================
        // RAYCAST
        // ============================

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

        // ============================
        // EFFECTS
        // ============================

        RPC_PlayShotEffects(
            shotOrigin,
            tracerEndPoint
        );

        Debug.Log(
            $"SHOT | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Ammo: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );

        // ============================
        // AUTO RELOAD
        // ============================

        if (CurrentAmmoNetworked <= 0)
        {
            StartReloadAuthoritative();
        }
    }

    // ============================
    // SHOT EFFECTS RPC
    // ============================

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

    // ============================
    // RELOAD ROUTINE
    // ============================

    private IEnumerator ReloadRoutine()
    {
        if (!Object.HasStateAuthority)
            yield break;

        if (IsReloadingNetworked)
            yield break;

        IsReloadingNetworked =
            true;

        Debug.Log(
            $"RELOADING... | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Current Ammo: " +
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
            $"RELOAD COMPLETE | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Ammo: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );
    }

    // ============================
    // TRACER
    // ============================

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

    // ============================
    // RESET PISTOL AMMO
    // ============================

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
            $"Ammo: " +
            $"{CurrentAmmoNetworked}/" +
            $"{magazineSize}"
        );
    }
}