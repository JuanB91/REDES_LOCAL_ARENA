using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float _initialForce;
    [SerializeField] private int _damage;
    [SerializeField] private float _lifeTime;

    //TickTimer para manejar timers de manera eficiente en red
    private TickTimer _lifeTimer;
    
    public override void Spawned()
    {
        //Aplicar una fuerza al rigidbody
        GetComponent<Rigidbody>()?.AddForce(Vector3.right * _initialForce , ForceMode.Impulse);
        
        //crear el timer
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, _lifeTime);
        
    }

    public override void FixedUpdateNetwork()
    {
        //Chequear si el timer expiro
        if (!_lifeTimer.Expired(Runner))
            return;
        //eliminar la bala si paso el timer
        Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority)
            return;
        //aplicar daño al personaje con el que colisiono la bala
        if (other.TryGetComponent(out OnlinePlayer player))
            player.RPC_TakeDamage(_damage);


        //eliminar la bala al colisionar

        Runner.Despawn(Object); 
    }
}
