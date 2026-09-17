using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RocketProjectile : NetworkBehaviour
{
    // ============================
    // MOVIMIENTO
    // ============================

    [Header("Movimiento")]
    [SerializeField] private float speed = 35f;

    // ============================
    // DAÑO DIRECTO
    // ============================

    [Header("Daño Directo")]
    [SerializeField] private int directDamage = 80;

    // ============================
    // EXPLOSIÓN
    // ============================

    [Header("Explosión")]
    [SerializeField] private float explosionRadius = 5f;

    [SerializeField] private int maxExplosionDamage = 60;

    [SerializeField] private int minExplosionDamage = 0;

    // ============================
    // DEBUG VISUAL
    // ============================

    [Header("Debug Visual")]
    [SerializeField] private Transform visualObject;

    [SerializeField] private float debugExplosionDuration = 0.5f;

    [SerializeField] private bool showDebugExplosion = true;

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

    private bool hasExploded = false;

    // ============================
    // INICIALIZAR
    // ============================

    public void Initialize(
        NetworkObject shooterObject)
    {
        shooter =
            shooterObject;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET INICIALIZADO | " +
                $"Shooter: {(shooter != null ? shooter.name : "NULL")}"
            );
        }
    }

    // ============================
    // MOVIMIENTO
    // ============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (hasExploded)
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

    // ============================
    // IMPACTO
    // ============================

    private void OnTriggerEnter(
        Collider other)
    {
        if (!Object.HasStateAuthority)
            return;

        if (hasExploded)
            return;

        if (other == null)
            return;

        // Evitar partes del propio cohete
        if (other.transform.IsChildOf(transform))
            return;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET IMPACTÓ CONTRA: " +
                $"{other.gameObject.name}"
            );
        }

        PHealth directTarget =
            other.GetComponentInParent<PHealth>();

        Explode(
            directTarget
        );
    }

    // ============================
    // EXPLOSIÓN
    // ============================

    private void Explode(
        PHealth directTarget)
    {
        if (!Object.HasStateAuthority)
            return;

        if (hasExploded)
            return;

        hasExploded = true;

        Vector3 explosionPosition =
            transform.position;

        if (showDebug)
        {
            Debug.Log(
                $"ROCKET EXPLOTA EN: " +
                $"{explosionPosition}"
            );
        }

        // ============================
        // DAÑO DIRECTO
        // ============================

        if (directTarget != null)
        {
            if (showDebug)
            {
                Debug.Log(
                    $"ROCKET DIRECT HIT | " +
                    $"Target: {directTarget.gameObject.name} | " +
                    $"Damage: {directDamage}"
                );
            }

            directTarget.RPC_TakeDamage(
                directDamage,
                shooter
            );
        }

        // ============================
        // DAÑO EN ÁREA
        // ============================

        ApplyExplosionDamage(
            explosionPosition,
            directTarget
        );

        // ============================
        // DEBUG DE EXPLOSIÓN
        // ============================

        if (showDebugExplosion)
        {
            StartCoroutine(
                DebugExplosionCoroutine()
            );
        }
        else
        {
            DespawnRocket();
        }
    }

    // ============================
    // SPLASH DAMAGE
    // ============================

    private void ApplyExplosionDamage(
        Vector3 explosionPosition,
        PHealth directTarget)
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                explosionPosition,
                explosionRadius
            );

        Dictionary<PHealth, float> targets =
            new Dictionary<PHealth, float>();

        foreach (Collider hitCollider in colliders)
        {
            if (hitCollider == null)
                continue;

            PHealth health =
                hitCollider.GetComponentInParent<PHealth>();

            if (health == null)
                continue;

            // El que recibió impacto directo
            // no recibe splash extra.
            if (health == directTarget)
                continue;

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
                if (distance < targets[health])
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
            KeyValuePair<PHealth, float> target
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

            if (showDebug)
            {
                bool isShooter =
                    shooter != null &&
                    health.gameObject ==
                    shooter.gameObject;

                Debug.Log(
                    $"ROCKET SPLASH | " +
                    $"Target: {health.gameObject.name} | " +
                    $"Distance: {distance:F2} | " +
                    $"Damage: {explosionDamage}" +
                    (isShooter ? " | SELF DAMAGE" : "")
                );
            }

            health.RPC_TakeDamage(
                explosionDamage,
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
    // DEBUG EXPLOSIÓN VISUAL
    // ============================

    private IEnumerator DebugExplosionCoroutine()
    {
        // Desactivamos el collider para que
        // la esfera gigante no genere
        // nuevos impactos.
        Collider rocketCollider =
            GetComponent<Collider>();

        if (rocketCollider != null)
        {
            rocketCollider.enabled =
                false;
        }

        // ============================
        // AGRANDAR VISUAL
        // ============================

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
                "ROCKET: No hay Visual Object asignado para mostrar el área de explosión."
            );
        }

        if (showDebug)
        {
            Debug.Log(
                $"DEBUG EXPLOSION | " +
                $"Radio: {explosionRadius} | " +
                $"Diámetro visual: {explosionRadius * 2f}"
            );
        }

        // Mantener visible la explosión
        yield return new WaitForSeconds(
            debugExplosionDuration
        );

        DespawnRocket();
    }

    // ============================
    // DESPAWN
    // ============================

    private void DespawnRocket()
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