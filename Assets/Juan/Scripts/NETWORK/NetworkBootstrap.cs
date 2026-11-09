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
    // ============================
    // NETWORK
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
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]

    [SerializeField]
    private string teamVsTeamSceneName =
        "ARENA";

    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // INPUT
    // ============================

    [Header("Input")]
    [SerializeField]
    private float doubleTapTime =
        0.25f;

    // ============================
    // NETWORK RUNNER
    // ============================

    private NetworkRunner runner;

    // ============================
    // GHOST STEP INPUT
    // ============================

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
    // UPDATE
    // ============================

    private void Update()
    {
        CaptureDoubleTapInput();
    }

    // ============================
    // CAPTURE DOUBLE TAP
    // ============================

    private void CaptureDoubleTapInput()
    {
        if (runner == null)
            return;

        if (!runner.IsRunning)
            return;

        // ============================
        // W
        // ============================

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

        // ============================
        // S
        // ============================

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

        // ============================
        // A
        // ============================

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

        // ============================
        // D
        // ============================

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

    // ============================
    // PLAYER JOINED
    // ============================

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

        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        if (!IsGameplayScene(
            sceneName))
        {
            return;
        }

        if (!runner.IsServer)
            return;

        TrySpawnPlayer(
            player
        );
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
            $"{sceneName} | " +
            $"SERVER: {runner.IsServer}"
        );

        if (!IsGameplayScene(
            sceneName))
        {
            return;
        }

        if (!runner.IsServer)
            return;

        Debug.Log(
            $"GAMEPLAY SCENE READY | " +
            $"SCENE: {sceneName} | " +
            $"SPAWNING ACTIVE PLAYERS"
        );

        foreach (
            PlayerRef player
            in runner.ActivePlayers)
        {
            TrySpawnPlayer(
                player
            );
        }
    }

    // ============================
    // GAMEPLAY SCENE CHECK
    // ============================

    private bool IsGameplayScene(
        string sceneName)
    {
        return
            sceneName ==
                teamVsTeamSceneName ||
            sceneName ==
                deathmatchSceneName;
    }

    // ============================
    // DEATHMATCH CHECK
    // ============================

    private bool IsDeathmatchScene()
    {
        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        return
            sceneName ==
            deathmatchSceneName;
    }

    // ============================
    // TRY SPAWN PLAYER
    // ============================

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

        // ============================
        // ALREADY SPAWNED?
        // ============================

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

        // ============================
        // PREFAB VALIDATION
        // ============================

        if (playerPrefab == null)
        {
            Debug.LogError(
                "NETWORK BOOTSTRAP: " +
                "Player Prefab not assigned."
            );

            return;
        }

        // ============================
        // SPAWN DATA
        // ============================

        Transform selectedSpawn =
            null;

        Vector3 spawnPosition =
            Vector3.zero;

        Quaternion spawnRotation =
            Quaternion.identity;

        string spawnDescription =
            "";

        // ============================
        // DEATHMATCH SPAWN
        // ============================

        if (IsDeathmatchScene())
        {
            DeathmatchSpawnManager
                deathmatchSpawnManager =
                FindFirstObjectByType<
                    DeathmatchSpawnManager
                >();

            if (deathmatchSpawnManager == null)
            {
                Debug.LogError(
                    "NETWORK BOOTSTRAP: " +
                    "DeathmatchSpawnManager " +
                    "not found in DEATHMATCH."
                );

                return;
            }

            selectedSpawn =
                deathmatchSpawnManager
                    .GetRandomSpawnPoint();

            if (selectedSpawn == null)
            {
                Debug.LogError(
                    $"NO DEATHMATCH SPAWN FOUND | " +
                    $"Player: {player.PlayerId}"
                );

                return;
            }

            spawnPosition =
                selectedSpawn.position;

            spawnRotation =
                selectedSpawn.rotation;

            Transform lookTarget =
                deathmatchSpawnManager
                    .LookTarget;

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

            spawnDescription =
                $"MODE: DEATHMATCH | " +
                $"SPAWN: {selectedSpawn.name}";

            Debug.Log(
                $"DEATHMATCH INITIAL SPAWN SELECTED | " +
                $"PLAYER: {player.PlayerId} | " +
                $"SPAWN: {selectedSpawn.name} | " +
                $"POSITION: {spawnPosition} | " +
                $"ROTATION: {spawnRotation.eulerAngles}"
            );
        }

        // ============================
        // TEAM VS TEAM SPAWN
        // ============================

        else
        {
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
                    "in TEAM VS TEAM scene."
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

            selectedSpawn =
                teamSpawnManager
                    .GetSpawnPoint(
                        team,
                        teamIndex
                    );

            if (selectedSpawn == null)
            {
                Debug.LogError(
                    $"NO TEAM SPAWN FOUND | " +
                    $"Player: {player.PlayerId} | " +
                    $"Team: {team} | " +
                    $"TeamIndex: {teamIndex}"
                );

                return;
            }

            spawnPosition =
                selectedSpawn.position;

            spawnRotation =
                selectedSpawn.rotation;

            Transform lookTarget =
                teamSpawnManager
                    .LookTarget;

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

            spawnDescription =
                $"MODE: TEAM VS TEAM | " +
                $"TEAM: {team} | " +
                $"TEAM INDEX: {teamIndex} | " +
                $"SPAWN: {selectedSpawn.name}";

            Debug.Log(
                $"TEAM INITIAL SPAWN SELECTED | " +
                $"PLAYER: {player.PlayerId} | " +
                $"TEAM: {team} | " +
                $"TEAM INDEX: {teamIndex} | " +
                $"SPAWN: {selectedSpawn.name} | " +
                $"POSITION: {spawnPosition}"
            );
        }

        // ============================
        // SPAWN PLAYER
        // ============================

        Debug.Log(
            $"ABOUT TO SPAWN PLAYER | " +
            $"ID: {player.PlayerId} | " +
            $"{spawnDescription} | " +
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

        // ============================
        // REGISTER PLAYER OBJECT
        // ============================

        runner.SetPlayerObject(
            player,
            playerObject
        );

        // ============================
        // FORCE INITIAL POSITION
        // ============================

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
            $"{spawnDescription} | " +
            $"REQUESTED POSITION: " +
            $"{spawnPosition} | " +
            $"ACTUAL POSITION: " +
            $"{playerObject.transform.position} | " +
            $"INPUT AUTHORITY: " +
            $"{player.PlayerId}"
        );
    }

    // ============================
    // FORCE PLAYER TO SPAWN
    // ============================

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
                $"PLAYER " +
                $"{playerObject.InputAuthority.PlayerId} " +
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

    // ============================
    // VERIFY SPAWN NEXT FRAME
    // ============================

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

    // ============================
    // PLAYER LEFT
    // ============================

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

    // ============================
    // INPUT
    // ============================

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
        PlayerNetworkInput playerInput =
            new PlayerNetworkInput();

        Vector2 move =
            Vector2.zero;

        // ============================
        // MOVEMENT INPUT
        // ============================

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

        // ============================
        // JUMP
        // ============================

        playerInput.Buttons.Set(
            PlayerInputButton.Jump,
            Input.GetKey(
                KeyCode.Space
            )
        );

        // ============================
        // GHOST STEP
        // ============================

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

        // ============================
        // SEND INPUT
        // ============================

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

    // ============================
    // INPUT MISSING
    // ============================

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input)
    {
    }

    // ============================
    // CONNECTED
    // ============================

    public void OnConnectedToServer(
        NetworkRunner runner)
    {
        Debug.Log(
            $"CONNECTED TO SERVER | " +
            $"LOCAL PLAYER: " +
            $"{runner.LocalPlayer.PlayerId}"
        );
    }

    // ============================
    // DISCONNECTED
    // ============================

    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason)
    {
        Debug.Log(
            $"DISCONNECTED FROM SERVER | " +
            $"{reason}"
        );
    }

    // ============================
    // CONNECTION REQUEST
    // ============================

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

    // ============================
    // CONNECTION FAILED
    // ============================

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

    // ============================
    // SHUTDOWN
    // ============================

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason)
    {
        Debug.Log(
            $"FUSION SHUTDOWN | " +
            $"{shutdownReason}"
        );
    }

    // ============================
    // SESSION LIST
    // ============================

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList)
    {
    }

    // ============================
    // CUSTOM AUTHENTICATION
    // ============================

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data)
    {
    }

    // ============================
    // HOST MIGRATION
    // ============================

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken)
    {
    }

    // ============================
    // SCENE LOAD START
    // ============================

    public void OnSceneLoadStart(
        NetworkRunner runner)
    {
    }

    // ============================
    // AOI ENTER
    // ============================

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    // ============================
    // AOI EXIT
    // ============================

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    // ============================
    // RELIABLE DATA
    // ============================

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data)
    {
    }

    // ============================
    // RELIABLE DATA PROGRESS
    // ============================

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress)
    {
    }

    // ============================
    // USER SIMULATION MESSAGE
    // ============================

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message)
    {
    }
}