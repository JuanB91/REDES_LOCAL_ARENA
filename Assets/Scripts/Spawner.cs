using UnityEngine;
using Fusion;

public class Spawner : SimulationBehaviour , IPlayerJoined
{
    public NetWorkPlayer prefabPlayer;
   public void PlayerJoined(PlayerRef player) 
    {
        if (Runner.LocalPlayer == player) 
        Runner.Spawn(prefabPlayer);
    
    }
}
