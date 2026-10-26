using UnityEngine;

public class TeamSpawnManager : MonoBehaviour
{
 
    [Header("RED Spawns")]
    [SerializeField]
    private Transform[] redSpawnPoints;

    
    [Header("BLUE Spawns")]
    [SerializeField]
    private Transform[] blueSpawnPoints;


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


    public Transform GetSpawnPoint(
        PTeam.Team team,
        int teamIndex)
    {
        Transform[] spawnArray =
            GetSpawnArray(
                team
            );

        if (!ValidateSpawnArray(
            spawnArray,
            team))
        {
            return null;
        }

        int index =
            Mathf.Abs(
                teamIndex
            ) %
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
            $"ArrayIndex: {index} | " +
            $"Spawn: {selectedSpawn.name} | " +
            $"Position: {selectedSpawn.position}"
        );

        return selectedSpawn;
    }


    public Transform GetRandomSpawnPoint(
        PTeam.Team team)
    {
        Transform[] spawnArray =
            GetSpawnArray(
                team
            );

        if (!ValidateSpawnArray(
            spawnArray,
            team))
        {
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

        Debug.Log(
            $"RANDOM TEAM SPAWN SELECTED | " +
            $"Team: {team} | " +
            $"Index: {randomIndex} | " +
            $"Spawn: {selectedSpawn.name} | " +
            $"Position: {selectedSpawn.position}"
        );

        return selectedSpawn;
    }


    private Transform[] GetSpawnArray(
        PTeam.Team team)
    {
        if (team ==
            PTeam.Team.Red)
        {
            return redSpawnPoints;
        }

        return blueSpawnPoints;
    }


    private bool ValidateSpawnArray(
        Transform[] spawnArray,
        PTeam.Team team)
    {
        if (spawnArray == null)
        {
            Debug.LogError(
                $"TEAM SPAWN MANAGER: " +
                $"Array de spawns NULL para {team}"
            );

            return false;
        }

        if (spawnArray.Length == 0)
        {
            Debug.LogError(
                $"TEAM SPAWN MANAGER: " +
                $"No hay spawns configurados para {team}"
            );

            return false;
        }

        return true;
    }
}