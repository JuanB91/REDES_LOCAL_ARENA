using Fusion;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    // ============================
    // CONFIG
    // ============================

    [Header("Match")]
    [SerializeField] private int killsToWin = 5;
    [SerializeField] private int minimumPlayers = 2;

    // ============================
    // SPAWNS
    // ============================

    [Header("Spawns")]
    [SerializeField] private Transform spawnPointP1;
    [SerializeField] private Transform spawnPointP2;

    [Header("Look Target")]
    [SerializeField] private Transform lookTarget;

    // ============================
    // NETWORK STATE
    // ============================

    [Networked]
    public NetworkBool MatchStarted { get; set; }

    [Networked]
    public NetworkBool GameOver { get; set; }

    [Networked]
    public PlayerRef Winner { get; set; }

    // ============================
    // PUBLIC
    // ============================

    public int KillsToWin =>
        killsToWin;

    public Transform LookTarget =>
        lookTarget;

    public bool IsReady =>
        Object != null &&
        Object.IsValid;

    public bool CanPlay =>
        IsReady &&
        MatchStarted &&
        !GameOver;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        MatchStarted = false;
        GameOver = false;
        Winner = PlayerRef.None;

        Debug.Log(
            "NETWORK GAME MANAGER READY"
        );
    }

    // ============================
    // NETWORK UPDATE
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        UpdateMatchState();
    }

    // ============================
    // MATCH STATE
    // ============================

    private void UpdateMatchState()
    {
        if (GameOver)
            return;

        int playerCount = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            playerCount++;
        }

        if (playerCount >= minimumPlayers)
        {
            if (!MatchStarted)
            {
                MatchStarted = true;

                Debug.Log(
                    $"MATCH STARTED | Players: {playerCount}"
                );
            }
        }
        else
        {
            MatchStarted = false;
        }
    }

    // ============================
    // CHECK VICTORY
    // ============================

    public void CheckVictory(
        PHealth killer)
    {
        if (!Object.HasStateAuthority)
            return;

        if (GameOver)
            return;

        if (killer == null)
            return;

        if (killer.Kills < killsToWin)
            return;

        PlayerRef winner =
            killer.Object.InputAuthority;

        EndGame(
            winner
        );
    }

    // ============================
    // END GAME
    // ============================

    private void EndGame(
        PlayerRef winner)
    {
        if (!Object.HasStateAuthority)
            return;

        if (GameOver)
            return;

        GameOver = true;
        Winner = winner;

        RPC_GameOver(
            winner
        );
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_GameOver(
        PlayerRef winner)
    {
        Debug.Log(
            $"GAME OVER | WINNER: PLAYER {winner.PlayerId}"
        );
    }

    // ============================
    // SPAWN POINT
    // ============================

    public Transform GetSpawnPoint(
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
    // RESTART
    // ============================

    public void RequestRestart()
    {
        if (Object.HasStateAuthority)
        {
            RestartMatch();
        }
        else
        {
            RPC_RequestRestart();
        }
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestRestart()
    {
        RestartMatch();
    }

    private void RestartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            "RESTARTING MATCH..."
        );

        GameOver = false;
        MatchStarted = false;
        Winner = PlayerRef.None;

        foreach (
            PlayerRef player
            in Runner.ActivePlayers)
        {
            NetworkObject playerObject =
                Runner.GetPlayerObject(
                    player
                );

            if (playerObject == null)
                continue;

            PHealth health =
                playerObject.GetComponent<PHealth>();

            if (health != null)
            {
                health.RPC_ResetPlayer();
            }
        }

        Debug.Log(
            "MATCH RESET COMPLETE"
        );
    }
}