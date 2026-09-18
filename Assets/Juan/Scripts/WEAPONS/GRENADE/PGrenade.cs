using System.Collections;
using Fusion;
using UnityEngine;

public class PGrenade : NetworkBehaviour
{
    // ============================
    // REFERENCIAS
    // ============================

    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PWeaponInventory weaponInventory;

    [Header("Proyectil")]
    [SerializeField] private NetworkPrefabRef grenadePrefab;
    [SerializeField] private Transform grenadeSpawnPoint;

    // ============================
    // LANZAMIENTO
    // ============================

    [Header("Lanzamiento")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float upwardForce = 4f;

    [SerializeField] private float timeBetweenThrows = 0.8f;

    [SerializeField] private float spawnForwardOffset = 1f;

    // ============================
    // RECARGA
    // ============================

    [Header("Recarga")]
    [SerializeField] private float reloadTime = 1f;

    // ============================
    // DEBUG
    // ============================

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // ============================
    // ESTADO
    // ============================

    private float nextThrowTime = 0f;

    private bool isReloading = false;

    private bool wasGrenadeEquipped = false;

    public bool IsReloading =>
        isReloading;

    // ============================
    // SPAWN
    // ============================

    public override void Spawned()
    {
        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (playerCamera == null &&
            Object.HasStateAuthority)
        {
            playerCamera =
                GetComponentInChildren<Camera>();
        }

    }

    // ============================
    // UPDATE
    // ============================

    private void Update()
    {
        if (Object == null)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (weaponInventory == null)
            return;

        bool grenadeEquipped =
            weaponInventory.IsGrenadeEquipped();

        // ============================
        // ACABA DE EQUIPARSE
        // ============================

        if (grenadeEquipped &&
            !wasGrenadeEquipped)
        {
            OnGrenadeEquipped();
        }

        wasGrenadeEquipped =
            grenadeEquipped;

        if (!grenadeEquipped)
            return;

        // ============================
        // RECARGANDO
        // ============================

        if (isReloading)
            return;

        // ============================
        // RECARGA MANUAL
        // ============================

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
            return;
        }

        // ============================
        // LANZAMIENTO
        // ============================

        if (Input.GetMouseButtonDown(0))
        {
            TryThrowGrenade();
        }
    }

    // ============================
    // AL EQUIPAR
    // ============================

    private void OnGrenadeEquipped()
    {
      

        if (weaponInventory.GrenadeLoaded <= 0 &&
            weaponInventory.GrenadeReserve > 0)
        {
            TryReload();
        }
    }

    // ============================
    // INTENTAR LANZAR
    // ============================

    private void TryThrowGrenade()
    {
        if (Time.time < nextThrowTime)
        {
            if (showDebug)
            {
                Debug.Log(
                    "GRENADE: todavía está en cooldown."
                );
            }

            return;
        }

        // ============================
        // SIN GRANADA CARGADA
        // ============================

        if (weaponInventory.GrenadeLoaded <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    $"GRENADE VACÍA | " +
                    $"Loaded: {weaponInventory.GrenadeLoaded} | " +
                    $"Reserve: {weaponInventory.GrenadeReserve}"
                );
            }

