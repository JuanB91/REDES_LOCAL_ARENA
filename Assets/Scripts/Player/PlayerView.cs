using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _shotParticles;
    [SerializeField] private GameObject _playerVisual;

    private NetworkMecanimAnimator _mecanimAnimator;

    [Networked, OnChangedRender(nameof(TriggerShotParticles))] private NetworkBool Firing { get; set; }

    public override void Spawned()
    {
        var weaponComponent = GetComponentInParent<WeaponHandler>();

        if (weaponComponent)
        {
            weaponComponent.OnShot += ()=> Firing = !Firing;
        }
        
        var lifeComponent = GetComponentInParent<LifeHandler>();


        
        _mecanimAnimator = GetComponent<NetworkMecanimAnimator>();

        if (_mecanimAnimator)
        {
            var movementComponent = GetComponentInParent<NetworkCharacterControllerCustom>();

            if (movementComponent)
            {
                movementComponent.OnMoving += MoveAnimation;
            }
        }
    }

    void MoveAnimation(float xValue)
    {
        _mecanimAnimator.Animator.SetFloat("xAxi", xValue);
    }

    void TriggerShotParticles()
    {
        _shotParticles.Play();
    }

    void EnableMeshRender(bool e)
    {
        _playerVisual.SetActive(!e);
    }
}
