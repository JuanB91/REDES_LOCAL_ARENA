using System.Collections.Generic;
using System.Reflection;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathmatchScoreboard : MonoBehaviour
{
    // ============================
    // UI
    // ============================

    [Header("UI")]
    [SerializeField]
    private GameObject scoreboardPanel;

    [SerializeField]
    private TMP_Text scoreboardText;

    // ============================
    // GAME SCENE
    // ============================

    [Header("Game Scene")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // INPUT
    // ============================

    [Header("Input")]
    [SerializeField]
    private KeyCode scoreboardKey =
        KeyCode.Tab;

    // ============================
    // SETTINGS
    // ============================

    [Header("Settings")]
    [SerializeField]
    private bool sortByKills =
        true;

    [SerializeField]
    private int maxPlayerNameLength =
        20;

    [Header("Column Positions")]
    [Range(0f, 100f)]
    [SerializeField]
    private float killsColumnPosition =
        65f;

    [Range(0f, 100f)]
    [SerializeField]
    private float deathsColumnPosition =
        82f;

    // ============================
    // REFERENCES
    // ============================

    private NetworkRunner runner;

    // ============================
    // PLAYER DATA
    // ============================

    private class ScoreboardPlayerData
    {
        public int PlayerId;

        public string PlayerName;

        public int Kills;

        public int Deaths;
    }

    private readonly List<ScoreboardPlayerData>
        playerData =
            new List<ScoreboardPlayerData>();

    // ============================
    // START
    // ============================

    private void Start()
    {
        FindRunner();

        HideScoreboard();
    }

    // ============================
    // UPDATE
    // ============================

    private void Update()
    {
        if (!IsDeathmatch())
        {
            HideScoreboard();

            return;
        }

        if (Input.GetKey(
            scoreboardKey))
        {
            ShowScoreboard();
        }
        else
        {
            HideScoreboard();
        }
    }

    // ============================
    // IS DEATHMATCH
    // ============================

    private bool IsDeathmatch()
    {
        return
            SceneManager
                .GetActiveScene()
                .name ==
            deathmatchSceneName;
    }

    // ============================
    // SHOW SCOREBOARD
    // ============================

    private void ShowScoreboard()
    {
        if (scoreboardPanel != null &&
            !scoreboardPanel.activeSelf)
        {
            scoreboardPanel.SetActive(
                true
            );
        }

        UpdateScoreboard();
    }

    // ============================
    // HIDE SCOREBOARD
    // ============================

    private void HideScoreboard()
    {
        if (scoreboardPanel == null)
            return;

        if (!scoreboardPanel.activeSelf)
            return;

        scoreboardPanel.SetActive(
            false
        );
    }

    // ============================
    // UPDATE SCOREBOARD
    // ============================

    private void UpdateScoreboard()
    {
        if (scoreboardText == null)
            return;

        if (runner == null)
        {
            FindRunner();
        }

        if (runner == null)
            return;

        playerData.Clear();

        // ============================
        // GET ALL ACTIVE PLAYERS
        // ============================

        foreach (
            PlayerRef player
            in runner.ActivePlayers)
        {
            NetworkObject playerObject =
                runner.GetPlayerObject(
                    player
                );

            if (playerObject == null)
                continue;

            if (!playerObject.IsValid)
                continue;

            PHealth health =
                playerObject
                    .GetComponent<PHealth>();

            if (health == null)
                continue;

            PPlayerName playerName =
                playerObject
                    .GetComponent<PPlayerName>();

            string displayName =
                GetPlayerName(
                    playerName,
                    player
                );

            ScoreboardPlayerData data =
                new ScoreboardPlayerData();

            data.PlayerId =
                player.PlayerId;

            data.PlayerName =
                displayName;

            data.Kills =
                health.Kills;

            data.Deaths =
                health.Deaths;

            playerData.Add(
                data
            );
        }

        // ============================
        // SORT
        // ============================

        if (sortByKills)
        {
            playerData.Sort(
                ComparePlayers
            );
        }

        // ============================
        // BUILD TABLE
        // ============================

        string table =
            "<b>" +
            "PLAYER" +
            $"<pos={killsColumnPosition}%>" +
            "KILLS" +
            $"<pos={deathsColumnPosition}%>" +
            "DEATHS" +
            "</b>\n";

        table +=
            "------------------------------------------\n";

        foreach (
            ScoreboardPlayerData data
            in playerData)
        {
            string playerName =
                data.PlayerName;

            if (string.IsNullOrWhiteSpace(
                playerName))
            {
                playerName =
                    $"PLAYER {data.PlayerId}";
            }

            if (playerName.Length >
                maxPlayerNameLength)
            {
                playerName =
                    playerName.Substring(
                        0,
                        maxPlayerNameLength
                    );
            }

            table +=
                playerName +
                $"<pos={killsColumnPosition}%>" +
                data.Kills +
                $"<pos={deathsColumnPosition}%>" +
                data.Deaths +
                "\n";
        }

        scoreboardText.text =
            table;
    }

    // ============================
    // SORT PLAYERS
    // ============================

    private int ComparePlayers(
        ScoreboardPlayerData a,
        ScoreboardPlayerData b)
    {
        // Most kills first.
        int killsComparison =
            b.Kills.CompareTo(
                a.Kills
            );

        if (killsComparison != 0)
            return killsComparison;

        // If kills are equal,
        // fewer deaths first.
        int deathsComparison =
            a.Deaths.CompareTo(
                b.Deaths
            );

        if (deathsComparison != 0)
            return deathsComparison;

        // Final tie breaker:
        // PlayerId.
        return
            a.PlayerId.CompareTo(
                b.PlayerId
            );
    }

    // ============================
    // FIND RUNNER
    // ============================

    private void FindRunner()
    {
        NetworkRunner[] runners =
            FindObjectsByType<
                NetworkRunner
            >(
                FindObjectsSortMode.None
            );

        foreach (
            NetworkRunner foundRunner
            in runners)
        {
            if (foundRunner == null)
                continue;

            if (!foundRunner.IsRunning)
                continue;

            runner =
                foundRunner;

            return;
        }
    }

    // ============================
    // GET PLAYER NAME
    // ============================

    private string GetPlayerName(
        PPlayerName playerName,
        PlayerRef player)
    {
        if (playerName == null)
        {
            return
                $"PLAYER {player.PlayerId}";
        }

        PropertyInfo property =
            typeof(PPlayerName)
                .GetProperty(
                    "NetworkPlayerName",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );

        if (property == null)
        {
            return
                $"PLAYER {player.PlayerId}";
        }

        object networkString =
            property.GetValue(
                playerName
            );

        if (networkString == null)
        {
            return
                $"PLAYER {player.PlayerId}";
        }

        PropertyInfo valueProperty =
            networkString
                .GetType()
                .GetProperty(
                    "Value",
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

        if (valueProperty == null)
        {
            return
                $"PLAYER {player.PlayerId}";
        }

        object value =
            valueProperty.GetValue(
                networkString
            );

        string result =
            value != null
                ? value.ToString()
                : "";

        if (string.IsNullOrWhiteSpace(
            result))
        {
            return
                $"PLAYER {player.PlayerId}";
        }

        return
            result.Trim();
    }
}