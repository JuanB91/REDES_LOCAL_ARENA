using System;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(LocalInputs))]
public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local { get; private set; }
    public LocalInputs LocalInputs { get; private set; }
    
    public override void Spawned()
    {
        LocalInputs = GetComponent<LocalInputs>();
        
        //si mi objeto tiene la autoridad necesaria seteo todo lo local, sino desactivo lo local
        if (Object.HasInputAuthority) 
        {
            Local = this;
            LocalInputs.enabled = true;
        
        }
        else 
        {
            LocalInputs.enabled = false;
        
        }
    }
    
}
