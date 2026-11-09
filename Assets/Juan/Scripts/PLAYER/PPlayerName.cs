using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PPlayerName : NetworkBehaviour
{
    // ============================
    // REFERENCES
    // ============================

    [Header("References")]
    [SerializeField]
    private TMP_Text nameText;

    // ============================
    // SETTINGS
    // ============================

    [Header("Settings")]
    [SerializeField]
    private bool hideForLocalPlayer = true;

    [SerializeField]
    private string fallbackName = "PLAYER";

    [SerializeField]
    private bool rotateOnlyYAxis = true;

    [SerializeField]
    private float rotationOffsetY = 180f;

    // ============================
    // TEAM COLORS
    // ============================

    [Header("Team Colors")]
    [SerializeField]
    private string teamVsTeamSceneName =
        "ARENA";

    [SerializeField]
    private Color redTeamColor =
        Color.red;

    [SerializeField]
    private Color blueTeamColor =
        Color.blue;

    [SerializeField]
    private Color defaultNameColor =
        Color.white;

    // ============================
    // NETWORK
    // ============================

    [Networked]
    private NetworkString<_16> NetworkPlayerName
    {
        get;
        set;
    }

    // ============================
    // LOCAL
    // ============================

    private Camera localCamera;

    private bool nameSubmitted;

    private string lastDisplayedName =
        "";

    private PTeam team;

    private PTeam.Team lastDisplayedTeam;

    private bool teamColorInitialized;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        nameSubmitted =
            false;

        teamColorInitialized =
            false;

        if (nameText == null)
        {
            nameText =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }

        if (nameText == null)
        {
            Debug.LogError(
                $"PPLAYERNAME: No TMP_Text found | " +
                $"Player: {Object.InputAuthority.PlayerId}"
            );
        }

        team =
            GetComponent<PTeam>();

        if (Object.HasInputAuthority)
        {
            Debug.Log(
                $"LOCAL PLAYER NAME BEFORE SUBMIT | " +
                $"Player: {Object.InputAuthority.PlayerId} | " +
                $"PlayerProfile: '{PlayerProfile.PlayerName}'"
            );

            SubmitLocalPlayerName();
        }

        FindLocalCamera();

        UpdateNameVisual();

        Debug.Log(
            $"PLAYER NAME COMPONENT READY | " +
            $"Player: {Object.InputAuthority.PlayerId} | " +
            $"StateAuthority: {Object.HasStateAuthority} | " +
            $"InputAuthority: {Object.HasInputAuthority} | " +
            $"NetworkName: '{NetworkPlayerName.Value}'"
        );
    }

    // ============================
    // SUBMIT LOCAL PLAYER NAME
    // ============================

    private void SubmitLocalPlayerName()
    {
        if (!Object.HasInputAuthority)
            return;

        if (nameSubmitted)
            return;

        string localName =
            PlayerProfile.PlayerName;

        localName =
            SanitizePlayerName(
                localName
            );

        nameSubmitted =
            true;

        Debug.Log(
            $"SUBMITTING PLAYER NAME | " +
            $"Player: {Object.InputAuthority.PlayerId} | " +
            $"Name: '{localName}'"
        );

        if (Object.HasStateAuthority)
        {
            SetPlayerNameAuthoritative(
                localName
            );

            return;
        }

        RPC_SubmitPlayerName(
            localName
        );
    }

    // ============================
    // SUBMIT NAME RPC
    // ============================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_SubmitPlayerName(
        string playerName)
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            $"HOST RECEIVED PLAYER NAME | " +
            $"Player: {Object.InputAuthority.PlayerId} | " +
            $"Received: '{playerName}'"
        );

        SetPlayerNameAuthoritative(
            playerName
        );
    }

    // ============================
    // SET PLAYER NAME
    // ============================

    private void SetPlayerNameAuthoritative(
        string playerName)
    {
        if (!Object.HasStateAuthority)
            return;

        playerName =
            SanitizePlayerName(
                playerName
            );

        NetworkPlayerName =
            playerName;

        Debug.Log(
            $"PLAYER NAME REGISTERED | " +
            $"Player: {Object.InputAuthority.PlayerId} | " +
            $"Name: '{playerName}'"
        );
    }

    // ============================
    // SANITIZE NAME
    // ============================

    private string SanitizePlayerName(
        string playerName)
    {
        if (string.IsNullOrWhiteSpace(
            playerName))
        {
            return fallbackName;
        }

        playerName =
            playerName.Trim();

        if (playerName.Equals(
                "join",
                System.StringComparison
                    .OrdinalIgnoreCase) ||
            playerName.Equals(
                "host",
                System.StringComparison
                    .OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                $"INVALID PLAYER NAME DETECTED: " +
                $"'{playerName}'. " +
                $"Using fallback."
            );

            return fallbackName;
        }

        if (playerName.Length > 16)
        {
            playerName =
                playerName.Substring(
                    0,
                    16
                );
        }

        return playerName;
    }

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        UpdateNameVisual();
    }

    // ============================
    // FIND LOCAL CAMERA
    // ============================

    private void FindLocalCamera()
    {
        PPlayerName[] players =
            FindObjectsByType<PPlayerName>(
                FindObjectsSortMode.None
            );

        foreach (
            PPlayerName player
            in players)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;

            if (!player.Object
                .HasInputAuthority)
            {
                continue;
            }

            Camera camera =
                player
                    .GetComponentInChildren<
                        Camera
                    >(
                        true
                    );

            if (camera != null)
            {
                localCamera =
                    camera;

                return;
            }
        }

        localCamera =
            Camera.main;
    }

    // ============================
    // UPDATE NAME
    // ============================

    private void UpdateNameVisual()
    {
        if (nameText == null)
            return;

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        // ============================
        // HIDE LOCAL PLAYER NAME
        // ============================

        if (hideForLocalPlayer &&
            Object.HasInputAuthority)
        {
            if (nameText
                .gameObject
                .activeSelf)
            {
                nameText.gameObject
                    .SetActive(
                        false
                    );
            }

            return;
        }

        if (!nameText
            .gameObject
            .activeSelf)
        {
            nameText.gameObject
                .SetActive(
                    true
                );
        }

        // ============================
        // DISPLAY NAME
        // ============================

        string displayName =
            NetworkPlayerName.Value;

        displayName =
            SanitizePlayerName(
                displayName
            );

        if (nameText.text !=
            displayName)
        {
            nameText.text =
                displayName;
        }

        if (lastDisplayedName !=
            displayName)
        {
            lastDisplayedName =
                displayName;

            Debug.Log(
                $"DISPLAYING PLAYER NAME | " +
                $"Player: {Object.InputAuthority.PlayerId} | " +
                $"Name: '{displayName}'"
            );
        }

        // ============================
        // TEAM COLOR
        // ============================

        UpdateTeamColor();
    }

    // ============================
    // UPDATE TEAM COLOR
    // ============================

    private void UpdateTeamColor()
    {
        if (nameText == null)
            return;

        // Team colors are only used
        // in the Team vs Team scene.
        if (SceneManager
                .GetActiveScene()
                .name !=
            teamVsTeamSceneName)
        {
            if (nameText.color !=
                defaultNameColor)
            {
                nameText.color =
                    defaultNameColor;
            }

            teamColorInitialized =
                false;

            return;
        }

        if (team == null)
        {
            team =
                GetComponent<PTeam>();
        }

        if (team == null)
        {
            nameText.color =
                defaultNameColor;

            return;
        }

        if (team.Object == null)
            return;

        if (!team.Object.IsValid)
            return;

        PTeam.Team currentTeam =
            team.CurrentTeam;

        if (teamColorInitialized &&
            currentTeam ==
            lastDisplayedTeam)
        {
            return;
        }

        switch (currentTeam)
        {
            case PTeam.Team.Red:

                nameText.color =
                    redTeamColor;

                break;

            case PTeam.Team.Blue:

                nameText.color =
                    blueTeamColor;

                break;

            default:

                nameText.color =
                    defaultNameColor;

                break;
        }

        lastDisplayedTeam =
            currentTeam;

        teamColorInitialized =
            true;

        Debug.Log(
            $"PLAYER NAME COLOR UPDATED | " +
            $"Player: {Object.InputAuthority.PlayerId} | " +
            $"Team: {currentTeam}"
        );
    }

    // ============================
    // BILLBOARD
    // ============================

    private void LateUpdate()
    {
        if (nameText == null)
            return;

        if (!nameText
            .gameObject
            .activeSelf)
        {
            return;
        }

        if (localCamera == null ||
            !localCamera.enabled)
        {
            FindLocalCamera();

            if (localCamera == null)
                return;
        }

        Vector3 targetPosition =
            localCamera
                .transform
                .position;

        Vector3 textPosition =
            nameText
                .transform
                .position;

        if (rotateOnlyYAxis)
        {
            targetPosition.y =
                textPosition.y;
        }

        Vector3 direction =
            targetPosition -
            textPosition;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction
            );

        Quaternion offsetRotation =
            Quaternion.Euler(
                0f,
                rotationOffsetY,
                0f
            );

        nameText.transform.rotation =
            lookRotation *
            offsetRotation;
    }
}