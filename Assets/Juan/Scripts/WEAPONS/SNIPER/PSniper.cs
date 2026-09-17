using System.Collections;
using Fusion;
using UnityEngine;

public class PSniper : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;

    // ============================
    // SNIPER
    // ============================

    [Header("Sniper")]
    [SerializeField] private float range = 200f;
    [SerializeField] private int damage = 75;

    // ============================
    // CADENCIA
    // ============================

    [Header("Cadencia")]
    [SerializeField] private float timeBetweenShots = 1f;

    // ============================
    // RECARGA
    // ============================

    [Header("Recarga")]
    [SerializeField] private float reloadTime = 2.8f;

    // ============================
    // DISPERSIÓN
    // ============================

    [Header("Dispersión")]
    [SerializeField] private float spread = 0.002f;

    // ============================
    // TRACER
    // ============================

    [Header("Tracer")]
    [SerializeField] private Material tracerMaterial;
    [SerializeField] private Color tracerColor = Color.white;
    [SerializeField] private float tracerDuration = 0.35f;
    [SerializeField] private float tracerWidth = 0.02f;

    [Header("Origen visual del tracer")]
    [SerializeField] private float tracerForwardOffset = 0.5f;
    [SerializeField] private float tracerDownOffset = 0.15f;

    // ============================
    // REFERENCIAS INTERNAS
    // ============================

    private PWeaponInventory weaponInventory;

    private bool isReloading;
    private float nextShotTime;

    // ============================
    // DATOS PÚBLICOS PARA HUD
    // ============================

    public bool IsReloading => isReloading;

    public float TimeBetweenShots =>
        timeBetweenShots;

    public int Damage =>
        damage;

    public float Range =>
        range;

    // ============================
    // AWAKE
    // ============================

    private void Awake()
    {
        weaponInventory =
            GetComponent<PWeaponInventory>();
    }

    // ============================
    // UPDATE
    // ============================

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

        // Solo funciona si SNIPER
        // está seleccionada.
        if (!weaponInventory.IsSniperEquipped())
            return;

        // -------------------------
        // RECARGA MANUAL
        // -------------------------

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        // Mientras recarga
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

    // ============================
    // INTENTAR DISPARAR
    // ============================

    private void TryShoot()
    {
        // Sin balas cargadas.
        if (weaponInventory.SniperLoaded <= 0)
        {
            TryReload();
            return;
        }

        // Todavía estamos dentro
        // del cooldown entre tiros.
        if (Time.time < nextShotTime)
        {
            return;
        }

        // Marcamos cuándo se puede
        // volver a disparar.
        nextShotTime =
            Time.time +
            timeBetweenShots;

        // Consumimos una bala.
        weaponInventory.SniperLoaded--;

        Shoot();

        Debug.Log(
            $"SNIPER FIRE | " +
            $"Cargadas: {weaponInventory.SniperLoaded} | " +
            $"Reserva: {weaponInventory.SniperReserve}"
        );

        // Si vaciamos el cargador,
        // recarga automática.
        if (weaponInventory.SniperLoaded <= 0)
        {
            TryReload();
        }
    }

    // ============================
    // DISPARO
    // ============================

    private void Shoot()
    {
        if (playerCamera == null)
        {
            Debug.LogError(
                "PSniper: Player Camera no está asignada."
            );

            return;
        }

        // -------------------------
        // DIRECCIÓN BASE
        // -------------------------

        Vector3 shotDirection =
            playerCamera.transform.forward;

        // -------------------------
        // DISPERSIÓN
        // -------------------------

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

        // -------------------------
        // RAYCAST
        // -------------------------

        Ray ray = new Ray(
            playerCamera.transform.position,
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

            Debug.Log(
                $"SNIPER HIT: {hit.collider.name}"
            );

            PHealth health =
                hit.collider.GetComponentInParent<PHealth>();

            if (health != null)
            {
                health.RPC_TakeDamage(
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

        // -------------------------
        // ORIGEN VISUAL DEL TRACER
        // -------------------------

        Vector3 tracerStart =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            tracerForwardOffset -
            playerCamera.transform.up *
            tracerDownOffset;

        StartCoroutine(
            ShowTracer(
                tracerStart,
                tracerEndPoint
            )
        );
    }

    // ============================
    // RECARGA
    // ============================

    private void TryReload()
    {
        if (isReloading)
            return;

        // Cargador lleno.
        if (weaponInventory.SniperLoaded >=
            weaponInventory.SniperMagazineSize)
        {
            return;
        }

        // Sin reserva.
        if (weaponInventory.SniperReserve <= 0)
        {
            Debug.Log(
                "SNIPER SIN MUNICIÓN DE RESERVA"
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
            $"SNIPER RELOADING... | " +
            $"Cargadas: {weaponInventory.SniperLoaded} | " +
            $"Reserva: {weaponInventory.SniperReserve}"
        );

        yield return new WaitForSeconds(
            reloadTime
        );

        int bulletsNeeded =
            weaponInventory.SniperMagazineSize -
            weaponInventory.SniperLoaded;

        int bulletsToLoad =
            Mathf.Min(
                bulletsNeeded,
                weaponInventory.SniperReserve
            );

        weaponInventory.SniperLoaded +=
            bulletsToLoad;

        weaponInventory.SniperReserve -=
            bulletsToLoad;

        isReloading = false;

        Debug.Log(
            $"SNIPER RELOAD COMPLETE | " +
            $"Cargadas: {weaponInventory.SniperLoaded} | " +
            $"Reserva: {weaponInventory.SniperReserve}"
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
                "SniperTracer"
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
        // MATERIAL
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