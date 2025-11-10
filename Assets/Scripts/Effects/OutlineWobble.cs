using UnityEngine;

[ExecuteAlways]
public class OutlineWobble : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float intensity = 0.005f;
    [SerializeField] private float baseThickness = 0.01f;

    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");

    private void Update()
    {
        if (material == null)
        {
            return;
        }

        float value = Mathf.Sin(Time.time * speed) * intensity;
        material.SetFloat(OutlineThicknessId, baseThickness + value);
    }
}
