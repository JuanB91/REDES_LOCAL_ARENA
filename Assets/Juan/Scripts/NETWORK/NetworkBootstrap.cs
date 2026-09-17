using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap :
    MonoBehaviour,
    INetworkRunnerCallbacks
{
    // ============================
    // CONFIG
    // ============================

    [Header("Network")]
    [SerializeField] private NetworkObject playerPrefab;

    [SerializeField]
    private string sessionName =
        "ArenaRoom";

    // ============================
    // SPAWNS
    // ============================

    [Header("Spawns")]
    [SerializeField] private Transform spawnPointP1;
    [SerializeField] private Transform spawnPointP2;

    [Header("Look Target")]
    [SerializeField] private Transform lookTarget;

    // ============================
    // RUNTIME
    // ============================

    private NetworkRunner runner;

    // ============================
    // START
    // ============================

    private async void Start()
    {
        await StartGame();
    }

    // ============================
    // START GAME
    // ============================

    private async Task StartGame()
    {
        runner =
            GetComponent<NetworkRunner>();

        if (runner == null)
        {
            Debug.LogError(
                "NETWORK BOOTSTRAP: NetworkRunner not found."
            );

            return;
        }

        if (runner.IsRunning)
            return;

        runner.ProvideInput = true;

        runner.AddCallbacks(
            this
        );

        NetworkSceneManagerDefault sceneManager =
            GetComponent<NetworkSceneManagerDefault>();

        if (sceneManager == null)
        {
            sceneManager =
                gameObject.AddComponent<
                    NetworkSceneManagerDefault
                >();
        }

        SceneRef scene =
            SceneRef.FromIndex(
                SceneManager
                    .GetActiveScene()
                    .buildIndex
            );

        StartGameArgs args =
            new StartGameArgs
            {
                GameMode =
                    GameMode.Shared,

                SessionName =
                    sessionName,

                Scene =
                    scene,

                SceneManager =
                    sceneManager
            };

        StartGameResult result =
            await runner.StartGame(
                args
            );

        if (result.Ok)
        {
            Debug.Log(
                $"FUSION STARTED | Session: {sessionName}"
            );
        }
        else
        {
            Debug.LogError(
                $"FUSION START ERROR | {result.ShutdownReason}"
            );
        }
    }

    // ============================
    // PLAYER JOINED
    // ============================

    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log(
            $"PLAYER JOINED | PlayerId: {player.PlayerId}"
        );

        if (player != runner.LocalPlayer)
            return;

        SpawnLocalPlayer(
            runner,
            player
        );
    }

    // ============================
    // SPAWN PLAYER
    // ============================

    private void SpawnLocalPlayer(
        NetworkRunner runner,
        PlayerRef player)
    {
        if (playerPrefab == null)
        {
            Debug.LogError(
                "PLAYER PREFAB NOT ASSIGNED"
            );

            return;
        }

        Transform selectedSpawn =
            GetSpawnPoint(
                player
            );

        if (selectedSpawn == null)
        {
            Debug.LogError(
                $"NO SPAWN FOUND FOR PLAYER {player.PlayerId}"
            );

            return;
        }

        Quaternion spawnRotation =
            selectedSpawn.rotation;

        if (lookTarget != null)
        {
            Vector3 direction =
                lookTarget.position -
                selectedSpawn.position;

            direction.y = 0f;

            if (direction.sqrMagnitude >
                0.001f)
            {
                spawnRotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
            }
        }

        NetworkObject playerObject =
            runner.Spawn(
                playerPrefab,
                selectedSpawn.position,
                spawnRotation,
                player
            );

        if (playerObject == null)
        {
            Debug.LogError(
                "PLAYER SPAWN FAILED"
            );

            return;
        }

        runner.SetPlayerObject(
            player,
            playerObject
        );

        Debug.Log(
            $"PLAYER SPAWNED | " +
            $"ID: {player.PlayerId} | " +
            $"SPAWN: {selectedSpawn.name}"
        );
    }

    // ============================
    // GET SPAWN
    // ============================

    private Transform GetSpawnPoint(
        PlayerRef player)
    {
        if (player.PlayerId == 1)
        {
            return spawnPointP1;
        }

        if (player.PlayerId == 2)
        {
            return spawnPointP2;
        }

        return spawnPointP1;
    }

    // ============================
    // PLAYER LEFT
    // ============================

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player)
    {
        NetworkObject playerObject =
            runner.GetPlayerObject(
                player
            );

        if (playerObject != null &&
            playerObject.HasStateAuthority)
        {
            runner.Despawn(
                playerObject
            );
        }
    }

    // ============================
    // CALLBACKS
    // ============================

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
        Debug.Log(
            "CONNECTED TO SERVER"
        );
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
        request.Accept();
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason)
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

    public void OnSceneLoadDone(
        NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(
        NetworkRunner runner)
    {
    }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
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

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message)
    {
    }
}