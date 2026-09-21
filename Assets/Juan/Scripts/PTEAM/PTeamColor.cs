using Fusion;
using UnityEngine;

public class PTeamColor : NetworkBehaviour
{
    // ============================
    // REFERENCES
    // ============================

    [Header("Referencias")]
    [SerializeField]
    private PTeam team;

    [SerializeField]
    private Renderer capsuleRenderer;

    // ============================
    // COLORS
    // ============================

    [Header("Colores")]
    [SerializeField]
    private Color redTeamColor =
        Color.red;

    [SerializeField]
    private Color blueTeamColor =
        Color.blue;

    // ============================
    // RUNTIME
    // ============================

    private Material runtimeMaterial;

    private PTeam.Team lastTeam;

    private bool initialized;

    // ============================
    // AWAKE
    // ============================

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

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        UpdateTeamColor();

        if (team != null)
        {
            lastTeam =
                team.CurrentTeam;
        }

        initialized =
            true;
    }

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        if (team == null)
            return;

        if (!initialized ||
            team.CurrentTeam != lastTeam)
        {
            UpdateTeamColor();

            lastTeam =
                team.CurrentTeam;

            initialized =
                true;
        }
    }

    // ============================
    // UPDATE COLOR
    // ============================

    private void UpdateTeamColor()
    {
        if (team == null)
        {
            Debug.LogWarning(
                "PTEAM COLOR: PTeam no encontrado."
            );

            return;
        }

        if (capsuleRenderer == null)
        {
            Debug.LogWarning(
                "PTEAM COLOR: Renderer de la cápsula no asignado."
            );

            return;
        }

        if (runtimeMaterial == null)
        {
            runtimeMaterial =
                capsuleRenderer.material;
        }

        switch (team.CurrentTeam)
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
    }
}