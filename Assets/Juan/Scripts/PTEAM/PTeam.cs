using Fusion;
using UnityEngine;

public class PTeam : NetworkBehaviour
{

    public enum Team
    {
        Red = 0,
        Blue = 1
    }


    [Networked]
    public Team CurrentTeam { get; set; }

    [Networked]
    public int TeamIndex { get; set; }


    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.Log(
                $"PTEAM READY | CLIENT COPY | " +
                $"Player: {Object.InputAuthority.PlayerId}"
            );

            return;
        }

        AssignTeamAuthoritative();
    }


    private void AssignTeamAuthoritative()
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerRef player =
            Object.InputAuthority;

        if (player == PlayerRef.None)
        {
            Debug.LogError(
                "PTEAM: Player has no InputAuthority."
            );

            return;
        }

        int playerId =
            player.PlayerId;

        if (playerId % 2 != 0)
        {
            CurrentTeam =
                Team.Red;

            TeamIndex =
                (playerId - 1) / 2;
        }

        else
        {
            CurrentTeam =
                Team.Blue;

            TeamIndex =
                (playerId / 2) - 1;
        }

        Debug.Log(
            $"TEAM ASSIGNED BY HOST | " +
            $"Player: {playerId} | " +
            $"Team: {CurrentTeam} | " +
            $"TeamIndex: {TeamIndex}"
        );
    }


    public bool IsRed()
    {
        if (Object == null)
            return false;

        if (!Object.IsValid)
            return false;

        return CurrentTeam ==
               Team.Red;
    }

    public bool IsBlue()
    {
        if (Object == null)
            return false;

        if (!Object.IsValid)
            return false;

        return CurrentTeam ==
               Team.Blue;
    }
}