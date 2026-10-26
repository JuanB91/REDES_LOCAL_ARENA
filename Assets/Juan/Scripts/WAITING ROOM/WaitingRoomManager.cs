using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomManager : NetworkBehaviour
{
    
    [Header("Countdown")]
    [SerializeField]
    private float countdownDuration = 30f;

    [Header("Arena Scene")]
    [SerializeField]
    private string arenaSceneName = "ARENA";

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
    private NetworkDictionary<
        PlayerRef,
        NetworkString<_16>>
        PlayerNames => default;

    
    private bool localNameSubmitted;


    public override void Spawned()
    {
        localNameSubmitted =
            false;


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

            Debug.Log(
                "WAITING ROOM HOST INITIALIZED"
            );
        }

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


    public override void FixedUpdateNetwork()
    {

        if (!Object.HasStateAuthority)
            return;

        UpdatePlayerCounts();

        UpdateCountdown();
    }


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


    private bool CanStartMatch()
    {
        return
            RedPlayerCount >= 1 &&
            BluePlayerCount >= 1 &&
            !SceneChangeStarted;
    }


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
                $"RED: {RedPlayerCount} | " +
                $"BLUE: {BluePlayerCount}"
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
                "NEED AT LEAST 1 RED AND 1 BLUE"
            );

            return;
        }

        Debug.Log(
            "HOST PRESSED START MATCH"
        );

        StartMatch(
            "MANUAL START"
        );
    }


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


    private void LoadArena()
    {
        if (Runner == null)
            return;


        if (!Object.HasStateAuthority)
        {
            Debug.Log(
                "WAITING ROOM: " +
                "NOT STATE AUTHORITY"
            );

            return;
        }

        if (!Runner.IsSceneAuthority)
        {
            Debug.Log(
                "WAITING ROOM: " +
                "THIS CLIENT IS NOT SCENE AUTHORITY"
            );

            return;
        }


        int arenaBuildIndex =
            SceneUtility
                .GetBuildIndexByScenePath(
                    $"Assets/Scenes/" +
                    $"{arenaSceneName}.unity"
                );

        if (arenaBuildIndex < 0)
        {
            Debug.LogError(
                $"WAITING ROOM: " +
                $"SCENE {arenaSceneName} " +
                $"NOT FOUND IN BUILD PROFILE"
            );

            SceneChangeStarted =
                false;

            return;
        }

        SceneRef arenaScene =
            SceneRef.FromIndex(
                arenaBuildIndex
            );

        Debug.Log(
            $"HOST LOADING NETWORK SCENE | " +
            $"{arenaSceneName} | " +
            $"Build Index: {arenaBuildIndex}"
        );


        Runner.LoadScene(
            arenaScene,
            LoadSceneMode.Single
        );
    }


    public override void Render()
    {
        UpdateUI();
    }


    private void UpdateUI()
    {
        if (Runner == null)
            return;

        UpdateStatusText();

        UpdateCountdownText();

        UpdatePlayerSlots();

        UpdateStartButton();
    }


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