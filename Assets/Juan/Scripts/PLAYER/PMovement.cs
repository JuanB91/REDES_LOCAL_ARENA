using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PMovement : NetworkBehaviour
{
  
    [Header("Movimiento")]
    [SerializeField]
    private float moveSpeed = 6f;

    [SerializeField]
    private float jumpForce = 6f;

    [SerializeField]
    private float gravity = -10f;

    
    [Header("Ghost Step")]
    [SerializeField]
    private float ghostStepDistance = 3f;

    [SerializeField]
    private float ghostStepDuration = 0.18f;

    [SerializeField]
    private float ghostStepCooldown = 0.5f;

    
    [Header("Partículas")]
    [SerializeField]
    private ParticleSystem footstepParticles;

    
    [Networked]
    private NetworkBool IsMoving
    {
        get;
        set;
    }

    [Networked]
    private NetworkButtons PreviousButtons
    {
        get;
        set;
    }


    private CharacterController controller;

    private PHealth health;

    private NetworkGameManager gameManager;


    private float verticalVelocity;


    private bool isGhostStepping;

    private Vector3 ghostStepDirection;

    private float ghostStepTimeRemaining;

    private float lastGhostStepTime =
        -10f;


    private void Awake()
    {
        controller =
            GetComponent<CharacterController>();

        health =
            GetComponent<PHealth>();
    }


    public override void Spawned()
    {
        if (controller == null)
        {
            controller =
                GetComponent<CharacterController>();
        }

        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }

        gameManager =
            FindFirstObjectByType<
                NetworkGameManager
            >();

        verticalVelocity =
            0f;

        isGhostStepping =
            false;

        ghostStepTimeRemaining =
            0f;

        lastGhostStepTime =
            -10f;


        if (Object.HasStateAuthority)
        {
            IsMoving =
                false;

            PreviousButtons =
                default;
        }

        Debug.Log(
            $"PMOVEMENT READY | " +
            $"PLAYER: {Object.InputAuthority.PlayerId} | " +
            $"STATE AUTHORITY: {Object.HasStateAuthority} | " +
            $"INPUT AUTHORITY: {Object.HasInputAuthority} | " +
            $"POSITION: {transform.position}"
        );
    }


    public override void FixedUpdateNetwork()
    {
        if (!GetInput(
            out PlayerNetworkInput input))
        {
            return;
        }


        if (!Object.HasStateAuthority)
        {
            return;
        }


        NetworkButtons pressed =
            input.Buttons.GetPressed(
                PreviousButtons
            );

        PreviousButtons =
            input.Buttons;


        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }


        if (health != null &&
            health.IsDead)
        {
            StopMovementState();

            return;
        }


        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }


        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            StopMovementState();

            return;
        }


        if (controller == null)
        {
            controller =
                GetComponent<CharacterController>();
        }

        if (controller == null)
        {
            IsMoving =
                false;

            return;
        }

        if (!controller.enabled)
        {
            IsMoving =
                false;

            return;
        }


        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity =
                -2f;
        }

        verticalVelocity +=
            gravity *
            Runner.DeltaTime;


        if (pressed.IsSet(
                PlayerInputButton.Jump) &&
            controller.isGrounded)
        {
            verticalVelocity =
                jumpForce;
        }


        if (!isGhostStepping)
        {
            if (pressed.IsSet(
                PlayerInputButton
                    .GhostForward))
            {
                TryGhostStep(
                    transform.forward
                );
            }

            else if (pressed.IsSet(
                PlayerInputButton
                    .GhostBackward))
            {
                TryGhostStep(
                    -transform.forward
                );
            }

            else if (pressed.IsSet(
                PlayerInputButton
                    .GhostLeft))
            {
                TryGhostStep(
                    -transform.right
                );
            }

            else if (pressed.IsSet(
                PlayerInputButton
                    .GhostRight))
            {
                TryGhostStep(
                    transform.right
                );
            }
        }


        if (isGhostStepping)
        {
            UpdateGhostStep();

            IsMoving =
                true;

            return;
        }


        Vector2 moveInput =
            input.MoveDirection;

        Vector3 inputDirection =
            transform.forward *
            moveInput.y +
            transform.right *
            moveInput.x;

        inputDirection.y =
            0f;

        inputDirection =
            Vector3.ClampMagnitude(
                inputDirection,
                1f
            );

        IsMoving =
            inputDirection.sqrMagnitude >
            0.001f;

        Vector3 movement =
            inputDirection *
            moveSpeed;

        movement +=
            Vector3.up *
            verticalVelocity;

        controller.Move(
            movement *
            Runner.DeltaTime
        );
    }


    private void StopMovementState()
    {
        if (!Object.HasStateAuthority)
            return;

        IsMoving =
            false;

        isGhostStepping =
            false;

        ghostStepTimeRemaining =
            0f;
    }


    private void TryGhostStep(
        Vector3 direction)
    {
        if (!Object.HasStateAuthority)
            return;

        float currentTime =
            Runner.SimulationTime;

        if (currentTime -
            lastGhostStepTime <
            ghostStepCooldown)
        {
            return;
        }

        direction.y =
            0f;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        direction.Normalize();

        ghostStepDirection =
            direction;

        ghostStepTimeRemaining =
            ghostStepDuration;

        isGhostStepping =
            true;

        lastGhostStepTime =
            currentTime;

        Debug.Log(
            $"GHOST STEP | " +
            $"Player: " +
            $"{Object.InputAuthority.PlayerId}"
        );
    }


    private void UpdateGhostStep()
    {
        if (!Object.HasStateAuthority)
            return;

        if (controller == null)
        {
            isGhostStepping =
                false;

            return;
        }

        if (!controller.enabled)
        {
            isGhostStepping =
                false;

            return;
        }

        if (ghostStepDuration <= 0f)
        {
            isGhostStepping =
                false;

            ghostStepTimeRemaining =
                0f;

            return;
        }

        float ghostStepSpeed =
            ghostStepDistance /
            ghostStepDuration;

        Vector3 movement =
            ghostStepDirection *
            ghostStepSpeed;

        movement +=
            Vector3.up *
            verticalVelocity;

        controller.Move(
            movement *
            Runner.DeltaTime
        );

        ghostStepTimeRemaining -=
            Runner.DeltaTime;

        if (ghostStepTimeRemaining <=
            0f)
        {
            isGhostStepping =
                false;

            ghostStepTimeRemaining =
                0f;
        }
    }


    public override void Render()
    {
        if (footstepParticles == null)
            return;

        if (IsMoving)
        {
            if (!footstepParticles
                .isPlaying)
            {
                footstepParticles.Play();
            }
        }
        else
        {
            if (footstepParticles
                .isPlaying)
            {
                footstepParticles.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmitting
                );
            }
        }
    }
}