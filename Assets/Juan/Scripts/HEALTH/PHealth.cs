using System.Collections;
using Fusion;
using UnityEngine;

public class PHealth : NetworkBehaviour
{
    // ============================
    // CONFIG
    // ============================

    [Header("Health")]
    [SerializeField]
    private int maxHealth = 100;

    [Header("Respawn")]
    [SerializeField]
    private float respawnDelay = 3f;

    [Header("Visual")]
    [SerializeField]
    private GameObject bodyObject;

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
    private TeamSpawnManager teamSpawnManager;

    private PTeam team;

    private PMovement movement;
    private PLook look;

    private NetworkTransform networkTransform;

    private PWeaponInventory weaponInventory;
    private PShooting pistolShooting;

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
            FindFirstObjectByType<
                NetworkGameManager
            >();

        teamSpawnManager =
            FindFirstObjectByType<
                TeamSpawnManager
            >();

        team =
            GetComponent<PTeam>();

        movement =
            GetComponent<PMovement>();

        look =
            GetComponent<PLook>();

        networkTransform =
            GetComponent<NetworkTransform>();

        weaponInventory =
            GetComponent<PWeaponInventory>();

        pistolShooting =
            GetComponent<PShooting>();

        if (Object.HasStateAuthority)
        {
            Health =
                maxHealth;

            Kills =
                0;

            IsDead =
                false;
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

        // ============================
        // FRIENDLY FIRE CHECK
        // ============================

        if (attacker != null &&
            attacker != Object)
        {
            PTeam attackerTeam =
                attacker.GetComponent<PTeam>();

            if (team == null)
            {
                team =
                    GetComponent<PTeam>();
            }

            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<
                        NetworkGameManager
                    >();
            }

            if (attackerTeam != null &&
                team != null &&
                gameManager != null)
            {
                bool sameTeam =
                    attackerTeam.CurrentTeam ==
                    team.CurrentTeam;

                if (sameTeam &&
                    !gameManager.FriendlyFireEnabled)
                {
                    Debug.Log(
                        $"FRIENDLY FIRE BLOCKED | " +
                        $"Attacker: " +
                        $"{attacker.InputAuthority.PlayerId} | " +
                        $"Victim: " +
                        $"{Object.InputAuthority.PlayerId} | " +
                        $"Team: {team.CurrentTeam}"
                    );

                    return;
                }
            }
        }

        // ============================
        // APPLY DAMAGE
        // ============================

        Health -=
            damage;

        if (Health < 0)
        {
            Health =
                0;
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

        IsDead =
            true;

        Health =
            0;

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} DIED"
        );

        // ============================
        // GIVE KILL
        // ============================

        if (attacker != null)
        {
            PHealth attackerHealth =
                attacker.GetComponent<PHealth>();

            if (attackerHealth != null &&
                attackerHealth != this)
            {
                attackerHealth.RPC_AddKill();
            }
        }

        // ============================
        // DISABLE PLAYER
        // ============================

        SetControls(
            false
        );

        UpdateBodyState();

        // ============================
        // MOVE WHILE DEAD
        // ============================

        MoveDeadPlayerToSpawn();

        // ============================
        // WAIT FOR RESPAWN
        // ============================

        StartCoroutine(
            RespawnCoroutine()
        );
    }

    // ============================
    // ADD KILL
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
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

        if (gameManager != null)
        {
            gameManager.CheckVictory(
                this
            );
        }
    }

    // ============================
    // MOVE DEAD PLAYER TO SPAWN
    // ============================

    private void MoveDeadPlayerToSpawn()
    {
        if (!Object.HasStateAuthority)
            return;

        if (teamSpawnManager == null)
        {
            teamSpawnManager =
                FindFirstObjectByType<
                    TeamSpawnManager
                >();
        }

        if (team == null)
        {
            team =
                GetComponent<PTeam>();
        }

        if (teamSpawnManager == null)
        {
            Debug.LogError(
                "PHEALTH: TeamSpawnManager not found."
            );

            return;
        }

        if (team == null)
        {
            Debug.LogError(
                "PHEALTH: PTeam not found."
            );

            return;
        }

        Transform spawnPoint =
            teamSpawnManager
                .GetRandomSpawnPoint(
                    team.CurrentTeam
                );

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"PHEALTH: No respawn found | " +
                $"Player: {Object.InputAuthority.PlayerId} | " +
                $"Team: {team.CurrentTeam}"
            );

            return;
        }

        MoveToSpawn(
            spawnPoint
        );

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} " +
            $"MOVED TO RESPAWN WHILE DEAD | " +
            $"TEAM: {team.CurrentTeam} | " +
            $"SPAWN: {spawnPoint.name}"
        );
    }

    // ============================
    // RESPAWN TIMER
    // ============================

    private IEnumerator RespawnCoroutine()
    {
        yield return
            new WaitForSeconds(
                respawnDelay
            );

        if (!Object.HasStateAuthority)
            yield break;

        Health =
            maxHealth;

        IsDead =
            false;

        SetControls(
            true
        );

        UpdateBodyState();

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} RESPAWNED | " +
            $"TEAM: {team.CurrentTeam}"
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
            controller.enabled =
                false;
        }

        Quaternion rotation =
            spawnPoint.rotation;

        if (teamSpawnManager != null &&
            teamSpawnManager.LookTarget != null)
        {
            Vector3 direction =
                teamSpawnManager
                    .LookTarget
                    .position -
                spawnPoint.position;

            direction.y =
                0f;

            if (direction.sqrMagnitude >
                0.001f)
            {
                rotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
            }
        }

        if (networkTransform == null)
        {
            networkTransform =
                GetComponent<NetworkTransform>();
        }

        if (networkTransform != null)
        {
            networkTransform.Teleport(
                spawnPoint.position,
                rotation
            );
        }
        else
        {
            transform.position =
                spawnPoint.position;

            transform.rotation =
                rotation;
        }

        if (controller != null)
        {
            controller.enabled =
                true;
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

        if (teamSpawnManager == null)
        {
            teamSpawnManager =
                FindFirstObjectByType<
                    TeamSpawnManager
                >();
        }

        if (team == null)
        {
            team =
                GetComponent<PTeam>();
        }

        if (weaponInventory == null)
        {
            weaponInventory =
                GetComponent<PWeaponInventory>();
        }

        if (pistolShooting == null)
        {
            pistolShooting =
                GetComponent<PShooting>();
        }

        Health =
            maxHealth;

        Kills =
            0;

        IsDead =
            true;

        // ============================
        // RESET INVENTORY
        // ============================

        if (weaponInventory != null)
        {
            weaponInventory.ResetInventory();
        }

        // ============================
        // RESET PISTOL
        // ============================

        if (pistolShooting != null)
        {
            pistolShooting.ResetPistolAmmo();
        }

        UpdateBodyState();

        // ============================
        // RESET POSITION
        // ============================

        if (teamSpawnManager != null &&
            team != null)
        {
            Transform spawnPoint =
                teamSpawnManager
                    .GetRandomSpawnPoint(
                        team.CurrentTeam
                    );

            if (spawnPoint != null)
            {
                MoveToSpawn(
                    spawnPoint
                );
            }
        }

        IsDead =
            false;

        SetControls(
            true
        );

        UpdateBodyState();

        Debug.Log(
            $"PLAYER {Object.InputAuthority.PlayerId} RESET"
        );
    }
}