using Fusion;
using TMPro;
using UnityEngine;

public class PPlayerName : NetworkBehaviour
{
    // ============================
    // REFERENCES
    // ============================

    [Header("References")]
    [SerializeField]
    private TMP_Text nameText;

    // ============================
    // SETTINGS
    // ============================

    [Header("Settings")]
    [SerializeField]
    private bool hideForLocalPlayer = true;

    [SerializeField]
    private string fallbackName = "PLAYER";

    [SerializeField]
    private bool rotateOnlyYAxis = true;

    [SerializeField]
    private float rotationOffsetY = 180f;

    // ============================
    // NETWORK
    // ============================

    [Networked]
    private NetworkString<_16> NetworkPlayerName { get; set; }

    // ============================
    // LOCAL
    // ============================

    private Camera localCamera;

    // ============================
    // SPAWNED
    // ============================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            string localName =
                PlayerProfile.PlayerName;

            if (string.IsNullOrWhiteSpace(localName))
            {
                localName =
                    fallbackName;
            }

            localName =
                localName.Trim();

            if (localName.Length > 16)
            {
                localName =
                    localName.Substring(
                        0,
                        16
                    );
            }

            NetworkPlayerName =
                localName;

            Debug.Log(
                $"PLAYER NAME SPAWNED | " +
                $"Player: {Object.InputAuthority.PlayerId} | " +
                $"Name: {localName}"
            );
        }

        FindLocalCamera();

        UpdateNameVisual();
    }

    // ============================
    // RENDER
    // ============================

    public override void Render()
    {
        UpdateNameVisual();
    }

    // ============================
    // FIND LOCAL CAMERA
    // ============================

    private void FindLocalCamera()
    {
        PPlayerName[] players =
            FindObjectsByType<PPlayerName>(
                FindObjectsSortMode.None
            );

        foreach (PPlayerName player in players)
        {
            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;

            if (!player.Object.HasStateAuthority)
                continue;

            Camera camera =
                player.GetComponentInChildren<Camera>(
                    true
                );

            if (camera != null)
            {
                localCamera =
                    camera;

                return;
            }
        }

        localCamera =
            Camera.main;
    }

    // ============================
    // UPDATE NAME
    // ============================

    private void UpdateNameVisual()
    {
        if (nameText == null)
            return;

        if (hideForLocalPlayer &&
            Object.HasStateAuthority)
        {
            nameText.gameObject.SetActive(
                false
            );

            return;
        }

        nameText.gameObject.SetActive(
            true
        );

        string displayName =
            NetworkPlayerName.Value;

        if (string.IsNullOrWhiteSpace(
            displayName
        ))
        {
            displayName =
                fallbackName;
        }

        nameText.text =
            displayName;
    }

    // ============================
    // BILLBOARD
    // ============================

    private void LateUpdate()
    {
        if (nameText == null)
            return;

        if (!nameText.gameObject.activeSelf)
            return;

        if (localCamera == null)
        {
            FindLocalCamera();

            if (localCamera == null)
                return;
        }

        Vector3 targetPosition =
            localCamera.transform.position;

        Vector3 textPosition =
            nameText.transform.position;

        if (rotateOnlyYAxis)
        {
            targetPosition.y =
                textPosition.y;
        }

        Vector3 direction =
            targetPosition -
            textPosition;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction
            );

        Quaternion offsetRotation =
            Quaternion.Euler(
                0f,
                rotationOffsetY,
                0f
            );

        nameText.transform.rotation =
            lookRotation *
            offsetRotation;
    }
}