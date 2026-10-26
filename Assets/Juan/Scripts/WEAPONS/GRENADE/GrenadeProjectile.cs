using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GrenadeProjectile : NetworkBehaviour
{

    [Header("Referencias")]
    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private Transform visualObject;


    [Header("Física")]
    [SerializeField]
    private bool useGravity = true;

    [SerializeField]
    private float mass = 1f;

    [SerializeField]
    private float linearDamping = 0.05f;

    [SerializeField]
    private float angularDamping = 0.05f;


    [Header("Rotación al lanzar")]
    [SerializeField]
    private float spinForce = 8f;


    [Header("Fusible")]
    [SerializeField]
    private float fuseTime = 3f;


    [Header("Explosión")]
    [SerializeField]
    private float explosionRadius = 5f;

    [SerializeField]
    private int maxExplosionDamage = 70;

    [SerializeField]
    private int minExplosionDamage = 0;


    [Header("Debug Visual")]
    [SerializeField]
    private bool showDebugExplosion = true;

    [SerializeField]
    private float debugExplosionDuration = 0.5f;


    [Header("Debug")]
    [SerializeField]
    private bool showDebug = true;


    [Networked]
    private NetworkBool HasBeenLaunched
    {
        get;
        set;
    }

    [Networked]
    private NetworkBool HasExploded
    {
        get;
        set;
    }

    [Networked]
    private TickTimer FuseTimer
    {
        get;
        set;
    }

    private NetworkObject shooter;


    public override void Spawned()
    {

        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError(
                "GRENADE: No tiene Rigidbody."
            );

            return;
        }


        if (Object.HasStateAuthority)
        {
            rb.isKinematic =
                false;

            rb.useGravity =
                useGravity;

            rb.mass =
                mass;

            rb.linearDamping =
                linearDamping;

            rb.angularDamping =
                angularDamping;

            rb.collisionDetectionMode =
                CollisionDetectionMode
                    .ContinuousDynamic;

            HasBeenLaunched =
                false;

            HasExploded =
                false;

            FuseTimer =
                TickTimer.None;
        }


        else
        {
            rb.isKinematic =
                true;

            rb.useGravity =
                false;
        }

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE SPAWNED | " +
                $"STATE: {Object.HasStateAuthority} | " +
                $"INPUT: {Object.HasInputAuthority}"
            );
        }
    }


    public void Initialize(
        NetworkObject shooterObject,
        Vector3 launchVelocity)
    {

        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (HasBeenLaunched)
            return;


        shooter =
            shooterObject;


        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError(
                "GRENADE: No tiene Rigidbody al intentar lanzarla."
            );

            return;
        }


        rb.linearVelocity =
            launchVelocity;

        Vector3 randomSpin =
            new Vector3(
                Random.Range(
                    -1f,
                    1f
                ),
                Random.Range(
                    -1f,
                    1f
                ),
                Random.Range(
                    -1f,
                    1f
                )
            );

        if (randomSpin.sqrMagnitude >
            0.001f)
        {
            randomSpin.Normalize();
        }
        else
        {
            randomSpin =
                Vector3.one.normalized;
        }

        rb.angularVelocity =
            randomSpin *
            spinForce;

        HasBeenLaunched =
            true;

        HasExploded =
            false;

        FuseTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                fuseTime
            );

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE LAUNCHED | " +
                $"Player: " +
                $"{GetShooterId()} | " +
                $"Velocity: {launchVelocity} | " +
                $"Fuse: {fuseTime}s"
            );
        }
    }


    public override void FixedUpdateNetwork()
    {

        if (!Object.HasStateAuthority)
            return;

        if (!HasBeenLaunched)
            return;

        if (HasExploded)
            return;

        if (!FuseTimer.IsRunning)
            return;


        if (FuseTimer.Expired(
            Runner))
        {
            Explode();
        }
    }


    private void OnCollisionEnter(
        Collision collision)
    {
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (!HasBeenLaunched)
            return;

        if (HasExploded)
            return;

        if (collision == null)
            return;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE BOUNCED AGAINST: " +
                $"{collision.gameObject.name}"
            );
        }

    }


    private void Explode()
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

        FuseTimer =
            TickTimer.None;

        Vector3 explosionPosition =
            transform.position;


        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.isKinematic =
                true;
        }

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE EXPLODES | " +
                $"Player: {GetShooterId()} | " +
                $"Position: {explosionPosition}"
            );
        }


        ApplyExplosionDamage(
            explosionPosition
        );


        RPC_PlayExplosionVisual(
            explosionPosition
        );


        if (showDebugExplosion)
        {
            StartCoroutine(
                DespawnAfterDebugDelay()
            );
        }
        else
        {
            DespawnGrenade();
        }
    }


    private void ApplyExplosionDamage(
        Vector3 explosionPosition)
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

            int damage =
                CalculateExplosionDamage(
                    distance
                );

            if (damage <= 0)
                continue;

            bool isShooter =
                shooter != null &&
                health.gameObject ==
                shooter.gameObject;

            if (showDebug)
            {
                Debug.Log(
                    $"GRENADE SPLASH | " +
                    $"Target: " +
                    $"{health.gameObject.name} | " +
                    $"Distance: {distance:F2} | " +
                    $"Damage: {damage}" +
                    (
                        isShooter
                            ? " | SELF DAMAGE"
                            : ""
                    )
                );
            }

            health.RPC_TakeDamage(
                damage,
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

        Collider grenadeCollider =
            GetComponent<Collider>();

        if (grenadeCollider != null)
        {
            grenadeCollider.enabled =
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
                "GRENADE: " +
                "No hay Visual Object asignado."
            );
        }

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE DEBUG EXPLOSION | " +
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
        DespawnAfterDebugDelay()
    {
        yield return
            new WaitForSeconds(
                debugExplosionDuration
            );

        DespawnGrenade();
    }


    private void DespawnGrenade()
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
            shooter.InputAuthority.PlayerId
                .ToString();
    }
}