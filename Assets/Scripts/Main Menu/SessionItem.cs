using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _playerCount;
    [SerializeField] private Button _joinBtn;

    private SessionInfo _sessionInfo;

    public event Action<SessionInfo> OnJoinSession;

    private void Awake()
    {
        _joinBtn.onClick.AddListener(OnClick);
    }

    public void Initialize(SessionInfo sessionInfo)
    {
        //guardo la info de la sesion
        _sessionInfo = sessionInfo;

        //cambio los textos por el nombre de sesion y cantidades de jugadares (actual y maxima)
        _name.text = sessionInfo.Name;
        _playerCount.text = $"{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}";
        //activo el boton mientras
        _joinBtn.enabled = _sessionInfo.PlayerCount < _sessionInfo.MaxPlayers;
    }
    
    void OnClick()
    {
        //unirse a la sesion
        OnJoinSession?.Invoke(_sessionInfo);
    }
}
