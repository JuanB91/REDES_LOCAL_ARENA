using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private GameObject _winImage;
    [SerializeField] private GameObject _loseImage;

    private List<PlayerRef> _players;//PlayerRef sirve como identificacion de cada cliente conectado

    private void Awake()
    {
        Instance = this;

        _players = new List<PlayerRef>();
    }

    public void AddToList(OnlinePlayer player)
    {
        //Consigo el objecto con state autority
        var playerPref = player.Object.StateAuthority;
        //lo agrego a _players si no lo contiene.

        if(!_players.Contains(playerPref))
            _players.Add(playerPref);
    }
    
    void RemoveFromList(PlayerRef player)
    {
        _players.Remove(player);
    }
    
    [Rpc]
    public void RPC_Defeat(PlayerRef player)
    {
        //si el player que perdio es el local llamo al metodo local de derrota
        if (player == Runner.LocalPlayer)
            Defeat();
        //Llamo al metodo para removerme de la lista
        RemoveFromList(player);
        //si queda un solo jugadore en _players y tengo state authority llamo a la victoria
        if (_players.Count == 1 && HasStateAuthority) 
        {
            RPC_Win(_players[0]);
        
        }
    }
    
    //[RpcTarget] El llamado del RPC va a ir dirigido a ese jugador
    [Rpc]
    void RPC_Win([RpcTarget] PlayerRef player)
    {
        Win();
    }
    
    void Win()
    {
        _winImage.SetActive(true);
    }
    
    void Defeat()
    {
        _loseImage.SetActive(true);
    }
}
