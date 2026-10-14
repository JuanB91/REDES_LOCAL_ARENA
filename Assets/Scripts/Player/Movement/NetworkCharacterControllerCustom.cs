using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

//algunos pequeños cambios a la clase NetworkCharacterController (opcional para el parcial)
public class NetworkCharacterControllerCustom : NetworkCharacterController
{
    public event Action<float> OnMoving = delegate {  };
    
    public override void Move(Vector3 direction)
    {
        var deltaTime    = Runner.DeltaTime;
        var previousPos  = transform.position;
        var moveVelocity = Velocity;

        direction = direction.normalized;

        if (Grounded && moveVelocity.y < 0) {
            moveVelocity.y = 0f;
        }

        moveVelocity.y += gravity * Runner.DeltaTime;

        var horizontalVel = default(Vector3);
        horizontalVel.z = moveVelocity.x;

        if (direction == default) 
        {
            horizontalVel = Vector3.Lerp(horizontalVel, default, braking * deltaTime);
        } 
        else 
        {
            horizontalVel      = Vector3.ClampMagnitude(horizontalVel + direction * acceleration * deltaTime, maxSpeed);
            transform.rotation = Quaternion.Euler(Vector3.up * (Mathf.Sign(direction.z) < 0 ? -90 : 90));
        }

        moveVelocity.x = horizontalVel.z;

        Controller.Move(moveVelocity * deltaTime);

        Velocity = (transform.position - previousPos) * Runner.TickRate;
        Grounded = Controller.isGrounded;

        OnMoving(Velocity.magnitude);
    }
}
