using UnityEngine;
using UnityEngine.UI;

public class SketchPaperBackground : MonoBehaviour
{
    [Header("Background Setup")]
    public BackgroundType backgroundType = BackgroundType.Skybox;
    public Camera targetCamera;
    
    [Header("UI Canvas Settings")]
    [Tooltip("Solo para UIImage - Sorting order del Canvas. Números más negativos = más atrás")]
    public int canvasSortingOrder = -1000;
    public RenderMode canvasRenderMode = RenderMode.ScreenSpaceCamera;
    
    [Header("Canvas Position")]
    [Tooltip("Distancia en Z del Canvas. Números más altos = más lejos")]
    public float canvasZDistance = 100f;
    [Tooltip("Offset manual en X, Y, Z para ajustar posición")]
    public Vector3 canvasPositionOffset = Vector3.zero;
    
    [Header("Camera Following")]
    [Tooltip("Si el Canvas debe seguir la cámara en X")]
    public bool followCameraX = true;
    [Tooltip("Si el Canvas debe seguir la cámara en Y")]
    public bool followCameraY = true;
    [Tooltip("Si el Canvas debe seguir la cámara en Z (generalmente false para fondo fijo)")]
    public bool followCameraZ = false;
    [Tooltip("Posición Z fija del mundo cuando followCameraZ es false")]
    public float fixedWorldZ = 0f;
    
    [Header("Material Settings")]
    public Material paperMaterial;
    public bool createMaterialAutomatically = true;
    
    [Header("Custom Textures")]
    public Texture2D customPaperTexture;
    public Sprite customPaperSprite; // Para UI Image
    
    [Header("Paper Properties")]
    [ColorUsage(false)] public Color paperColor = new Color(0.98f, 0.97f, 0.95f, 1f);
    [ColorUsage(false)] public Color lineColor = new Color(0.92f, 0.91f, 0.89f, 1f);
    public float gridSize = 80f;
    [Range(0.001f, 0.02f)] public float lineWidth = 0.003f;
    
    [Header("Sketch Animation")]
    [Range(0f, 10f)] public float animationSpeed = 1f;
    [Range(0f, 1f)] public float sketchIntensity = 0.2f;  // Aumentado para que sea más visible
    public float sketchScale = 2f;
    
    [Header("Paper Wrinkles")]
    public float wrinkleScale = 15f;
    [Range(0f, 1f)] public float wrinkleIntensity = 0.12f;
    [Range(0f, 2f)] public float wrinkleSpeed = 0.1f;
    
    [Header("Noise")]
    public float noiseScale = 200f;
    [Range(0f, 1f)] public float noiseIntensity = 0.03f;
    
    public enum BackgroundType
    {
        Skybox,
        Quad,
        CameraBackground,
        UIImage
    }
    
    GameObject backgroundQuad;
    Material instanceMaterial;
    SketchTextureGenerator textureGenerator;
    
