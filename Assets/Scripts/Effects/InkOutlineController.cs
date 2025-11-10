using UnityEngine;

/// <summary>
/// Permite editar en el inspector las propiedades clave del shader SombrasDeTinta/InkOutline
/// sin necesidad de abrir el material manualmente.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class InkOutlineController : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField, Tooltip("Si el Renderer tiene varios materiales indica cuál usa el shader de tinta.")]
    private int materialIndex = 0;

    [Header("Outline")]
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField, Range(0f, 0.05f)] private float outlineWidth = 0.01f;

    [Header("Sombreado tipo cel")]
    [SerializeField] private Color baseColor = Color.black;
    [SerializeField, Range(1, 6)] private int shadeSteps = 3;
    [SerializeField] private float lightIntensity = 1f;

    [Header("Ruido (efecto dibujo)")]
    [SerializeField] private Texture sketchNoise;
    [SerializeField] private float noiseTiling = 2f;
    [SerializeField] private float noiseSpeed = 1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int ShadeStepsId = Shader.PropertyToID("_ShadeSteps");
    private static readonly int SketchNoiseId = Shader.PropertyToID("_SketchNoise");
    private static readonly int NoiseTilingId = Shader.PropertyToID("_NoiseTiling");
    private static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int LightIntensityId = Shader.PropertyToID("_LightIntensity");

    private void OnEnable()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
        ApplySettings();
    }

    private void OnValidate()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
        ApplySettings();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            ApplySettings();
        }
    }

    private Material TargetMaterial
    {
        get
        {
            if (targetRenderer == null)
            {
                return null;
            }

            var materials = targetRenderer.sharedMaterials;
            if (materials == null || materialIndex < 0 || materialIndex >= materials.Length)
            {
                return null;
            }

            return materials[materialIndex];
        }
    }

    private void ApplySettings()
    {
        var mat = TargetMaterial;
        if (mat == null)
        {
            return;
        }

        if (outlineColor != null) mat.SetColor(OutlineColorId, outlineColor);
        mat.SetFloat(OutlineWidthId, outlineWidth);
        mat.SetColor(BaseColorId, baseColor);
        mat.SetFloat(ShadeStepsId, Mathf.Clamp(shadeSteps, 1, 6));
        mat.SetFloat(LightIntensityId, lightIntensity);
        mat.SetFloat(NoiseTilingId, noiseTiling);
        mat.SetFloat(NoiseSpeedId, noiseSpeed);
        if (sketchNoise != null)
        {
            mat.SetTexture(SketchNoiseId, sketchNoise);
        }
    }
}
