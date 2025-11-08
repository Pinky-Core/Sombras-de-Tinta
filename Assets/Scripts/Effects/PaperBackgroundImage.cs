using UnityEngine;
using UnityEngine.UI;

public class PaperBackgroundImage : MonoBehaviour
{
    [Header("Paper Texture")]
    public Sprite paperSprite;
    public bool createCanvasAutomatically = true;
    
    [Header("Image Settings")]
    [ColorUsage(false)] public Color tintColor = Color.white;
    public Image.Type imageType = Image.Type.Tiled;
    public float pixelsPerUnit = 100f;
    
    [Header("Animation")]
    public bool animateTexture = false;
    [Range(0f, 1f)] public float animationSpeed = 0.1f;
    public Vector2 animationDirection = Vector2.one;
    
    Canvas backgroundCanvas;
    Image backgroundImage;
    RectTransform canvasRect;
    RectTransform imageRect;
    Vector2 startUVRect;
    
    void Start()
    {
        SetupBackgroundImage();
    }
    
    void SetupBackgroundImage()
    {
        // Crear Canvas si no existe
        if (createCanvasAutomatically)
        {
            CreateCanvas();
        }
        
        // Crear Image si no existe
        CreateBackgroundImage();
        
        // Configurar Image
        ConfigureImage();
    }
    
    void CreateCanvas()
    {
        // Buscar canvas existente primero
        backgroundCanvas = FindFirstObjectByType<Canvas>();
        
        if (backgroundCanvas == null)
        {
            // Crear nuevo canvas
            GameObject canvasGO = new GameObject("Paper Background Canvas");
            backgroundCanvas = canvasGO.AddComponent<Canvas>();
            backgroundCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            backgroundCanvas.sortingOrder = -100; // Muy atrás
            
            // Añadir CanvasScaler para que se adapte a diferentes resoluciones
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Añadir GraphicRaycaster
            canvasGO.AddComponent<GraphicRaycaster>();
            
            canvasRect = canvasGO.GetComponent<RectTransform>();
        }
        else
        {
            canvasRect = backgroundCanvas.GetComponent<RectTransform>();
        }
    }
    
    void CreateBackgroundImage()
    {
        // Crear GameObject para la imagen
        GameObject imageGO = new GameObject("Paper Background Image");
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
    }
    
    void ConfigureImage()
    {
        if (backgroundImage == null) return;
        
        // Asignar sprite
        if (paperSprite != null)
        {
            backgroundImage.sprite = paperSprite;
        }
        else if (Application.isPlaying)
        {
            Debug.LogWarning("No paper sprite assigned to PaperBackgroundImage!");
        }
        
        // Configurar tipo de imagen
        backgroundImage.type = imageType;
        
        // Configurar color
        backgroundImage.color = tintColor;
        
        // Si es Tiled, configurar pixelsPerUnit
        if (imageType == Image.Type.Tiled)
        {
            backgroundImage.pixelsPerUnitMultiplier = 100f / pixelsPerUnit;
        }
        
        // Deshabilitar raycast para que no bloquee UI
        backgroundImage.raycastTarget = false;
        
        // Guardar UV inicial para animación
        if (paperSprite != null)
        {
            startUVRect = new Vector2(paperSprite.rect.x / paperSprite.texture.width,
                                    paperSprite.rect.y / paperSprite.texture.height);
        }
    }
    
    void Update()
    {
        // Animar textura si está habilitado
        if (animateTexture && backgroundImage != null && paperSprite != null)
        {
            AnimateTexture();
        }
    }
    
    void AnimateTexture()
    {
        // Crear efecto de movimiento sutil de la textura
        Vector2 offset = animationDirection * Time.time * animationSpeed * 0.01f;
        
        // Aplicar offset usando uvRect (solo funciona con sprites)
        if (backgroundImage.type == Image.Type.Tiled)
        {
            // Para imagenes tiled, podemos usar fillAmount o crear un material custom
            // Por simplicidad, usaremos transform
            imageRect.anchoredPosition = offset * 10f;
        }
    }
    
    // Método para cambiar la textura en runtime
    public void SetPaperSprite(Sprite newSprite)
    {
        paperSprite = newSprite;
        if (backgroundImage != null)
        {
            backgroundImage.sprite = paperSprite;
        }
    }
    
    // Método para cambiar el color
    public void SetTintColor(Color newColor)
    {
        tintColor = newColor;
        if (backgroundImage != null)
        {
            backgroundImage.color = tintColor;
        }
    }
    
    // Método para cambiar el tipo de imagen
    public void SetImageType(Image.Type newType)
    {
        imageType = newType;
        if (backgroundImage != null)
        {
            backgroundImage.type = imageType;
            if (imageType == Image.Type.Tiled)
            {
                backgroundImage.pixelsPerUnitMultiplier = 100f / pixelsPerUnit;
            }
        }
    }
    
    [ContextMenu("Create Paper Texture Sprite")]
    public void CreatePaperTextureSprite()
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
                float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.04f;
                float fineNoise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.02f;
                
                Color pixelColor = baseColor;
                pixelColor.r += noise + fineNoise - 0.03f;
                pixelColor.g += noise + fineNoise - 0.03f;
                pixelColor.b += noise + fineNoise - 0.03f;
                
                pixels[y * size + x] = pixelColor;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Crear sprite desde la textura
        paperSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        paperSprite.name = "Generated Paper Sprite";
        
        // Aplicar si ya existe la imagen
        if (backgroundImage != null)
        {
            backgroundImage.sprite = paperSprite;
        }
        
        Debug.Log("Paper texture sprite created!");
    }
    
    void OnValidate()
    {
        // Actualizar en tiempo real en el editor
        if (Application.isPlaying && backgroundImage != null)
        {
            ConfigureImage();
        }
    }
}