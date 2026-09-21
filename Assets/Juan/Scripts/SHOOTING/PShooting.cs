using System.Collections;
using Fusion;
using UnityEngine;

public class PShooting : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;

    [Header("Arma")]
    [SerializeField] private string weaponName = "PISTOL";

    [Header("Disparo")]
    [SerializeField] private float range = 100f;
    [SerializeField] private int damage = 25;

    [Header("Munición")]
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private float reloadTime = 2f;

    [Header("Dispersión")]
    [SerializeField] private float spread = 0.03f;

    [Header("Tracer")]
    [SerializeField] private Color tracerColor = Color.yellow;
    [SerializeField] private float tracerDuration = 0.5f;
    [SerializeField] private float tracerWidth = 0.06f;

    [Header("Origen visual del tracer")]
    [SerializeField] private float tracerForwardOffset = 0.5f;
    [SerializeField] private float tracerDownOffset = 0.15f;

    [SerializeField] private Transform firePoint;

    private int currentAmmo;
    private bool isReloading;

    private PWeaponInventory weaponInventory;

    // ============================
    // DATOS PUBLICOS PARA HUD
    // ============================

    public string WeaponName => weaponName;

    public int CurrentAmmo =>
        currentAmmo;

    public int MagazineSize =>
        magazineSize;

    public bool IsReloading =>
        isReloading;

    // ============================
    // AWAKE
    // ============================

    private void Awake()
    {
        weaponInventory =
            GetComponent<PWeaponInventory>();
    }

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            currentAmmo =
                magazineSize;

            isReloading =
                false;
        }
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

        // ============================
        // SOLO SI PISTOLA EQUIPADA
        // ============================

        if (weaponInventory != null &&
            !weaponInventory.IsPistolEquipped())
        {
            return;
        }

        // ============================
        // RECARGA MANUAL
        // ============================

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        if (isReloading)
            return;

        // ============================
        // DISPARO
        // ============================

        if (Input.GetMouseButtonDown(0))
        {
            TryShoot();
        }
    }

    // ============================
    // TRY SHOOT
    // ============================

    private void TryShoot()
    {
        if (currentAmmo <= 0)
        {
            TryReload();

            return;
        }

        currentAmmo--;

        Shoot();

        Debug.Log(
            $"DISPARO | Balas: {currentAmmo}/{magazineSize}"
        );

        // Recarga automática al vaciar
        if (currentAmmo <= 0)
        {
            TryReload();
        }
    }

    // ============================
    // TRY RELOAD
    // ============================

    private void TryReload()
    {
        if (isReloading)
            return;

        if (currentAmmo >= magazineSize)
            return;

        StartCoroutine(
            ReloadRoutine()
        );
    }

    // ============================
    // SHOOT
    // ============================

    private void Shoot()
    {
        if (playerCamera == null)
        {
            Debug.LogError(
                "PShooting: Player Camera no está asignada."
            );

            return;
        }

        // ============================
        // DIRECCION BASE
        // ============================

        Vector3 shotDirection =
            playerCamera.transform.forward;

        // ============================
        // DISPERSION
        // ============================

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
        // RAYCAST
        // ============================

        Ray ray =
            new Ray(
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
                $"Disparo impactó en: {hit.collider.name}"
            );

            PHealth health =
                hit.collider
                    .GetComponentInParent<PHealth>();

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

            Debug.Log(
                "Disparo no impactó en nada"
            );
        }

        // ============================
        // ORIGEN DEL TRACER
        // ============================

        Vector3 tracerStart =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            tracerForwardOffset -
            playerCamera.transform.up *
            tracerDownOffset;

        // ============================
        // TRACER
        // ============================

        StartCoroutine(
            ShowTracer(
                tracerStart,
                tracerEndPoint
            )
        );
    }

    // ============================
    // SHOW TRACER
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
                .AddComponent<LineRenderer>();

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

    // ============================
    // RESET PISTOL
    // ============================

    public void ResetPistolAmmo()
    {
        if (Object == null ||
            !Object.HasStateAuthority)
        {
            return;
        }

        // Cancela una recarga que haya quedado
        // corriendo al terminar la partida.
        StopAllCoroutines();

        currentAmmo =
            magazineSize;

        isReloading =
            false;

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} " +
            $"PISTOL RESET | " +
            $"Balas: {currentAmmo}/{magazineSize}"
        );
    }

    // ============================
    // RELOAD ROUTINE
    // ============================

    private IEnumerator ReloadRoutine()
    {
        if (isReloading)
            yield break;

        isReloading =
            true;

        Debug.Log(
            $"RECARGANDO... | " +
            $"Balas actuales: {currentAmmo}/{magazineSize}"
        );

        yield return new WaitForSeconds(
            reloadTime
        );

        currentAmmo =
            magazineSize;

        isReloading =
            false;

        Debug.Log(
            $"RECARGA COMPLETA | " +
            $"Balas: {currentAmmo}/{magazineSize}"
        );
    }
}