using Fusion;
using UnityEngine;

public class ShotgunPickup : NetworkBehaviour
{
    [Header("Pickup")]
    [SerializeField] private int shotgunAmmoAmount = 6;

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
        // Solo la autoridad del pickup inicializa el estado.
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
            $"SHOTGUN TRIGGER detectó: {other.name}"
        );

        // Buscamos el inventario del jugador.
        PWeaponInventory inventory =
            other.GetComponentInParent<PWeaponInventory>();

        if (inventory == null)
        {
            Debug.Log(
                "El objeto que entró no tiene PWeaponInventory."
            );

            return;
        }

        // IMPORTANTE:
        // solo el dueño local de ese Player puede recogerla.
        if (!inventory.Object.HasStateAuthority)
        {
            return;
        }

        // Si el pickup ya está desactivado, no hacemos nada.
        if (!IsAvailable)
        {
            return;
        }

        Debug.Log(
            "PLAYER LOCAL ENTRÓ AL PICKUP DE ESCOPETA"
        );

        // Damos la escopeta / munición al jugador local.
        inventory.AddShotgun(
            shotgunAmmoAmount
        );

        // Pedimos a la autoridad del pickup que lo desactive.
        RPC_RequestPickup();
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestPickup()
    {
        // Por seguridad, comprobamos otra vez.
        if (!IsAvailable)
            return;

        IsAvailable = false;

        RespawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnTime
            );

        Debug.Log(
            $"SHOTGUN RECOGIDA. " +
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
            RespawnTimer = TickTimer.None;

            Debug.Log(
                "SHOTGUN PICKUP DISPONIBLE NUEVAMENTE"
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