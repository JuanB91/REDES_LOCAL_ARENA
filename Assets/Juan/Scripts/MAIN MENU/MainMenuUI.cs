using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
 
    [Header("Player Name")]

    [SerializeField]
    private TMP_InputField playerNameInput;

    [SerializeField]
    private int maxNameLength = 16;

    
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

    
    [Header("Scene")]

    [SerializeField]
    private string waitingSceneName =
        "WAITING";

    
    private bool isLoading;

    
    private void Start()
    {
        isLoading = false;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;

        
        if (playerNameInput != null)
        {
            playerNameInput.characterLimit =
                maxNameLength;

            if (!string.IsNullOrWhiteSpace(
                    PlayerProfile.PlayerName
                ) &&
                PlayerProfile.PlayerName != "PLAYER")
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


        GameSettings.FriendlyFireEnabled =
            false;

        UpdateFriendlyFireVisual();

        Debug.Log(
            "MAIN MENU READY"
        );
    }


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


    public void HostGame()
    {
        if (isLoading)
            return;

        SavePlayerName();

        NetworkLaunchSettings.SetHost();

        Debug.Log(
            $"HOST GAME PRESSED | " +
            $"Player: {PlayerProfile.PlayerName} | " +
            $"Mode: HOST"
        );

        LoadWaitingRoom();
    }


    public void JoinGame()
    {
        if (isLoading)
            return;

        SavePlayerName();

        NetworkLaunchSettings.SetClient();

        Debug.Log(
            $"JOIN GAME PRESSED | " +
            $"Player: {PlayerProfile.PlayerName} | " +
            $"Mode: CLIENT"
        );

        LoadWaitingRoom();
    }


    private void LoadWaitingRoom()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(
            waitingSceneName))
        {
            Debug.LogError(
                "MAIN MENU: " +
                "Waiting Scene Name está vacío."
            );

            return;
        }

        isLoading =
            true;

        Debug.Log(
            $"LOADING SCENE | " +
            $"{waitingSceneName}"
        );

        SceneManager.LoadScene(
            waitingSceneName
        );
    }


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