using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointP1;
    [SerializeField] private Transform spawnPointP2;

    [Header("Look Target")]
    [SerializeField] private Transform lookTarget;

    private NetworkRunner runner;

    private async void Start()
    {
        runner = GetComponent<NetworkRunner>();

        if (runner == null)
        {
            Debug.LogError("No se encontró NetworkRunner.");
            return;
        }

        runner.AddCallbacks(this);

        var sceneRef = SceneRef.FromIndex(
            SceneManager.GetActiveScene().buildIndex
        );

        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();

        sceneInfo.AddSceneRef(
            sceneRef,
            LoadSceneMode.Single
        );

        var result = await runner.StartGame(
            new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = "ArenaRoom",
                Scene = sceneInfo,
                SceneManager =
                    GetComponent<NetworkSceneManagerDefault>()
            }
        );

        if (result.Ok)
        {
            Debug.Log(
                "Conectado a ArenaRoom en Shared Mode."
            );
        }
        else
        {
            Debug.LogError(
                $"Error al conectar: {result.ShutdownReason}"
            );
        }
    }

    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log($"Jugador conectado: {player}");

        // Cada cliente crea únicamente su propio player.
        if (player != runner.LocalPlayer)
            return;

        Transform selectedSpawn;

        if (player.PlayerId == 1)
        {
            selectedSpawn = spawnPointP1;
        }
        else if (player.PlayerId == 2)
        {
            selectedSpawn = spawnPointP2;
        }
        else
        {
            Debug.LogError(
                $"PlayerId no reconocido: {player.PlayerId}"
            );

            return;
        }

        if (selectedSpawn == null)
        {
            Debug.LogError(
                $"No hay SpawnPoint asignado para {player}"
            );

            return;
        }

        Quaternion spawnRotation =
            CalculateLookRotation(
                selectedSpawn.position
            );

        NetworkObject playerObject =
            runner.Spawn(
                playerPrefab,
                selectedSpawn.position,
                spawnRotation
            );

        runner.SetPlayerObject(
            player,
            playerObject
        );

        Debug.Log(
            $"Player spawneado para {player} " +
            $"en {selectedSpawn.name} " +
            $"mirando hacia LOOK TARGET"
        );
    }

    private Quaternion CalculateLookRotation(
        Vector3 spawnPosition)
    {
        if (lookTarget == null)
        {
            Debug.LogWarning(
                "LOOK TARGET no está asignado. " +
                "Se usará Quaternion.identity."
            );

            return Quaternion.identity;
        }

        Vector3 direction =
            lookTarget.position - spawnPosition;

        // Solo queremos girar horizontalmente.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning(
                "LOOK TARGET está demasiado cerca del SpawnPoint."
            );

            return Quaternion.identity;
        }

        return Quaternion.LookRotation(
            direction.normalized
        );
    }

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log(
            $"Jugador desconectado: {player}"
        );
    }

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
    }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input)
    {
    }

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(
        NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token)
    {
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data)
    {
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress)
    {
    }

    public void OnSceneLoadDone(
        NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(
        NetworkRunner runner)
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }
}