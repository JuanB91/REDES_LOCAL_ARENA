using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalInputs : MonoBehaviour
{
    private NetworkInputData _networkInputData;

    //crear variable que chequea si salto
    private bool _isJumpPressed;
    //crear variable que chequea si disparo
    private bool _isFirePressed;


    void Start()
    {
        //creo y guardo un NetworkInputData
        _networkInputData = new NetworkInputData();
    }

    void Update()
    {
        //setear movimiento a mi network input
        _networkInputData.movementImput = Input.GetAxis("Horizontal");

        //chequear si salto
        if(Input.GetKeyDown(KeyCode.Escape))
            _isJumpPressed = true;
        
        //chequear si disparo
        _isFirePressed |= Input.GetKeyDown(KeyCode.F);
    }

    public NetworkInputData GetLocalInputs()
    {
        //seteo el disparo de mi network input

        _networkInputData.isFirePressed = _isFirePressed;
        _isFirePressed = false;
        //reinicio el disparo local;

        //seteo el salto de mi network input

        _networkInputData.networkButtons.Set(MyButtons.Jump, _isJumpPressed);
        //reinicio el salto local
        _isJumpPressed = false;

        return _networkInputData;
    }
}
