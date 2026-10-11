using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuHandler : MonoBehaviour
{
    [SerializeField] private NetworkRunnerHandler _networkRunnerHandler;

    [Header("Panels")]
    [SerializeField] private GameObject _initialPanel;
    [SerializeField] private GameObject _statusPanel;
    [SerializeField] private GameObject _sessionBrowserPanel;
    [SerializeField] private GameObject _hostGamePanel;

    [Header("Buttons")]
    [SerializeField] private Button _joinLobbyBTN;
    [SerializeField] private Button _hostPanelBTN;
    [SerializeField] private Button _hostGameBTN;

    [Header("InputFields")] 
    [SerializeField] private TMP_InputField _sessionName;


    [Header("Texts")]
    [SerializeField] private TMP_Text _statusText;
    
    void Start()
    {
        _joinLobbyBTN.onClick.AddListener(Btn_JoinLobby);
        _hostPanelBTN.onClick.AddListener(Btn_ShowHostPanel);
        _hostGameBTN.onClick.AddListener(Btn_CreateGameSession);

        //Apagar el status panel y prender el browser cuando me uno al lobby

        _networkRunnerHandler.OnJoinedLobby += () =>
        {
            _statusPanel.gameObject.SetActive(false);
            _sessionBrowserPanel.gameObject.SetActive(true);


        };
        
    }

    void Btn_JoinLobby()
    {
        _networkRunnerHandler.JoinLobby();
        
        _initialPanel.SetActive(false);
        _statusPanel.SetActive(true);

        _statusText.text = "Joining Lobby...";
    }

    void Btn_ShowHostPanel()
    {
        _sessionBrowserPanel.SetActive(false);
        _hostGamePanel.SetActive(true);
    }
    
    void Btn_CreateGameSession()
    {
        _hostGameBTN.interactable = false;
        //Creo el juego con el nombre de sesion y la escena de juego.
        _networkRunnerHandler.CreateGame(_sessionName.text, "Game");
    }
}
