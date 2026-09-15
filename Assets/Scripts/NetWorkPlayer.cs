using UnityEngine;
using Fusion;

public class NetWorkPlayer : NetworkBehaviour
{
    public float speed = 5f;
    private NetworkTransform _transform;

    public override void Spawned()
    {
        _transform = GetComponent<NetworkTransform>();
    } 


    // Update is called once per frame
    void Update()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
       _transform.transform.position += new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")) * speed * Runner.DeltaTime;
    }
}
