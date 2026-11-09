using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PHealth : NetworkBehaviour
{
    // ============================
    // HEALTH
    // ============================

    [Header("Health")]
    [SerializeField]
    private int maxHealth = 100;

    // ============================
    // RESPAWN
    // ============================

    [Header("Respawn")]
    [SerializeField]
    private float respawnDelay = 3f;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // VISUAL
    // ============================

    [Header("Visual")]
    [SerializeField]
    private GameObject bodyObject;

    // ============================
    // NETWORK STATE
    // ============================

    [Networked]
    public int Health { get; set; }

    [Networked]
    public int Kills { get; set; }

    [Networked]
    public int Deaths { get; set; }

    [Networked]
    public NetworkBool IsDead { get; set; }

    // ============================
    // REFERENCES
    // ============================

    private NetworkGameManager gameManager;

    private DeathmatchGameManager
        deathmatchGameManager;

    private TeamSpawnManager teamSpawnManager;

    private DeathmatchSpawnManager
        deathmatchSpawnManager;

    private PTeam team;

    private NetworkTransform networkTransform;

    private PWeaponInventory weaponInventory;

    private PShooting pistolShooting;

    private Renderer[] bodyRenderers;

    // ============================
    // PUBLIC PROPERTIES
    // ============================

    public int MaxHealth =>
        maxHealth;

    // ============================
    // SPAWNED
    // ============================

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

            Deaths =
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
            $"SCENE: {SceneManager.GetActiveScene().name} | " +
            $"HEALTH: {Health} | " +
            $"KILLS: {Kills} | " +
            $"DEATHS: {Deaths} | " +
            $"DEAD: {IsDead}"
        );
    }

    // ============================
    // IS DEATHMATCH
    // ============================

    private bool IsDeathmatch()
    {
        return
            SceneManager
                .GetActiveScene()
                .name ==
            deathmatchSceneName;
    }

    // ============================
    // CACHE REFERENCES
    // ============================

    private void CacheReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

        if (deathmatchGameManager == null)
        {
            deathmatchGameManager =
                FindFirstObjectByType<
                    DeathmatchGameManager
                >();
        }

        if (teamSpawnManager == null)
        {
            teamSpawnManager =
                FindFirstObjectByType<
                    TeamSpawnManager
                >();
        }

        if (deathmatchSpawnManager == null)
        {
            deathmatchSpawnManager =
                FindFirstObjectByType<
                    DeathmatchSpawnManager
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

    // ============================
    // CACHE BODY RENDERERS
    // ============================

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

    // ============================
    // TAKE DAMAGE RPC
    // ============================

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
            $"IsDead: {IsDead} | " +
            $"Mode: " +
            $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")}"
        );

        // ============================
        // DEAD PLAYER
        // ============================

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

        // ============================
        // DEATHMATCH DAMAGE CHECK
        // ============================

        if (IsDeathmatch())
        {
            if (deathmatchGameManager == null)
            {
                deathmatchGameManager =
                    FindFirstObjectByType<
                        DeathmatchGameManager
                    >();
            }

            // If the manager is not ready yet,
            // damage stays disabled for safety.
            if (deathmatchGameManager == null)
            {
                Debug.LogWarning(
                    $"DAMAGE IGNORED - " +
                    $"DEATHMATCH MANAGER NOT FOUND | " +
                    $"Victim: " +
                    $"{Object.InputAuthority.PlayerId}"
                );

                return;
            }

            if (!deathmatchGameManager
                .DamageEnabled)
            {
                string reason =
                    deathmatchGameManager
                        .WarmupActive
                    ? "WARMUP"
                    : "MATCH NOT ACTIVE";

                Debug.Log(
                    $"DAMAGE IGNORED - " +
                    $"DEATHMATCH DAMAGE DISABLED | " +
                    $"Victim: " +
                    $"{Object.InputAuthority.PlayerId} | " +
                    $"Reason: {reason}"
                );

                return;
            }
        }

        // ============================
        // TEAM VS TEAM FRIENDLY FIRE
        // ============================

        if (!IsDeathmatch())
        {
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
        }

        // ============================
        // DAMAGE
        // ============================

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

    // ============================
    // DIE
    // ============================

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

        Deaths++;

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} DIED | " +
            $"DEATHS: {Deaths} | " +
            $"MODE: " +
            $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")}"
        );

        // ============================
        // KILL CREDIT
        // ============================

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

        // Move player immediately while invisible.
        MoveDeadPlayerToSpawn();

        StartCoroutine(
            RespawnCoroutine()
        );
    }

    // ============================
    // ADD KILL
    // ============================

    private void AddKillAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        Kills++;

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} KILL | " +
            $"TOTAL: {Kills} | " +
            $"MODE: " +
            $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")}"
        );

        // ============================
        // TEAM VS TEAM VICTORY CHECK
        // ============================

        if (!IsDeathmatch())
        {
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

        // Deathmatch victory is handled
        // by DeathmatchGameManager.
    }

    // ============================
    // MOVE DEAD PLAYER TO SPAWN
    // ============================

    private void MoveDeadPlayerToSpawn()
    {
        if (!Object.HasStateAuthority)
            return;

        CacheReferences();

        // ============================
        // DEATHMATCH
        // ============================

        if (IsDeathmatch())
        {
            MoveDeadPlayerToDeathmatchSpawn();

            return;
        }

        // ============================
        // TEAM VS TEAM
        // ============================

        MoveDeadPlayerToTeamSpawn();
    }

    // ============================
    // TEAM RESPAWN
    // ============================

    private void MoveDeadPlayerToTeamSpawn()
    {
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
                $"PHEALTH: No team respawn found | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"Team: {team.CurrentTeam}"
            );

            return;
        }

        MoveToSpawn(
            spawnPoint,
            teamSpawnManager.LookTarget
        );

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} " +
            $"MOVED TO TEAM RESPAWN WHILE DEAD | " +
            $"TEAM: {team.CurrentTeam} | " +
            $"SPAWN: {spawnPoint.name}"
        );
    }

    // ============================
    // DEATHMATCH RESPAWN
    // ============================

    private void MoveDeadPlayerToDeathmatchSpawn()
    {
        if (deathmatchSpawnManager == null)
        {
            deathmatchSpawnManager =
                FindFirstObjectByType<
                    DeathmatchSpawnManager
                >();
        }

        if (deathmatchSpawnManager == null)
        {
            Debug.LogError(
                "PHEALTH: " +
                "DeathmatchSpawnManager not found."
            );

            return;
        }

        Transform spawnPoint =
            deathmatchSpawnManager
                .GetRandomSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"PHEALTH: No deathmatch respawn found | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId}"
            );

            return;
        }

        MoveToSpawn(
            spawnPoint,
            deathmatchSpawnManager.LookTarget
        );

        Debug.Log(
            $"PLAYER " +
            $"{Object.InputAuthority.PlayerId} " +
            $"MOVED TO DEATHMATCH RESPAWN WHILE DEAD | " +
            $"SPAWN: {spawnPoint.name}"
        );
    }

    // ============================
    // RESPAWN COROUTINE
    // ============================

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
            $"Kills: {Kills} | " +
            $"Deaths: {Deaths} | " +
            $"IsDead: {IsDead} | " +
            $"Mode: " +
            $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")}"
        );
    }

    // ============================
    // MOVE TO SPAWN
    // ============================

    private void MoveToSpawn(
        Transform spawnPoint,
        Transform lookTarget)
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

        // ============================
        // ROTATION
        // ============================

        Quaternion rotation =
            spawnPoint.rotation;

        if (lookTarget != null)
        {
            Vector3 direction =
                lookTarget.position -
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

        // ============================
        // TRANSFORM
        // ============================

        transform.SetPositionAndRotation(
            spawnPoint.position,
            rotation
        );

        // ============================
        // NETWORK TRANSFORM
        // ============================

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

        // ============================
        // LOOK SYNC
        // ============================

        PLook look =
            GetComponent<PLook>();

        if (look != null &&
            Object.HasInputAuthority)
        {
            look.SyncHorizontalRotation();
        }
    }

    // ============================
    // UPDATE BODY STATE
    // ============================

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

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        UpdateBodyState();
    }

    // ============================
    // RESET PLAYER RPC
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

        CacheReferences();

        Health =
            maxHealth;

        Kills =
            0;

        Deaths =
            0;

        IsDead =
            true;

        UpdateBodyState();

        // ============================
        // RESET INVENTORY
        // ============================

        if (weaponInventory != null)
        {
            weaponInventory
                .ResetInventory();
        }

        // ============================
        // RESET PISTOL
        // ============================

        if (pistolShooting != null)
        {
            pistolShooting
                .ResetPistolAmmo();
        }

        // ============================
        // RESET POSITION
        // ============================

        if (IsDeathmatch())
        {
            MoveDeadPlayerToDeathmatchSpawn();
        }
        else
        {
            MoveDeadPlayerToTeamSpawn();
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
            $"Kills: {Kills} | " +
            $"Deaths: {Deaths} | " +
            $"IsDead: {IsDead} | " +
            $"Mode: " +
            $"{(IsDeathmatch() ? "DEATHMATCH" : "TEAM VS TEAM")}"
        );
    }
}