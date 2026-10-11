using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner _runnerPrefab;
    private NetworkRunner _currentRunner;

    public event Action OnJoinedLobby = delegate { };
    public event Action<List<SessionInfo>> OnSessionListUpdate = delegate { };

  

    //Unirse a un lobby - lobby es un contenedor de "listas" de sesiones
    #region Lobby
    public void JoinLobby()
    {
        if (_currentRunner) 
            Destroy(_currentRunner.gameObject);

        //instancio el runner y lo guardo en mi variable
        _currentRunner = Instantiate(_runnerPrefab);
        
        //Agregar los callbacks a nuestro runner
        _currentRunner.AddCallbacks(this);

        JoinLobbyAsync();
    }

    async void JoinLobbyAsync()
    {
        var result = await _currentRunner.JoinSessionLobby(SessionLobby.Custom, "Normal lobby");

        if (!result.Ok)
        {
            Debug.LogError($"[Custom Error] Unable to Join Lobby");
        }
        else
        {
            Debug.Log($"[Custom Msg] Joined Lobby");

            //Que llamo cuando me uni al lobby?
            OnJoinedLobby();
        }
    }
    
    #endregion

    //Crear una sesion de juego - donde nos conectamos para jugar o interactuar con otros jugadores
    #region StartGame
    
    //El host de la sesion es el que la crea
    public async void CreateGame(string sessionName, string sceneName)
    {
        await InitializeGame(GameMode.Host, sessionName, SceneUtility.GetBuildIndexByScenePath($"Scenes/{sceneName}"));
    }
    
    //El cliente de la sesion es quien se une
    public async void JoinGame(SessionInfo sessionInfo)
    {
        await InitializeGame(GameMode.Client, sessionInfo.Name, SceneManager.GetActiveScene().buildIndex);
    }
    
    async Task InitializeGame(GameMode gameMode, string sessionName, int sceneIndex)
    {
        _currentRunner.ProvideInput = true;

        
        var result = await _currentRunner.StartGame(new StartGameArgs()
        {
            //pasarle el game mode, escena y nombre de sesion
            GameMode = gameMode,
            Scene = SceneRef.FromIndex(sceneIndex),
            SessionName = sessionName
        });
        
        if (!result.Ok)
        {
            Debug.LogError($"[Custom Error] Unable to Start Game");
        }
        else
        {
            Debug.Log($"[Custom Msg] Game Started");
        }
    }
    
    #endregion
    
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        //Que llamo cuando se actualiza la lista de sesiones?
        OnSessionListUpdate(sessionList);
    }
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){ }

    public void OnReliableDataReceived(
    NetworkRunner runner,
    PlayerRef player,
    ReliableKey key,
    ReadOnlySpan<byte> data)
    {
    }
}