    // Para UI Image
    Canvas backgroundCanvas;
    Image backgroundImage;
    RectTransform imageRect;
    
    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        SetupBackground();
    }
    
    void SetupBackground()
    {
        // Crear o encontrar generador de texturas
        textureGenerator = FindFirstObjectByType<SketchTextureGenerator>();
        if (textureGenerator == null)
        {
            GameObject genGO = new GameObject("Sketch Texture Generator");
            textureGenerator = genGO.AddComponent<SketchTextureGenerator>();
        }
        
        // Crear material si es necesario
        if (createMaterialAutomatically || paperMaterial == null)
        {
            CreatePaperMaterial();
        }
        
        // Configurar según el tipo de fondo
        switch (backgroundType)
        {
            case BackgroundType.Skybox:
                SetupSkybox();
                break;
            case BackgroundType.Quad:
                SetupQuad();
                break;
            case BackgroundType.CameraBackground:
                SetupCameraBackground();
                break;
            case BackgroundType.UIImage:
                SetupUIImage();
                break;
        }
    }
    
    void CreatePaperMaterial()
    {
        Shader paperShader;
        
        // Usar shader UI si el tipo es UIImage
        if (backgroundType == BackgroundType.UIImage)
        {
            paperShader = Shader.Find("UI/SketchPaper");
            if (paperShader == null)
            {
                Debug.LogWarning("Shader 'UI/SketchPaper' no encontrado. Usando UI/Default.");
                paperShader = Shader.Find("UI/Default");
            }
        }
        else
        {
            paperShader = Shader.Find("Custom/SketchPaperBackground");
            if (paperShader == null)
            {
                Debug.LogWarning("Shader 'Custom/SketchPaperBackground' no encontrado. Usando Standard.");
                paperShader = Shader.Find("Standard");
            }
        }
        
        instanceMaterial = new Material(paperShader);
        instanceMaterial.name = "Generated Paper Background";
        
        UpdateMaterialProperties();
        
        // Crear textura de papel base si no hay personalizada
        if (customPaperTexture == null)
        {
            customPaperTexture = GenerateDefaultPaperTexture();
        }
        
        // Asignar textura base
        if (instanceMaterial.HasProperty("_MainTex"))
        {
            instanceMaterial.SetTexture("_MainTex", customPaperTexture);
        }
        
        // Asignar al generador de texturas para que genere la textura de boceto
        textureGenerator.targetMaterial = instanceMaterial;
        textureGenerator.GenerateSketchTexture();
        
        // Debug para verificar que las texturas están asignadas
        if (instanceMaterial.HasProperty("_SketchTex"))
        {
            var sketchTex = instanceMaterial.GetTexture("_SketchTex");
            Debug.Log($"Sketch texture assigned: {sketchTex != null}");
        }
    }
    
    void UpdateMaterialProperties()
    {
        if (instanceMaterial == null) return;
        
        // Propiedades del papel
        if (instanceMaterial.HasProperty("_PaperColor"))
            instanceMaterial.SetColor("_PaperColor", paperColor);
        if (instanceMaterial.HasProperty("_LineColor"))
            instanceMaterial.SetColor("_LineColor", lineColor);
        if (instanceMaterial.HasProperty("_GridSize"))
            instanceMaterial.SetFloat("_GridSize", gridSize);
        if (instanceMaterial.HasProperty("_LineWidth"))
            instanceMaterial.SetFloat("_LineWidth", lineWidth);
            
        // Propiedades de animación
        if (instanceMaterial.HasProperty("_AnimationSpeed"))
            instanceMaterial.SetFloat("_AnimationSpeed", animationSpeed);
        if (instanceMaterial.HasProperty("_SketchIntensity"))
            instanceMaterial.SetFloat("_SketchIntensity", sketchIntensity);
        if (instanceMaterial.HasProperty("_SketchScale"))
            instanceMaterial.SetFloat("_SketchScale", sketchScale);
            
        // Propiedades de arrugas
        if (instanceMaterial.HasProperty("_WrinkleScale"))
            instanceMaterial.SetFloat("_WrinkleScale", wrinkleScale);
        if (instanceMaterial.HasProperty("_WrinkleIntensity"))
            instanceMaterial.SetFloat("_WrinkleIntensity", wrinkleIntensity);
        if (instanceMaterial.HasProperty("_WrinkleSpeed"))
            instanceMaterial.SetFloat("_WrinkleSpeed", wrinkleSpeed);
        
        // Para UI Image, configurar propiedades especiales
        if (backgroundType == BackgroundType.UIImage)
        {
            if (instanceMaterial.HasProperty("_Color"))
                instanceMaterial.SetColor("_Color", Color.white);
            
            // Asegurar que el sketch funcione verificando la textura
            if (!instanceMaterial.HasProperty("_SketchTex") || instanceMaterial.GetTexture("_SketchTex") == null)
            {
                Debug.LogWarning("Sketch texture no encontrada. Regenerando...");
                if (textureGenerator != null)
                {
                    textureGenerator.GenerateSketchTexture();
                }
            }
        }
        
        // Propiedades de ruido
        if (instanceMaterial.HasProperty("_NoiseScale"))
            instanceMaterial.SetFloat("_NoiseScale", noiseScale);
        if (instanceMaterial.HasProperty("_NoiseIntensity"))
            instanceMaterial.SetFloat("_NoiseIntensity", noiseIntensity);
    }
    
    void SetupSkybox()
    {
        // Crear material skybox si no existe
        if (instanceMaterial.shader.name != "Skybox/6 Sided")
        {
            // Convertir material a skybox o crear uno nuevo
            Material skyboxMat = new Material(Shader.Find("Skybox/6 Sided"));
            // Aquí podrías asignar la textura generada a todas las caras
            RenderSettings.skybox = skyboxMat;
        }
        else
        {
            RenderSettings.skybox = instanceMaterial;
        }
    }
    
    void SetupQuad()
    {
        if (backgroundQuad == null)
        {
            backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backgroundQuad.name = "Paper Background Quad";
            
            // Remover collider
            var col = backgroundQuad.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
        }
        
        // Posicionar detrás de todo
        if (targetCamera != null)
        {
            float distance = targetCamera.farClipPlane * 0.9f;
            backgroundQuad.transform.position = targetCamera.transform.position + targetCamera.transform.forward * distance;
            backgroundQuad.transform.LookAt(targetCamera.transform);
            backgroundQuad.transform.Rotate(0, 180, 0); // Voltear para que sea visible
            
            // Escalar para cubrir toda la vista
            float height = 2f * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float width = height * targetCamera.aspect;
            backgroundQuad.transform.localScale = new Vector3(width * 1.1f, height * 1.1f, 1f);
        }
        
        // Asignar material
        var renderer = backgroundQuad.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = instanceMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
    
    void SetupCameraBackground()
    {
        if (targetCamera != null)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = paperColor;
        }
    }
    
    void Update()
    {
        // Actualizar propiedades en tiempo real si cambian
        if (instanceMaterial != null)
        {
            UpdateMaterialProperties();
        }
        
        // Actualizar posición del quad si la cámara se mueve
        if (backgroundType == BackgroundType.Quad && backgroundQuad != null && targetCamera != null)
        {
            float distance = targetCamera.farClipPlane * 0.9f;
            backgroundQuad.transform.position = targetCamera.transform.position + targetCamera.transform.forward * distance;
        }
        
        // Actualizar posición del Canvas si es World Space
        if (backgroundType == BackgroundType.UIImage && canvasRenderMode == RenderMode.WorldSpace)
        {
            UpdateCanvasWorldPosition();
        }
    }
    
    [ContextMenu("Regenerate Sketch Texture")]
    public void RegenerateSketchTexture()
    {
        if (textureGenerator != null)
        {
            Debug.Log("Regenerating sketch texture...");
            textureGenerator.GenerateSketchTexture();
            
            // Verificar que se asignó correctamente
            if (instanceMaterial != null && instanceMaterial.HasProperty("_SketchTex"))
            {
                var sketchTex = instanceMaterial.GetTexture("_SketchTex");
                Debug.Log($"Sketch texture after regeneration: {sketchTex != null}");
                
                if (sketchTex != null)
                {
                    Debug.Log($"Sketch texture size: {sketchTex.width}x{sketchTex.height}");
                }
            }
        }
        else
        {
            Debug.LogError("TextureGenerator not found!");
        }
    }
    
    [ContextMenu("Debug Material Properties")]
    public void DebugMaterialProperties()
    {
        if (instanceMaterial == null)
        {
            Debug.LogError("No material found!");
            return;
        }
        
        Debug.Log($"Material: {instanceMaterial.name}");
        Debug.Log($"Shader: {instanceMaterial.shader.name}");
        
        // Verificar propiedades del sketch
        if (instanceMaterial.HasProperty("_SketchTex"))
        {
            var tex = instanceMaterial.GetTexture("_SketchTex");
            Debug.Log($"_SketchTex: {tex?.name} ({tex?.width}x{tex?.height})");
        }
        
        if (instanceMaterial.HasProperty("_SketchIntensity"))
        {
            Debug.Log($"_SketchIntensity: {instanceMaterial.GetFloat("_SketchIntensity")}");
        }
        
        if (instanceMaterial.HasProperty("_AnimationSpeed"))
        {
            Debug.Log($"_AnimationSpeed: {instanceMaterial.GetFloat("_AnimationSpeed")}");
        }
    }
    
    [ContextMenu("Force Regenerate Everything")]
    public void ForceRegenerateEverything()
    {
        Debug.Log("Force regenerating everything...");
        
        // Limpiar texturas y sprites existentes
        customPaperTexture = null;
        customPaperSprite = null;
        
        // Regenerar material
        if (instanceMaterial != null)
        {
            DestroyImmediate(instanceMaterial);
            instanceMaterial = null;
        }
        
        // Recrear todo desde cero
        if (backgroundType == BackgroundType.UIImage)
        {
            SetupUIImage();
        }
        else
        {
            CreatePaperMaterial();
        }
        
        Debug.Log("Everything regenerated!");
    }
    
    // Métodos públicos para cambiar texturas
    public void SetCustomPaperTexture(Texture2D texture)
    {
        customPaperTexture = texture;
        
        // Aplicar a material existente
        if (instanceMaterial != null && instanceMaterial.HasProperty("_MainTex"))
        {
            instanceMaterial.SetTexture("_MainTex", customPaperTexture);
        }
        
        // Recrear sprite para UI Image si es necesario
        if (backgroundType == BackgroundType.UIImage && backgroundImage != null && customPaperTexture != null)
        {
            Sprite newSprite = Sprite.Create(customPaperTexture,
                new Rect(0, 0, customPaperTexture.width, customPaperTexture.height),
                Vector2.one * 0.5f, 100f);
            backgroundImage.sprite = newSprite;
        }
    }
    
    public void SetCustomPaperSprite(Sprite sprite)
    {
        customPaperSprite = sprite;
        
        if (backgroundType == BackgroundType.UIImage && backgroundImage != null)
        {
            backgroundImage.sprite = customPaperSprite;
        }
    }
    
    // Método para cambiar el sorting order en runtime
    public void SetCanvasSortingOrder(int newOrder)
    {
        canvasSortingOrder = newOrder;
        if (backgroundCanvas != null)
        {
            backgroundCanvas.sortingOrder = canvasSortingOrder;
            Debug.Log($"Canvas sorting order cambiado a: {canvasSortingOrder}");
        }
    }
    
    [ContextMenu("Move Canvas to Back (-2000)")]
    public void MoveCanvasToBack()
    {
        SetCanvasSortingOrder(-2000);
    }
    
    [ContextMenu("Move Canvas to Front (-10)")]
    public void MoveCanvasToFront()
    {
        SetCanvasSortingOrder(-10);
    }
    
    void UpdateCanvasWorldPosition()
    {
        if (backgroundCanvas == null || targetCamera == null) return;
        
        Vector3 cameraPos = targetCamera.transform.position;
        
        // Calcular posición del Canvas basada en qué ejes debe seguir
        Vector3 canvasPosition = Vector3.zero;
        
        // Seguir cámara en X si está habilitado
        if (followCameraX)
        {
            canvasPosition.x = cameraPos.x;
        }
        else
        {
            canvasPosition.x = canvasPositionOffset.x;
        }
        
        // Seguir cámara en Y si está habilitado
        if (followCameraY)
        {
            canvasPosition.y = cameraPos.y;
        }
        else
        {
            canvasPosition.y = canvasPositionOffset.y;
        }
        
        // Seguir cámara en Z si está habilitado, sino usar posición fija
        if (followCameraZ)
        {
            canvasPosition.z = cameraPos.z + canvasZDistance;
        }
        else
        {
            canvasPosition.z = fixedWorldZ + canvasZDistance;
        }
        
        // Aplicar offset final
        canvasPosition += canvasPositionOffset;
        
        backgroundCanvas.transform.position = canvasPosition;
        
        // Hacer que mire hacia la cámara
        backgroundCanvas.transform.LookAt(targetCamera.transform);
        backgroundCanvas.transform.Rotate(0, 180, 0); // Voltear para que sea visible
        
        // Escalar para cubrir la vista de la cámara
        float distance = Vector3.Distance(targetCamera.transform.position, backgroundCanvas.transform.position);
        float height = 2f * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
        float width = height * targetCamera.aspect;
        
        // Ajustar el RectTransform del Canvas
        var canvasRect = backgroundCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = new Vector2(width, height);
        }
    }
    
    // Métodos públicos para cambiar posición
    public void SetCanvasZDistance(float distance)
    {
        canvasZDistance = distance;
        
        if (backgroundCanvas != null)
        {
            if (canvasRenderMode == RenderMode.ScreenSpaceCamera)
            {
                backgroundCanvas.planeDistance = canvasZDistance;
            }
            else if (canvasRenderMode == RenderMode.WorldSpace)
            {
                UpdateCanvasWorldPosition();
            }
            
            Debug.Log($"Canvas Z distance cambiada a: {canvasZDistance}");
        }
    }
    
    public void SetCanvasPositionOffset(Vector3 offset)
    {
        canvasPositionOffset = offset;
        
        if (canvasRenderMode == RenderMode.WorldSpace)
        {
            UpdateCanvasWorldPosition();
        }
    }
    
    [ContextMenu("Move Canvas Far Back (Z +200)")]
    public void MoveCanvasFarBack()
    {
        SetCanvasZDistance(200f);
    }
    
    [ContextMenu("Move Canvas Near (Z +50)")]
    public void MoveCanvasNear()
    {
        SetCanvasZDistance(50f);
    }
    
    [ContextMenu("Move Canvas to World Space")]
    public void MoveCanvasToWorldSpace()
    {
        canvasRenderMode = RenderMode.WorldSpace;
        if (backgroundCanvas != null)
        {
            backgroundCanvas.renderMode = RenderMode.WorldSpace;
            UpdateCanvasWorldPosition();
        }
    }
    
    // Métodos para configurar seguimiento de cámara
    public void SetCameraFollowing(bool x, bool y, bool z)
    {
        followCameraX = x;
        followCameraY = y;
        followCameraZ = z;
        
        if (canvasRenderMode == RenderMode.WorldSpace)
        {
            UpdateCanvasWorldPosition();
        }
        
        Debug.Log($"Camera following set to: X={x}, Y={y}, Z={z}");
    }
    
    public void SetFixedWorldZ(float zPos)
    {
        fixedWorldZ = zPos;
        
        if (canvasRenderMode == RenderMode.WorldSpace && !followCameraZ)
        {
            UpdateCanvasWorldPosition();
        }
    }
    
    [ContextMenu("Set Background Mode (Follow X,Y only)")]
    public void SetBackgroundMode()
    {
        SetCameraFollowing(true, true, false);
        SetFixedWorldZ(0f);
    }
    
    [ContextMenu("Set Fixed Position Mode (No following)")]
    public void SetFixedPositionMode()
    {
        SetCameraFollowing(false, false, false);
    }
    
    [ContextMenu("Set Full Following Mode (X,Y,Z)")]
    public void SetFullFollowingMode()
    {
        SetCameraFollowing(true, true, true);
    }
    
    void SetupUIImage()
    {
        // Crear Canvas si no existe
        CreateUICanvas();
        
        // Crear UI Image
        CreateUIBackgroundImage();
        
        // Crear material con shader especial
        CreatePaperMaterial();
        
        // Configurar Image con material personalizado
        ConfigureUIImage();
    }
    
    void CreateUICanvas()
    {
        // No usar canvas existente para evitar conflictos
        // Crear siempre nuevo canvas específico para el fondo
        GameObject canvasGO = new GameObject("Sketch Paper Background Canvas");
        canvasGO.transform.SetParent(transform);
        backgroundCanvas = canvasGO.AddComponent<Canvas>();
        
        // Configurar modo de renderizado
        backgroundCanvas.renderMode = canvasRenderMode;
        
        if (canvasRenderMode == RenderMode.ScreenSpaceCamera)
        {
            // Usar la cámara target para renderizar detrás de todo
            backgroundCanvas.worldCamera = targetCamera;
            backgroundCanvas.planeDistance = canvasZDistance;
        }
        else if (canvasRenderMode == RenderMode.WorldSpace)
        {
            // Para World Space, posicionar manualmente
            UpdateCanvasWorldPosition();
        }
        
        // Sorting order muy negativo para estar detrás
        backgroundCanvas.sortingOrder = canvasSortingOrder;
        
        // Añadir CanvasScaler
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // NO añadir GraphicRaycaster para que no interfiera con el gameplay
        Debug.Log($"Canvas creado con sorting order: {canvasSortingOrder} y render mode: {canvasRenderMode}");
    }
    
    void CreateUIBackgroundImage()
    {
        // Crear GameObject para la imagen
        GameObject imageGO = new GameObject("Sketch Paper Background Image");
        imageGO.transform.SetParent(backgroundCanvas.transform, false);
        
        // Añadir componente Image
        backgroundImage = imageGO.AddComponent<Image>();
        imageRect = imageGO.GetComponent<RectTransform>();
        
        // Configurar RectTransform para cubrir toda la pantalla
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;
        
        // Poner detrás de todo en el Canvas
        imageGO.transform.SetAsFirstSibling();
        
        // Deshabilitar raycast completamente
        backgroundImage.raycastTarget = false;
        
        // Configurar para que esté muy atrás
        var canvasGroup = imageGO.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = imageGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    
    void ConfigureUIImage()
    {
        if (backgroundImage == null || instanceMaterial == null) return;
        
        // Crear sprite por defecto si no hay personalizado
        if (customPaperSprite == null)
        {
            // Usar la textura que ya tenemos (personalizada o generada)
            Texture2D textureToUse = customPaperTexture;
            
            // Si aún no hay textura, crear una por defecto
            if (textureToUse == null)
            {
                textureToUse = GenerateDefaultPaperTexture();
            }
            
            // Crear sprite desde la textura
            customPaperSprite = Sprite.Create(textureToUse, 
                new Rect(0, 0, textureToUse.width, textureToUse.height), 
                Vector2.one * 0.5f, 100f);
            customPaperSprite.name = "Generated Paper Sprite";
        }
        
        // Asignar sprite
        backgroundImage.sprite = customPaperSprite;
        
        // Asignar material personalizado al Image
        backgroundImage.material = instanceMaterial;
        
        // Configurar como Tiled para repetir la textura
        backgroundImage.type = Image.Type.Tiled;
        
        // Configurar color
        backgroundImage.color = paperColor;
    }
    
    void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            DestroyImmediate(instanceMaterial);
        }
        
        if (backgroundQuad != null)
        {
            DestroyImmediate(backgroundQuad);
        }
        
        if (backgroundCanvas != null && backgroundCanvas.transform.parent == transform)
        {
            DestroyImmediate(backgroundCanvas.gameObject);
        }
    }
    
    Texture2D GenerateDefaultPaperTexture()
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        texture.name = "Generated Default Paper Texture";
        
        Color[] pixels = new Color[size * size];
        
        // Color base del papel
        Color baseColor = paperColor;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Ruido sutil para textura de papel
                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f;
                float fineNoise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.04f;
                
                // Efecto de fibras de papel
                float fibers = Mathf.PerlinNoise(x * 0.8f, y * 0.1f) * 0.02f;
                fibers += Mathf.PerlinNoise(x * 0.1f, y * 0.8f) * 0.02f;
                
                Color pixelColor = baseColor;
                float variation = noise + fineNoise + fibers - 0.07f;
                
                pixelColor.r += variation;
                pixelColor.g += variation;
                pixelColor.b += variation;
                
                // Mantener en rango válido
                pixelColor.r = Mathf.Clamp01(pixelColor.r);
                pixelColor.g = Mathf.Clamp01(pixelColor.g);
                pixelColor.b = Mathf.Clamp01(pixelColor.b);
                
                pixels[y * size + x] = pixelColor;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        Debug.Log($"Generated default paper texture: {size}x{size}");
        return texture;
    }
    
    void OnValidate()
    {
        // Actualizar en tiempo real en el editor
        if (Application.isPlaying && instanceMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }
}