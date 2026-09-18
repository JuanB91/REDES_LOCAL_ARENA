using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            CountdownTimer =
                TickTimer.None;

            SceneChangeStarted =
                false;
        }

        UpdateUI();

        Debug.Log(
            "WAITING ROOM MANAGER READY"
        );
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
    // COUNTDOWN
    // ============================

    private void UpdateCountdown()
    {
        if (SceneChangeStarted)
            return;

        bool hasRed =
            RedPlayerCount >= 1;

        bool hasBlue =
            BluePlayerCount >= 1;

        bool canStart =
            hasRed &&
            hasBlue;

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

        if (CountdownTimer.Expired(Runner))
        {
            CountdownTimer =
                TickTimer.None;

            SceneChangeStarted =
                true;

            Debug.Log(
                "COUNTDOWN FINISHED | " +
                "LOADING ARENA"
            );

            LoadArena();
        }
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
            seconds = 0;
        }

        countdownText.text =
            $"START IN 00:{seconds:00}";
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
            if (player.PlayerId % 2 != 0)
            {
                if (redSlots != null &&
                    redIndex < redSlots.Length)
                {
                    if (redSlots[redIndex] != null)
                    {
                        redSlots[redIndex].text =
                            $"PLAYER {player.PlayerId}";
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
                            $"PLAYER {player.PlayerId}";
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