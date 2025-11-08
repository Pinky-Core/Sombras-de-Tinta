using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [Tooltip("Configuración automática basada en la escena actual")]
    public bool autoConfigureFromScene = true;
    
    [Header("Manual Configuration")]
    public string currentLevelName;
    public string nextLevelName;
    
    [Header("Components References")]
    public LevelFinishTrigger finishTrigger;
    public LevelVictoryUI victoryUI;
    
    [Header("Auto Setup")]
    [Tooltip("Crear automáticamente los componentes necesarios si no existen")]
    public bool autoCreateComponents = true;
    
    private static LevelManager _instance;
    public static LevelManager Instance => _instance;
    
    void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Auto-configurar si está habilitado
        if (autoConfigureFromScene)
        {
            AutoConfigureLevel();
        }
        
        // Auto-crear componentes si es necesario
        if (autoCreateComponents)
        {
            AutoCreateComponents();
        }
    }
    
    void AutoConfigureLevel()
    {
        // Obtener nombre del nivel actual
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        currentLevelName = currentScene.name;
        
        // Intentar determinar el siguiente nivel
        if (string.IsNullOrEmpty(nextLevelName))
        {
            nextLevelName = GuessNextLevelName(currentLevelName);
        }
        
        Debug.Log($"LevelManager auto-configured: Current={currentLevelName}, Next={nextLevelName}");
    }
    
    string GuessNextLevelName(string currentLevel)
    {
        // Intentar extraer número del nivel actual y sugerir el siguiente
        
        // Patrones comunes: Level1, Level_1, Level01, etc.
        if (System.Text.RegularExpressions.Regex.IsMatch(currentLevel, @"Level(\d+)"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(currentLevel, @"Level(\d+)");
            if (match.Success)
            {
                int levelNum = int.Parse(match.Groups[1].Value);
                return $"Level{levelNum + 1}";
            }
        }
        
        // Patrón con guión bajo
        if (System.Text.RegularExpressions.Regex.IsMatch(currentLevel, @"Level_(\d+)"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(currentLevel, @"Level_(\d+)");
            if (match.Success)
            {
                int levelNum = int.Parse(match.Groups[1].Value);
                return $"Level_{levelNum + 1}";
            }
        }
        
        // Patrón con ceros (Level01, Level02, etc.)
        if (System.Text.RegularExpressions.Regex.IsMatch(currentLevel, @"Level(\d{2,})"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(currentLevel, @"Level(\d{2,})");
            if (match.Success)
            {
                int levelNum = int.Parse(match.Groups[1].Value);
                string format = new string('0', match.Groups[1].Value.Length);
                return $"Level{(levelNum + 1).ToString(format)}";
            }
        }
        
        return ""; // No se pudo determinar
    }
    
    void AutoCreateComponents()
    {
        // Buscar LevelFinishTrigger
        if (finishTrigger == null)
        {
            finishTrigger = FindFirstObjectByType<LevelFinishTrigger>();
        }
        
        // Buscar LevelVictoryUI
        if (victoryUI == null)
        {
            victoryUI = FindFirstObjectByType<LevelVictoryUI>();
        }
        
        // Crear advertencias si faltan componentes
        if (finishTrigger == null)
        {
            Debug.LogWarning("LevelFinishTrigger not found! Add one to your level for completion detection.");
        }
        else
        {
            // Configurar el trigger con los datos del manager
            if (string.IsNullOrEmpty(finishTrigger.currentLevelName))
            {
                finishTrigger.currentLevelName = currentLevelName;
            }
            
            if (string.IsNullOrEmpty(finishTrigger.nextLevelName))
            {
                finishTrigger.nextLevelName = nextLevelName;
            }
        }
        
        if (victoryUI == null)
        {
            Debug.LogWarning("LevelVictoryUI not found! Add one to your UI Canvas for victory panel.");
        }
    }
    
    // Métodos públicos para controlar el nivel
    
    public void CompleteLevel()
    {
        if (finishTrigger != null)
        {
            finishTrigger.ForceCompleteLevel();
        }
        else
        {
            Debug.LogWarning("No LevelFinishTrigger found to complete level!");
        }
    }
    
    public void ShowVictoryPanel()
    {
        if (victoryUI != null)
        {
            victoryUI.ShowVictoryPanel(currentLevelName, nextLevelName);
        }
        else
        {
            Debug.LogWarning("No LevelVictoryUI found to show victory panel!");
        }
    }
    
    public void RestartLevel()
    {
        if (victoryUI != null)
        {
            victoryUI.OnRestartLevel();
        }
        else
        {
            SceneLoader.LoadScene(currentLevelName);
        }
    }
    
    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            SceneLoader.LoadScene(nextLevelName);
        }
        else
        {
            Debug.LogWarning("Next level name not configured!");
        }
    }
    
    public void ReturnToMainMenu()
    {
        SceneLoader.LoadScene("MainMenu");
    }
    
    // Métodos de configuración
    
    public void SetLevelNames(string current, string next)
    {
        currentLevelName = current;
        nextLevelName = next;
        
        // Actualizar componentes si existen
        if (finishTrigger != null)
        {
            finishTrigger.currentLevelName = current;
            finishTrigger.nextLevelName = next;
        }
    }
    
    public void SetNextLevel(string next)
    {
        nextLevelName = next;
        
        if (finishTrigger != null)
        {
            finishTrigger.nextLevelName = next;
        }
    }
    
    // Métodos de información
    
    public bool IsLevelCompleted()
    {
        if (finishTrigger != null)
        {
            return finishTrigger.GetType()
                .GetField("levelCompleted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(finishTrigger) as bool? ?? false;
        }
        return false;
    }
    
    public string GetCurrentLevelName()
    {
        return currentLevelName;
    }
    
    public string GetNextLevelName()
    {
        return nextLevelName;
    }
    
    // Métodos de debug
    
    [ContextMenu("Complete Level (Debug)")]
    public void DebugCompleteLevel()
    {
        Debug.Log("Debug: Completing level manually...");
        CompleteLevel();
    }
    
    [ContextMenu("Show Victory Panel (Debug)")]
    public void DebugShowVictoryPanel()
    {
        Debug.Log("Debug: Showing victory panel manually...");
        ShowVictoryPanel();
    }
    
    [ContextMenu("Log Level Info")]
    public void LogLevelInfo()
    {
        Debug.Log("=== Level Manager Info ===");
        Debug.Log($"Current Level: {currentLevelName}");
        Debug.Log($"Next Level: {nextLevelName}");
        Debug.Log($"Finish Trigger: {(finishTrigger != null ? "Found" : "Missing")}");
        Debug.Log($"Victory UI: {(victoryUI != null ? "Found" : "Missing")}");
        Debug.Log($"Level Completed: {IsLevelCompleted()}");
        
        // Info del progreso general
        Debug.Log($"Max Unlocked Level (Build Index): {LevelProgress.GetMaxUnlocked()}");
    }
    
    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}