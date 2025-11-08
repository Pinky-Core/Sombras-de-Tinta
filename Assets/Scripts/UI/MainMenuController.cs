using UnityEngine;
using UnityEngine.UI;

// Controlador único de Menú Principal: Jugar (muestra panel de niveles), Opciones y Salir.
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;      // Contiene botones: Jugar, Opciones, Salir
    public GameObject levelsPanel;    // Panel con los botones de niveles
    public GameObject optionsPanel;   // Panel de opciones (placeholder)

    [Header("Level Configuration")]
    public LevelButtonData[] levelButtonsData;  // Configuración detallada de cada botón
    
    [System.Serializable]
    public class LevelButtonData
    {
        [Header("Button Reference")]
        public Button button;                    // Referencia al botón
        
        [Header("Level Info")]
        public string levelName = "Level 1";    // Nombre mostrado del nivel
        public string sceneName = "Level1";     // Nombre exacto de la escena
        public Sprite levelPreview;             // Imagen de preview (opcional)
        public int requiredLevel = 0;           // Nivel requerido para desbloquear (0 = siempre desbloqueado)
        
        [Header("Visual Settings")]
        public Color unlockedColor = Color.white;
        public Color lockedColor = Color.gray;
        public bool showLevelNumber = true;
        
        [Header("UI References (Optional)")]
        public Text levelNameText;              // Texto del nombre del nivel
        public Text levelNumberText;            // Texto del número del nivel
        public Image levelPreviewImage;         // Imagen de preview
        public Image buttonBackground;          // Fondo del botón para cambiar color
        public GameObject lockedIcon;           // Ícono de bloqueado
    }

    void Start()
    {
        ShowMain();
        WireLevelButtons();
    }

    void WireLevelButtons()
    {
        if (levelButtonsData == null) return;
        
        for (int i = 0; i < levelButtonsData.Length; i++)
        {
            var levelData = levelButtonsData[i];
            if (levelData.button == null) continue;

            int idx = i;
            SetupLevelButton(levelData, idx);
        }
    }
    
    void SetupLevelButton(LevelButtonData levelData, int levelIndex)
    {
        bool unlocked = IsLevelUnlocked(levelData, levelIndex);
        
        // Configurar interactividad
        levelData.button.interactable = unlocked;
        
        // Configurar evento click
        levelData.button.onClick.RemoveAllListeners();
        if (unlocked)
        {
            levelData.button.onClick.AddListener(() => LoadLevel(levelData.sceneName));
        }
        
        // Configurar visuales
        UpdateLevelButtonVisuals(levelData, levelIndex, unlocked);
    }
    
    void UpdateLevelButtonVisuals(LevelButtonData levelData, int levelIndex, bool unlocked)
    {
        // Actualizar nombre del nivel
        if (levelData.levelNameText != null)
        {
            levelData.levelNameText.text = levelData.levelName;
            levelData.levelNameText.color = unlocked ? levelData.unlockedColor : levelData.lockedColor;
        }
        
        // Actualizar número del nivel
        if (levelData.levelNumberText != null && levelData.showLevelNumber)
        {
            levelData.levelNumberText.text = (levelIndex + 1).ToString();
            levelData.levelNumberText.color = unlocked ? levelData.unlockedColor : levelData.lockedColor;
        }
        
        // Actualizar imagen de preview
        if (levelData.levelPreviewImage != null)
        {
            if (levelData.levelPreview != null)
            {
                levelData.levelPreviewImage.sprite = levelData.levelPreview;
            }
            levelData.levelPreviewImage.color = unlocked ? Color.white : Color.gray;
        }
        
        // Actualizar fondo del botón
        if (levelData.buttonBackground != null)
        {
            levelData.buttonBackground.color = unlocked ? levelData.unlockedColor : levelData.lockedColor;
        }
        
        // Mostrar/ocultar ícono de bloqueado
        if (levelData.lockedIcon != null)
        {
            levelData.lockedIcon.SetActive(!unlocked);
        }
    }

    bool IsLevelUnlocked(LevelButtonData levelData, int levelIndex)
    {
        // Si no requiere nivel específico, está siempre desbloqueado
        if (levelData.requiredLevel <= 0) return true;
        
        // Verificar usando el sistema de progreso existente
        int buildIndex = GetBuildIndexByName(levelData.sceneName);
        if (buildIndex < 0) return true; // Si no está en build settings, asumir desbloqueado
        
        return LevelProgress.IsUnlocked(buildIndex);
    }
    
    void LoadLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is empty!");
            return;
        }
        
        Debug.Log($"Loading level: {sceneName}");
        SceneLoader.LoadScene(sceneName);
    }

    int GetBuildIndexByName(string scene)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == scene) return i;
        }
        return -1;
    }

    // Botón: Jugar (muestra panel de niveles)
    public void OnPlay()
    {
        SetActive(mainPanel, false);
        SetActive(levelsPanel, true);
        SetActive(optionsPanel, false);
        WireLevelButtons(); // refrescar desbloqueos
    }

    // Botón: Opciones
    public void OnOptions()
    {
        SetActive(mainPanel, false);
        SetActive(levelsPanel, false);
        SetActive(optionsPanel, true);
    }

    // Botón: Volver (desde Niveles u Opciones)
    public void OnBack()
    {
        ShowMain();
    }

    // Botón: Salir del juego
    public void OnQuit()
    {
        SceneLoader.Quit();
    }

    void ShowMain()
    {
        SetActive(mainPanel, true);
        SetActive(levelsPanel, false);
        SetActive(optionsPanel, false);
    }

    static void SetActive(GameObject go, bool v)
    {
        if (go) go.SetActive(v);
    }
    
    // Métodos adicionales para gestión de niveles
    
    [ContextMenu("Refresh Level Buttons")]
    public void RefreshLevelButtons()
    {
        WireLevelButtons();
        Debug.Log("Level buttons refreshed!");
    }
    
    public void UnlockLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelButtonsData.Length) return;
        
        var levelData = levelButtonsData[levelIndex];
        int buildIndex = GetBuildIndexByName(levelData.sceneName);
        
        if (buildIndex >= 0)
        {
            LevelProgress.UnlockUpTo(buildIndex);
            SetupLevelButton(levelData, levelIndex);
            Debug.Log($"Level {levelIndex + 1} ({levelData.levelName}) unlocked!");
        }
    }
    
    public void UnlockAllLevels()
    {
        // Encontrar el buildIndex más alto de todos los niveles
        int maxBuildIndex = -1;
        for (int i = 0; i < levelButtonsData.Length; i++)
        {
            var levelData = levelButtonsData[i];
            int buildIndex = GetBuildIndexByName(levelData.sceneName);
            if (buildIndex > maxBuildIndex)
            {
                maxBuildIndex = buildIndex;
            }
        }
        
        // Desbloquear hasta el nivel más alto
        if (maxBuildIndex >= 0)
        {
            LevelProgress.UnlockUpTo(maxBuildIndex);
        }
        
        WireLevelButtons();
        Debug.Log("All levels unlocked!");
    }
    
    [ContextMenu("Debug Level States")]
    public void DebugLevelStates()
    {
        Debug.Log("=== Level States ===");
        for (int i = 0; i < levelButtonsData.Length; i++)
        {
            var levelData = levelButtonsData[i];
            bool unlocked = IsLevelUnlocked(levelData, i);
            Debug.Log($"Level {i + 1}: {levelData.levelName} ({levelData.sceneName}) - {(unlocked ? "UNLOCKED" : "LOCKED")}");
        }
    }
    
    // Métodos adicionales para gestión del progreso
    
    public void UnlockLevelByName(string sceneName)
    {
        int buildIndex = GetBuildIndexByName(sceneName);
        if (buildIndex >= 0)
        {
            LevelProgress.UnlockUpTo(buildIndex);
            WireLevelButtons(); // Refrescar todos los botones
            Debug.Log($"Level '{sceneName}' unlocked!");
        }
        else
        {
            Debug.LogWarning($"Scene '{sceneName}' not found in build settings!");
        }
    }
    
    public int GetMaxUnlockedLevel()
    {
        return LevelProgress.GetMaxUnlocked();
    }
    
    public bool IsLevelUnlockedByIndex(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelButtonsData.Length) return false;
        return IsLevelUnlocked(levelButtonsData[levelIndex], levelIndex);
    }
    
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteKey("MaxUnlockedLevelIndex"); // Key del LevelProgress
        PlayerPrefs.Save();
        WireLevelButtons();
        Debug.Log("All progress reset! Only first level is now unlocked.");
    }
    
    // Método para configurar automáticamente desde arrays antiguos (compatibilidad)
    [ContextMenu("Convert Old Arrays to New System")]
    public void ConvertOldArraysToNewSystem()
    {
        // Este método ayuda a migrar desde el sistema anterior
        // Se puede usar si tienes los arrays anteriores todavía
        
        Debug.Log("Use this method to manually convert old levelButtons[] and sceneNames[] arrays to the new LevelButtonData[] system.");
        Debug.Log("1. Create LevelButtonData entries in the inspector");
        Debug.Log("2. Assign each button and scene name");
        Debug.Log("3. Configure additional settings like level names, previews, etc.");
    }
}
