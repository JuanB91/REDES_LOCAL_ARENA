using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class OnlinePlayer : NetworkBehaviour
{
    [SerializeField] float _speed;
    [SerializeField] float _jumpForce;
    [SerializeField] int _maxLife;


    //Crear una variable networkeada de vida actual y que tenga una forma de debugear el valor de vida cada vez que este se actualice
    [Networked, OnChangedRender(nameof(DebugLife))] private int CurrentLife {get; set;}


    //crear referencia a la bala y a una posicion para spawnear balas.
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private Transform _bulletSpawnPoint;
    [SerializeField] private float _dashForce = 12f;

    private bool _isJumpPressed;
    private bool _isShootPressed;
    private bool _isDashPressed;

    private float _horizontalInput;

    private Rigidbody _rb;

    private float _normalSpeed;

    private void DebugLife() 
    {
        Debug.Log($"Current Life = {CurrentLife}");
    
    }
    
    
    
    public override void Spawned()
    {
        //conseguir rigidbody
        _rb = GetComponent<Rigidbody>();

        //setear vida actual igual a la vida maxima
        CurrentLife = _maxLife;

        _normalSpeed = _speed;

        GameManager.Instance.AddToList(this);


        //Setear el follow target al cumplir la condicion correcta (piensen cual puede ser esa condicion)

        GameManager.Instance.AddToList(this);
    }
    
    void Update()
    {
        if (!HasStateAuthority) return;
        
        _horizontalInput = Input.GetAxis("Horizontal");

        //Chequear input para ejecutar el salto
        if(Input.GetKeyDown(KeyCode.Space))
            _isJumpPressed = true;
        //Chequear input para ejecutar el disparo
        if (Input.GetKeyDown(KeyCode.F))
            _isShootPressed = true;

        if (Input.GetKeyDown(KeyCode.LeftShift))
            _isDashPressed = true;
    }

    //En Shared la logica de fisicas se usa en el fixedUpdate
    private void FixedUpdate()
    {
        Movement(_horizontalInput);

        if (_isJumpPressed)
        {
            Jump();
            
            _isJumpPressed = false;
        }
        if (_isDashPressed)
        {
            Dash();

            _isDashPressed = false;
        }
    }

    //Ejecutamos la logica dentro del update de red
    public override void FixedUpdateNetwork()
    {
        //llamamos a nuestras funciones de accion en el FixedUpdateNetwork

        if (_isShootPressed)
        {
            SpawnShot();
            
            _isShootPressed = false;
        }

    }

    void Movement(float xAxi)
    {
        if (xAxi != 0)
        {
            //roto el transform hacia donde me estoy moviendo
            transform.forward = Vector3.right * Mathf.Sign(xAxi);

            //muevo a traves del rigidbody
            _rb.linearVelocity += Vector3.right * xAxi * _speed * Runner.DeltaTime;
            
            if (Mathf.Abs(_rb.linearVelocity.z) > _speed)//Clampeo la velocidad
            {
                var velocity = Vector3.ClampMagnitude(_rb.linearVelocity, _speed);
            
                velocity.y = _rb.linearVelocity.y;
            
                _rb.linearVelocity = velocity;
            }
        }

    }

    void Jump()
    {
        //aplico fuerza al rigidbody
        _rb.AddForce(Vector3.up * _jumpForce, ForceMode.VelocityChange);
    }

    void Dash()
    {
        Vector3 direction = transform.forward;

        _rb.linearVelocity = new Vector3(
            direction.x * _dashForce,
            _rb.linearVelocity.y,
            direction.z * _dashForce
        );

        Debug.Log("¡DASH!");
    }

    void SpawnShot()
    {
        //spawneo la bala
        var bullet = Runner.Spawn(_bulletPrefab, _bulletSpawnPoint.position, _bulletSpawnPoint.rotation);
    }

    //Hacer una funcion local para recibir daño y que llame a la funcion de morir cuando la vida sea <= 0

    private void Local_TakeDamage(int damage) 
    {
        CurrentLife -= damage;

        if (CurrentLife <= 0)
            Death();
    
    }

    //Hacer una funcion networkeada para recibir daño que llame a la funcion local de recibir daño.
    [Rpc(RpcSources.All , RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage) 
    {
        Local_TakeDamage(damage);
    
    
    }

    public void ActivateSpeedPowerUp(float extraSpeed, float duration)
    {
        StartCoroutine(SpeedPowerUp(extraSpeed, duration));
    }

    private IEnumerator SpeedPowerUp(float extraSpeed, float duration)
    {
        _speed += extraSpeed;

        Debug.Log($"POWER UP ACTIVADO: Velocidad aumentada a {_speed}");

        yield return new WaitForSeconds(duration);

        _speed = _normalSpeed;

        Debug.Log("POWER UP TERMINADO: Velocidad restaurada");
    }


    void Death()
    {
        Debug.Log("Mori :(");

        //Llamo a la funcion de derrota del game manager y paso mi local player
        GameManager.Instance.RPC_Defeat(Runner.LocalPlayer);
        
        Runner.Despawn(Object);
    }
}