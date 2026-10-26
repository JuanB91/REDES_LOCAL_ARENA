using Fusion;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
  
    [Header("Match")]
    [SerializeField]
    private int killsToWin = 5;

    [Header("Match Start")]
    [SerializeField]
    private float matchStartDelay = 1f;

 
    [Header("Friendly Fire")]
    [SerializeField]
    private bool defaultFriendlyFire = false;

    
    [Networked]
    public NetworkBool MatchStarted { get; set; }

    [Networked]
    public NetworkBool GameOver { get; set; }

    [Networked]
    public PlayerRef Winner { get; set; }

    
    [Networked]
    public int RedScore { get; set; }

    [Networked]
    public int BlueScore { get; set; }

    
    [Networked]
    public int RedPlayerCount { get; set; }

    [Networked]
    public int BluePlayerCount { get; set; }

    
    [Networked]
    public PTeam.Team WinningTeam { get; set; }

    [Networked]
    public NetworkBool HasWinningTeam { get; set; }

   
    [Networked]
    public NetworkBool FriendlyFireEnabled { get; set; }

    
    private bool startCountdownActive;

    private float startCountdownRemaining;


    public int KillsToWin =>
        killsToWin;

    public bool IsReady =>
        Object != null &&
        Object.IsValid;

    public bool CanPlay =>
        IsReady &&
        MatchStarted &&
        !GameOver;


    public bool CanLocalRestart =>
        IsReady &&
        Object.HasStateAuthority;


    public override void Spawned()
    {

        if (!Object.HasStateAuthority)
        {
            Debug.Log(
                "NETWORK GAME MANAGER READY | CLIENT"
            );

            return;
        }


        MatchStarted =
            false;

        GameOver =
            false;

        Winner =
            PlayerRef.None;

        RedScore =
            0;

        BlueScore =
            0;

        RedPlayerCount =
            0;

        BluePlayerCount =
            0;

        HasWinningTeam =
            false;


        startCountdownActive =
            false;

        startCountdownRemaining =
            matchStartDelay;


        FriendlyFireEnabled =
            GameSettings.FriendlyFireEnabled;

        if (!GameSettings.FriendlyFireEnabled)
        {
            FriendlyFireEnabled =
                defaultFriendlyFire;
        }

        Debug.Log(
            $"NETWORK GAME MANAGER READY | HOST | " +
            $"FRIENDLY FIRE: " +
            $"{(FriendlyFireEnabled ? "ON" : "OFF")} | " +
            $"KILLS TO WIN: {killsToWin} | " +
            $"START DELAY: {matchStartDelay:F2}s"
        );
    }


    private void Update()
    {
        if (!IsReady)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (Input.GetKeyDown(
            KeyCode.F1))
        {
            ToggleFriendlyFire();
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        UpdateTeamCounts();

        UpdateMatchState();

        UpdateTeamScores();

        CheckTeamVictory();
    }


    public void ToggleFriendlyFire()
    {
        if (!IsReady)
            return;

        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "FRIENDLY FIRE CHANGE BLOCKED | " +
                "CLIENT IS NOT HOST"
            );

            return;
        }

        SetFriendlyFire(
            !FriendlyFireEnabled
        );
    }


    private void SetFriendlyFire(
        NetworkBool enabled)
    {
        if (!Object.HasStateAuthority)
            return;

        FriendlyFireEnabled =
            enabled;

        RPC_FriendlyFireChanged(
            enabled
        );
    }


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


    private void UpdateTeamCounts()
    {
        if (!Object.HasStateAuthority)
            return;

        int redCount =
            0;

        int blueCount =
            0;

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


    private void UpdateMatchState()
    {
        if (!Object.HasStateAuthority)
            return;

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
            if (MatchStarted)
            {
                startCountdownActive =
                    false;

                return;
            }


            if (!startCountdownActive)
            {
                startCountdownActive =
                    true;

                startCountdownRemaining =
                    Mathf.Max(
                        0f,
                        matchStartDelay
                    );

                Debug.Log(
                    $"MATCH READY | " +
                    $"WAITING {startCountdownRemaining:F2}s " +
                    $"FOR NETWORK SPAWN SYNC | " +
                    $"RED: {RedPlayerCount} | " +
                    $"BLUE: {BluePlayerCount}"
                );
            }


            startCountdownRemaining -=
                Runner.DeltaTime;

            if (startCountdownRemaining >
                0f)
            {
                return;
            }


            startCountdownActive =
                false;

            startCountdownRemaining =
                0f;

            MatchStarted =
                true;

            Debug.Log(
                $"MATCH STARTED | " +
                $"RED: {RedPlayerCount} | " +
                $"BLUE: {BluePlayerCount}"
            );

            return;
        }

        if (startCountdownActive)
        {
            Debug.Log(
                "MATCH START COUNTDOWN CANCELLED | " +
                "A TEAM IS MISSING"
            );
        }

        startCountdownActive =
            false;

        startCountdownRemaining =
            matchStartDelay;

        if (MatchStarted)
        {
            Debug.Log(
                $"MATCH PAUSED | " +
                $"RED: {RedPlayerCount} | " +
                $"BLUE: {BluePlayerCount}"
            );
        }

        MatchStarted =
            false;
    }


    private void UpdateTeamScores()
    {
        if (!Object.HasStateAuthority)
            return;

        int redScore =
            0;

        int blueScore =
            0;

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

            if (health.Object == null)
                continue;

            if (!health.Object.IsValid)
                continue;

            PTeam team =
                health.GetComponent<PTeam>();

            if (team == null)
                continue;

            if (team.Object == null)
                continue;

            if (!team.Object.IsValid)
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


    private void CheckTeamVictory()
    {
        if (!Object.HasStateAuthority)
            return;

        if (GameOver)
            return;

        if (!MatchStarted)
            return;

        if (RedScore >=
            killsToWin)
        {
            EndGame(
                PTeam.Team.Red
            );

            return;
        }

        if (BlueScore >=
            killsToWin)
        {
            EndGame(
                PTeam.Team.Blue
            );
        }
    }


    public void CheckVictory(
        PHealth killer)
    {
        if (!Object.HasStateAuthority)
            return;

        UpdateTeamScores();

        CheckTeamVictory();
    }


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

        startCountdownActive =
            false;

        startCountdownRemaining =
            matchStartDelay;

        Winner =
            PlayerRef.None;

        Debug.Log(
            $"GAME OVER | " +
            $"WINNING TEAM: {winningTeam} | " +
            $"RED: {RedScore} | " +
            $"BLUE: {BlueScore}"
        );
    }


    public void RequestRestart()
    {
        if (!IsReady)
            return;


        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "RESTART BLOCKED | " +
                "ONLY HOST CAN RESTART MATCH"
            );

            return;
        }

        RestartMatch();
    }


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


        startCountdownActive =
            false;

        startCountdownRemaining =
            matchStartDelay;


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

            if (health.Object == null)
                continue;

            if (!health.Object.IsValid)
                continue;

            health.RPC_ResetPlayer();
        }

        Debug.Log(
            "MATCH RESET COMPLETE | " +
            $"WAITING {matchStartDelay:F2}s " +
            "BEFORE MATCH RESUMES"
        );
    }
}