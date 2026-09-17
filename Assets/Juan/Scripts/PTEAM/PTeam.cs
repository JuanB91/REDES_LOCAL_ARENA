using Fusion;
using UnityEngine;

public class PTeam : NetworkBehaviour
{
    public enum Team
    {
        Red = 0,
        Blue = 1
    }

    [Header("Equipo")]

    [Networked]
    public Team CurrentTeam { get; set; }

    [Networked]
    public int TeamIndex { get; set; }

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        AssignTeam();
    }

    // ============================
    // ASIGNAR EQUIPO
    // ============================

    private void AssignTeam()
    {
        int playerId =
            Object.InputAuthority.PlayerId;

        // Player 1, 3, 5... RED
        // Player 2, 4, 6... BLUE

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
            $"PLAYER {playerId} | " +
            $"TEAM: {CurrentTeam} | " +
            $"TEAM INDEX: {TeamIndex}"
        );
    }

    // ============================
    // CONSULTAS
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