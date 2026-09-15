using Fusion;
using UnityEngine;

public class PShooting : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;

    [Header("Disparo")]
    [SerializeField] private float range = 100f;
    [SerializeField] private int damage = 25;

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

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log(
                $"Disparo impactó en: {hit.collider.name}"
            );

            PHealth health =
                hit.collider.GetComponentInParent<PHealth>();

            if (health != null)
            {
                health.RPC_TakeDamage(
                damage,
                Object
                );
            }

            Debug.DrawLine(
                ray.origin,
                hit.point,
                Color.red,
                1f
            );
        }
        else
        {
            Debug.Log("Disparo no impactó en nada");

            Debug.DrawRay(
                ray.origin,
                ray.direction * range,
                Color.red,
                1f
            );
        }
    }
}