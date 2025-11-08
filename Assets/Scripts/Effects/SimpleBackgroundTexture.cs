using UnityEngine;

public class SimpleBackgroundTexture : MonoBehaviour
{
    [Header("Background Texture")]
    public Texture2D paperTexture;
    public BackgroundType backgroundType = BackgroundType.Quad;
    public Camera targetCamera;
    
    [Header("Texture Settings")]
    [ColorUsage(false)] public Color tintColor = Color.white;
    [Range(0f, 2f)] public float textureScale = 1f;
    public Vector2 textureOffset = Vector2.zero;
    
    [Header("Animation (Optional)")]
    public bool animateTexture = false;
    [Range(0f, 1f)] public float animationSpeed = 0.1f;
    
    public enum BackgroundType
    {
        Quad,
        CameraBackground,
        SkyboxSingleTexture
    }
    
    GameObject backgroundQuad;
    Material backgroundMaterial;
    Vector2 currentOffset;
    
    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        SetupBackground();
    }
    
    void SetupBackground()
    {
        if (paperTexture == null)
        {
            Debug.LogWarning("No paper texture assigned to SimpleBackgroundTexture!");
            return;
        }
        
        switch (backgroundType)
        {
            case BackgroundType.Quad:
                SetupQuad();
                break;
            case BackgroundType.CameraBackground:
                SetupCameraBackground();
                break;
            case BackgroundType.SkyboxSingleTexture:
                SetupSkybox();
                break;
        }
    }
    
    void SetupQuad()
    {
        // Crear el quad si no existe
        if (backgroundQuad == null)
        {
            backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backgroundQuad.name = "Paper Background Quad";
            backgroundQuad.transform.SetParent(transform);
            
            // Remover collider
            var col = backgroundQuad.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
        }
        
        // Crear material
        CreateMaterial();
        
        // Posicionar detrás de todo
        if (targetCamera != null)
        {
            UpdateQuadPosition();
        }
        
        // Asignar material
        var renderer = backgroundQuad.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = backgroundMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
    
    void UpdateQuadPosition()
    {
        if (backgroundQuad == null || targetCamera == null) return;
        
        float distance = targetCamera.farClipPlane * 0.95f;
        backgroundQuad.transform.position = targetCamera.transform.position + targetCamera.transform.forward * distance;
        backgroundQuad.transform.LookAt(targetCamera.transform);
        backgroundQuad.transform.Rotate(0, 180, 0);
        
        // Escalar para cubrir toda la vista con un poco de margen
        float height = 2f * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
        float width = height * targetCamera.aspect;
        backgroundQuad.transform.localScale = new Vector3(width * 1.2f, height * 1.2f, 1f);
    }
    
    void CreateMaterial()
    {
        // Intentar usar shader URP primero, luego Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? 
                       Shader.Find("Universal Render Pipeline/Unlit") ?? 
                       Shader.Find("Standard") ?? 
                       Shader.Find("Legacy Shaders/Diffuse");
        
        backgroundMaterial = new Material(shader);
        backgroundMaterial.name = "Generated Paper Background Material";
        
        // Configurar textura
        if (backgroundMaterial.HasProperty("_BaseMap"))
            backgroundMaterial.SetTexture("_BaseMap", paperTexture);
        else if (backgroundMaterial.HasProperty("_MainTex"))
            backgroundMaterial.SetTexture("_MainTex", paperTexture);
        
        // Configurar color
        if (backgroundMaterial.HasProperty("_BaseColor"))
            backgroundMaterial.SetColor("_BaseColor", tintColor);
        else if (backgroundMaterial.HasProperty("_Color"))
            backgroundMaterial.SetColor("_Color", tintColor);
        else
            backgroundMaterial.color = tintColor;
        
        // Configurar tiling y offset
        UpdateTextureProperties();
    }
    
    void UpdateTextureProperties()
    {
        if (backgroundMaterial == null) return;
        
        Vector2 tiling = Vector2.one * textureScale;
        Vector2 offset = textureOffset + currentOffset;
        
        if (backgroundMaterial.HasProperty("_BaseMap"))
        {
            backgroundMaterial.SetTextureScale("_BaseMap", tiling);
            backgroundMaterial.SetTextureOffset("_BaseMap", offset);
        }
        else if (backgroundMaterial.HasProperty("_MainTex"))
        {
            backgroundMaterial.SetTextureScale("_MainTex", tiling);
            backgroundMaterial.SetTextureOffset("_MainTex", offset);
        }
        
        // Actualizar color si cambió
        if (backgroundMaterial.HasProperty("_BaseColor"))
            backgroundMaterial.SetColor("_BaseColor", tintColor);
        else if (backgroundMaterial.HasProperty("_Color"))
            backgroundMaterial.SetColor("_Color", tintColor);
        else
            backgroundMaterial.color = tintColor;
    }
    
    void SetupCameraBackground()
    {
        if (targetCamera != null && paperTexture != null)
        {
            targetCamera.clearFlags = CameraClearFlags.Skybox;
            
            // Crear material skybox simple
            Material skyboxMat = new Material(Shader.Find("Skybox/6 Sided"));
            
            // Asignar la textura a todas las caras
            skyboxMat.SetTexture("_FrontTex", paperTexture);
            skyboxMat.SetTexture("_BackTex", paperTexture);
            skyboxMat.SetTexture("_LeftTex", paperTexture);
            skyboxMat.SetTexture("_RightTex", paperTexture);
            skyboxMat.SetTexture("_UpTex", paperTexture);
            skyboxMat.SetTexture("_DownTex", paperTexture);
            
            skyboxMat.SetColor("_Tint", tintColor);
            
            RenderSettings.skybox = skyboxMat;
            backgroundMaterial = skyboxMat;
        }
    }
    
    void SetupSkybox()
    {
        SetupCameraBackground(); // Mismo proceso
    }
    
    void Update()
    {
        // Animar textura si está habilitado
        if (animateTexture && backgroundMaterial != null)
        {
            currentOffset += Vector2.one * animationSpeed * Time.deltaTime * 0.01f;
            UpdateTextureProperties();
        }
        
        // Actualizar posición del quad si la cámara se mueve
        if (backgroundType == BackgroundType.Quad && backgroundQuad != null)
        {
            UpdateQuadPosition();
        }
    }
    
    // Método para cambiar la textura en runtime
    public void SetPaperTexture(Texture2D newTexture)
    {
        paperTexture = newTexture;
        if (backgroundMaterial != null)
        {
            if (backgroundMaterial.HasProperty("_BaseMap"))
                backgroundMaterial.SetTexture("_BaseMap", paperTexture);
            else if (backgroundMaterial.HasProperty("_MainTex"))
                backgroundMaterial.SetTexture("_MainTex", paperTexture);
        }
    }
    
    // Para cambiar el color de tinte
    public void SetTintColor(Color newColor)
    {
        tintColor = newColor;
        UpdateTextureProperties();
    }
    
    void OnDestroy()
    {
        if (backgroundMaterial != null)
        {
            DestroyImmediate(backgroundMaterial);
        }
        
        if (backgroundQuad != null)
        {
            DestroyImmediate(backgroundQuad);
        }
    }
    
    void OnValidate()
    {
        // Actualizar en tiempo real en el editor
        if (Application.isPlaying && backgroundMaterial != null)
        {
            UpdateTextureProperties();
        }
    }
    
    // Método para generar una textura de papel simple procedural si no tienes una
    [ContextMenu("Generate Simple Paper Texture")]
    public void GenerateSimplePaperTexture()
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        
        Color[] pixels = new Color[size * size];
        
        // Color base del papel
        Color baseColor = new Color(0.98f, 0.97f, 0.95f, 1f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Ruido sutil para textura de papel
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.05f;
                float fineNoise = Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.02f;
                
                Color pixelColor = baseColor;
                pixelColor.r += noise + fineNoise - 0.035f;
                pixelColor.g += noise + fineNoise - 0.035f;
                pixelColor.b += noise + fineNoise - 0.035f;
                
                pixels[y * size + x] = pixelColor;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        paperTexture = texture;
        
        // Recrear fondo si ya existe
        if (Application.isPlaying)
        {
            SetupBackground();
        }
        
        Debug.Log("Simple paper texture generated!");
    }
}