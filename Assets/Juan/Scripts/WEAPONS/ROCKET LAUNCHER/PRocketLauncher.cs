using System.Collections;
using Fusion;
using UnityEngine;

public class PRocketLauncher : NetworkBehaviour
{
    // ============================
    // REFERENCIAS
    // ============================

    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PWeaponInventory weaponInventory;

    [Header("Proyectil")]
    [SerializeField] private NetworkPrefabRef rocketPrefab;
    [SerializeField] private Transform rocketSpawnPoint;

    // ============================
    // DISPARO
    // ============================

    [Header("Disparo")]
    [SerializeField] private float timeBetweenShots = 0.8f;
    [SerializeField] private float spawnForwardOffset = 1f;

    // ============================
    // RECARGA
    // ============================

    [Header("Recarga")]
    [SerializeField] private float reloadTime = 1.5f;

    // ============================
    // DEBUG
    // ============================

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // ============================
    // ESTADO
    // ============================

    private float nextShotTime = 0f;
    private bool isReloading = false;

    private bool wasRocketLauncherEquipped = false;

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

        if (showDebug &&
            Object.HasStateAuthority)
        {
            Debug.Log(
                "ROCKET LAUNCHER INICIALIZADA"
            );
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

        bool rocketEquipped =
            weaponInventory.IsRocketLauncherEquipped();

        // ============================
        // ACABA DE EQUIPARSE
        // ============================

        if (rocketEquipped &&
            !wasRocketLauncherEquipped)
        {
            OnRocketLauncherEquipped();
        }

        wasRocketLauncherEquipped =
            rocketEquipped;

        if (!rocketEquipped)
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
        // DISPARO
        // ============================

        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }

    // ============================
    // AL EQUIPAR
    // ============================

    private void OnRocketLauncherEquipped()
    {
        if (showDebug)
        {
            Debug.Log(
                $"ROCKET EQUIPADA | " +
                $"Loaded: {weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: {weaponInventory.RocketLauncherReserve}"
            );
        }

        if (weaponInventory.RocketLauncherLoaded <= 0 &&
            weaponInventory.RocketLauncherReserve > 0)
        {
            TryReload();
        }
    }

    // ============================
    // INTENTAR DISPARAR
    // ============================

    private void TryFire()
    {
        if (Time.time < nextShotTime)
        {
            if (showDebug)
            {
                Debug.Log(
                    "ROCKET: todavía está en cooldown."
                );
            }

            return;
        }

        // ============================
        // SIN ROCKET CARGADO
        // ============================

        if (weaponInventory.RocketLauncherLoaded <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    $"ROCKET VACÍA | " +
                    $"Loaded: {weaponInventory.RocketLauncherLoaded} | " +
                    $"Reserve: {weaponInventory.RocketLauncherReserve}"
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
                "ROCKET LAUNCHER: Player Camera no está asignada."
            );

            return;
        }

        // ============================
        // VALIDAR PREFAB
        // ============================

        if (!rocketPrefab.IsValid)
        {
            Debug.LogError(
                "ROCKET LAUNCHER: Rocket Prefab no está asignado."
            );

            return;
        }

        // ============================
        // DISPARAR
        // ============================

        FireRocket();

        weaponInventory.RocketLauncherLoaded--;

        nextShotTime =
            Time.time +
            timeBetweenShots;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET DISPARADO | " +
                $"Loaded: {weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: {weaponInventory.RocketLauncherReserve}"
            );
        }

        // ============================
        // AUTO RELOAD
        // ============================

        if (weaponInventory.RocketLauncherLoaded <= 0 &&
            weaponInventory.RocketLauncherReserve > 0)
        {
            StartCoroutine(
                StartReloadNextFrame()
            );
        }
    }

    // ============================
    // SPAWN DEL COHETE
    // ============================

    private void FireRocket()
    {
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        // ============================
        // POSICIÓN DE SPAWN
        // ============================

        if (rocketSpawnPoint != null)
        {
            spawnPosition =
                rocketSpawnPoint.position;
        }
        else
        {
            spawnPosition =
                playerCamera.transform.position +
                playerCamera.transform.forward *
                spawnForwardOffset;
        }

        // ============================
        // ROTACIÓN
        // ============================

        spawnRotation =
            Quaternion.LookRotation(
                playerCamera.transform.forward,
                Vector3.up
            );

        // ============================
        // SPAWN EN RED
        // ============================

        NetworkObject spawnedRocket =
            Runner.Spawn(
                rocketPrefab,
                spawnPosition,
                spawnRotation,
                Object.InputAuthority
            );

        // ============================
        // PASAR SHOOTER AL PROYECTIL
        // ============================

        if (spawnedRocket != null)
        {
            RocketProjectile projectile =
                spawnedRocket.GetComponent<RocketProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(
                    Object
                );
            }
            else
            {
                Debug.LogError(
                    "ROCKET: El prefab no tiene RocketProjectile."
                );
            }

            if (showDebug)
            {
                Debug.Log(
                    $"ROCKET PREFAB CREADO | " +
                    $"Posición: {spawnPosition}"
                );
            }
        }
        else
        {
            Debug.LogError(
                "ROCKET: Runner.Spawn devolvió NULL."
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

        if (weaponInventory.RocketLauncherLoaded >=
            weaponInventory.RocketLauncherMagazineSize)
        {
            return;
        }

        if (weaponInventory.RocketLauncherReserve <= 0)
        {
            if (showDebug)
            {
                Debug.Log(
                    "ROCKET: sin munición de reserva."
                );
            }

            return;
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
                $"ROCKET RELOADING... | " +
                $"Loaded: {weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: {weaponInventory.RocketLauncherReserve}"
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

        int rocketsNeeded =
            weaponInventory.RocketLauncherMagazineSize -
            weaponInventory.RocketLauncherLoaded;

        int rocketsToLoad =
            Mathf.Min(
                rocketsNeeded,
                weaponInventory.RocketLauncherReserve
            );

        weaponInventory.RocketLauncherLoaded +=
            rocketsToLoad;

        weaponInventory.RocketLauncherReserve -=
            rocketsToLoad;

        isReloading = false;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET RECARGADA | " +
                $"Loaded: {weaponInventory.RocketLauncherLoaded} | " +
                $"Reserve: {weaponInventory.RocketLauncherReserve}"
            );
        }
    }
}