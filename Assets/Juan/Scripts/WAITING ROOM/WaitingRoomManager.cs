using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomManager : NetworkBehaviour
{
    // ============================
    // COUNTDOWN
    // ============================

    [Header("Countdown")]
    [SerializeField]
    private float countdownDuration = 30f;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string teamVsTeamSceneName =
        "ARENA";

    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // UI
    // ============================

    [Header("UI")]
    [SerializeField]
    private TMP_Text waitingText;

    [SerializeField]
    private TMP_Text countdownText;

    [SerializeField]
    private Button startMatchButton;

    // ============================
    // GAME MODE PANELS
    // ============================

    [Header("Game Mode Panels")]
    [SerializeField]
    private GameObject redTeamPanel;

    [SerializeField]
    private GameObject blueTeamPanel;

    [SerializeField]
    private GameObject deathmatchPanel;

    // ============================
    // TEAM VS TEAM SLOTS
    // ============================

    [Header("RED Slots")]
    [SerializeField]
    private TMP_Text[] redSlots;

    [Header("BLUE Slots")]
    [SerializeField]
    private TMP_Text[] blueSlots;

    // ============================
    // DEATHMATCH SLOTS
    // ============================

    [Header("DEATHMATCH Slots")]
    [SerializeField]
    private TMP_Text[] deathmatchSlots;

    // ============================
    // NETWORK STATE
    // ============================

    [Networked]
    public int RedPlayerCount { get; set; }

    [Networked]
    public int BluePlayerCount { get; set; }

    [Networked]
    public NetworkBool CountdownStarted { get; set; }

    [Networked]
    private TickTimer CountdownTimer { get; set; }

    [Networked]
    private NetworkBool SceneChangeStarted { get; set; }

    // ============================
    // NETWORK GAME MODE
    // ============================

    [Networked]
    public int NetworkGameMode { get; set; }

    // ============================
    // PLAYER NAMES
    // ============================

    [Networked]
    [Capacity(16)]
    private NetworkDictionary<
        PlayerRef,
        NetworkString<_16>>
        PlayerNames => default;

    // ============================
    // LOCAL STATE
    // ============================

    private bool localNameSubmitted;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        localNameSubmitted =
            false;

        // ============================
        // HOST INITIALIZATION
        // ============================

        if (Object.HasStateAuthority)
        {
            RedPlayerCount =
                0;

            BluePlayerCount =
                0;

            CountdownStarted =
                false;

            CountdownTimer =
                TickTimer.None;

            SceneChangeStarted =
                false;

            // ============================
            // SYNC SELECTED GAME MODE
            // ============================

            NetworkGameMode =
                (int)GameSettings.SelectedGameMode;

            Debug.Log(
                $"WAITING ROOM HOST INITIALIZED | " +
                $"GAME MODE: {GetGameModeName()}"
            );
        }

        UpdateGameModePanels();

        UpdateUI();

        Debug.Log(
            $"WAITING ROOM MANAGER READY | " +
            $"STATE AUTHORITY: " +
            $"{Object.HasStateAuthority} | " +
            $"SCENE AUTHORITY: " +
            $"{Runner.IsSceneAuthority} | " +
            $"LOCAL PLAYER: " +
            $"{Runner.LocalPlayer.PlayerId}"
        );
    }

    // ============================
    // UPDATE
    // ============================

    private void Update()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (Runner == null)
            return;

        if (!Runner.IsRunning)
            return;

        SubmitLocalPlayerName();
    }

    // ============================
    // FIXED UPDATE NETWORK
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        UpdatePlayerCounts();

        UpdateCountdown();
    }

    // ============================
    // GAME MODE HELPERS
    // ============================

    private bool IsDeathmatch()
    {
        return NetworkGameMode ==
            (int)GameSettings
                .GameMode
                .Deathmatch;
    }

    private string GetGameModeName()
    {
        return IsDeathmatch()
            ? "DEATHMATCH"
            : "TEAM VS TEAM";
    }

    // ============================
    // SUBMIT LOCAL PLAYER NAME
    // ============================

    private void SubmitLocalPlayerName()
    {
        if (localNameSubmitted)
            return;

        if (Runner == null)
            return;

        if (!Runner.IsRunning)
            return;

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        PlayerRef localPlayer =
            Runner.LocalPlayer;

        if (localPlayer ==
            PlayerRef.None)
        {
            return;
        }

        string playerName =
            PlayerProfile.PlayerName;

        if (string.IsNullOrWhiteSpace(
            playerName))
        {
            playerName =
                $"PLAYER {localPlayer.PlayerId}";
        }

        playerName =
            playerName.Trim();

        if (playerName.Length > 16)
        {
            playerName =
                playerName.Substring(
                    0,
                    16
                );
        }

        Debug.Log(
            $"SENDING PLAYER NAME | " +
            $"PlayerId: {localPlayer.PlayerId} | " +
            $"Name: {playerName}"
        );

        RPC_SubmitPlayerName(
            localPlayer,
            playerName
        );

        localNameSubmitted =
            true;

        Debug.Log(
            $"PLAYER NAME SENT | " +
            $"PlayerId: {localPlayer.PlayerId} | " +
            $"Name: {playerName}"
        );
    }

    // ============================
    // PLAYER NAME RPC
    // ============================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_SubmitPlayerName(
        PlayerRef player,
        NetworkString<_16> playerName)
    {
        if (!Object.HasStateAuthority)
            return;

        if (player ==
            PlayerRef.None)
        {
            return;
        }

        string validatedName =
            playerName.Value;

        if (string.IsNullOrWhiteSpace(
            validatedName))
        {
            validatedName =
                $"PLAYER {player.PlayerId}";
        }

        validatedName =
            validatedName.Trim();

        if (validatedName.Length > 16)
        {
            validatedName =
                validatedName.Substring(
                    0,
                    16
                );
        }

        PlayerNames.Set(
            player,
            validatedName
        );

        Debug.Log(
            $"PLAYER NAME REGISTERED BY HOST | " +
            $"PlayerId: {player.PlayerId} | " +
            $"Name: {validatedName}"
        );
    }

    // ============================
    // GET PLAYER NAME
    // ============================

    private string GetPlayerName(
        PlayerRef player)
    {
        if (PlayerNames.TryGet(
            player,
            out NetworkString<_16> networkName))
        {
            string name =
                networkName.Value;

            if (!string.IsNullOrWhiteSpace(
                name))
            {
                return name;
            }
        }

        return
            $"PLAYER {player.PlayerId}";
    }

    // ============================
    // UPDATE PLAYER COUNTS
    // ============================

    private void UpdatePlayerCounts()
    {
        if (!Object.HasStateAuthority)
            return;

        if (Runner == null)
            return;

        int redCount =
            0;

        int blueCount =
            0;

        foreach (
            PlayerRef player
            in Runner.ActivePlayers)
        {
            if (player.PlayerId % 2 != 0)
            {
                redCount++;
            }
            else
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
    // CAN START MATCH
    // ============================

    private bool CanStartMatch()
    {
        return
            RedPlayerCount >= 1 &&
            BluePlayerCount >= 1 &&
            !SceneChangeStarted;
    }

    // ============================
    // UPDATE COUNTDOWN
    // ============================

    private void UpdateCountdown()
    {
        if (!Object.HasStateAuthority)
            return;

        if (SceneChangeStarted)
            return;

        bool canStart =
            CanStartMatch();

        if (!canStart)
        {
            if (CountdownStarted)
            {
                CountdownStarted =
                    false;

                CountdownTimer =
                    TickTimer.None;

                Debug.Log(
                    "COUNTDOWN CANCELLED | " +
                    "WAITING FOR PLAYERS"
                );
            }

            return;
        }

        if (!CountdownStarted)
        {
            CountdownStarted =
                true;

            CountdownTimer =
                TickTimer.CreateFromSeconds(
                    Runner,
                    countdownDuration
                );

            Debug.Log(
                $"COUNTDOWN STARTED | " +
                $"{countdownDuration} SECONDS | " +
                $"GAME MODE: {GetGameModeName()}"
            );

            return;
        }

        if (CountdownTimer.Expired(
            Runner))
        {
            StartMatch(
                "COUNTDOWN FINISHED"
            );
        }
    }

    // ============================
    // REQUEST START MATCH
    // ============================

    public void RequestStartMatch()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
        {
            Debug.Log(
                "START MATCH BLOCKED | " +
                "NOT STATE AUTHORITY"
            );

            return;
        }

        if (!Runner.IsSceneAuthority)
        {
            Debug.Log(
                "START MATCH BLOCKED | " +
                "NOT SCENE AUTHORITY"
            );

            return;
        }

        if (SceneChangeStarted)
            return;

        if (!CanStartMatch())
        {
            Debug.Log(
                "START MATCH BLOCKED | " +
                "NOT ENOUGH PLAYERS"
            );

            return;
        }

        Debug.Log(
            $"HOST PRESSED START MATCH | " +
            $"GAME MODE: {GetGameModeName()}"
        );

        StartMatch(
            "MANUAL START"
        );
    }

    // ============================
    // START MATCH
    // ============================

    private void StartMatch(
        string reason)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!Runner.IsSceneAuthority)
            return;

        if (SceneChangeStarted)
            return;

        if (!CanStartMatch())
            return;

        CountdownStarted =
            false;

        CountdownTimer =
            TickTimer.None;

        SceneChangeStarted =
            true;

        string selectedScene =
            GetSelectedSceneName();

        Debug.Log(
            $"{reason} | " +
            $"GAME MODE: {GetGameModeName()} | " +
            $"LOADING SCENE: {selectedScene}"
        );

        LoadSelectedGameScene();
    }

    // ============================
    // GET SELECTED SCENE
    // ============================

    private string GetSelectedSceneName()
    {
        if (IsDeathmatch())
        {
            return deathmatchSceneName;
        }

        return teamVsTeamSceneName;
    }

    // ============================
    // LOAD SELECTED GAME SCENE
    // ============================

    private void LoadSelectedGameScene()
    {
        if (Runner == null)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (!Runner.IsSceneAuthority)
            return;

        string selectedSceneName =
            GetSelectedSceneName();

        if (string.IsNullOrWhiteSpace(
            selectedSceneName))
        {
            Debug.LogError(
                "WAITING ROOM: " +
                "SELECTED SCENE NAME IS EMPTY"
            );

            SceneChangeStarted =
                false;

            return;
        }

        int sceneBuildIndex =
            SceneUtility
                .GetBuildIndexByScenePath(
                    $"Assets/Scenes/" +
                    $"{selectedSceneName}.unity"
                );

        if (sceneBuildIndex < 0)
        {
            Debug.LogError(
                $"WAITING ROOM: " +
                $"SCENE {selectedSceneName} " +
                $"NOT FOUND IN BUILD PROFILE"
            );

            SceneChangeStarted =
                false;

            return;
        }

        SceneRef selectedScene =
            SceneRef.FromIndex(
                sceneBuildIndex
            );

        Debug.Log(
            $"HOST LOADING NETWORK SCENE | " +
            $"GAME MODE: {GetGameModeName()} | " +
            $"SCENE: {selectedSceneName} | " +
            $"BUILD INDEX: {sceneBuildIndex}"
        );

        Runner.LoadScene(
            selectedScene,
            LoadSceneMode.Single
        );
    }

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        UpdateUI();
    }

    // ============================
    // UPDATE UI
    // ============================

    private void UpdateUI()
    {
        if (Runner == null)
            return;

        UpdateGameModePanels();

        UpdateStatusText();

        UpdateCountdownText();

        UpdatePlayerSlots();

        UpdateStartButton();
    }

    // ============================
    // GAME MODE PANELS
    // ============================

    private void UpdateGameModePanels()
    {
        bool isDeathmatch =
            IsDeathmatch();

        if (redTeamPanel != null)
        {
            redTeamPanel.SetActive(
                !isDeathmatch
            );
        }

        if (blueTeamPanel != null)
        {
            blueTeamPanel.SetActive(
                !isDeathmatch
            );
        }

        if (deathmatchPanel != null)
        {
            deathmatchPanel.SetActive(
                isDeathmatch
            );
        }
    }

    // ============================
    // STATUS TEXT
    // ============================

    private void UpdateStatusText()
    {
        if (waitingText == null)
            return;

        if (SceneChangeStarted)
        {
            waitingText.text =
                "STARTING MATCH...";

            return;
        }

        if (RedPlayerCount >= 1 &&
            BluePlayerCount >= 1)
        {
            waitingText.text =
                "WAITING";
        }
        else
        {
            waitingText.text =
                "WAITING FOR PLAYERS...";
        }
    }

    // ============================
    // COUNTDOWN TEXT
    // ============================

    private void UpdateCountdownText()
    {
        if (countdownText == null)
            return;

        if (SceneChangeStarted)
        {
            countdownText.text =
                "STARTING...";

            return;
        }

        if (!CountdownStarted)
        {
            countdownText.text =
                "START IN --:--";

            return;
        }

        float? remaining =
            CountdownTimer
                .RemainingTime(
                    Runner
                );

        if (!remaining.HasValue)
        {
            countdownText.text =
                "START IN 00:00";

            return;
        }

        int seconds =
            Mathf.CeilToInt(
                remaining.Value
            );

        if (seconds < 0)
        {
            seconds =
                0;
        }

        int minutes =
            seconds / 60;

        int displaySeconds =
            seconds % 60;

        countdownText.text =
            $"START IN " +
            $"{minutes:00}:" +
            $"{displaySeconds:00}";
    }

    // ============================
    // START BUTTON
    // ============================

    private void UpdateStartButton()
    {
        if (startMatchButton == null)
            return;

        if (Runner == null)
        {
            startMatchButton
                .gameObject
                .SetActive(
                    false
                );

            return;
        }

        bool isHost =
            Object != null &&
            Object.IsValid &&
            Object.HasStateAuthority &&
            Runner.IsSceneAuthority;

        if (!isHost)
        {
            startMatchButton
                .gameObject
                .SetActive(
                    false
                );

            return;
        }

        bool shouldShow =
            RedPlayerCount >= 1 &&
            BluePlayerCount >= 1 &&
            !SceneChangeStarted;

        startMatchButton
            .gameObject
            .SetActive(
                shouldShow
            );

        startMatchButton.interactable =
            shouldShow;
    }

    // ============================
    // PLAYER SLOTS
    // ============================

    private void UpdatePlayerSlots()
    {
        if (Runner == null)
            return;

        ClearSlots(
            redSlots
        );

        ClearSlots(
            blueSlots
        );

        ClearSlots(
            deathmatchSlots
        );

        // ============================
        // DEATHMATCH
        // ============================

        if (IsDeathmatch())
        {
            int deathmatchIndex =
                0;

            foreach (
                PlayerRef player
                in Runner.ActivePlayers)
            {
                if (deathmatchSlots == null)
                    break;

                if (deathmatchIndex >=
                    deathmatchSlots.Length)
                {
                    break;
                }

                TMP_Text slot =
                    deathmatchSlots[
                        deathmatchIndex
                    ];

                if (slot != null)
                {
                    slot.text =
                        GetPlayerName(
                            player
                        );
                }

                deathmatchIndex++;
            }

            return;
        }

        // ============================
        // TEAM VS TEAM
        // ============================

        int redIndex =
            0;

        int blueIndex =
            0;

        foreach (
            PlayerRef player
            in Runner.ActivePlayers)
        {
            string displayName =
                GetPlayerName(
                    player
                );

            if (player.PlayerId % 2 != 0)
            {
                if (redSlots != null &&
                    redIndex <
                    redSlots.Length)
                {
                    if (redSlots[redIndex] !=
                        null)
                    {
                        redSlots[
                            redIndex
                        ].text =
                            displayName;
                    }
                }

                redIndex++;
            }
            else
            {
                if (blueSlots != null &&
                    blueIndex <
                    blueSlots.Length)
                {
                    if (blueSlots[blueIndex] !=
                        null)
                    {
                        blueSlots[
                            blueIndex
                        ].text =
                            displayName;
                    }
                }

                blueIndex++;
            }
        }
    }

    // ============================
    // CLEAR SLOTS
    // ============================

    private void ClearSlots(
        TMP_Text[] slots)
    {
        if (slots == null)
            return;

        for (
            int i = 0;
            i < slots.Length;
            i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].text =
                "";
        }
    }
}