            TryReload();
            return;
        }

        // ============================
        // VALIDAR CÁMARA
        // ============================

        if (playerCamera == null)
        {
            Debug.LogError(
                "GRENADE: Player Camera no está asignada."
            );

            return;
        }

        // ============================
        // VALIDAR PREFAB
        // ============================

        if (!grenadePrefab.IsValid)
        {
            Debug.LogError(
                "GRENADE: Grenade Prefab no está asignado."
            );

            return;
        }

        // ============================
        // LANZAR
        // ============================

        ThrowGrenade();

        weaponInventory.GrenadeLoaded--;

        nextThrowTime =
            Time.time +
            timeBetweenThrows;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE LANZADA | " +
                $"Loaded: {weaponInventory.GrenadeLoaded} | " +
                $"Reserve: {weaponInventory.GrenadeReserve}"
            );
        }

        // ============================
        // AUTO RELOAD
        // ============================

        if (weaponInventory.GrenadeLoaded <= 0 &&
            weaponInventory.GrenadeReserve > 0)
        {
            StartCoroutine(
                StartReloadNextFrame()
            );
        }
    }

    // ============================
    // CREAR Y LANZAR GRANADA
    // ============================

    private void ThrowGrenade()
    {
        Vector3 spawnPosition;

        // ============================
        // POSICIÓN
        // ============================

        if (grenadeSpawnPoint != null)
        {
            spawnPosition =
                grenadeSpawnPoint.position;
        }
        else
        {
            spawnPosition =
                playerCamera.transform.position +
                playerCamera.transform.forward *
                spawnForwardOffset;
        }

        // ============================
        // DIRECCIÓN
        // ============================

        Vector3 forwardDirection =
            playerCamera.transform.forward.normalized;

        Vector3 launchVelocity =
            forwardDirection *
            throwForce;

        launchVelocity +=
            Vector3.up *
            upwardForce;

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                forwardDirection,
                Vector3.up
            );

        // ============================
        // SPAWN EN RED
        // ============================

        NetworkObject spawnedGrenade =
            Runner.Spawn(
                grenadePrefab,
                spawnPosition,
                spawnRotation,
                Object.InputAuthority
            );

        // ============================
        // INICIALIZAR PROYECTIL
        // ============================

        if (spawnedGrenade != null)
        {
            GrenadeProjectile grenade =
                spawnedGrenade.GetComponent<GrenadeProjectile>();

            if (grenade != null)
            {
                grenade.Initialize(
                    Object,
                    launchVelocity
                );
            }
            else
            {
                Debug.LogError(
                    "GRENADE: El prefab no tiene GrenadeProjectile."
                );
            }

            if (showDebug)
            {
                Debug.Log(
                    $"GRENADE PREFAB CREADO | " +
                    $"Posición: {spawnPosition} | " +
                    $"Velocidad: {launchVelocity}"
                );
            }
        }
        else
        {
            Debug.LogError(
                "GRENADE: Runner.Spawn devolvió NULL."
            );
        }
    }

    // ============================
    // AUTO RELOAD
    // ============================

    private IEnumerator StartReloadNextFrame()
    {
        yield return null;

        TryReload();
    }

    // ============================
    // INTENTAR RECARGAR
    // ============================

    private void TryReload()
    {
        if (isReloading)
            return;

        if (weaponInventory.GrenadeLoaded >=
            weaponInventory.GrenadeMagazineSize)
        {
            return;
        }

        if (weaponInventory.GrenadeReserve <= 0)
        {
            
        }

        StartCoroutine(
            ReloadCoroutine()
        );
    }

    // ============================
    // RECARGA
    // ============================

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE RELOADING... | " +
                $"Loaded: {weaponInventory.GrenadeLoaded} | " +
                $"Reserve: {weaponInventory.GrenadeReserve}"
            );
        }

        yield return new WaitForSeconds(
            reloadTime
        );

        if (weaponInventory == null)
        {
            isReloading = false;
            yield break;
        }

        int grenadesNeeded =
            weaponInventory.GrenadeMagazineSize -
            weaponInventory.GrenadeLoaded;

        int grenadesToLoad =
            Mathf.Min(
                grenadesNeeded,
                weaponInventory.GrenadeReserve
            );

        weaponInventory.GrenadeLoaded +=
            grenadesToLoad;

        weaponInventory.GrenadeReserve -=
            grenadesToLoad;

        isReloading = false;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE RECARGADA | " +
                $"Loaded: {weaponInventory.GrenadeLoaded} | " +
                $"Reserve: {weaponInventory.GrenadeReserve}"
            );
        }
    }
}