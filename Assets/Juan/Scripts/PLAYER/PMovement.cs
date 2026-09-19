using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PMovement : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = -10f;

    [Header("Ghost Step")]
    [SerializeField] private float ghostStepDistance = 3f;
    [SerializeField] private float ghostStepDuration = 0.18f;
    [SerializeField] private float doubleTapTime = 0.25f;
    [SerializeField] private float ghostStepCooldown = 0.5f;

    [Header("Partículas")]
    [SerializeField] private ParticleSystem footstepParticles;

    [Networked] private NetworkBool IsMoving { get; set; }

    private CharacterController controller;
    private float verticalVelocity;

    // Double tap
    private float lastWPress = -10f;
    private float lastSPress = -10f;
    private float lastAPress = -10f;
    private float lastDPress = -10f;

    // Cooldown
    private float lastGhostStepTime = -10f;

    // Estado del Ghost Step
    private bool isGhostStepping = false;
    private Vector3 ghostStepDirection;
    private float ghostStepTimeRemaining;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        CheckGhostStepInput();
    }

    private void CheckGhostStepInput()
    {
        // Mientras estamos haciendo Ghost Step
        // ignoramos nuevos double taps.
        if (isGhostStepping)
            return;

        // W
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (Time.time - lastWPress <= doubleTapTime)
            {
                TryGhostStep(transform.forward);
            }

            lastWPress = Time.time;
        }

        // S
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (Time.time - lastSPress <= doubleTapTime)
            {
                TryGhostStep(-transform.forward);
            }

            lastSPress = Time.time;
        }

        // A
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastAPress <= doubleTapTime)
            {
                TryGhostStep(-transform.right);
            }

            lastAPress = Time.time;
        }

        // D
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastDPress <= doubleTapTime)
            {
                TryGhostStep(transform.right);
            }

            lastDPress = Time.time;
        }

        
    }

    private void TryGhostStep(Vector3 direction)
    {
        // Cooldown
        if (Time.time - lastGhostStepTime < ghostStepCooldown)
            return;

        direction.y = 0f;
        direction.Normalize();

        ghostStepDirection = direction;

        ghostStepTimeRemaining =
            ghostStepDuration;

        isGhostStepping = true;

        lastGhostStepTime = Time.time;

        Debug.Log("GHOST STEP");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        // -------------------------
        // GRAVEDAD
        // -------------------------

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity +=
            gravity * Runner.DeltaTime;

        // -------------------------
        // GHOST STEP
        // -------------------------

        if (isGhostStepping)
        {
            UpdateGhostStep();

            return;
        }

        // -------------------------
        // MOVIMIENTO NORMAL
        // -------------------------

        Vector3 inputDirection =
            Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            inputDirection += transform.forward;

        if (Input.GetKey(KeyCode.S))
            inputDirection -= transform.forward;

        if (Input.GetKey(KeyCode.D))
            inputDirection += transform.right;

        if (Input.GetKey(KeyCode.A))
            inputDirection -= transform.right;

        inputDirection.y = 0f;
        inputDirection.Normalize();

        IsMoving = inputDirection != Vector3.zero;

        // -------------------------
        // SALTO
        // -------------------------

        if (controller.isGrounded)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                verticalVelocity =
                    jumpForce;
            }
        }

        Vector3 movement =
            inputDirection * moveSpeed +
            Vector3.up * verticalVelocity;

        controller.Move(
            movement * Runner.DeltaTime
        );
    }

    private void UpdateGhostStep()
    {
        if (ghostStepDuration <= 0f)
        {
            isGhostStepping = false;
            return;
        }

        // Velocidad necesaria para recorrer
        // ghostStepDistance durante ghostStepDuration.
        float ghostStepSpeed =
            ghostStepDistance /
            ghostStepDuration;

        Vector3 movement =
            ghostStepDirection *
            ghostStepSpeed;

        // Conservamos gravedad durante el dash.
        movement +=
            Vector3.up * verticalVelocity;

        controller.Move(
            movement * Runner.DeltaTime
        );

        ghostStepTimeRemaining -=
            Runner.DeltaTime;

        if (ghostStepTimeRemaining <= 0f)
        {
            isGhostStepping = false;
            ghostStepTimeRemaining = 0f;
        }
    }
    public override void Render()
    {
        if (footstepParticles == null) return;

        if (IsMoving)
        {
            if (!footstepParticles.isPlaying)
            {
                footstepParticles.Play();
            }
        }
        else
        {
            if (footstepParticles.isPlaying)
            {
                footstepParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }
        }
    }
}