using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PMovement : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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


        Vector3 inputDirection = Vector3.zero;

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

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetKey(KeyCode.Space))
                verticalVelocity = jumpForce;
        }

        verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 movement =
            inputDirection * moveSpeed +
            Vector3.up * verticalVelocity;

        controller.Move(
            movement * Runner.DeltaTime
        );
    }
}