using Fusion;
using UnityEngine;

public class DeathmatchGameManager : NetworkBehaviour
{
    // ============================
    // MATCH SETTINGS
    // ============================

    [Header("Match Settings")]
    [SerializeField]
    private int minimumPlayers = 2;

    // This is the GLOBAL kill limit.
    // The match ends when the sum of all
    // player kills reaches this value.
    [SerializeField]
    private int killsToWin = 20;

    // ============================
    // WARMUP SETTINGS
    // ============================

    [Header("Warmup Settings")]
    [SerializeField]
    private float warmupDuration = 30f;

    // ============================
    // NETWORK STATE
    // ============================

    [Networked]
    public NetworkBool MatchStarted { get; set; }

    [Networked]
    public NetworkBool WarmupActive { get; set; }

    [Networked]
    public NetworkBool GameOver { get; set; }

    [Networked]
    public NetworkBool HasWinner { get; set; }

    [Networked]
    public PlayerRef Winner { get; set; }

    [Networked]
    private TickTimer WarmupTimer { get; set; }

    // ============================
    // PUBLIC PROPERTIES
    // ============================

    public int KillsToWin =>
        killsToWin;

    public int MinimumPlayers =>
        minimumPlayers;

    public float WarmupDuration =>
        warmupDuration;

    // Current GLOBAL kill count.
    public int TotalKills =>
        GetTotalKills();

    // Players can move, look and use weapons
    // during warmup and during the match.
    public bool CanPlay =>
        (WarmupActive || MatchStarted) &&
        !GameOver;

    // Damage is only enabled after
    // the warmup has finished.
    public bool DamageEnabled =>
        MatchStarted &&
        !GameOver;

    public bool IsReady =>
        Object != null &&
        Object.IsValid;

    public bool CanLocalRestart =>
        Runner != null &&
        Runner.IsServer &&
        GameOver;

    // ============================
    // WARMUP TIME
    // ============================

    public float WarmupTimeRemaining
    {
        get
        {
            if (Runner == null)
                return 0f;

            if (!WarmupActive)
                return 0f;

            float? remaining =
                WarmupTimer.RemainingTime(
                    Runner
                );

            if (!remaining.HasValue)
                return 0f;

            return Mathf.Max(
                remaining.Value,
                0f
            );
        }
    }

    public int WarmupSecondsRemaining =>
        Mathf.CeilToInt(
            WarmupTimeRemaining
        );

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            MatchStarted =
                false;

            WarmupActive =
                false;

            GameOver =
                false;

            HasWinner =
                false;

            Winner =
                PlayerRef.None;

