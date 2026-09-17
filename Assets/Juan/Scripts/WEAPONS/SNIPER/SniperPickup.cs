using Fusion;
using UnityEngine;

public class SniperPickup : NetworkBehaviour
{
    [Header("Pickup")]
    [SerializeField] private int sniperAmmoAmount = 5;

    [Header("Respawn")]
    [SerializeField] private float respawnTime = 10f;

    [Header("Referencias")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Collider pickupCollider;

    [Networked]
    private NetworkBool IsAvailable { get; set; }

    [Networked]
    private TickTimer RespawnTimer { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsAvailable = true;
            RespawnTimer = TickTimer.None;
        }

        UpdateVisualState();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            $"SNIPER TRIGGER detectó: {other.name}"
        );

        PWeaponInventory inventory =
            other.GetComponentInParent<PWeaponInventory>();

        if (inventory == null)
            return;

        // Solo el jugador local recoge su propio pickup.
        if (!inventory.Object.HasStateAuthority)
            return;

        if (!IsAvailable)
            return;

        inventory.AddSniper(
            sniperAmmoAmount
        );

        RPC_RequestPickup();
    }

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
            $"SNIPER RECOGIDA | " +
            $"Respawn en {respawnTime} segundos."
        );
    }

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
                "SNIPER PICKUP DISPONIBLE NUEVAMENTE"
            );
        }
    }

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