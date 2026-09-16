using System.Collections;
using Fusion;
using UnityEngine;

public class PHealth : NetworkBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 100;

    public int MaxHealth => maxHealth;

    [Header("Respawn")]
    [SerializeField] private float respawnTime = 3f;

    [Header("Referencias")]
    [SerializeField] private GameObject body;

    [Networked]
    public int Health { get; set; }

    [Networked]
    public int Kills { get; set; }

    [Networked]
    public NetworkBool IsDead { get; set; }

    private CharacterController controller;
    private PMovement movement;
    private PShooting shooting;
    private PLook playerLook;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PMovement>();
        shooting = GetComponent<PShooting>();
        playerLook = GetComponent<PLook>();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Health = maxHealth;
            Kills = 0;
            IsDead = false;
        }

        UpdateDeadState();
    }

    public override void Render()
    {
        UpdateDeadState();
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_TakeDamage(
        int amount,
        NetworkObject attacker,
        RpcInfo info = default)
    {
        TakeDamage(amount, attacker);
    }

    private void TakeDamage(
        int amount,
        NetworkObject attacker)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        Health -= amount;

        Debug.Log(
            $"Daño recibido: {amount} | Vida: {Health}"
        );

        if (Health <= 0)
        {
            Health = 0;
            Die(attacker);
        }
    }

    private void Die(NetworkObject attacker)
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("Jugador muerto");

        if (attacker != null)
        {
            PHealth attackerHealth =
                attacker.GetComponent<PHealth>();

            if (attackerHealth != null)
            {
                attackerHealth.RPC_AddKill();
            }
        }

        StartCoroutine(RespawnRoutine());
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_AddKill(
        RpcInfo info = default)
    {
        Kills++;

        Debug.Log(
            $"Kill sumada. Total: {Kills}"
        );

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager != null &&
            gameManager.IsReady)
        {
            gameManager.CheckVictory(this);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        if (IsDead == false)
            yield break;

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null)
        {
            Debug.LogError(
                "No se encontró NetworkGameManager para respawn."
            );

            yield break;
        }

        Transform spawnPoint =
            gameManager.GetSpawnPoint(
                Object.StateAuthority
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                "No se encontró SpawnPoint para este jugador."
            );

            yield break;
        }

        if (controller != null)
            controller.enabled = false;

        transform.position = spawnPoint.position;

        LookAtTarget(gameManager);

        Health = maxHealth;
        IsDead = false;

        if (controller != null)
            controller.enabled = true;

        UpdateDeadState();

        Debug.Log(
            $"Jugador respawneado en {spawnPoint.name}"
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_ResetPlayer(
        RpcInfo info = default)
    {
        StopAllCoroutines();

        Health = maxHealth;
        Kills = 0;
        IsDead = false;

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null)
        {
            Debug.LogError(
                "No se encontró NetworkGameManager para reset."
            );

            return;
        }

        Transform spawnPoint =
            gameManager.GetSpawnPoint(
                Object.StateAuthority
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                "No se encontró SpawnPoint para este jugador."
            );

            return;
        }

        if (controller != null)
            controller.enabled = false;

        transform.position = spawnPoint.position;

        LookAtTarget(gameManager);

        if (controller != null)
            controller.enabled = true;

        UpdateDeadState();

        Debug.Log(
            $"Player {Object.StateAuthority.PlayerId} " +
            $"reiniciado en {spawnPoint.name}"
        );
    }

    private void LookAtTarget(
      NetworkGameManager gameManager)
    {
        if (gameManager.LookTarget == null)
        {
            Debug.LogWarning(
                "LOOK TARGET no está asignado en NetworkGameManager."
            );

            return;
        }

        Vector3 lookDirection =
            gameManager.LookTarget.position -
            transform.position;

        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    lookDirection.normalized
                );

           
            if (playerLook != null)
            {
                playerLook.SyncHorizontalRotation();
            }
        }
    }

    private void UpdateDeadState()
    {
        bool dead = IsDead;

        if (body != null)
        {
            body.SetActive(!dead);
        }

        if (Object != null &&
            Object.HasStateAuthority)
        {
            if (movement != null)
                movement.enabled = !dead;

            if (shooting != null)
                shooting.enabled = !dead;
        }
    }
}