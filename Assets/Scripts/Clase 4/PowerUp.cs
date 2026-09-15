using Fusion;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    [SerializeField] private float _extraSpeed = 5f;
    [SerializeField] private float _duration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority)
            return;

        OnlinePlayer player = other.GetComponent<OnlinePlayer>();

        if (player == null)
            return;

        player.ActivateSpeedPowerUp(_extraSpeed, _duration);

        Debug.Log("¡POWER UP DE VELOCIDAD OBTENIDO!");

        Runner.Despawn(Object);
    }
}