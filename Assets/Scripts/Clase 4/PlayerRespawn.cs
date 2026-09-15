using Fusion;
using UnityEngine;

public class PlayerRespawn : NetworkBehaviour
{
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    public override void Spawned()
    {
       
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    private void Update()
    {
       
        if (!Object.HasStateAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        transform.position = _spawnPosition;
        transform.rotation = _spawnRotation;
    }
}