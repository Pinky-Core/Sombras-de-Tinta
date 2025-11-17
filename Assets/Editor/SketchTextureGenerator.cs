using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera texturas de líneas de lápiz y ruido para el shader SombrasDeTinta/PencilSketchPlatform.
/// Ejecutar desde Tools/Generate Sketch Textures.
/// </summary>
public static class SketchTextureGenerator
{
    private const int TextureSize = 512;
    private const float Frequency = 16f;
    private const float LineWidth = 0.18f; // fracción del periodo ocupada por la línea
    private const string OutputFolder = "Assets/Textures/Sketch";

    [MenuItem("Tools/Generate Sketch Textures")]
    public static void Generate()
    {
        Directory.CreateDirectory(OutputFolder);

        // Set base
        var line1 = GenerateHatch(0f, Frequency, LineWidth);
        var line2 = GenerateHatch(60f * Mathf.Deg2Rad, Frequency * 0.85f, LineWidth * 0.9f);
        var noise = GenerateNoise();

        SaveTexture(line1, Path.Combine(OutputFolder, "LineTex1.png"));
        SaveTexture(line2, Path.Combine(OutputFolder, "LineTex2.png"));
        SaveTexture(noise, Path.Combine(OutputFolder, "NoiseTex.png"));

        // Hand-drawn sketchier set
        var sketchLine1 = GenerateSketchyHatch(12f * Mathf.Deg2Rad);
        var sketchLine2 = GenerateSketchyHatch(78f * Mathf.Deg2Rad);
        var sketchNoise = GeneratePaperNoise();

        SaveTexture(sketchLine1, Path.Combine(OutputFolder, "LineTex1_Sketch.png"));
        SaveTexture(sketchLine2, Path.Combine(OutputFolder, "LineTex2_Sketch.png"));
        SaveTexture(sketchNoise, Path.Combine(OutputFolder, "NoiseTex_Sketch.png"));

        GenerateMaterial("PencilSketchPlatform", "PencilSketchPlatform.mat", line1, line2, noise);
        GenerateMaterial("PencilSketchPlatform", "PencilSketchPlatform_Sketch.mat", sketchLine1, sketchLine2, sketchNoise);

        AssetDatabase.Refresh();
        Debug.Log($"Sketch textures generated in {OutputFolder}");
    }

    private static Texture2D GenerateHatch(float angle, float frequency, float lineWidth)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / TextureSize, (y + 0.5f) / TextureSize);
                float phase = Vector2.Dot(uv, dir) * frequency * Mathf.PI * 2f;
                float s = Mathf.Sin(phase);

                // Intensidad de línea (0 = negro, 1 = blanco)
                float d = Mathf.Clamp01(1f - Mathf.Abs(s));
                float line = Mathf.SmoothStep(0f, 1f, d / lineWidth);

                // Oscilar grosor a lo largo del trazo para que no sea perfecto
                float wobble = Mathf.PerlinNoise(uv.x * 8f, uv.y * 8f) * 0.15f;
                line = Mathf.Clamp01(line + wobble);

                float value = Mathf.Lerp(0.1f, 1f, line);
                pixels[y * TextureSize + x] = new Color(value, value, value, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Variante más “a mano”: grosor variable, pequeñas irregularidades y gaps.
    /// </summary>
    private static Texture2D GenerateSketchyHatch(float angleRad)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
        Vector2 dirPerp = new Vector2(-dir.y, dir.x);
        float freq = 11f; // menos denso
        float baseWidth = 0.22f; // un poco más grueso

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / TextureSize, (y + 0.5f) / TextureSize);

                float along = Vector2.Dot(uv, dir) * freq;
                float across = Vector2.Dot(uv, dirPerp);

                // Jitter de línea en el sentido perpendicular
                float jitter = (Mathf.PerlinNoise(along * 0.35f, across * 9f) - 0.5f) * 0.25f;
                float widthNoise = (Mathf.PerlinNoise(along * 0.8f, across * 4f) - 0.5f) * 0.15f;
                float localWidth = Mathf.Max(0.05f, baseWidth + widthNoise);

                float dist = Mathf.Abs(Mathf.Repeat(along + jitter, 1f) - 0.5f);
                float line = Mathf.SmoothStep(0f, 1f, (0.5f - dist) / localWidth);

                // Micro gaps para simular saltos de lápiz
                float gapMask = Mathf.SmoothStep(0.2f, 0.6f, Mathf.PerlinNoise(along * 2.2f, across * 18f));
                line *= gapMask;

                // Textura de carboncillo: grano aleatorio
                float grain = Mathf.PerlinNoise(x * 0.8f, y * 0.8f) * 0.08f + Random.value * 0.02f;

                float value = Mathf.Clamp01(Mathf.Lerp(0.08f, 1f, line) + grain);
                pixels[y * TextureSize + x] = new Color(value, value, value, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateNoise()
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float nx = (x + 0.5f) / TextureSize;
                float ny = (y + 0.5f) / TextureSize;

                float n =
                    0.55f * Mathf.PerlinNoise(nx * 6f, ny * 6f) +
                    0.30f * Mathf.PerlinNoise(nx * 12f, ny * 12f) +
                    0.15f * Mathf.PerlinNoise(nx * 24f, ny * 24f);

                pixels[y * TextureSize + x] = new Color(n, n, n, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GeneratePaperNoise()
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float nx = (x + 0.5f) / TextureSize;
                float ny = (y + 0.5f) / TextureSize;

                float baseNoise =
                    0.6f * Mathf.PerlinNoise(nx * 4f, ny * 4f) +
                    0.25f * Mathf.PerlinNoise(nx * 14f, ny * 14f) +
                    0.15f * Mathf.PerlinNoise(nx * 28f, ny * 28f);

                // Grano fino
                float grain = (Random.value - 0.5f) * 0.08f;
                float v = Mathf.Clamp01(baseNoise + grain);
                pixels[y * TextureSize + x] = new Color(v, v, v, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void SaveTexture(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    private static void GenerateMaterial(string shaderName, string matName, Texture2D line1, Texture2D line2, Texture2D noise)
    {
        Shader shader = Shader.Find($"SombrasDeTinta/{shaderName}");
        if (shader == null)
        {
            Debug.LogWarning($"Shader SombrasDeTinta/{shaderName} no encontrado. Se omite creación de material {matName}.");
            return;
        }

        string matPath = Path.Combine(OutputFolder, matName);
        var mat = new Material(shader);

        mat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f, 1f));
        mat.SetFloat("_Tiling", 6f);
        mat.SetFloat("_Wiggle", 0.05f);
        mat.SetFloat("_Speed", 0.4f);
        mat.SetFloat("_Contrast", 2f);
        mat.SetFloat("_EdgeNoise", 0.5f);

        if (line1) mat.SetTexture("_LineTex1", line1);
        if (line2) mat.SetTexture("_LineTex2", line2);
        if (noise) mat.SetTexture("_NoiseTex", noise);

        AssetDatabase.CreateAsset(mat, matPath);
    }
}
