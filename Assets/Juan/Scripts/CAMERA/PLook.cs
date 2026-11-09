using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PLook : NetworkBehaviour
{
    // ============================
    // REFERENCES
    // ============================

    [Header("References")]
    [SerializeField]
    private Transform cameraPivot;

    [SerializeField]
    private Camera playerCamera;

    // ============================
    // GAME SCENES
    // ============================

    [Header("Game Scenes")]
    [SerializeField]
    private string deathmatchSceneName =
        "DEATHMATCH";

    // ============================
    // MOUSE
    // ============================

    [Header("Mouse")]
    [SerializeField]
    private float mouseSensitivity = 2f;

    [SerializeField]
    private float maxLookAngle = 85f;

    // ============================
    // ROTATION STATE
    // ============================

    private float verticalRotation;

    private float horizontalRotation;

    private float lastSentHorizontalRotation;

    private const float RotationSendThreshold =
        0.05f;

    // ============================
    // REFERENCES
    // ============================

    private PHealth health;

    private NetworkGameManager gameManager;

    private DeathmatchGameManager
        deathmatchGameManager;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        health =
            GetComponent<PHealth>();

        gameManager =
            FindFirstObjectByType<
                NetworkGameManager
            >();

        deathmatchGameManager =
            FindFirstObjectByType<
                DeathmatchGameManager
            >();

        bool isLocalPlayer =
            Object.HasInputAuthority;

        // ============================
        // CAMERA
        // ============================

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

        // ============================
        // LOCAL PLAYER
        // ============================

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
                $"SCENE: " +
                $"{SceneManager.GetActiveScene().name} | " +
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

    // ============================
    // UPDATE
    // ============================

    private void Update()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasInputAuthority)
            return;

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
            return;
        }

        // ============================
        // CAN LOOK?
        // ============================

        if (!CanPlayerLook())
        {
            return;
        }

        // ============================
        // MOUSE INPUT
        // ============================

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

        // ============================
        // HORIZONTAL ROTATION
        // ============================

        horizontalRotation +=
            mouseX;

        horizontalRotation =
            Mathf.Repeat(
                horizontalRotation,
                360f
            );

        // ============================
        // VERTICAL ROTATION
        // ============================

        verticalRotation -=
            mouseY;

        verticalRotation =
            Mathf.Clamp(
                verticalRotation,
                -maxLookAngle,
                maxLookAngle
            );

        // ============================
        // CAMERA PIVOT
        // ============================

        if (cameraPivot != null)
        {
            cameraPivot.localRotation =
                Quaternion.Euler(
                    verticalRotation,
                    0f,
                    0f
                );
        }

        // ============================
        // LOCAL YAW
        // ============================

        ApplyHorizontalRotation(
            horizontalRotation
        );
    }

    // ============================
    // FIXED UPDATE NETWORK
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasInputAuthority)
            return;

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
            return;
        }

        // ============================
        // CAN LOOK?
        // ============================

        if (!CanPlayerLook())
        {
            return;
        }

        // ============================
        // HOST PLAYER
        // ============================

        if (Object.HasStateAuthority)
        {
            ApplyHorizontalRotation(
                horizontalRotation
            );

            lastSentHorizontalRotation =
                horizontalRotation;

            return;
        }

        // ============================
        // CLIENT ROTATION SYNC
        // ============================

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

    // ============================
    // CAN PLAYER LOOK
    // ============================

    private bool CanPlayerLook()
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

        if (!gameManager.CanPlay)
        {
            return false;
        }

        if (gameManager.IsReady &&
            gameManager.GameOver)
        {
            return false;
        }

        return true;
    }

    // ============================
    // ROTATION RPC
    // ============================

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

    // ============================
    // APPLY ROTATION
    // ============================

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

    // ============================
    // SYNC ROTATION
    // ============================

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