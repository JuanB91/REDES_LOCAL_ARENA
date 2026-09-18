using UnityEngine;

public class TeamSpawnManager : MonoBehaviour
{
    // ============================
    // RED SPAWNS
    // ============================

    [Header("RED Spawns")]
    [SerializeField]
    private Transform[] redSpawnPoints;

    // ============================
    // BLUE SPAWNS
    // ============================

    [Header("BLUE Spawns")]
    [SerializeField]
    private Transform[] blueSpawnPoints;

    // ============================
    // LOOK TARGET
    // ============================

    [Header("Look Target")]
    [SerializeField]
    private Transform lookTarget;

    // ============================
    // PUBLIC
    // ============================

    public Transform LookTarget
    {
        get
        {
            return lookTarget;
        }
    }

    // ============================
    // GET SPAWN BY TEAM INDEX
    // ============================

    public Transform GetSpawnPoint(
        PTeam.Team team,
        int teamIndex)
    {
        Transform[] spawnArray =
            team == PTeam.Team.Red
                ? redSpawnPoints
                : blueSpawnPoints;

        if (spawnArray == null ||
            spawnArray.Length == 0)
        {
            Debug.LogError(
                $"TEAM SPAWN MANAGER: " +
                $"No hay spawns configurados para {team}"
            );

            return null;
        }

        int index =
            Mathf.Abs(teamIndex) %
            spawnArray.Length;

        Transform selectedSpawn =
            spawnArray[index];

        if (selectedSpawn == null)
        {
            Debug.LogError(
                $"TEAM SPAWN MANAGER: " +
                $"Spawn NULL | " +
                $"Team: {team} | " +
                $"Index: {index}"
            );

            return null;
        }

        Debug.Log(
            $"TEAM SPAWN SELECTED | " +
            $"Team: {team} | " +
            $"TeamIndex: {teamIndex} | " +
            $"Spawn: {selectedSpawn.name}"
        );

        return selectedSpawn;
    }

    // ============================
    // GET RANDOM SPAWN
    // ============================

    public Transform GetRandomSpawnPoint(
        PTeam.Team team)
    {
        Transform[] spawnArray =
            team == PTeam.Team.Red
                ? redSpawnPoints
                : blueSpawnPoints;

        if (spawnArray == null ||
            spawnArray.Length == 0)
        {
            Debug.LogError(
                $"TEAM SPAWN MANAGER: " +
                $"No hay spawns configurados para {team}"
            );

            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                spawnArray.Length
            );

        Transform selectedSpawn =
            spawnArray[randomIndex];

        if (selectedSpawn == null)
        {
            Debug.LogError(
                $"TEAM SPAWN MANAGER: " +
                $"Random spawn NULL | " +
                $"Team: {team} | " +
                $"Index: {randomIndex}"
            );

            return null;
        }

        return selectedSpawn;
    }
}