using UnityEngine;

public class AllyIndicatorRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f;  // Grados por segundo
    public Vector3 rotationAxis = Vector3.up;
    
    void Update()
    {
        // Rotar suavemente alrededor del eje Y
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}
