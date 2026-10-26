using System;
using System.Collections;
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
    
    [Header("Network")]
    [SerializeField]
    private NetworkObject playerPrefab;

    [SerializeField]
    private string sessionName =
        "ArenaRoom";

    [SerializeField]
    private int maxPlayers =
        16;

    
    [Header("Input")]
    [SerializeField]
    private float doubleTapTime =
        0.25f;

    
    private NetworkRunner runner;

    
    private float lastWPress =
        -10f;

    private float lastSPress =
        -10f;

    private float lastAPress =
        -10f;

    private float lastDPress =
        -10f;

    private bool pendingGhostForward;
    private bool pendingGhostBackward;
    private bool pendingGhostLeft;
    private bool pendingGhostRight;

    
    private async void Start()
    {
        DontDestroyOnLoad(
            gameObject
        );

        await StartGame();
    }

    
    private void Update()
    {
        CaptureDoubleTapInput();
    }


    private void CaptureDoubleTapInput()
    {
        if (runner == null)
            return;

        if (!runner.IsRunning)
            return;


        if (Input.GetKeyDown(
            KeyCode.W))
        {
            if (Time.time -
                lastWPress <=
                doubleTapTime)
            {
                pendingGhostForward =
                    true;
            }

            lastWPress =
                Time.time;
        }


        if (Input.GetKeyDown(
            KeyCode.S))
        {
            if (Time.time -
                lastSPress <=
                doubleTapTime)
            {
                pendingGhostBackward =
                    true;
            }

            lastSPress =
                Time.time;
        }


        if (Input.GetKeyDown(
            KeyCode.A))
        {
            if (Time.time -
                lastAPress <=
                doubleTapTime)
            {
                pendingGhostLeft =
                    true;
            }

            lastAPress =
                Time.time;
        }


        if (Input.GetKeyDown(
            KeyCode.D))
        {
            if (Time.time -
                lastDPress <=
                doubleTapTime)
            {
                pendingGhostRight =
                    true;
            }

            lastDPress =
                Time.time;
        }
    }


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
            Debug.LogWarning(
                "NETWORK BOOTSTRAP: " +
                "Runner is already running."
            );

            return;
        }

        runner.ProvideInput =
            true;

        runner.AddCallbacks(
            this
        );


        NetworkSceneManagerDefault
            sceneManager =
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


        GameMode selectedMode =
            NetworkLaunchSettings
                .SelectedGameMode;

        Debug.Log(
            $"STARTING FUSION | " +
            $"MODE: {selectedMode} | " +
            $"SESSION: {sessionName}"
        );

        StartGameArgs args =
            new StartGameArgs
            {
                GameMode =
                    selectedMode,

                SessionName =
                    sessionName,

                PlayerCount =
                    maxPlayers,

                SceneManager =
                    sceneManager
            };


        if (selectedMode ==
            GameMode.Host)
        {
            Scene activeScene =
                SceneManager
                    .GetActiveScene();

            SceneRef currentScene =
                SceneRef.FromIndex(
                    activeScene.buildIndex
                );

            args.Scene =
                currentScene;
        }


        StartGameResult result =
            await runner.StartGame(
                args
            );

        if (!result.Ok)
        {
            Debug.LogError(
                $"FUSION START ERROR | " +
                $"MODE: {selectedMode} | " +
                $"REASON: " +
                $"{result.ShutdownReason}"
            );

            return;
        }

        string role =
            runner.IsServer
                ? "HOST"
                : "CLIENT";

        Debug.Log(
            $"FUSION STARTED | " +
            $"ROLE: {role} | " +
            $"Session: {sessionName} | " +
            $"Max Players: {maxPlayers} | " +
            $"Scene: " +
            $"{SceneManager.GetActiveScene().name}"
        );
    }


    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log(
            $"PLAYER JOINED | " +
            $"PlayerId: {player.PlayerId} | " +
            $"Local: " +
            $"{player == runner.LocalPlayer} | " +
            $"Server: {runner.IsServer}"
        );

        if (SceneManager
            .GetActiveScene()
            .name != "ARENA")
        {
            return;
        }

        if (!runner.IsServer)
            return;

        TrySpawnPlayer(
            player
        );
    }


    public void OnSceneLoadDone(
        NetworkRunner runner)
    {
        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        Debug.Log(
            $"NETWORK SCENE LOADED | " +
            $"{sceneName} | " +
            $"SERVER: {runner.IsServer}"
        );

        if (sceneName != "ARENA")
            return;

        if (!runner.IsServer)
            return;

        foreach (
            PlayerRef player
            in runner.ActivePlayers)
        {
            TrySpawnPlayer(
                player
            );
        }
    }


    private void TrySpawnPlayer(
        PlayerRef player)
    {
        if (runner == null)
            return;

        if (!runner.IsRunning)
            return;


        if (!runner.IsServer)
            return;

        if (player ==
            PlayerRef.None)
        {
            return;
        }


        if (runner.TryGetPlayerObject(
            player,
            out NetworkObject existingPlayer))
        {
            if (existingPlayer != null &&
                existingPlayer.IsValid)
            {
                Debug.Log(
                    $"PLAYER ALREADY SPAWNED | " +
                    $"ID: {player.PlayerId} | " +
                    $"POSITION: " +
                    $"{existingPlayer.transform.position}"
                );

                return;
            }
        }


        TeamSpawnManager
            teamSpawnManager =
            FindFirstObjectByType<
                TeamSpawnManager
            >();

        if (teamSpawnManager == null)
        {
            Debug.LogError(
                "NETWORK BOOTSTRAP: " +
                "TeamSpawnManager not found " +
                "in ARENA."
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

        Vector3 spawnPosition =
            selectedSpawn.position;


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

        Debug.Log(
            $"ABOUT TO SPAWN PLAYER | " +
            $"ID: {player.PlayerId} | " +
            $"TEAM: {team} | " +
            $"SPAWN: {selectedSpawn.name} | " +
            $"POSITION: {spawnPosition} | " +
            $"ROTATION: {spawnRotation.eulerAngles}"
        );


        NetworkObject playerObject =
            runner.Spawn(
                playerPrefab,
                spawnPosition,
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


        ForcePlayerToSpawn(
            playerObject,
            spawnPosition,
            spawnRotation
        );

        StartCoroutine(
            ForcePlayerToSpawnNextFrame(
                playerObject,
                spawnPosition,
                spawnRotation
            )
        );

        Debug.Log(
            $"PLAYER SPAWNED BY HOST | " +
            $"ID: {player.PlayerId} | " +
            $"TEAM: {team} | " +
            $"TEAM INDEX: {teamIndex} | " +
            $"SPAWN: {selectedSpawn.name} | " +
            $"REQUESTED POSITION: " +
            $"{spawnPosition} | " +
            $"ACTUAL POSITION: " +
            $"{playerObject.transform.position} | " +
            $"INPUT AUTHORITY: " +
            $"{player.PlayerId}"
        );
    }


    private void ForcePlayerToSpawn(
        NetworkObject playerObject,
        Vector3 position,
        Quaternion rotation)
    {
        if (playerObject == null)
            return;

        if (!playerObject.IsValid)
            return;

        if (runner == null)
            return;

        if (!runner.IsServer)
            return;

        CharacterController controller =
            playerObject
                .GetComponent<CharacterController>();

        bool controllerWasEnabled =
            controller != null &&
            controller.enabled;

        if (controllerWasEnabled)
        {
            controller.enabled =
                false;
        }

        playerObject.transform
            .SetPositionAndRotation(
                position,
                rotation
            );

        NetworkTransform networkTransform =
            playerObject
                .GetComponent<NetworkTransform>();

        if (networkTransform != null)
        {
            networkTransform.Teleport(
                position,
                rotation
            );
        }
        else
        {
            Debug.LogWarning(
                $"PLAYER {playerObject.InputAuthority.PlayerId} " +
                $"does not have NetworkTransform."
            );
        }

        if (controllerWasEnabled)
        {
            controller.enabled =
                true;
        }

        Debug.Log(
            $"FORCED INITIAL SPAWN | " +
            $"PLAYER: " +
            $"{playerObject.InputAuthority.PlayerId} | " +
            $"POSITION: " +
            $"{playerObject.transform.position}"
        );
    }


    private IEnumerator
        ForcePlayerToSpawnNextFrame(
            NetworkObject playerObject,
            Vector3 position,
            Quaternion rotation)
    {
        yield return null;

        if (playerObject == null)
            yield break;

        if (!playerObject.IsValid)
            yield break;

        if (runner == null)
            yield break;

        if (!runner.IsServer)
            yield break;

        ForcePlayerToSpawn(
            playerObject,
            position,
            rotation
        );

        Debug.Log(
            $"INITIAL SPAWN VERIFIED NEXT FRAME | " +
            $"PLAYER: " +
            $"{playerObject.InputAuthority.PlayerId} | " +
            $"POSITION: " +
            $"{playerObject.transform.position}"
        );
    }


    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log(
            $"PLAYER LEFT | " +
            $"PlayerId: {player.PlayerId}"
        );

        if (!runner.IsServer)
            return;

        if (runner.TryGetPlayerObject(
            player,
            out NetworkObject playerObject))
        {
            if (playerObject != null &&
                playerObject.IsValid)
            {
                runner.Despawn(
                    playerObject
                );
            }
        }
    }


    public void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
        PlayerNetworkInput playerInput =
            new PlayerNetworkInput();


        Vector2 move =
            Vector2.zero;

        if (Input.GetKey(
            KeyCode.W))
        {
            move.y +=
                1f;
        }

        if (Input.GetKey(
            KeyCode.S))
        {
            move.y -=
                1f;
        }

        if (Input.GetKey(
            KeyCode.D))
        {
            move.x +=
                1f;
        }

        if (Input.GetKey(
            KeyCode.A))
        {
            move.x -=
                1f;
        }

        playerInput.MoveDirection =
            Vector2.ClampMagnitude(
                move,
                1f
            );


        playerInput.Buttons.Set(
            PlayerInputButton.Jump,
            Input.GetKey(
                KeyCode.Space
            )
        );


        playerInput.Buttons.Set(
            PlayerInputButton.GhostForward,
            pendingGhostForward
        );

        playerInput.Buttons.Set(
            PlayerInputButton.GhostBackward,
            pendingGhostBackward
        );

        playerInput.Buttons.Set(
            PlayerInputButton.GhostLeft,
            pendingGhostLeft
        );

        playerInput.Buttons.Set(
            PlayerInputButton.GhostRight,
            pendingGhostRight
        );


        input.Set(
            playerInput
        );

        pendingGhostForward =
            false;

        pendingGhostBackward =
            false;

        pendingGhostLeft =
            false;

        pendingGhostRight =
            false;
    }


    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input)
    {
    }


    public void OnConnectedToServer(
        NetworkRunner runner)
    {
        Debug.Log(
            $"CONNECTED TO SERVER | " +
            $"LOCAL PLAYER: " +
            $"{runner.LocalPlayer.PlayerId}"
        );
    }


    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason)
    {
        Debug.Log(
            $"DISCONNECTED FROM SERVER | " +
            $"{reason}"
        );
    }


    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs
            .ConnectRequest request,
        byte[] token)
    {
        if (!runner.IsServer)
            return;

        request.Accept();

        Debug.Log(
            "CONNECTION REQUEST ACCEPTED"
        );
    }


    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason)
    {
        Debug.LogError(
            $"CONNECTION FAILED | " +
            $"{reason}"
        );
    }


    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason)
    {
        Debug.Log(
            $"FUSION SHUTDOWN | " +
            $"{shutdownReason}"
        );
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