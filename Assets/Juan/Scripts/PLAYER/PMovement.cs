using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PMovement : NetworkBehaviour
{
    // ============================
    // MOVEMENT
    // ============================

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 6f;

    [SerializeField]
    private float jumpForce = 6f;

    [SerializeField]
    private float gravity = -10f;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // GHOST STEP
    // ============================

    [Header("Ghost Step")]
    [SerializeField]
    private float ghostStepDistance = 3f;

    [SerializeField]
    private float ghostStepDuration = 0.18f;

    [SerializeField]
    private float ghostStepCooldown = 0.5f;

    // ============================
    // PARTICLES
    // ============================

    [Header("Particles")]
    [SerializeField]
    private ParticleSystem footstepParticles;

    // ============================
    // ANIMATION
    // ============================

    [Header("Animations")]
    [SerializeField]
    private Animator animator;

    // ============================
    // NETWORK STATE
    // ============================

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

    [Networked]
    private float MoveX
    {
        get;
        set;
    }

    [Networked]
    private float MoveY
    {
        get;
        set;
    }

    [Networked]
    private NetworkBool IsJumping
    {
        get;
        set;
    }

    // ============================
    // REFERENCES
    // ============================

    private CharacterController controller;

    private PHealth health;

    private NetworkGameManager gameManager;

    private DeathmatchGameManager
        deathmatchGameManager;

    // ============================
    // MOVEMENT STATE
    // ============================

    private float verticalVelocity;

    // ============================
    // GHOST STEP STATE
    // ============================

    private bool isGhostStepping;

    private Vector3 ghostStepDirection;

    private float ghostStepTimeRemaining;

    private float lastGhostStepTime =
        -10f;

    // ============================
    // AWAKE
    // ============================

    private void Awake()
    {
        controller =
            GetComponent<CharacterController>();

        health =
            GetComponent<PHealth>();

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }
    }

    // ============================
    // SPAWNED
    // ============================

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

        deathmatchGameManager =
            FindFirstObjectByType<
                DeathmatchGameManager
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

            IsJumping =
                false;

            MoveX =
                0f;

            MoveY =
                0f;

            PreviousButtons =
                default;
        }

        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        Debug.Log(
            $"PMOVEMENT READY | " +
            $"PLAYER: {Object.InputAuthority.PlayerId} | " +
            $"STATE AUTHORITY: {Object.HasStateAuthority} | " +
            $"INPUT AUTHORITY: {Object.HasInputAuthority} | " +
            $"SCENE: {sceneName} | " +
            $"POSITION: {transform.position}"
        );
    }

    // ============================
    // FIXED UPDATE NETWORK
    // ============================

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

        // ============================
        // HEALTH
        // ============================

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

        // ============================
        // CAN PLAYER MOVE?
        // ============================

        if (!CanPlayerMove())
        {
            StopMovementState();

            return;
        }

        // ============================
        // CHARACTER CONTROLLER
        // ============================

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

        // ============================
        // GRAVITY
        // ============================

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity =
                -2f;
        }

        verticalVelocity +=
            gravity *
            Runner.DeltaTime;

        // ============================
        // JUMP
        // ============================

        if (pressed.IsSet(
                PlayerInputButton.Jump) &&
            controller.isGrounded)
        {
            verticalVelocity =
                jumpForce;

            IsJumping =
                true;
        }

        // ============================
        // GHOST STEP INPUT
        // ============================

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

        // ============================
        // GHOST STEP MOVEMENT
        // ============================

        if (isGhostStepping)
        {
            UpdateGhostStep();

            IsMoving =
                true;

            return;
        }

        // ============================
        // NORMAL MOVEMENT
        // ============================

        Vector2 moveInput =
            input.MoveDirection;

        MoveX =
            moveInput.x;

        MoveY =
            moveInput.y;

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

        // ============================
        // JUMP STATE
        // ============================

        if (IsJumping &&
            controller.isGrounded &&
            verticalVelocity <= 0f)
        {
            IsJumping =
                false;
        }
    }

    // ============================
    // CAN PLAYER MOVE
    // ============================

    private bool CanPlayerMove()
    {
        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        // ============================
        // DEATHMATCH
        // ============================

        if (sceneName ==
            deathmatchSceneName)
        {
            if (deathmatchGameManager == null)
            {
                deathmatchGameManager =
                    FindFirstObjectByType<
                        DeathmatchGameManager
                    >();
            }

            if (deathmatchGameManager == null)
            {
                return false;
            }

            return
                deathmatchGameManager
                    .CanPlay;
        }

        // ============================
        // TEAM VS TEAM
        // ============================

        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    NetworkGameManager
                >();
        }

        if (gameManager == null)
        {
            return false;
        }

        return
            gameManager.CanPlay;
    }

    // ============================
    // STOP MOVEMENT STATE
    // ============================

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

        MoveX =
            0f;

        MoveY =
            0f;

        IsJumping =
            false;

        verticalVelocity =
            0f;
    }

    // ============================
    // TRY GHOST STEP
    // ============================

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

    // ============================
    // UPDATE GHOST STEP
    // ============================

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

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        if (animator != null)
        {
            animator.SetBool(
                "IsMoving",
                IsMoving
            );

            animator.SetFloat(
                "MoveX",
                MoveX
            );

            animator.SetFloat(
                "MoveY",
                MoveY
            );

            animator.SetBool(
                "IsJumping",
                IsJumping
            );
        }

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