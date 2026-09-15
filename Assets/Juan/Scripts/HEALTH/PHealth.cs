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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PMovement>();
        shooting = GetComponent<PShooting>();
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

        Vector3 respawnPosition;

        if (Object.StateAuthority.PlayerId == 1)
        {
            respawnPosition =
                new Vector3(-3f, 1f, 0f);
        }
        else
        {
            respawnPosition =
                new Vector3(3f, 1f, 0f);
        }

        if (controller != null)
            controller.enabled = false;

        transform.position = respawnPosition;

        Health = maxHealth;
        IsDead = false;

        if (controller != null)
            controller.enabled = true;

        UpdateDeadState();

        Debug.Log("Jugador respawneado");
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

        Vector3 spawnPosition;

        if (Object.StateAuthority.PlayerId == 1)
        {
            spawnPosition =
                new Vector3(-3f, 1f, 0f);
        }
        else
        {
            spawnPosition =
                new Vector3(3f, 1f, 0f);
        }

        if (controller != null)
            controller.enabled = false;

        transform.position = spawnPosition;

        if (controller != null)
            controller.enabled = true;

        UpdateDeadState();

        Debug.Log(
            $"Player {Object.StateAuthority.PlayerId} reiniciado"
        );
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