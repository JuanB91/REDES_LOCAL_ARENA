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
    [SerializeField]
    private NetworkObject playerPrefab;

    [SerializeField]
    private string sessionName =
        "ArenaRoom";

    [SerializeField]
    private int maxPlayers =
        16;

    // ============================
    // RUNTIME
    // ============================

    private NetworkRunner runner;

    private bool localPlayerSpawned;

    // ============================
    // START
    // ============================

    private async void Start()
    {
        DontDestroyOnLoad(
            gameObject
        );

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
                "NETWORK BOOTSTRAP: " +
                "NetworkRunner not found."
            );

            return;
        }

        if (runner.IsRunning)
        {
            return;
        }

        runner.ProvideInput =
            true;

        runner.AddCallbacks(
            this
        );

        NetworkSceneManagerDefault sceneManager =
            GetComponent<
                NetworkSceneManagerDefault
            >();

        if (sceneManager == null)
        {
            sceneManager =
                gameObject.AddComponent<
                    NetworkSceneManagerDefault
                >();
        }

        SceneRef currentScene =
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

                PlayerCount =
                    maxPlayers,

                Scene =
                    currentScene,

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
                $"FUSION STARTED | " +
                $"Session: {sessionName} | " +
                $"Max Players: {maxPlayers} | " +
                $"Scene: {SceneManager.GetActiveScene().name}"
            );
        }
        else
        {
            Debug.LogError(
                $"FUSION START ERROR | " +
                $"{result.ShutdownReason}"
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
            $"PLAYER JOINED | " +
            $"PlayerId: {player.PlayerId}"
        );

        // En WAITING solamente conectamos.
        // Todavía NO creamos el personaje.
        if (SceneManager
            .GetActiveScene()
            .name != "ARENA")
        {
            return;
        }

        if (player != runner.LocalPlayer)
        {
            return;
        }

        TrySpawnLocalPlayer();
    }

    // ============================
    // SCENE LOAD DONE
    // ============================

    public void OnSceneLoadDone(
        NetworkRunner runner)
    {
        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        Debug.Log(
            $"NETWORK SCENE LOADED | " +
            $"{sceneName}"
        );

        if (sceneName == "ARENA")
        {
            TrySpawnLocalPlayer();
        }
    }

    // ============================
    // SPAWN LOCAL PLAYER
    // ============================

    private void TrySpawnLocalPlayer()
    {
        if (runner == null)
            return;

        if (!runner.IsRunning)
            return;

        if (localPlayerSpawned)
            return;

        PlayerRef player =
            runner.LocalPlayer;

        if (player == PlayerRef.None)
            return;

        TeamSpawnManager teamSpawnManager =
            FindFirstObjectByType<
                TeamSpawnManager
            >();

        if (teamSpawnManager == null)
        {
            Debug.LogError(
                "NETWORK BOOTSTRAP: " +
                "TeamSpawnManager not found in ARENA."
            );

            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError(
                "NETWORK BOOTSTRAP: " +
                "Player Prefab not assigned."
            );

            return;
        }

        // ============================
        // TEAM
        // ============================

        PTeam.Team team;
        int teamIndex;

        if (player.PlayerId % 2 != 0)
        {
            team =
                PTeam.Team.Red;

            teamIndex =
                (player.PlayerId - 1) / 2;
        }
        else
        {
            team =
                PTeam.Team.Blue;

            teamIndex =
                (player.PlayerId / 2) - 1;
        }

        // ============================
        // SPAWN POINT
        // ============================

        Transform selectedSpawn =
            teamSpawnManager
                .GetSpawnPoint(
                    team,
                    teamIndex
                );

        if (selectedSpawn == null)
        {
            Debug.LogError(
                $"NO SPAWN FOUND | " +
                $"Player: {player.PlayerId} | " +
                $"Team: {team} | " +
                $"TeamIndex: {teamIndex}"
            );

            return;
        }

        // ============================
        // ROTATION
        // ============================

        Quaternion spawnRotation =
            selectedSpawn.rotation;

        Transform lookTarget =
            teamSpawnManager.LookTarget;

        if (lookTarget != null)
        {
            Vector3 direction =
                lookTarget.position -
                selectedSpawn.position;

            direction.y =
                0f;

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

        // ============================
        // SPAWN
        // ============================

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
                "NETWORK BOOTSTRAP: " +
                "Player spawn failed."
            );

            return;
        }

        runner.SetPlayerObject(
            player,
            playerObject
        );

        localPlayerSpawned =
            true;

        Debug.Log(
            $"PLAYER SPAWNED | " +
            $"ID: {player.PlayerId} | " +
            $"TEAM: {team} | " +
            $"TEAM INDEX: {teamIndex} | " +
            $"SPAWN: {selectedSpawn.name}"
        );
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
        NetworkRunnerCallbackArgs
            .ConnectRequest request,
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