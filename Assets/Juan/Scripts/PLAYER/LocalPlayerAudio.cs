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
                GetComponentInChildren<
                    AudioListener
                >(
                    true
                );
        }

        if (audioListener == null)
        {
            Debug.LogWarning(
                $"LOCAL PLAYER AUDIO: " +
                $"AudioListener not found | " +
                $"Player: " +
                $"{Object.InputAuthority.PlayerId}"
            );

            return;
        }

        bool isLocalPlayer =
            Object.HasInputAuthority;

        audioListener.enabled =
            isLocalPlayer;

        if (isLocalPlayer)
        {
            Debug.Log(
                $"LOCAL AUDIO ENABLED | " +
                $"PLAYER: " +
                $"{Object.InputAuthority.PlayerId}"
            );
        }
        else
        {
            Debug.Log(
                $"REMOTE AUDIO DISABLED | " +
                $"PLAYER: " +
                $"{Object.InputAuthority.PlayerId}"
            );
        }
    }

    
    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (audioListener != null)
        {
            audioListener.enabled =
                false;
        }
    }
}