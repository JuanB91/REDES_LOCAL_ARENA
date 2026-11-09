using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    // ============================
    // PLAYER NAME
    // ============================

    [Header("Player Name")]

    [SerializeField]
    private TMP_InputField playerNameInput;

    [SerializeField]
    private int maxNameLength = 16;

    // ============================
    // FRIENDLY FIRE
    // ============================

    [Header("Friendly Fire")]

    [SerializeField]
    private Button friendlyFireButton;

    [SerializeField]
    private TMP_Text friendlyFireText;

    [Header("Friendly Fire Colors")]

    [SerializeField]
    private Color friendlyFireOffColor =
        Color.red;

    [SerializeField]
    private Color friendlyFireOnColor =
        Color.green;

    // ============================
    // GAME MODE
    // ============================

    [Header("Game Mode")]

    [SerializeField]
    private Button gameModeButton;

    [SerializeField]
    private TMP_Text gameModeText;

    // ============================
    // SCENE
    // ============================

    [Header("Scene")]

    [SerializeField]
    private string waitingSceneName =
        "WAITING";

    // ============================
    // RUNTIME
    // ============================

    private bool isLoading;

    // ============================
    // START
    // ============================

    private void Start()
    {
        isLoading =
            false;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;

        // ============================
        // PLAYER NAME
        // ============================

        if (playerNameInput != null)
        {
            playerNameInput.characterLimit =
                maxNameLength;

            if (!string.IsNullOrWhiteSpace(
                    PlayerProfile.PlayerName
                ) &&
                PlayerProfile.PlayerName !=
                "PLAYER")
            {
                playerNameInput.text =
                    PlayerProfile.PlayerName;
            }
            else
            {
                playerNameInput.text =
                    "";
            }
        }

        // ============================
        // FRIENDLY FIRE DEFAULT
        // ============================

        GameSettings.FriendlyFireEnabled =
            false;

        UpdateFriendlyFireVisual();

        // ============================
        // GAME MODE DEFAULT
        // ============================

        GameSettings.SelectedGameMode =
            GameSettings.GameMode.TeamVsTeam;

        UpdateGameModeVisual();

        Debug.Log(
            "MAIN MENU READY | " +
            $"GAME MODE: {GetCurrentGameModeName()}"
        );
    }

    // ============================
    // SAVE PLAYER NAME
    // ============================

    private void SavePlayerName()
    {
        string enteredName =
            "";

        if (playerNameInput != null)
        {
            enteredName =
                playerNameInput.text.Trim();
        }

        if (string.IsNullOrWhiteSpace(
            enteredName))
        {
            enteredName =
                "PLAYER";
        }

        if (enteredName.Length >
            maxNameLength)
        {
            enteredName =
                enteredName.Substring(
                    0,
                    maxNameLength
                );
        }

        PlayerProfile.PlayerName =
            enteredName;

        Debug.Log(
            $"PLAYER NAME SAVED | " +
            $"{PlayerProfile.PlayerName}"
        );
    }

    // ============================
    // HOST GAME
    // ============================

    public void HostGame()
    {
        if (isLoading)
            return;

        SavePlayerName();

        NetworkLaunchSettings.SetHost();

        Debug.Log(
            $"HOST GAME PRESSED | " +
            $"Player: {PlayerProfile.PlayerName} | " +
            $"Network Mode: HOST | " +
            $"Game Mode: {GetCurrentGameModeName()}"
        );

        LoadWaitingRoom();
    }

    // ============================
    // JOIN GAME
    // ============================

    public void JoinGame()
    {
        if (isLoading)
            return;

        SavePlayerName();

        NetworkLaunchSettings.SetClient();

        Debug.Log(
            $"JOIN GAME PRESSED | " +
            $"Player: {PlayerProfile.PlayerName} | " +
            $"Network Mode: CLIENT"
        );

        LoadWaitingRoom();
    }

    // ============================
    // LOAD WAITING ROOM
    // ============================

    private void LoadWaitingRoom()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(
            waitingSceneName))
        {
            Debug.LogError(
                "MAIN MENU: " +
                "Waiting Scene Name is empty."
            );

            return;
        }

        isLoading =
            true;

        Debug.Log(
            $"LOADING SCENE | " +
            $"{waitingSceneName} | " +
            $"GAME MODE: {GetCurrentGameModeName()}"
        );

        SceneManager.LoadScene(
            waitingSceneName
        );
    }

    // ============================
    // GAME MODE TOGGLE
    // ============================

    public void ToggleGameMode()
    {
        if (isLoading)
            return;

        if (GameSettings.SelectedGameMode ==
            GameSettings.GameMode.TeamVsTeam)
        {
            GameSettings.SelectedGameMode =
                GameSettings.GameMode.Deathmatch;
        }
        else
        {
            GameSettings.SelectedGameMode =
                GameSettings.GameMode.TeamVsTeam;
        }

        UpdateGameModeVisual();

        Debug.Log(
            $"GAME MODE SELECTED | " +
            $"{GetCurrentGameModeName()}"
        );
    }

    // ============================
    // GAME MODE VISUAL
    // ============================

    private void UpdateGameModeVisual()
    {
        if (gameModeText == null)
            return;

        gameModeText.text =
            GetCurrentGameModeName();
    }

    // ============================
    // GAME MODE NAME
    // ============================

    private string GetCurrentGameModeName()
    {
        switch (
            GameSettings.SelectedGameMode)
        {
            case GameSettings
                .GameMode
                .Deathmatch:

                return "DEATHMATCH";

            case GameSettings
                .GameMode
                .TeamVsTeam:

            default:

                return "TEAM VS TEAM";
        }
    }

    // ============================
    // FRIENDLY FIRE
    // ============================

    public void ToggleFriendlyFire()
    {
        if (isLoading)
            return;

        GameSettings.FriendlyFireEnabled =
            !GameSettings.FriendlyFireEnabled;

        UpdateFriendlyFireVisual();

        string state =
            GameSettings.FriendlyFireEnabled
                ? "ON"
                : "OFF";

        Debug.Log(
            $"FRIENDLY FIRE: {state}"
        );
    }

    // ============================
    // FRIENDLY FIRE VISUAL
    // ============================

    private void UpdateFriendlyFireVisual()
    {
        if (friendlyFireText != null)
        {
            friendlyFireText.text =
                GameSettings.FriendlyFireEnabled
                    ? "FRIENDLY FIRE: ON"
                    : "FRIENDLY FIRE: OFF";
        }

        if (friendlyFireButton == null)
            return;

        Image buttonImage =
            friendlyFireButton
                .GetComponent<Image>();

        if (buttonImage == null)
            return;

        buttonImage.color =
            GameSettings.FriendlyFireEnabled
                ? friendlyFireOnColor
                : friendlyFireOffColor;
    }

    // ============================
    // QUIT GAME
    // ============================

    public void QuitGame()
    {
        if (isLoading)
            return;

        Debug.Log(
            "QUIT BUTTON PRESSED"
        );

#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying =
            false;

#else

        Application.Quit();

#endif
    }
}