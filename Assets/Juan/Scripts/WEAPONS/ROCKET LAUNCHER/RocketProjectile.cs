using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RocketProjectile : NetworkBehaviour
{

    [Header("Movimiento")]
    [SerializeField]
    private float speed = 35f;


    [Header("Daño Directo")]
    [SerializeField]
    private int directDamage = 80;


    [Header("Explosión")]
    [SerializeField]
    private float explosionRadius = 5f;

    [SerializeField]
    private int maxExplosionDamage = 60;

    [SerializeField]
    private int minExplosionDamage = 0;


    [Header("Debug Visual")]
    [SerializeField]
    private Transform visualObject;

    [SerializeField]
    private float debugExplosionDuration = 0.5f;

    [SerializeField]
    private bool showDebugExplosion = true;


    [Header("Debug")]
    [SerializeField]
    private bool showDebug = true;


    private NetworkObject shooter;


    [Networked]
    private NetworkBool HasExploded
    {
        get;
        set;
    }

    [Networked]
    private NetworkBool HasBeenInitialized
    {
        get;
        set;
    }


    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            HasExploded =
                false;

            HasBeenInitialized =
                false;
        }

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET PROJECTILE SPAWNED | " +
                $"STATE: {Object.HasStateAuthority} | " +
                $"INPUT: {Object.HasInputAuthority}"
            );
        }
    }


    public void Initialize(
        NetworkObject shooterObject)
    {

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (HasBeenInitialized)
            return;

        shooter =
            shooterObject;

        HasBeenInitialized =
            true;

        if (showDebug)
        {
            string shooterName =
                shooter != null
                    ? shooter.name
                    : "NULL";

            Debug.Log(
                $"ROCKET INITIALIZED | " +
                $"Shooter: {shooterName} | " +
                $"Player: {GetShooterId()}"
            );
        }
    }


    public override void FixedUpdateNetwork()
    {

        if (!Object.HasStateAuthority)
            return;

        if (HasExploded)
            return;

        MoveRocket();
    }


    private void MoveRocket()
    {
        Vector3 movement =
            transform.forward *
            speed *
            Runner.DeltaTime;

        transform.position +=
            movement;
    }


    private void OnTriggerEnter(
        Collider other)
    {

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (HasExploded)
            return;

        if (other == null)
            return;


        if (other.transform.IsChildOf(
            transform))
        {
            return;
        }


        if (shooter != null &&
            shooter.IsValid)
        {
            if (other.transform.IsChildOf(
                shooter.transform))
            {
                return;
            }
        }

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET IMPACTED: " +
                $"{other.gameObject.name}"
            );
        }


        PHealth directTarget =
            other
                .GetComponentInParent<
                    PHealth
                >();

        Explode(
            directTarget
        );
    }


    private void Explode(
        PHealth directTarget)
    {

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (HasExploded)
            return;

        HasExploded =
            true;

        Vector3 explosionPosition =
            transform.position;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET EXPLODES | " +
                $"Player: {GetShooterId()} | " +
                $"Position: {explosionPosition}"
            );
        }


        if (directTarget != null)
        {
            if (directTarget.Object != null &&
                directTarget.Object.IsValid)
            {
                if (showDebug)
                {
                    Debug.Log(
                        $"ROCKET DIRECT HIT | " +
                        $"Target: " +
                        $"{directTarget.gameObject.name} | " +
                        $"Damage: {directDamage}"
                    );
                }

                directTarget
                    .RPC_TakeDamage(
                        directDamage,
                        shooter
                    );
            }
        }


        ApplyExplosionDamage(
            explosionPosition,
            directTarget
        );


        RPC_PlayExplosionVisual(
            explosionPosition
        );


        if (showDebugExplosion)
        {
            StartCoroutine(
                DespawnAfterExplosionDelay()
            );
        }
        else
        {
            DespawnRocket();
        }
    }


    private void ApplyExplosionDamage(
        Vector3 explosionPosition,
        PHealth directTarget)
    {
        if (!Object.HasStateAuthority)
            return;

        Collider[] colliders =
            Physics.OverlapSphere(
                explosionPosition,
                explosionRadius
            );

        Dictionary<PHealth, float>
            targets =
                new Dictionary<
                    PHealth,
                    float
                >();

        foreach (
            Collider hitCollider
            in colliders)
        {
            if (hitCollider == null)
                continue;

            PHealth health =
                hitCollider
                    .GetComponentInParent<
                        PHealth
                    >();

            if (health == null)
                continue;

            if (health.Object == null)
                continue;

            if (!health.Object.IsValid)
                continue;

            if (health ==
                directTarget)
            {
                continue;
            }


            Vector3 closestPoint =
                hitCollider
                    .ClosestPoint(
                        explosionPosition
                    );

            float distance =
                Vector3.Distance(
                    explosionPosition,
                    closestPoint
                );

            if (targets.ContainsKey(
                health))
            {
                if (distance <
                    targets[health])
                {
                    targets[health] =
                        distance;
                }
            }
            else
            {
                targets.Add(
                    health,
                    distance
                );
            }
        }


        foreach (
            KeyValuePair<PHealth, float>
                target
            in targets)
        {
            PHealth health =
                target.Key;

            float distance =
                target.Value;

            int explosionDamage =
                CalculateExplosionDamage(
                    distance
                );

            if (explosionDamage <= 0)
                continue;

            bool isShooter =
                shooter != null &&
                shooter.IsValid &&
                health.gameObject ==
                shooter.gameObject;

            if (showDebug)
            {
                Debug.Log(
                    $"ROCKET SPLASH | " +
                    $"Target: " +
                    $"{health.gameObject.name} | " +
                    $"Distance: {distance:F2} | " +
                    $"Damage: {explosionDamage}" +
                    (
                        isShooter
                            ? " | SELF DAMAGE"
                            : ""
                    )
                );
            }

            health.RPC_TakeDamage(
                explosionDamage,
                shooter
            );
        }
    }


    private int CalculateExplosionDamage(
        float distance)
    {
        if (explosionRadius <= 0f)
        {
            return
                maxExplosionDamage;
        }

        distance =
            Mathf.Clamp(
                distance,
                0f,
                explosionRadius
            );

        float normalizedDistance =
            distance /
            explosionRadius;

        float calculatedDamage =
            Mathf.Lerp(
                maxExplosionDamage,
                minExplosionDamage,
                normalizedDistance
            );

        return
            Mathf.RoundToInt(
                calculatedDamage
            );
    }


    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayExplosionVisual(
        Vector3 explosionPosition)
    {
        if (!showDebugExplosion)
            return;

        StartCoroutine(
            DebugExplosionVisualCoroutine(
                explosionPosition
            )
        );
    }


    private IEnumerator
        DebugExplosionVisualCoroutine(
            Vector3 explosionPosition)
    {
        transform.position =
            explosionPosition;


        Collider rocketCollider =
            GetComponent<Collider>();

        if (rocketCollider != null)
        {
            rocketCollider.enabled =
                false;
        }


        if (visualObject != null)
        {
            float diameter =
                explosionRadius *
                2f;

            visualObject.localScale =
                new Vector3(
                    diameter,
                    diameter,
                    diameter
                );
        }
        else
        {
            Debug.LogWarning(
                "ROCKET: " +
                "No hay Visual Object asignado " +
                "para mostrar el área de explosión."
            );
        }

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET DEBUG EXPLOSION | " +
                $"Radius: {explosionRadius} | " +
                $"Diameter: " +
                $"{explosionRadius * 2f}"
            );
        }

        yield return
            new WaitForSeconds(
                debugExplosionDuration
            );
    }


    private IEnumerator
        DespawnAfterExplosionDelay()
    {
        yield return
            new WaitForSeconds(
                debugExplosionDuration
            );

        DespawnRocket();
    }


    private void DespawnRocket()
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (Runner == null)
            return;

        Runner.Despawn(
            Object
        );
    }


    private string GetShooterId()
    {
        if (shooter == null)
        {
            return "NONE";
        }

        if (!shooter.IsValid)
        {
            return "INVALID";
        }

        return
            shooter
                .InputAuthority
                .PlayerId
                .ToString();
    }
}