using System.Collections;
using Fusion;
using UnityEngine;

public class PShotgun : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform firePoint;

    [Header("Escopeta")]
    [SerializeField] private float range = 30f;
    [SerializeField] private int pelletsPerShot = 8;
    [SerializeField] private int damagePerPellet = 8;

    [Header("Munición")]
    [SerializeField] private int maxLoadedShells = 2;
    [SerializeField] private float reloadTime = 2f;

    [Header("Dispersión")]
    [SerializeField] private float spread = 0.08f;

    [Header("Falloff")]
    [SerializeField] private float fullDamageDistance = 5f;
    [SerializeField] private float minimumDamageDistance = 12f;
    [SerializeField] private float minimumDamageMultiplier = 0.3f;

    [Header("Tracer")]
    [SerializeField] private Material tracerMaterial;
    [SerializeField] private Color tracerColor = Color.red;
    [SerializeField] private float tracerDuration = 0.5f;
    [SerializeField] private float tracerWidth = 0.035f;

    [Header("Origen visual del tracer")]
    [SerializeField] private float tracerForwardOffset = 0.5f;
    [SerializeField] private float tracerDownOffset = 0.15f;
    [SerializeField] private ParticleSystem muzzleFlash;


    private PWeaponInventory weaponInventory;

    private bool isReloading;

    public bool IsReloading => isReloading;

    private void Awake()
    {
        weaponInventory =
            GetComponent<PWeaponInventory>();
    }

    private void Update()
    {
        if (Object == null ||
            !Object.HasStateAuthority)
        {
            return;
        }

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        if (weaponInventory == null)
            return;

        // Solo funciona si la escopeta
        // está seleccionada.
        if (!weaponInventory.IsShotgunEquipped())
            return;

        // -------------------------
        // RECARGA MANUAL
        // -------------------------

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        // Mientras recarga,
        // no puede disparar.
        if (isReloading)
            return;

        // -------------------------
        // DISPARO
        // -------------------------

        if (Input.GetMouseButtonDown(0))
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (weaponInventory.ShotgunLoaded <= 0)
        {
            Debug.Log(
                "SHOTGUN VACÍA"
            );

            TryReload();

            return;
        }

        weaponInventory.ShotgunLoaded--;

        Debug.Log(
            $"SHOTGUN FIRE | " +
            $"Cargados: {weaponInventory.ShotgunLoaded} | " +
            $"Reserva: {weaponInventory.ShotgunReserve}"
        );

        FirePellets();

        if (weaponInventory.ShotgunLoaded <= 0)
        {
            TryReload();
        }
    }

    private void TryReload()
    {
        if (isReloading)
            return;

        if (weaponInventory.ShotgunLoaded >= maxLoadedShells)
            return;

        if (weaponInventory.ShotgunReserve <= 0)
        {
            Debug.Log(
                "SHOTGUN SIN MUNICIÓN DE RESERVA"
            );

            return;
        }

        StartCoroutine(
            ReloadRoutine()
        );
    }

    private IEnumerator ReloadRoutine()
    {
        if (isReloading)
            yield break;

        isReloading = true;

        Debug.Log(
            $"SHOTGUN RELOADING... | " +
            $"Cargados: {weaponInventory.ShotgunLoaded} | " +
            $"Reserva: {weaponInventory.ShotgunReserve}"
        );

        yield return new WaitForSeconds(
            reloadTime
        );

        int shellsNeeded =
            maxLoadedShells -
            weaponInventory.ShotgunLoaded;

        int shellsToLoad =
            Mathf.Min(
                shellsNeeded,
                weaponInventory.ShotgunReserve
            );

        weaponInventory.ShotgunLoaded +=
            shellsToLoad;

        weaponInventory.ShotgunReserve -=
            shellsToLoad;

        isReloading = false;

        Debug.Log(
            $"SHOTGUN RELOAD COMPLETE | " +
            $"Cargados: {weaponInventory.ShotgunLoaded} | " +
            $"Reserva: {weaponInventory.ShotgunReserve}"
        );
    }

    private void FirePellets()
    {
        if (playerCamera == null)
        {
            Debug.LogError(
                "PShotgun: Player Camera no está asignada."
            );

            return;
        }

        for (
            int i = 0;
            i < pelletsPerShot;
            i++)
        {
            FireSinglePellet();
        }
    }

    private void FireSinglePellet()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PShotgun: Fire Point no está asignado."
            );

            return;
        }

        Vector3 shotDirection =
            playerCamera.transform.forward;

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
            playerCamera.transform.right *
            randomHorizontal;

        shotDirection +=
            playerCamera.transform.up *
            randomVertical;

        shotDirection.Normalize();

        // ============================
        // RAYCAST DESDE FIRE POINT
        // ============================

        Ray ray = new Ray(
            firePoint.position,
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

            PHealth health =
                hit.collider.GetComponentInParent<PHealth>();

            if (health != null)
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
                    $"Distancia: {hit.distance:F1}m | " +
                    $"Daño pellet: {finalDamage}"
                );

                health.RPC_TakeDamage(
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

        // ============================
        // ORIGEN DEL TRACER
        // ============================

        Vector3 tracerStart =
            firePoint.position;

        StartCoroutine(
            ShowTracer(
                tracerStart,
                tracerEndPoint
            )
        );
    }

    private float CalculateDamageFalloff(
        float distance)
    {
        if (distance <= fullDamageDistance)
        {
            return 1f;
        }

        if (distance >= minimumDamageDistance)
        {
            return minimumDamageMultiplier;
        }

        float t =
            Mathf.InverseLerp(
                fullDamageDistance,
                minimumDamageDistance,
                distance
            );

        return Mathf.Lerp(
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
            tracerObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;

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

        // -------------------------
        // MATERIAL DEL TRACER
        // -------------------------

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

        yield return new WaitForSeconds(
            tracerDuration
        );

        Destroy(
            tracerObject
        );
    }
}