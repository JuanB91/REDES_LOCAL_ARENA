using Fusion;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    // ============================
    // CONFIG
    // ============================

    [Header("Match")]
    [SerializeField]
    private int killsToWin = 5;

    // ============================
    // FRIENDLY FIRE CONFIG
    // ============================

    [Header("Friendly Fire")]
    [SerializeField]
    private bool defaultFriendlyFire = false;

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
    // TEAM SCORES
    // ============================

    [Networked]
    public int RedScore { get; set; }

    [Networked]
    public int BlueScore { get; set; }

    // ============================
    // TEAM COUNTS
    // ============================

    [Networked]
    public int RedPlayerCount { get; set; }

    [Networked]
    public int BluePlayerCount { get; set; }

    // ============================
    // WINNER TEAM
    // ============================

    [Networked]
    public PTeam.Team WinningTeam { get; set; }

    [Networked]
    public NetworkBool HasWinningTeam { get; set; }

    // ============================
    // FRIENDLY FIRE
    // ============================

    [Networked]
    public NetworkBool FriendlyFireEnabled { get; set; }

    // ============================
    // PUBLIC
    // ============================

    public int KillsToWin =>
        killsToWin;

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

        Winner =
            PlayerRef.None;

        RedScore = 0;
        BlueScore = 0;

        RedPlayerCount = 0;
        BluePlayerCount = 0;

        HasWinningTeam = false;
        
        FriendlyFireEnabled =
            GameSettings.FriendlyFireEnabled;

        Debug.Log(
            $"NETWORK GAME MANAGER READY | " +
            $"FRIENDLY FIRE: " +
            $"{(FriendlyFireEnabled ? "ON" : "OFF")}"
        );
    }

    // ============================
    // LOCAL INPUT
    // ============================

    private void Update()
    {
        if (!IsReady)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleFriendlyFire();
        }
    }

    // ============================
    // NETWORK UPDATE
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        UpdateTeamCounts();
        UpdateMatchState();
        UpdateTeamScores();
        CheckTeamVictory();
    }

    // ============================
    // FRIENDLY FIRE TOGGLE
    // ============================

    public void ToggleFriendlyFire()
    {
        if (!IsReady)
            return;

        RPC_RequestFriendlyFireToggle();
    }

    // ============================
    // FRIENDLY FIRE RPC
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestFriendlyFireToggle()
    {
        FriendlyFireEnabled =
            !FriendlyFireEnabled;

        RPC_FriendlyFireChanged(
            FriendlyFireEnabled
        );
    }

    // ============================
    // FRIENDLY FIRE MESSAGE
    // ============================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_FriendlyFireChanged(
        NetworkBool enabled)
    {
        Debug.Log(
            $"FRIENDLY FIRE: " +
            $"{(enabled ? "ON" : "OFF")}"
        );
    }

    // ============================
    // UPDATE TEAM COUNTS
    // ============================

    private void UpdateTeamCounts()
    {
        int redCount = 0;
        int blueCount = 0;

        PTeam[] teams =
            FindObjectsByType<PTeam>(
                FindObjectsSortMode.None
            );

        foreach (
            PTeam team
            in teams)
        {
            if (team == null)
                continue;

            if (team.Object == null)
                continue;

            if (!team.Object.IsValid)
                continue;

            if (team.CurrentTeam ==
                PTeam.Team.Red)
            {
                redCount++;
            }
            else if (
                team.CurrentTeam ==
                PTeam.Team.Blue)
            {
                blueCount++;
            }
        }

        RedPlayerCount =
            redCount;

        BluePlayerCount =
            blueCount;
    }

    // ============================
    // MATCH STATE
    // ============================

    private void UpdateMatchState()
    {
        if (GameOver)
            return;

        bool hasRed =
            RedPlayerCount >= 1;

        bool hasBlue =
            BluePlayerCount >= 1;

        bool canStart =
            hasRed &&
            hasBlue;

        if (canStart)
        {
            if (!MatchStarted)
            {
                MatchStarted =
                    true;

                Debug.Log(
                    $"MATCH STARTED | " +
                    $"RED: {RedPlayerCount} | " +
                    $"BLUE: {BluePlayerCount}"
                );
            }
        }
        else
        {
            MatchStarted =
                false;
        }
    }

    // ============================
    // UPDATE TEAM SCORES
    // ============================

    private void UpdateTeamScores()
    {
        int redScore = 0;
        int blueScore = 0;

        PHealth[] players =
            FindObjectsByType<PHealth>(
                FindObjectsSortMode.None
            );

        foreach (
            PHealth health
            in players)
        {
            if (health == null)
                continue;

            PTeam team =
                health.GetComponent<PTeam>();

            if (team == null)
                continue;

            if (team.CurrentTeam ==
                PTeam.Team.Red)
            {
                redScore +=
                    health.Kills;
            }
            else if (
                team.CurrentTeam ==
                PTeam.Team.Blue)
            {
                blueScore +=
                    health.Kills;
            }
        }

        RedScore =
            redScore;

        BlueScore =
            blueScore;
    }

    // ============================
    // CHECK TEAM VICTORY
    // ============================

    private void CheckTeamVictory()
    {
        if (GameOver)
            return;

        if (!MatchStarted)
            return;

        if (RedScore >= killsToWin)
        {
            EndGame(
                PTeam.Team.Red
            );

            return;
        }

        if (BlueScore >= killsToWin)
        {
            EndGame(
                PTeam.Team.Blue
            );
        }
    }

    // ============================
    // LEGACY CHECK
    // ============================

    public void CheckVictory(
        PHealth killer)
    {
        // Se mantiene por compatibilidad
        // con PHealth.
    }

    // ============================
    // END GAME
    // ============================

    private void EndGame(
        PTeam.Team winningTeam)
    {
        if (!Object.HasStateAuthority)
            return;

        if (GameOver)
            return;

        WinningTeam =
            winningTeam;

        HasWinningTeam =
            true;

        GameOver =
            true;

        MatchStarted =
            false;

        Winner =
            PlayerRef.None;

        Debug.Log(
            $"GAME OVER | " +
            $"WINNING TEAM: {winningTeam} | " +
            $"RED: {RedScore} | " +
            $"BLUE: {BlueScore}"
        );
    }

    // ============================
    // RESTART REQUEST
    // ============================

    public void RequestRestart()
    {
        if (!IsReady)
            return;

        RPC_RequestRestart();
    }

    // ============================
    // RESTART RPC
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestRestart()
    {
        RestartMatch();
    }

    // ============================
    // RESTART MATCH
    // ============================

    private void RestartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            "RESTARTING MATCH..."
        );

        GameOver =
            false;

        MatchStarted =
            false;

        Winner =
            PlayerRef.None;

        RedScore =
            0;

        BlueScore =
            0;

        HasWinningTeam =
            false;

        PHealth[] players =
            FindObjectsByType<PHealth>(
                FindObjectsSortMode.None
            );

        foreach (
            PHealth health
            in players)
        {
            if (health == null)
                continue;

            health.RPC_ResetPlayer();
        }

        Debug.Log(
            "MATCH RESET COMPLETE"
        );
    }
}