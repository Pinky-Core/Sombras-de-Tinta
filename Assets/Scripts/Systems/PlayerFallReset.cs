using UnityEngine;

/// <summary>
/// Reinicia el nivel si el jugador cae por debajo de un umbral Y.
/// Adjuntar al jugador.
/// </summary>
public class PlayerFallReset : MonoBehaviour
{
    [Tooltip("Altura mínima antes de reiniciar la escena.")]
    public float fallThresholdY = -10f;

    void Update()
    {
        if (transform.position.y < fallThresholdY)
        {
            SceneLoader.Reload();
        }
    }
}
