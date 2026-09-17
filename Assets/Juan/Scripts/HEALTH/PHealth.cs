using System.Collections;
using Fusion;
using UnityEngine;

public class PHealth : NetworkBehaviour
{
    // ============================
    // CONFIG
    // ============================

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 3f;

    [Header("Visual")]
    [SerializeField] private GameObject bodyObject;

    // ============================
    // NETWORK
    // ============================

    [Networked]
    public int Health { get; set; }

    [Networked]
    public int Kills { get; set; }

    [Networked]
    public NetworkBool IsDead { get; set; }

    // ============================
    // REFERENCES
    // ============================

    private NetworkGameManager gameManager;
    private PMovement movement;
    private PLook look;

    // ============================
    // PUBLIC
    // ============================

    public int MaxHealth =>
        maxHealth;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        movement =
            GetComponent<PMovement>();

        look =
            GetComponent<PLook>();

        if (Object.HasStateAuthority)
        {
            Health = maxHealth;
            Kills = 0;
            IsDead = false;
        }

        UpdateBodyState();
    }

    // ============================
    // DAMAGE
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_TakeDamage(
        int damage,
        NetworkObject attacker)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;

        Health -= damage;

        if (Health < 0)
        {
            Health = 0;
        }

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} DAMAGE | " +
            $"Damage: {damage} | " +
            $"Health: {Health}/{maxHealth}"
        );

        if (Health <= 0)
        {
            Die(
                attacker
            );
        }
    }

    // ============================
    // DEATH
    // ============================

    private void Die(
        NetworkObject attacker)
    {
        if (IsDead)
            return;

        IsDead = true;
        Health = 0;

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} DIED"
        );

        // ============================
        // GIVE KILL TO ATTACKER
        // ============================

        if (attacker != null)
        {
            PHealth attackerHealth =
                attacker.GetComponent<PHealth>();

            if (attackerHealth != null &&
                attackerHealth != this)
            {
                // IMPORTANT:
                // The victim cannot directly modify
                // the attacker's Networked Kills.
                //
                // Send an RPC to the attacker's
                // StateAuthority instead.
                attackerHealth.RPC_AddKill();
            }
        }

        SetControls(
            false
        );

        UpdateBodyState();

        StartCoroutine(
            RespawnCoroutine()
        );
    }

    // ============================
    // ADD KILL RPC
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_AddKill()
    {
        if (!Object.HasStateAuthority)
            return;

        Kills++;

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} KILL | " +
            $"TOTAL: {Kills}"
        );

        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<NetworkGameManager>();
        }

        if (gameManager != null)
        {
            gameManager.CheckVictory(
                this
            );
        }
    }

    // ============================
    // RESPAWN TIMER
    // ============================

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(
            respawnDelay
        );

        if (!Object.HasStateAuthority)
            yield break;

        RespawnPlayer();
    }

    // ============================
    // RESPAWN
    // ============================

    private void RespawnPlayer()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<NetworkGameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError(
                "PHEALTH: NetworkGameManager not found."
            );

            return;
        }

        Transform spawnPoint =
            gameManager.GetSpawnPoint(
                Object.InputAuthority
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"PHEALTH: Spawn not found for Player " +
                $"{Object.InputAuthority.PlayerId}"
            );

            return;
        }

        MoveToSpawn(
            spawnPoint
        );

        Health = maxHealth;
        IsDead = false;

        SetControls(
            true
        );

        UpdateBodyState();

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} RESPAWNED"
        );
    }

    // ============================
    // MOVE TO SPAWN
    // ============================

    private void MoveToSpawn(
        Transform spawnPoint)
    {
        CharacterController controller =
            GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position =
            spawnPoint.position;

        Quaternion rotation =
            spawnPoint.rotation;

        if (gameManager != null &&
            gameManager.LookTarget != null)
        {
            Vector3 direction =
                gameManager.LookTarget.position -
                spawnPoint.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                rotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
            }
        }

        transform.rotation =
            rotation;

        if (controller != null)
        {
            controller.enabled = true;
        }

        if (look != null)
        {
            look.SyncHorizontalRotation();
        }
    }

    // ============================
    // CONTROLS
    // ============================

    private void SetControls(
        bool state)
    {
        if (movement != null)
        {
            movement.enabled =
                state;
        }

        if (look != null)
        {
            look.enabled =
                state;
        }
    }

    // ============================
    // BODY
    // ============================

    private void UpdateBodyState()
    {
        if (bodyObject != null)
        {
            bodyObject.SetActive(
                !IsDead
            );
        }
    }

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        UpdateBodyState();
    }

    // ============================
    // RESET PLAYER
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_ResetPlayer()
    {
        if (!Object.HasStateAuthority)
            return;

        StopAllCoroutines();

        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<NetworkGameManager>();
        }

        Health = maxHealth;
        Kills = 0;
        IsDead = false;

        if (gameManager != null)
        {
            Transform spawnPoint =
                gameManager.GetSpawnPoint(
                    Object.InputAuthority
                );

            if (spawnPoint != null)
            {
                MoveToSpawn(
                    spawnPoint
                );
            }
        }

        SetControls(
            true
        );

        UpdateBodyState();

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} RESET"
        );
    }
}