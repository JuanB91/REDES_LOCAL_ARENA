using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterControllerCustom))]
[RequireComponent(typeof(WeaponHandler))]
public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterControllerCustom _characterMovement;
    private WeaponHandler _weaponHandler;
    
    public override void Spawned()
    {
        _characterMovement = GetComponent<NetworkCharacterControllerCustom>();
        _weaponHandler = GetComponent<WeaponHandler>();
    }

    public override void FixedUpdateNetwork()
    {
        //Metodo derivado de NetworkBehaviour retorna true si consigue un INetworkInput valido del objeto con InputAutority
        if (!GetInput(out NetworkInputData inputs))
            return;

        //Movimiento
        //consigo la direccion de movimiento
        //aplico el movimiento
        var moveDirection = Vector3.forward * inputs.movementImput;
        _characterMovement.Move(moveDirection);


        //Salto
        //si aprete el boton de salto -> salto
        if (inputs.networkButtons.IsSet(MyButtons.Jump))
            _characterMovement.Jump();
        
        //Disparo
        //si dispare -> disparo

        if(inputs.isFirePressed)
            _weaponHandler.Fire();
    }
}
