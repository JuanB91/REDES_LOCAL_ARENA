using UnityEngine;

public class DeathmatchSpawnManager : MonoBehaviour
{
    // ============================
    // SPAWN POINTS
    // ============================

    [Header("Deathmatch Spawns")]

    [SerializeField]
    private Transform[] spawnPoints;

    // ============================
    // LOOK TARGET
    // ============================

    [Header("Look Target")]

    [SerializeField]
    private Transform lookTarget;

    public Transform LookTarget
    {
        get
        {
            return lookTarget;
        }
    }

    // ============================
    // RANDOM SPAWN
    // ============================

    public Transform GetRandomSpawnPoint()
    {
        if (!ValidateSpawnPoints())
        {
            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                spawnPoints.Length
            );

        Transform selectedSpawn =
            spawnPoints[randomIndex];

        if (selectedSpawn == null)
        {
            Debug.LogError(
                $"DEATHMATCH SPAWN MANAGER: " +
                $"Spawn NULL | " +
                $"Index: {randomIndex}"
            );

            return null;
        }

        Debug.Log(
            $"DEATHMATCH SPAWN SELECTED | " +
            $"Index: {randomIndex} | " +
            $"Spawn: {selectedSpawn.name} | " +
            $"Position: {selectedSpawn.position}"
        );

        return selectedSpawn;
    }

    // ============================
    // VALIDATION
    // ============================

    private bool ValidateSpawnPoints()
    {
        if (spawnPoints == null)
        {
            Debug.LogError(
                "DEATHMATCH SPAWN MANAGER: " +
                "Spawn array is NULL"
            );

            return false;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError(
                "DEATHMATCH SPAWN MANAGER: " +
                "No spawn points configured"
            );

            return false;
        }

        return true;
    }
}