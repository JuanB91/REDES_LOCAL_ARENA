using Fusion;
using UnityEngine;

public class PTeam : NetworkBehaviour
{
    public enum Team
    {
        Red = 0,
        Blue = 1
    }

    // ============================
    // NETWORK DATA
    // ============================

    [Networked]
    public Team CurrentTeam { get; set; }

    [Networked]
    public int TeamIndex { get; set; }

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        AssignTeam();
    }

    // ============================
    // ASSIGN TEAM
    // ============================

    private void AssignTeam()
    {
        int playerId =
            Object.InputAuthority.PlayerId;

        // Odd PlayerIds go to RED.
        if (playerId % 2 != 0)
        {
            CurrentTeam =
                Team.Red;

            TeamIndex =
                (playerId - 1) / 2;
        }

        // Even PlayerIds go to BLUE.
        else
        {
            CurrentTeam =
                Team.Blue;

            TeamIndex =
                (playerId / 2) - 1;
        }

        Debug.Log(
            $"TEAM ASSIGNED | " +
            $"Player: {playerId} | " +
            $"Team: {CurrentTeam} | " +
            $"TeamIndex: {TeamIndex}"
        );
    }

    // ============================
    // HELPERS
    // ============================

    public bool IsRed()
    {
        return CurrentTeam ==
               Team.Red;
    }

    public bool IsBlue()
    {
        return CurrentTeam ==
               Team.Blue;
    }
}