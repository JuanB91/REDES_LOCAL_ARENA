using Fusion;
using UnityEngine;

public enum PlayerInputButton
{
    Jump,
    GhostForward,
    GhostBackward,
    GhostLeft,
    GhostRight
}

public struct PlayerNetworkInput : INetworkInput
{
  
    public Vector2 MoveDirection;

    
    public NetworkButtons Buttons;
}