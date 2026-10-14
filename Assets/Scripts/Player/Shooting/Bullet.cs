using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class Bullet : NetworkBehaviour
{
    TickTimer _lifeTimer = TickTimer.None;

    [SerializeField] private byte _damage = 25;

    public override void Spawned()
    {
        //aplicar fuerzas al rigidbody
        GetComponent<NetworkRigidbody>().PhysicsBody.AddForce(transform.forward*10);
        

        //Crear el timer solo si se cumple una condicion
        if(HasStateAuthority)
            _lifeTimer = TickTimer.CreateFromSeconds(Runner,2);
    }

    public override void FixedUpdateNetwork()
    {
        if (_lifeTimer.Expired(Runner))
        {
            DespawnObject();
        }
    }

    void DespawnObject()
    {
        _lifeTimer = TickTimer.None;
        
        Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        //retornar si el objeto no existe o no tiene la autoridad correspondiente
        if(!Object || !Object.HasStateAuthority) 
            return;

        //si colisiono con alguien que puede recibir daño aplico el daño
        if (other.TryGetComponent(out LifeHandler lifeHandler))
            lifeHandler.TakeDamage(_damage);
        
        
        DespawnObject();
    }
}