            WarmupTimer =
                TickTimer.None;
        }

        Debug.Log(
            $"DEATHMATCH MANAGER READY | " +
            $"STATE AUTHORITY: {Object.HasStateAuthority} | " +
            $"MIN PLAYERS: {minimumPlayers} | " +
            $"GLOBAL KILL LIMIT: {killsToWin} | " +
            $"WARMUP: {warmupDuration}"
        );
    }

    // ============================
    // FIXED UPDATE NETWORK
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (GameOver)
            return;

        if (!MatchStarted)
        {
            HandlePreMatch();

            return;
        }

        CheckVictory();
    }

    // ============================
    // PRE MATCH
    // ============================

    private void HandlePreMatch()
    {
        int playerCount =
            GetActivePlayerCount();

        // ============================
        // WAIT FOR PLAYERS
        // ============================

        if (playerCount <
            minimumPlayers)
        {
            WarmupActive =
                false;

            WarmupTimer =
                TickTimer.None;

            return;
        }

        // ============================
        // START WARMUP
        // ============================

        if (!WarmupActive)
        {
            StartWarmup();

            return;
        }

        // ============================
        // WAIT FOR TIMER
        // ============================

        if (!WarmupTimer.Expired(
            Runner))
        {
            return;
        }

        // ============================
        // START MATCH
        // ============================

        StartMatch();
    }

    // ============================
    // START WARMUP
    // ============================

    private void StartWarmup()
    {
        if (!Object.HasStateAuthority)
            return;

        MatchStarted =
            false;

        WarmupActive =
            true;

        GameOver =
            false;

        HasWinner =
            false;

        Winner =
            PlayerRef.None;

        WarmupTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                warmupDuration
            );

        Debug.Log(
            $"DEATHMATCH WARMUP STARTED | " +
            $"PLAYERS: {GetActivePlayerCount()} | " +
            $"DURATION: {warmupDuration}"
        );
    }

    // ============================
    // START MATCH
    // ============================

    private void StartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        WarmupActive =
            false;

        MatchStarted =
            true;

        GameOver =
            false;

        HasWinner =
            false;

        Winner =
            PlayerRef.None;

        WarmupTimer =
            TickTimer.None;

        Debug.Log(
            $"DEATHMATCH STARTED | " +
            $"PLAYERS: {GetActivePlayerCount()} | " +
            $"GLOBAL KILL LIMIT: {killsToWin}"
        );
    }

    // ============================
    // CHECK VICTORY
    // ============================

    private void CheckVictory()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!MatchStarted)
            return;

        if (GameOver)
            return;

        int totalKills =
            GetTotalKills();

        // The match continues until
        // all player kills combined
        // reach the global kill limit.
        if (totalKills <
            killsToWin)
        {
            return;
        }

        PlayerRef leader =
            GetCurrentLeader();

        if (leader ==
            PlayerRef.None)
        {
            Debug.LogWarning(
                "DEATHMATCH: GLOBAL KILL LIMIT REACHED " +
                "BUT NO LEADER WAS FOUND"
            );

            return;
        }

        Debug.Log(
            $"DEATHMATCH GLOBAL KILL LIMIT REACHED | " +
            $"TOTAL KILLS: {totalKills}/{killsToWin} | " +
            $"LEADER: PLAYER {leader.PlayerId}"
        );

        EndMatch(
            leader
        );
    }

    // ============================
    // TOTAL KILLS
    // ============================

    private int GetTotalKills()
    {
        if (Runner == null)
            return 0;

        int totalKills =
            0;

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

            if (!playerObject.IsValid)
                continue;

            PHealth health =
                playerObject
                    .GetComponent<PHealth>();

            if (health == null)
                continue;

            totalKills +=
                health.Kills;
        }

        return totalKills;
    }

    // ============================
    // GET CURRENT LEADER
    // ============================

    private PlayerRef GetCurrentLeader()
    {
        if (Runner == null)
            return PlayerRef.None;

        PlayerRef bestPlayer =
            PlayerRef.None;

        int bestKills =
            int.MinValue;

        int bestDeaths =
            int.MaxValue;

        int bestPlayerId =
            int.MaxValue;

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

            if (!playerObject.IsValid)
                continue;

            PHealth health =
                playerObject
                    .GetComponent<PHealth>();

            if (health == null)
                continue;

            int playerKills =
                health.Kills;

            int playerDeaths =
                health.Deaths;

            int playerId =
                player.PlayerId;

            // ============================
            // MORE KILLS
            // ============================

            if (playerKills >
                bestKills)
            {
                bestPlayer =
                    player;

                bestKills =
                    playerKills;

                bestDeaths =
                    playerDeaths;

                bestPlayerId =
                    playerId;

                continue;
            }

            // ============================
            // SAME KILLS
            // ============================

            if (playerKills !=
                bestKills)
            {
                continue;
            }

            // ============================
            // FEWER DEATHS
            // ============================

            if (playerDeaths <
                bestDeaths)
            {
                bestPlayer =
                    player;

                bestDeaths =
                    playerDeaths;

                bestPlayerId =
                    playerId;

                continue;
            }

            // ============================
            // SAME DEATHS
            // ============================

            if (playerDeaths !=
                bestDeaths)
            {
                continue;
            }

            // ============================
            // LOWER PLAYER ID
            // ============================

            if (playerId <
                bestPlayerId)
            {
                bestPlayer =
                    player;

                bestPlayerId =
                    playerId;
            }
        }

        return bestPlayer;
    }

    // ============================
    // END MATCH
    // ============================

    private void EndMatch(
        PlayerRef winner)
    {
        if (!Object.HasStateAuthority)
            return;

        if (GameOver)
            return;

        Winner =
            winner;

        HasWinner =
            true;

        WarmupActive =
            false;

        GameOver =
            true;

        Debug.Log(
            $"DEATHMATCH GAME OVER | " +
            $"WINNER: PLAYER {winner.PlayerId} | " +
            $"TOTAL KILLS: {GetTotalKills()}"
        );
    }

    // ============================
    // ACTIVE PLAYER COUNT
    // ============================

    private int GetActivePlayerCount()
    {
        if (Runner == null)
            return 0;

        int count =
            0;

        foreach (
            PlayerRef player
            in Runner.ActivePlayers)
        {
            count++;
        }

        return count;
    }

    // ============================
    // RESTART MATCH
    // ============================

    public void RequestRestart()
    {
        if (Runner == null)
            return;

        if (!Runner.IsServer)
        {
            Debug.LogWarning(
                "DEATHMATCH RESTART BLOCKED | " +
                "ONLY HOST CAN RESTART"
            );

            return;
        }

        if (!Object.HasStateAuthority)
            return;

        RestartMatchAuthoritative();
    }

    // ============================
    // AUTHORITATIVE RESTART
    // ============================

    private void RestartMatchAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            "DEATHMATCH RESTART STARTED"
        );

        MatchStarted =
            false;

        WarmupActive =
            false;

        GameOver =
            false;

        HasWinner =
            false;

        Winner =
            PlayerRef.None;

        WarmupTimer =
            TickTimer.None;

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

            if (!playerObject.IsValid)
                continue;

            PHealth health =
                playerObject
                    .GetComponent<PHealth>();

            if (health == null)
                continue;

            health.RPC_ResetPlayer();
        }

        Debug.Log(
            "DEATHMATCH RESTART COMPLETE"
        );
    }

    // ============================
    // WINNER CHECK
    // ============================

    public bool IsWinner(
        PlayerRef player)
    {
        if (!HasWinner)
            return false;

        return
            Winner == player;
    }
}