using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionListHandler : MonoBehaviour
{
    [SerializeField] private SessionItem _sessionItemPrefab;
    
    [SerializeField] private NetworkRunnerHandler _networkRunnerHandler;

    [SerializeField] private TMP_Text _statusText;

    [SerializeField] private VerticalLayoutGroup _verticalLayout;

    private void OnEnable()
    {
        //Me subscribo cuando se actualiza la lista de sesion
        _networkRunnerHandler.OnSessionListUpdate += ReceiveSessionList;
    }

    private void OnDisable()
    {
        //me remuevo del evento
        _networkRunnerHandler.OnSessionListUpdate -= ReceiveSessionList;
        ClearBrowser();
    }

    void ReceiveSessionList(List<SessionInfo> sessionList)
    {
        ClearBrowser();
        
        //cortar si no hay sesiones y notificar al usuario con UI
        if(sessionList.Count == 0) 
        {
            NoSessionsFound();
            return;
        
        }
        
        foreach (var sessionInfo in sessionList)
        {
            //agrego las sesiones al browser
            AddToSessionBrowser(sessionInfo);
        }
    }

    void ClearBrowser()
    {
        foreach (Transform child in _verticalLayout.transform)
        {
            Destroy(child.gameObject);
        }
        
        _statusText.gameObject.SetActive(false);
    }
    
    void NoSessionsFound()
    {
        _statusText.text = "No sessions found";
        _statusText.gameObject.SetActive(true);
    }

    void AddToSessionBrowser(SessionInfo sessionInfo)
    {
        //creo el sessionItem

        SessionItem item = Instantiate(_sessionItemPrefab, _verticalLayout.transform) ;
        //lo inicializo

        item.Initialize(sessionInfo);
        //subscribo el metodo para unirme a la sesion
        item.OnJoinSession += JoinSelectedSession;
    }

    void JoinSelectedSession(SessionInfo sessionInfo)
    {
        //me uno a la sesion
        _networkRunnerHandler.JoinGame(sessionInfo);
    }
}
