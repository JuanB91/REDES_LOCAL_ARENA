using Fusion;
using UnityEngine;

public class PLook : NetworkBehaviour
{
    

    [Header("Referencias")]
    [SerializeField]
    private Transform cameraPivot;

    [SerializeField]
    private Camera playerCamera;

    
    [Header("Mouse")]
    [SerializeField]
    private float mouseSensitivity = 2f;

    [SerializeField]
    private float maxLookAngle = 85f;

    
    private float verticalRotation;

    private float horizontalRotation;

    private float lastSentHorizontalRotation;

    private const float RotationSendThreshold =
        0.05f;

    
    private PHealth health;

    private NetworkGameManager gameManager;

    
    public override void Spawned()
    {
        health =
            GetComponent<PHealth>();

        gameManager =
            FindFirstObjectByType<
                NetworkGameManager
            >();

        bool isLocalPlayer =
            Object.HasInputAuthority;

        
        if (playerCamera != null)
        {
            playerCamera.enabled =
                isLocalPlayer;

            AudioListener listener =
                playerCamera
                    .GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled =
                    isLocalPlayer;
            }
        }

 
        if (isLocalPlayer)
        {
            horizontalRotation =
                transform.eulerAngles.y;

            lastSentHorizontalRotation =
                horizontalRotation;

            verticalRotation =
                0f;

            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;

            Debug.Log(
                $"LOCAL CAMERA ENABLED | " +
                $"PLAYER: " +
                $"{Object.InputAuthority.PlayerId} | " +
                $"YAW: {horizontalRotation:F1}"
            );
        }
        else
        {
            Debug.Log(
                $"REMOTE CAMERA DISABLED | " +
                $"PLAYER: " +
                $"{Object.InputAuthority.PlayerId}"
            );
        }
    }

    private void Update()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasInputAuthority)
            return;

        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }

        if (health != null &&
            health.IsDead)
        {
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
            return;
        }

        if (gameManager.IsReady &&
            gameManager.GameOver)
        {
            return;
        }

        float mouseX =
            Input.GetAxis(
                "Mouse X"
            ) *
            mouseSensitivity;

        float mouseY =
            Input.GetAxis(
                "Mouse Y"
            ) *
            mouseSensitivity;

        
        horizontalRotation +=
            mouseX;

        horizontalRotation =
            Mathf.Repeat(
                horizontalRotation,
                360f
            );

        verticalRotation -=
            mouseY;

        verticalRotation =
            Mathf.Clamp(
                verticalRotation,
                -maxLookAngle,
                maxLookAngle
            );

        
        if (cameraPivot != null)
        {
            cameraPivot.localRotation =
                Quaternion.Euler(
                    verticalRotation,
                    0f,
                    0f
                );
        }
        ApplyHorizontalRotation(
            horizontalRotation
        );
    }


    public override void FixedUpdateNetwork()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasInputAuthority)
            return;


        if (health == null)
        {
            health =
                GetComponent<PHealth>();
        }

        if (health != null &&
            health.IsDead)
        {
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
            return;
        }

        if (gameManager.IsReady &&
            gameManager.GameOver)
        {
            return;
        }


        if (Object.HasStateAuthority)
        {
            ApplyHorizontalRotation(
                horizontalRotation
            );

            lastSentHorizontalRotation =
                horizontalRotation;

            return;
        }


        float difference =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    lastSentHorizontalRotation,
                    horizontalRotation
                )
            );

        if (difference <
            RotationSendThreshold)
        {
            return;
        }

        lastSentHorizontalRotation =
            horizontalRotation;

        RPC_SubmitHorizontalRotation(
            horizontalRotation
        );
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_SubmitHorizontalRotation(
        float yaw)
    {
        if (!Object.HasStateAuthority)
            return;

        yaw =
            Mathf.Repeat(
                yaw,
                360f
            );

        ApplyHorizontalRotation(
            yaw
        );
    }


    private void ApplyHorizontalRotation(
        float yaw)
    {
        transform.rotation =
            Quaternion.Euler(
                0f,
                yaw,
                0f
            );
    }


    public void SyncHorizontalRotation()
    {
        horizontalRotation =
            transform.eulerAngles.y;

        horizontalRotation =
            Mathf.Repeat(
                horizontalRotation,
                360f
            );

        lastSentHorizontalRotation =
            horizontalRotation;

        if (Object != null &&
            Object.IsValid &&
            Object.HasInputAuthority)
        {
            ApplyHorizontalRotation(
                horizontalRotation
            );
        }
    }
}