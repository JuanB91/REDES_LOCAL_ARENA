using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
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
    // START
    // ============================

    private void Start()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;

        GameSettings.FriendlyFireEnabled =
            false;

        UpdateFriendlyFireVisual();

        Debug.Log(
            "MAIN MENU READY | " +
            "FRIENDLY FIRE: OFF"
        );
    }

    // ============================
    // PLAY
    // ============================

    public void PlayGame()
    {
        Debug.Log(
            "PLAY BUTTON PRESSED | " +
            "LOADING WAITING"
        );

        SceneManager.LoadScene(
            "WAITING"
        );
    }

    // ============================
    // FRIENDLY FIRE
    // ============================

    public void ToggleFriendlyFire()
    {
        GameSettings.FriendlyFireEnabled =
            !GameSettings.FriendlyFireEnabled;

        UpdateFriendlyFireVisual();

        Debug.Log(
            $"FRIENDLY FIRE: " +
            $"{(GameSettings.FriendlyFireEnabled ? "ON" : "OFF")}"
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
            friendlyFireButton.GetComponent<Image>();

        if (buttonImage == null)
            return;

        buttonImage.color =
            GameSettings.FriendlyFireEnabled
                ? friendlyFireOnColor
                : friendlyFireOffColor;
    }

    // ============================
    // QUIT
    // ============================

    public void QuitGame()
    {
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