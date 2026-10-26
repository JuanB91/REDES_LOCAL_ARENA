using System.Collections;
using Fusion;
using UnityEngine;

public class PHealth : NetworkBehaviour
{
   
    [Header("Health")]
    [SerializeField]
    private int maxHealth = 100;

    [Header("Respawn")]
    [SerializeField]
    private float respawnDelay = 3f;

    [Header("Visual")]
    [SerializeField]
    private GameObject bodyObject;

    
    [Networked]
    public int Health { get; set; }

    [Networked]
    public int Kills { get; set; }

    [Networked]
    public NetworkBool IsDead { get; set; }

    
    private NetworkGameManager gameManager;

    private TeamSpawnManager teamSpawnManager;

    private PTeam team;

    private NetworkTransform networkTransform;

    private PWeaponInventory weaponInventory;

    private PShooting pistolShooting;

    
    private Renderer[] bodyRenderers;

 
    public int MaxHealth =>
        maxHealth;


    public override void Spawned()
    {
        CacheReferences();

        CacheBodyRenderers();

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

        Debug.Log(
            $"PHEALTH READY | " +
            $"PLAYER: {Object.InputAuthority.PlayerId} | " +
            $"STATE AUTHORITY: {Object.HasStateAuthority} | " +
            $"INPUT AUTHORITY: {Object.HasInputAuthority} | " +
            $"HEALTH: {Health} | " +
            $"DEAD: {IsDead}"
        );
    }


    private void CacheReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

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

        if (networkTransform == null)
        {
            networkTransform =
                GetComponent<NetworkTransform>();
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
    }


    private void CacheBodyRenderers()
    {
        if (bodyObject == null)
        {
            bodyRenderers =
                null;

            return;
        }

        bodyRenderers =
            bodyObject
                .GetComponentsInChildren<
                    Renderer
                >(
                    true
                );
    }


    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_TakeDamage(
        int damage,
        NetworkObject attacker)
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            $"DAMAGE REQUEST | " +
            $"Victim: {Object.InputAuthority.PlayerId} | " +
            $"Damage: {damage} | " +
            $"Health before: {Health} | " +
            $"IsDead: {IsDead}"
        );

        if (IsDead)
        {
            Debug.Log(
                $"DAMAGE IGNORED - PLAYER DEAD | " +
                $"Victim: " +
                $"{Object.InputAuthority.PlayerId}"
            );

            return;
        }

        if (damage <= 0)
            return;

        CacheReferences();


        if (attacker != null &&
            attacker != Object)
        {
            PTeam attackerTeam =
                attacker.GetComponent<PTeam>();

            if (attackerTeam != null &&
                team != null &&
                gameManager != null)
            {
                bool sameTeam =
                    attackerTeam.CurrentTeam ==
                    team.CurrentTeam;

                if (sameTeam &&
                    !gameManager
                        .FriendlyFireEnabled)
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


        Health -=
            damage;

        Health =
            Mathf.Max(
                Health,
                0
            );

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} DAMAGE | " +
            $"Damage: {damage} | " +
            $"Health: {Health}/{maxHealth} | " +
            $"Dead: {IsDead}"
        );

        if (Health <= 0)
        {
            Die(
                attacker
            );
        }
    }


    private void Die(
        NetworkObject attacker)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        IsDead =
            true;

        Health =
            0;

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} DIED"
        );


        if (attacker != null &&
            attacker != Object)
        {
            PHealth attackerHealth =
                attacker.GetComponent<PHealth>();

            if (attackerHealth != null)
            {
                attackerHealth
                    .AddKillAuthoritative();
            }
        }


        UpdateBodyState();


        MoveDeadPlayerToSpawn();


        StartCoroutine(
            RespawnCoroutine()
        );
    }


    private void AddKillAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        Kills++;

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} KILL | " +
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


    private void MoveDeadPlayerToSpawn()
    {
        if (!Object.HasStateAuthority)
            return;

        CacheReferences();

        if (teamSpawnManager == null)
        {
            Debug.LogError(
                "PHEALTH: " +
                "TeamSpawnManager not found."
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
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Team: {team.CurrentTeam}"
            );

            return;
        }

        MoveToSpawn(
            spawnPoint
        );

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} " +
            $"MOVED TO RESPAWN WHILE DEAD | " +
            $"TEAM: {team.CurrentTeam} | " +
            $"SPAWN: {spawnPoint.name}"
        );
    }


    private IEnumerator RespawnCoroutine()
    {
        yield return
            new WaitForSeconds(
                respawnDelay
            );

        if (Object == null)
            yield break;

        if (!Object.IsValid)
            yield break;

        if (!Object.HasStateAuthority)
            yield break;


        Health =
            maxHealth;

        IsDead =
            false;

        UpdateBodyState();

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} RESPAWNED | " +
            $"Health: {Health}/{maxHealth} | " +
            $"IsDead: {IsDead}"
        );
    }


    private void MoveToSpawn(
        Transform spawnPoint)
    {
        if (!Object.HasStateAuthority)
            return;

        if (spawnPoint == null)
            return;

        CharacterController controller =
            GetComponent<CharacterController>();

        bool controllerWasEnabled =
            controller != null &&
            controller.enabled;

        if (controllerWasEnabled)
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


        transform.SetPositionAndRotation(
            spawnPoint.position,
            rotation
        );


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


        if (controllerWasEnabled)
        {
            controller.enabled =
                true;
        }


        PLook look =
            GetComponent<PLook>();

        if (look != null &&
            Object.HasInputAuthority)
        {
            look.SyncHorizontalRotation();
        }
    }


    private void UpdateBodyState()
    {
        if (bodyObject == null)
            return;

        if (bodyRenderers == null ||
            bodyRenderers.Length == 0)
        {
            CacheBodyRenderers();
        }

        bool shouldBeVisible =
            !IsDead;

        if (bodyRenderers == null)
            return;

        foreach (
            Renderer bodyRenderer
            in bodyRenderers)
        {
            if (bodyRenderer == null)
                continue;

            if (bodyRenderer.enabled ==
                shouldBeVisible)
            {
                continue;
            }

            bodyRenderer.enabled =
                shouldBeVisible;
        }
    }


    public override void Render()
    {
        UpdateBodyState();
    }


    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_ResetPlayer()
    {
        if (!Object.HasStateAuthority)
            return;

        StopAllCoroutines();

        CacheReferences();


        Health =
            maxHealth;

        Kills =
            0;

        IsDead =
            true;

        UpdateBodyState();


        if (weaponInventory != null)
        {
            weaponInventory
                .ResetInventory();
        }


        if (pistolShooting != null)
        {
            pistolShooting
                .ResetPistolAmmo();
        }


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


        Health =
            maxHealth;

        IsDead =
            false;

        UpdateBodyState();

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} RESET | " +
            $"Health: {Health} | " +
            $"IsDead: {IsDead}"
        );
    }
}