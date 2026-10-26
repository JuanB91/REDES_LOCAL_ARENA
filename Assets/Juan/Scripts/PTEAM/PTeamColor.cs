using Fusion;
using UnityEngine;

public class PTeamColor : NetworkBehaviour
{
 
    [Header("Referencias")]

    [SerializeField]
    private PTeam team;

    [SerializeField]
    private Renderer capsuleRenderer;


    [Header("Colores")]

    [SerializeField]
    private Color redTeamColor =
        Color.red;

    [SerializeField]
    private Color blueTeamColor =
        Color.blue;


    private Material runtimeMaterial;

    private PTeam.Team lastTeam;

    private bool initialized;


    private void Awake()
    {
        if (team == null)
        {
            team =
                GetComponent<PTeam>();
        }

        if (capsuleRenderer != null)
        {
            runtimeMaterial =
                capsuleRenderer.material;
        }
    }


    public override void Spawned()
    {
        initialized =
            false;

        if (team == null)
        {
            team =
                GetComponent<PTeam>();
        }

        TryUpdateTeamColor();
    }


    public override void Render()
    {
        if (!CanReadTeam())
            return;

        PTeam.Team currentTeam =
            team.CurrentTeam;

        if (!initialized ||
            currentTeam != lastTeam)
        {
            TryUpdateTeamColor();
        }
    }


    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        initialized =
            false;
    }


    private bool CanReadTeam()
    {
        if (team == null)
            return false;

        if (team.Object == null)
            return false;

        if (!team.Object.IsValid)
            return false;

        if (Object == null)
            return false;

        if (!Object.IsValid)
            return false;

        return true;
    }


    private void TryUpdateTeamColor()
    {
        if (!CanReadTeam())
            return;

        if (capsuleRenderer == null)
        {
            Debug.LogWarning(
                $"PTEAM COLOR: " +
                $"Capsule Renderer no asignado | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId}"
            );

            return;
        }

        if (runtimeMaterial == null)
        {
            runtimeMaterial =
                capsuleRenderer.material;
        }

        PTeam.Team currentTeam =
            team.CurrentTeam;

        switch (currentTeam)
        {
            case PTeam.Team.Red:

                runtimeMaterial.color =
                    redTeamColor;

                break;

            case PTeam.Team.Blue:

                runtimeMaterial.color =
                    blueTeamColor;

                break;
        }

        lastTeam =
            currentTeam;

        initialized =
            true;

        Debug.Log(
            $"TEAM COLOR UPDATED | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId} | " +
            $"Team: {currentTeam}"
        );
    }
}