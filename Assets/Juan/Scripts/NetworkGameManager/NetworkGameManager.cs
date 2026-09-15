using Fusion;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Condición de victoria")]
    [SerializeField] private int killsToWin = 5;

    [Header("Jugadores")]
    [SerializeField] private int minimumPlayers = 2;

    [Networked]
    public NetworkBool GameOver { get; set; }

    [Networked]
    public NetworkBool MatchStarted { get; set; }

    [Networked]
    public PlayerRef Winner { get; set; }

    public int KillsToWin => killsToWin;

    public bool IsReady { get; private set; }

    public bool CanPlay
    {
        get
        {
            return IsReady &&
                   MatchStarted &&
                   !GameOver;
        }
    }

    public override void Spawned()
    {
        IsReady = true;

        if (Object.HasStateAuthority)
        {
            GameOver = false;
            MatchStarted = false;
            Winner = PlayerRef.None;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        int playerCount = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            playerCount++;
        }

        // Arranca cuando llegan los jugadores necesarios.
        if (!MatchStarted &&
            !GameOver &&
            playerCount >= minimumPlayers)
        {
            MatchStarted = true;

            Debug.Log(
                $"PARTIDA INICIADA | Jugadores: {playerCount}"
            );
        }

        // Si alguien abandona, volvemos a esperar.
        if (MatchStarted &&
            !GameOver &&
            playerCount < minimumPlayers)
        {
            MatchStarted = false;

            Debug.Log(
                "PARTIDA PAUSADA: esperando jugadores"
            );
        }
    }

    public void CheckVictory(PHealth playerHealth)
    {
        if (!IsReady)
            return;

        if (!MatchStarted)
            return;

        if (GameOver)
            return;

        if (playerHealth.Kills >= killsToWin)
        {
            RPC_RequestGameOver(
                playerHealth.Object.StateAuthority
            );
        }
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestGameOver(
        PlayerRef winner,
        RpcInfo info = default)
    {
        if (GameOver)
            return;

        GameOver = true;
        Winner = winner;

        Debug.Log(
            $"FIN DE PARTIDA | Ganador: {Winner}"
        );
    }

    public void RequestRestart()
    {
        if (!IsReady)
            return;

        RPC_RequestRestart();
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestRestart(
        RpcInfo info = default)
    {
        if (!GameOver)
            return;

        Debug.Log("REINICIANDO PARTIDA");

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            NetworkObject playerObject =
                Runner.GetPlayerObject(player);

            if (playerObject == null)
                continue;

            PHealth health =
                playerObject.GetComponent<PHealth>();

            if (health != null)
            {
                health.RPC_ResetPlayer();
            }
        }

        Winner = PlayerRef.None;
        GameOver = false;

        // Si siguen estando los 2 conectados,
        // la partida continúa inmediatamente.
        int playerCount = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            playerCount++;
        }

        MatchStarted =
            playerCount >= minimumPlayers;
    }
}