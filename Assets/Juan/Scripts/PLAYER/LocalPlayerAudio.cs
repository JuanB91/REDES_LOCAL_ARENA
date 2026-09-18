using Fusion;
using UnityEngine;

public class LocalPlayerAudio : NetworkBehaviour
{
    [SerializeField]
    private AudioListener audioListener;

    public override void Spawned()
    {
        if (audioListener == null)
        {
            audioListener =
                GetComponentInChildren<AudioListener>();
        }

        if (audioListener != null)
        {
            audioListener.enabled =
                Object.HasInputAuthority;
        }
    }
}