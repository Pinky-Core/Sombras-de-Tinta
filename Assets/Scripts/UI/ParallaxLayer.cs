using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Cámara cuyo movimiento genera el parallax.")]
    public Transform cam;

    [Tooltip("Velocidad del efecto parallax para esta capa.")]
    public float parallaxSpeed = 0.5f;

    [Tooltip("Solo se mueve si la cámara se desplaza en X.")]
    public bool onlyWhenCameraMoves = true;

    private float lastCamX;

    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main.transform;

            if (cam == null)
            {
                Debug.LogError("[ParallaxLayer] No se encontró la cámara.");
                enabled = false;
                return;
            }
        }

        lastCamX = cam.position.x;
    }

    private void LateUpdate()
    {
        float camDeltaX = cam.position.x - lastCamX;

        if (onlyWhenCameraMoves && camDeltaX == 0f)
        {
            lastCamX = cam.position.x;
            return;
        }

        // Movimiento basado en desplazamiento de cámara
        float offsetX = camDeltaX * parallaxSpeed;

        transform.position = new Vector3(
            transform.position.x + offsetX,
            transform.position.y,
            transform.position.z
        );

        lastCamX = cam.position.x;
    }
}
