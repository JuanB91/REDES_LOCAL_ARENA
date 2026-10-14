using Fusion;

//struct que utilizamos para pasar data de inputs con sus valores a traves de la red
public struct NetworkInputData : INetworkInput//INetworkInput se usa para network inputs structs
{
    //Crear variable de movimiento
    public float movementImput;
    //crear variable de disparo
    public NetworkBool isFirePressed;


    public NetworkButtons networkButtons;//representa un grupo de botones que pueden ser networkeados
}

enum MyButtons
{
    //crear salto
    Jump = 0,
}
