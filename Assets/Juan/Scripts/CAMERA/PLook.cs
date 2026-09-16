using Fusion;
using UnityEngine;

public class PLook : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;

    public override void Spawned()
    {
        bool isLocalPlayer = Object.HasStateAuthority;

        if (playerCamera != null)
        {
            playerCamera.enabled = isLocalPlayer;

            AudioListener listener =
                playerCamera.GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled = isLocalPlayer;
            }
        }

        if (isLocalPlayer)
        {
            horizontalRotation = transform.eulerAngles.y;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (Object == null ||
            !Object.HasStateAuthority)
        {
            return;
        }

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        horizontalRotation += mouseX;

        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            -maxLookAngle,
            maxLookAngle
        );

        cameraPivot.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            0f
        );
    }
    public void SyncHorizontalRotation()
    {
        horizontalRotation = transform.eulerAngles.y;
    }
    public override void FixedUpdateNetwork()
    {
        if (Object == null ||
         !Object.HasStateAuthority)
        {
            return;
        }

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        if (gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        if (gameManager != null &&
            gameManager.IsReady &&
            gameManager.GameOver)
        {
            return;
        }

        transform.rotation = Quaternion.Euler(
            0f,
            horizontalRotation,
            0f
        );
    }
}