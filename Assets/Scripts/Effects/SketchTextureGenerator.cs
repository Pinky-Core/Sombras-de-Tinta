using UnityEngine;

public class SketchTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureSize = 1024;
    public int lineCount = 300;
    public float lineIntensity = 0.3f;
    
    [Header("Line Properties")]
    public float minLineLength = 0.02f;
    public float maxLineLength = 0.08f;
    public float lineThickness = 0.8f;
    public bool randomRotation = true;
    
    [Header("Patterns")]
    public bool addCrossHatch = true;
    public bool addRandomLines = true;
    public bool addCircularScribbles = true;
    
    [Header("Auto Generate")]
    public bool generateOnStart = true;
    public Material targetMaterial;
    
    Texture2D sketchTexture;
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateSketchTexture();
        }
    }
    
    [ContextMenu("Generate Sketch Texture")]
    public void GenerateSketchTexture()
    {
        if (sketchTexture != null)
        {
            DestroyImmediate(sketchTexture);
        }
        
        sketchTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        sketchTexture.name = "Generated Sketch Texture";
        
        // Llenar con blanco (fondo)
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        // Agregar diferentes tipos de líneas
        if (addRandomLines)
            AddRandomLines(pixels);
            
        if (addCrossHatch)
            AddCrossHatchPattern(pixels);
            
        if (addCircularScribbles)
            AddCircularScribbles(pixels);
        
        sketchTexture.SetPixels(pixels);
        sketchTexture.Apply();
        
        // Asignar al material si está especificado
        if (targetMaterial != null)
        {
            targetMaterial.SetTexture("_SketchTex", sketchTexture);
        }
        
        Debug.Log("Sketch texture generated!");
    }
    
    void AddRandomLines(Color[] pixels)
    {
        for (int i = 0; i < lineCount; i++)
        {
            // Punto de inicio aleatorio
            Vector2 start = new Vector2(
                Random.Range(0, textureSize),
                Random.Range(0, textureSize)
            );
            
            // Longitud y dirección aleatoria
            float length = Random.Range(minLineLength, maxLineLength) * textureSize;
            float angle = randomRotation ? Random.Range(0f, 360f) : Random.Range(-30f, 30f);
            
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            
            Vector2 end = start + direction * length;
            
            // Dibujar línea
            DrawLine(pixels, start, end, lineThickness);
        }
    }
    
    void AddCrossHatchPattern(Color[] pixels)
    {
        int hatchLines = lineCount / 6;
        
        // Líneas muy finas para sombreado sutil
        for (int i = 0; i < hatchLines; i++)
        {
            float y = Random.Range(0, textureSize);
            Vector2 start = new Vector2(0, y);
            Vector2 end = new Vector2(textureSize * Random.Range(0.1f, 0.4f), y + textureSize * Random.Range(0.05f, 0.15f));
            DrawLine(pixels, start, end, lineThickness * 0.3f);
        }
        
        // Líneas diagonales en la otra dirección, más sutiles
        for (int i = 0; i < hatchLines; i++)
        {
            float y = Random.Range(0, textureSize);
            Vector2 start = new Vector2(textureSize, y);
            Vector2 end = new Vector2(textureSize * Random.Range(0.6f, 0.9f), y + textureSize * Random.Range(0.05f, 0.15f));
            DrawLine(pixels, start, end, lineThickness * 0.3f);
        }
    }
    
    void AddCircularScribbles(Color[] pixels)
    {
        int scribbleCount = lineCount / 10;
        
        for (int i = 0; i < scribbleCount; i++)
        {
            Vector2 center = new Vector2(
                Random.Range(textureSize * 0.1f, textureSize * 0.9f),
                Random.Range(textureSize * 0.1f, textureSize * 0.9f)
            );
            
            float radius = Random.Range(textureSize * 0.01f, textureSize * 0.03f);
            int segments = Random.Range(6, 12);
            
            Vector2 lastPoint = center + Vector2.right * radius;
            
            for (int j = 1; j <= segments; j++)
            {
                float angle = (float)j / segments * 360f + Random.Range(-30f, 30f);
                float currentRadius = radius * Random.Range(0.5f, 1.5f);
                
                Vector2 currentPoint = center + new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius
                );
                
                DrawLine(pixels, lastPoint, currentPoint, lineThickness * 0.2f);
                lastPoint = currentPoint;
            }
        }
    }
    
    void DrawLine(Color[] pixels, Vector2 start, Vector2 end, float thickness)
    {
        int steps = Mathf.RoundToInt(Vector2.Distance(start, end));
        if (steps == 0) return;
        
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 point = Vector2.Lerp(start, end, t);
            
            // Agregar rugosidad muy sutil para líneas más naturales
            point += new Vector2(
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.2f, 0.2f)
            );
            
            DrawPixel(pixels, point, thickness);
        }
    }
    
    void DrawPixel(Color[] pixels, Vector2 position, float thickness)
    {
        int centerX = Mathf.RoundToInt(position.x);
        int centerY = Mathf.RoundToInt(position.y);
        int radius = Mathf.RoundToInt(thickness / 2f);
        
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance <= thickness / 2f)
                    {
                        int index = y * textureSize + x;
                        
                        // Mezclar color con intensidad más sutil
                        float alpha = 1f - (distance / (thickness / 2f));
                        alpha *= lineIntensity * Random.Range(0.3f, 0.7f);
                        
                        Color currentColor = pixels[index];
                        Color lineColor = Color.black;
                        pixels[index] = Color.Lerp(currentColor, lineColor, alpha);
                    }
                }
            }
        }
    }
    
    // Para guardar la textura como asset
    [ContextMenu("Save Texture as Asset")]
    public void SaveTextureAsAsset()
    {
        if (sketchTexture == null)
        {
            GenerateSketchTexture();
        }
        
        #if UNITY_EDITOR
        byte[] bytes = sketchTexture.EncodeToPNG();
        string path = $"Assets/Textures/GeneratedSketchTexture_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        System.IO.File.WriteAllBytes(path, bytes);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"Texture saved to: {path}");
        #endif
    }
    
    void OnDestroy()
    {
        if (sketchTexture != null)
        {
            DestroyImmediate(sketchTexture);
        }
    }
}