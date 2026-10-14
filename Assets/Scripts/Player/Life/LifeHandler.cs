using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class LifeHandler : NetworkBehaviour
{
    //Es mejor usar byte ya que son datos que ocupan menos espacio en memoria y son mas faciles de trasladar en la red
    [Networked, OnChangedRender(nameof(CurrentLifeChanged))]
    private byte CurrentLife { get; set; }

    private const byte MAX_LIFE = 100;
    

    public override void Spawned()
    {   
        if (HasStateAuthority)
        {
            CurrentLife = MAX_LIFE;
        }
    }

    public void TakeDamage(byte dmg)
    {
        //aplicar daño (recordar que los bytes no pueden tener valores negativos)
        if (dmg > CurrentLife)
            dmg = CurrentLife;

        CurrentLife -= dmg;

        if(CurrentLife > 0 )
            return;


        //al morir lo desconecto

        DisconnectPlayer(); 



    }



    void CurrentLifeChanged()
    {
        Debug.Log(CurrentLife);
    }
    
    void DisconnectPlayer()
    {
        //si no soy el host me desconecto
        if (!Object.HasStateAuthority)
            Runner.Disconnect(Object.InputAuthority);

        
        Runner.Despawn(Object);
    }
    
}
