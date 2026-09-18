using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GrenadeProjectile : NetworkBehaviour
{
    // ============================
    // REFERENCIAS
    // ============================

    [Header("Referencias")]
    [SerializeField] private Rigidbody rb;

    [SerializeField] private Transform visualObject;

    // ============================
    // FÍSICA
    // ============================

    [Header("Física")]
    [SerializeField] private bool useGravity = true;

    [SerializeField] private float mass = 1f;

    [SerializeField] private float linearDamping = 0.05f;

    [SerializeField] private float angularDamping = 0.05f;

    // ============================
    // ROTACIÓN
    // ============================

    [Header("Rotación al lanzar")]
    [SerializeField] private float spinForce = 8f;

    // ============================
    // FUSIBLE
    // ============================

    [Header("Fusible")]
    [SerializeField] private float fuseTime = 3f;

    // ============================
    // EXPLOSIÓN
    // ============================

    [Header("Explosión")]
    [SerializeField] private float explosionRadius = 5f;

    [SerializeField] private int maxExplosionDamage = 70;

    [SerializeField] private int minExplosionDamage = 0;

    // ============================
    // DEBUG VISUAL
    // ============================

    [Header("Debug Visual")]
    [SerializeField] private bool showDebugExplosion = true;

    [SerializeField] private float debugExplosionDuration = 0.5f;

    // ============================
    // DEBUG
    // ============================

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // ============================
    // SHOOTER
    // ============================

    private NetworkObject shooter;

    // ============================
    // ESTADO
    // ============================

    private bool hasBeenLaunched = false;

    private bool hasExploded = false;

    // ============================
    // SPAWN
    // ============================

    public override void Spawned()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError(
                "GRENADE: No tiene Rigidbody."
            );

            return;
        }

        // ============================
        // AUTORIDAD
        // ============================

        if (Object.HasStateAuthority)
        {
            rb.isKinematic = false;

            rb.useGravity =
                useGravity;

            rb.mass =
                mass;

            rb.linearDamping =
                linearDamping;

            rb.angularDamping =
                angularDamping;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            rb.isKinematic = true;

            rb.useGravity = false;
        }

    }

    // ============================
    // INICIALIZAR / LANZAR
    // ============================

    public void Initialize(
        NetworkObject shooterObject,
        Vector3 launchVelocity)
    {
        if (!Object.HasStateAuthority)
            return;

        shooter =
            shooterObject;

        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }

        if (rb == null)
            return;

        // ============================
        // VELOCIDAD
        // ============================

        rb.linearVelocity =
            launchVelocity;

        // ============================
        // GIRO
        // ============================

        Vector3 randomSpin =
            new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

        rb.angularVelocity =
            randomSpin *
            spinForce;

        hasBeenLaunched =
            true;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE LANZADA | " +
                $"Velocity: {launchVelocity} | " +
                $"Fuse: {fuseTime}s"
            );
        }

        StartCoroutine(
            FuseCoroutine()
        );
    }

    // ============================
    // FUSIBLE
    // ============================

    private IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(
            fuseTime
        );

        if (hasExploded)
            yield break;

        Explode();
    }

    // ============================
    // COLISIONES
    // ============================

    private void OnCollisionEnter(
        Collision collision)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!hasBeenLaunched)
            return;

        if (hasExploded)
            return;

        if (collision == null)
            return;

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE REBOTÓ CONTRA: " +
                $"{collision.gameObject.name}"
            );
        }

        // La granada NO explota por impacto.
        // Solo explota cuando termina el fuse.
    }

    // ============================
    // EXPLOSIÓN
    // ============================

    private void Explode()
    {
        if (!Object.HasStateAuthority)
            return;

        if (hasExploded)
            return;

        hasExploded = true;

        Vector3 explosionPosition =
            transform.position;

        // Frenamos completamente la granada.
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
                $"GRENADE EXPLOTA EN: " +
                $"{explosionPosition}"
            );
        }

        // ============================
        // DAÑO EN ÁREA
        // ============================

        ApplyExplosionDamage(
            explosionPosition
        );

        // ============================
        // DEBUG VISUAL
        // ============================

        if (showDebugExplosion)
        {
            StartCoroutine(
                DebugExplosionCoroutine()
            );
        }
        else
        {
            DespawnGrenade();
        }
    }

    // ============================
    // DAÑO EN ÁREA
    // ============================

    private void ApplyExplosionDamage(
        Vector3 explosionPosition)
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                explosionPosition,
                explosionRadius
            );

        // Un jugador puede tener más de un collider.
        // Guardamos solo la distancia más cercana.
        Dictionary<PHealth, float> targets =
            new Dictionary<PHealth, float>();

        foreach (
            Collider hitCollider
            in colliders)
        {
            if (hitCollider == null)
                continue;

            PHealth health =
                hitCollider.GetComponentInParent<PHealth>();

            if (health == null)
                continue;

            // IMPORTANTE:
            // NO excluimos al shooter.
            // La granada puede hacer self-damage.

            Vector3 closestPoint =
                hitCollider.ClosestPoint(
                    explosionPosition
                );

            float distance =
                Vector3.Distance(
                    explosionPosition,
                    closestPoint
                );

            if (targets.ContainsKey(health))
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

        // ============================
        // APLICAR DAÑO
        // ============================

        foreach (
            KeyValuePair<PHealth, float> target
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
                    $"Target: {health.gameObject.name} | " +
                    $"Distance: {distance:F2} | " +
                    $"Damage: {damage}" +
                    (isShooter
                        ? " | SELF DAMAGE"
                        : "")
                );
            }

            health.RPC_TakeDamage(
                damage,
                shooter
            );
        }
    }

    // ============================
    // CALCULAR DAÑO
    // ============================

    private int CalculateExplosionDamage(
        float distance)
    {
        distance =
            Mathf.Clamp(
                distance,
                0f,
                explosionRadius
            );

        float normalizedDistance =
            distance /
            explosionRadius;

        float damage =
            Mathf.Lerp(
                maxExplosionDamage,
                minExplosionDamage,
                normalizedDistance
            );

        return Mathf.RoundToInt(
            damage
        );
    }

    // ============================
    // DEBUG VISUAL
    // ============================

    private IEnumerator DebugExplosionCoroutine()
    {
        // Desactivamos el collider
        // para que la esfera gigante
        // no empuje ni choque cosas.
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
                explosionRadius * 2f;

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
                "GRENADE: No hay Visual Object asignado."
            );
        }

        if (showDebug)
        {
            Debug.Log(
                $"GRENADE DEBUG EXPLOSION | " +
                $"Radio: {explosionRadius} | " +
                $"Diámetro: {explosionRadius * 2f}"
            );
        }

        yield return new WaitForSeconds(
            debugExplosionDuration
        );

        DespawnGrenade();
    }

    // ============================
    // DESPAWN
    // ============================

    private void DespawnGrenade()
    {
        if (!Object.HasStateAuthority)
            return;

        if (Object == null)
            return;

        Runner.Despawn(
            Object
        );
    }
}