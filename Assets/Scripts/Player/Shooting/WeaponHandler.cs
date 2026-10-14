using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class WeaponHandler : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef _bulletPrefab;
    [SerializeField] private Transform _shotSpawnTransform;

    public event Action OnShot = delegate { };

    public void Fire()
    {
        if (!HasStateAuthority) return;

        SpawnBullet();
        OnShot();
    }

    void SpawnBullet()
    {
        Runner.Spawn(_bulletPrefab, _shotSpawnTransform.position, _shotSpawnTransform.rotation);
    }
    
}
