using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomManager : NetworkBehaviour
{
    // ============================
    // CONFIG
    // ============================

    [Header("Countdown")]
    [SerializeField]
    private float countdownDuration = 30f;

    [Header("Arena Scene")]
    [SerializeField]
    private string arenaSceneName = "ARENA";

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

    [Header("RED Slots")]
    [SerializeField]
    private TMP_Text[] redSlots;

    [Header("BLUE Slots")]
    [SerializeField]
    private TMP_Text[] blueSlots;

    // ============================
    // NETWORK
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

    [Networked]
    [Capacity(16)]
    private NetworkDictionary<PlayerRef, NetworkString<_16>>
        PlayerNames => default;

    // ============================
    // LOCAL
    // ============================

    private bool localNameSubmitted = false;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            RedPlayerCount = 0;
            BluePlayerCount = 0;

            CountdownStarted = false;
            CountdownTimer = TickTimer.None;

            SceneChangeStarted = false;
        }

        localNameSubmitted = false;

        UpdateUI();

        Debug.Log(
            $"WAITING ROOM MANAGER READY | " +
            $"SCENE AUTHORITY: {Runner.IsSceneAuthority}"
        );
    }

    // ============================
    // UNITY UPDATE
    // ============================

    private void Update()
    {
        // Cada cliente intenta enviar
        // SU propio nombre.
        SubmitLocalPlayerName();
    }

    // ============================
    // NETWORK UPDATE
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        UpdatePlayerCounts();

        UpdateCountdown();
    }

    // ============================
    // SUBMIT LOCAL NAME
    // ============================

    private void SubmitLocalPlayerName()
    {
        if (localNameSubmitted)
            return;

        if (Runner == null)
            return;

        if (Object == null ||
            !Object.IsValid)
            return;

        PlayerRef localPlayer =
            Runner.LocalPlayer;

        if (localPlayer == PlayerRef.None)
            return;

        string playerName =
            PlayerProfile.PlayerName;

        if (string.IsNullOrWhiteSpace(playerName))
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

        PlayerNames.Set(
            player,
            playerName
        );

        Debug.Log(
            $"PLAYER NAME REGISTERED | " +
            $"PlayerId: {player.PlayerId} | " +
            $"Name: {playerName.Value}"
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
                name
            ))
            {
                return name;
            }
        }

        return
            $"PLAYER {player.PlayerId}";
    }

    // ============================
    // PLAYER COUNTS
    // ============================

    private void UpdatePlayerCounts()
    {
        int redCount = 0;
        int blueCount = 0;

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
    // COUNTDOWN
    // ============================

    private void UpdateCountdown()
    {
        if (SceneChangeStarted)
            return;

        bool canStart =
            CanStartMatch();

        // ============================
        // WAITING FOR PLAYERS
        // ============================

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

        // ============================
        // START COUNTDOWN
        // ============================

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
                $"RED: {RedPlayerCount} | " +
                $"BLUE: {BluePlayerCount}"
            );

            return;
        }

        // ============================
        // COUNTDOWN FINISHED
        // ============================

        if (CountdownTimer.Expired(
            Runner
        ))
        {
            StartMatch(
                "COUNTDOWN FINISHED"
            );
        }
    }

    // ============================
    // START MATCH BUTTON
    // ============================

    public void RequestStartMatch()
    {
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
                "NEED AT LEAST 1 RED AND 1 BLUE"
            );

            return;
        }

        Debug.Log(
            "OWNER PRESSED START MATCH"
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

        Debug.Log(
            $"{reason} | " +
            $"LOADING ARENA | " +
            $"RED: {RedPlayerCount} | " +
            $"BLUE: {BluePlayerCount}"
        );

        LoadArena();
    }

    // ============================
    // LOAD ARENA
    // ============================

    private void LoadArena()
    {
        if (!Runner.IsSceneAuthority)
        {
            Debug.Log(
                "WAITING ROOM: " +
                "THIS CLIENT IS NOT SCENE AUTHORITY"
            );

            return;
        }

        int arenaBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(
                $"Assets/Scenes/{arenaSceneName}.unity"
            );

        if (arenaBuildIndex < 0)
        {
            Debug.LogError(
                $"WAITING ROOM: " +
                $"SCENE {arenaSceneName} " +
                $"NOT FOUND IN BUILD PROFILE"
            );

            return;
        }

        SceneRef arenaScene =
            SceneRef.FromIndex(
                arenaBuildIndex
            );

        Debug.Log(
            $"SCENE AUTHORITY LOADING | " +
            $"{arenaSceneName} | " +
            $"Build Index: {arenaBuildIndex}"
        );

        Runner.LoadScene(
            arenaScene,
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
    // UI
    // ============================

    private void UpdateUI()
    {
        UpdateStatusText();

        UpdateCountdownText();

        UpdatePlayerSlots();

        UpdateStartButton();
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
            CountdownTimer.RemainingTime(
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
            seconds = 0;
        }

        countdownText.text =
            $"START IN 00:{seconds:00}";
    }

    // ============================
    // START BUTTON
    // ============================

    private void UpdateStartButton()
    {
        if (startMatchButton == null)
            return;

        // Solo el dueño puede verlo.
        if (!Runner.IsSceneAuthority)
        {
            startMatchButton
                .gameObject
                .SetActive(false);

            return;
        }

        // Solo aparece cuando
        // existen ambos equipos.
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
        ClearSlots(
            redSlots
        );

        ClearSlots(
            blueSlots
        );

        int redIndex = 0;
        int blueIndex = 0;

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
                    redIndex < redSlots.Length)
                {
                    if (redSlots[redIndex] != null)
                    {
                        redSlots[redIndex].text =
                            displayName;
                    }
                }

                redIndex++;
            }
            else
            {
                if (blueSlots != null &&
                    blueIndex < blueSlots.Length)
                {
                    if (blueSlots[blueIndex] != null)
                    {
                        blueSlots[blueIndex].text =
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
            if (slots[i] != null)
            {
                slots[i].text =
                    "";
            }
        }
    }
}