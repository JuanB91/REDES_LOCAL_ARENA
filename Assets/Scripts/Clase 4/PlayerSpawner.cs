using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    // Prefab 0 = Player 1
    // Prefab 1 = Player 2
    [SerializeField] private GameObject[] _playerPrefabs;

    // Spawn 0 = Player 1
    // Spawn 1 = Player 2
    [SerializeField] private Transform[] _spawnTransforms;

    private bool _initialized;

    public void PlayerJoined(PlayerRef player)
    {
        var playersCount = Runner.SessionInfo.PlayerCount;

        // Si el primer cliente estaba esperando al segundo,
        // creo al Player 1.
        if (_initialized && playersCount >= 2)
        {
            CreatePlayer(0);
            return;
        }

        if (player == Runner.LocalPlayer)
        {
            if (playersCount < 2)
            {
                _initialized = true;
            }
            else
            {
                // Segundo jugador -> índice 1
                CreatePlayer(playersCount - 1);
            }
        }
    }

    void CreatePlayer(int playerIndex)
    {
        _initialized = false;

        if (playerIndex < 0 || playerIndex >= _playerPrefabs.Length)
        {
            Debug.LogError($"No existe un prefab para el jugador {playerIndex}");
            return;
        }

        if (playerIndex >= _spawnTransforms.Length)
        {
            Debug.LogError($"No existe un Spawn Point para el jugador {playerIndex}");
            return;
        }

        GameObject prefab = _playerPrefabs[playerIndex];
        Transform spawnPoint = _spawnTransforms[playerIndex];

        Runner.Spawn(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }
}