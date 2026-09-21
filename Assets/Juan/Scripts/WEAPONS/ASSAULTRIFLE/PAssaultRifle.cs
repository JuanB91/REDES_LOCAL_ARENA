using System.Collections;
using Fusion;
using UnityEngine;

public class PAssaultRifle : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform firePoint;

    [Header("Assault Rifle")]
    [SerializeField] private float range = 100f;
    [SerializeField] private int damage = 11;

    [Header("Cadencia")]
    [SerializeField] private float shotsPerSecond = 8f;

    [Header("Recarga")]
    [SerializeField] private float reloadTime = 2.2f;

    [Header("Dispersión")]
    [SerializeField] private float spread = 0.02f;

    [Header("Tracer")]
    [SerializeField] private Color tracerColor = Color.cyan;
    [SerializeField] private float tracerDuration = 0.15f;
    [SerializeField] private float tracerWidth = 0.025f;

    [Header("Origen visual del tracer")]
    [SerializeField] private float tracerForwardOffset = 0.5f;
    [SerializeField] private float tracerDownOffset = 0.15f;

    [SerializeField] private ParticleSystem muzzleFlash;

    private PWeaponInventory weaponInventory;

    private bool isReloading;
    private float nextShotTime;

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

        // Solo funciona cuando
        // ASSAULT RIFLE está equipada.
        if (!weaponInventory.IsAssaultRifleEquipped())
            return;

        // -------------------------
        // RECARGA MANUAL
        // -------------------------

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        if (isReloading)
            return;

        // -------------------------
        // DISPARO AUTOMÁTICO
        // -------------------------

        if (Input.GetMouseButton(0))
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        // Sin balas cargadas.
        if (weaponInventory.AssaultRifleLoaded <= 0)
        {
            TryReload();
            return;
        }

        // Control de cadencia.
        if (Time.time < nextShotTime)
            return;

        float shotInterval =
            1f / shotsPerSecond;

        nextShotTime =
            Time.time +
            shotInterval;

        // Consumimos una bala.
        weaponInventory.AssaultRifleLoaded--;

        Shoot();

        Debug.Log(
            $"ASSAULT RIFLE FIRE | " +
            $"Cargadas: {weaponInventory.AssaultRifleLoaded} | " +
            $"Reserva: {weaponInventory.AssaultRifleReserve}"
        );

        // Recarga automática
        // cuando vaciamos el cargador.
        if (weaponInventory.AssaultRifleLoaded <= 0)
        {
            TryReload();
        }
    }

    private void TryReload()
    {
        if (isReloading)
            return;

        // Cargador lleno.
        if (weaponInventory.AssaultRifleLoaded >=
            weaponInventory.AssaultRifleMagazineSize)
        {
            return;
        }

        // No tenemos reserva.
        if (weaponInventory.AssaultRifleReserve <= 0)
        {
            Debug.Log(
                "ASSAULT RIFLE SIN MUNICIÓN DE RESERVA"
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
            $"ASSAULT RIFLE RELOADING... | " +
            $"Cargadas: {weaponInventory.AssaultRifleLoaded} | " +
            $"Reserva: {weaponInventory.AssaultRifleReserve}"
        );

        yield return new WaitForSeconds(
            reloadTime
        );

        // Cuántas balas faltan
        // para llenar el cargador.
        int bulletsNeeded =
            weaponInventory.AssaultRifleMagazineSize -
            weaponInventory.AssaultRifleLoaded;

        // Solo podemos cargar lo que
        // realmente tenemos en reserva.
        int bulletsToLoad =
            Mathf.Min(
                bulletsNeeded,
                weaponInventory.AssaultRifleReserve
            );

        weaponInventory.AssaultRifleLoaded +=
            bulletsToLoad;

        weaponInventory.AssaultRifleReserve -=
            bulletsToLoad;

        isReloading = false;

        Debug.Log(
            $"ASSAULT RIFLE RELOAD COMPLETE | " +
            $"Cargadas: {weaponInventory.AssaultRifleLoaded} | " +
            $"Reserva: {weaponInventory.AssaultRifleReserve}"
        );
    }

    private void Shoot()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        if (playerCamera == null)
        {
            Debug.LogError(
                "PAssaultRifle: Player Camera no está asignada."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "PAssaultRifle: Fire Point no está asignado."
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
        // RAYCAST DESDE FIRE POINT
        // -------------------------

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
        // ORIGEN DEL TRACER
        // -------------------------

        Vector3 tracerStart =
            firePoint.position;

        StartCoroutine(
            ShowTracer(
                tracerStart,
                tracerEndPoint
            )
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

        yield return new WaitForSeconds(
            tracerDuration
        );

        Destroy(
            tracerMaterial
        );

        Destroy(
            tracerObject
        );
    }
}