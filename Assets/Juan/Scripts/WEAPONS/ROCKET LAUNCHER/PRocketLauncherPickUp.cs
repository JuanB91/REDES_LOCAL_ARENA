using Fusion;
using UnityEngine;

public class RocketLauncherPickup : NetworkBehaviour
{
    [Header("Pickup")]
    [SerializeField] private int rocketAmmoAmount = 2;

    [Header("Respawn")]
    [SerializeField] private float respawnTime = 10f;

    [Header("Referencias")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Collider pickupCollider;

    [Networked]
    private NetworkBool IsAvailable { get; set; }

    [Networked]
    private TickTimer RespawnTimer { get; set; }

    // ============================
    // SPAWN
    // ============================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsAvailable = true;
            RespawnTimer = TickTimer.None;
        }

        UpdateVisualState();
    }

    // ============================
    // TRIGGER
    // ============================

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            $"ROCKET PICKUP detectó: {other.name}"
        );

        PWeaponInventory inventory =
            other.GetComponentInParent<PWeaponInventory>();

        if (inventory == null)
            return;

        // Solo el jugador que tiene autoridad
        // sobre su personaje puede recogerlo.
        if (!inventory.Object.HasStateAuthority)
            return;

        if (!IsAvailable)
            return;

        // ----------------------------
        // AGREGAR ROCKETS
        // ----------------------------

        inventory.AddRocketLauncher(
            rocketAmmoAmount
        );

        // ----------------------------
        // PEDIR DESACTIVACIÓN
        // ----------------------------

        RPC_RequestPickup();
    }

    // ============================
    // RPC PICKUP
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestPickup()
    {
        if (!IsAvailable)
            return;

        IsAvailable = false;

        RespawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnTime
            );

        Debug.Log(
            $"ROCKET LAUNCHER PICKUP RECOGIDO | " +
            $"Respawn en {respawnTime} segundos."
        );
    }

    // ============================
    // RESPawn
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!IsAvailable &&
            RespawnTimer.Expired(Runner))
        {
            IsAvailable = true;

            RespawnTimer =
                TickTimer.None;

            Debug.Log(
                "ROCKET LAUNCHER PICKUP DISPONIBLE NUEVAMENTE"
            );
        }
    }

    // ============================
    // VISUAL
    // ============================

    public override void Render()
    {
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        bool available =
            IsAvailable;

        if (visualObject != null)
        {
            visualObject.SetActive(
                available
            );
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled =
                available;
        }
    }
}