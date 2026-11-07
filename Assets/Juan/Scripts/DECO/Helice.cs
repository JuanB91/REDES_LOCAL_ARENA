using UnityEngine;

public class Helice : MonoBehaviour
{
    [SerializeField] private float velocidad = 500f;

    void Update()
    {
        transform.Rotate(0, velocidad * Time.deltaTime, 0);
    }
}