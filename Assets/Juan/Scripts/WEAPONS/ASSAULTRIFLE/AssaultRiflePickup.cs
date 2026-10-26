using Fusion;
using UnityEngine;

public class AssaultRiflePickup : NetworkBehaviour
{

    [Header("Pickup")]
    [SerializeField]
    private int assaultRifleAmmoAmount = 30;


    [Header("Respawn")]
    [SerializeField]
    private float respawnTime = 10f;


    [Header("Referencias")]
    [SerializeField]
    private GameObject visualObject;

    [SerializeField]
    private Collider pickupCollider;


    [Networked]
    private NetworkBool IsAvailable { get; set; }

    [Networked]
    private TickTimer RespawnTimer { get; set; }


    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsAvailable =
                true;

            RespawnTimer =
                TickTimer.None;
        }

        UpdateVisualState();

        Debug.Log(
            $"ASSAULT RIFLE PICKUP READY | " +
            $"STATE AUTHORITY: " +
            $"{Object.HasStateAuthority}"
        );
    }


    private void OnTriggerEnter(
        Collider other)
    {

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;


        if (!IsAvailable)
            return;


        PWeaponInventory inventory =
            other
                .GetComponentInParent<
                    PWeaponInventory
                >();

        if (inventory == null)
            return;

        if (inventory.Object == null)
            return;

        if (!inventory.Object.IsValid)
            return;

        if (!inventory.Object
            .HasStateAuthority)
        {
            return;
        }

        Debug.Log(
            $"ASSAULT RIFLE PICKUP DETECTED | " +
            $"Player: " +
            $"{inventory.Object.InputAuthority.PlayerId}"
        );


        inventory.AddAssaultRifle(
            assaultRifleAmmoAmount
        );


        ConsumePickup();
    }


    private void ConsumePickup()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!IsAvailable)
            return;

        IsAvailable =
            false;

        RespawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnTime
            );

        Debug.Log(
            $"ASSAULT RIFLE PICKUP CONSUMED | " +
            $"RESPAWN IN: {respawnTime} SECONDS"
        );
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsAvailable)
            return;

        if (!RespawnTimer.IsRunning)
            return;

        if (!RespawnTimer.Expired(
            Runner))
        {
            return;
        }

        IsAvailable =
            true;

        RespawnTimer =
            TickTimer.None;

        Debug.Log(
            "ASSAULT RIFLE PICKUP RESPAWNED"
        );
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
            if (visualObject.activeSelf !=
                available)
            {
                visualObject.SetActive(
                    available
                );
            }
        }


        if (pickupCollider != null)
        {
            if (pickupCollider.enabled !=
                available)
            {
                pickupCollider.enabled =
                    available;
            }
        }
    }
}