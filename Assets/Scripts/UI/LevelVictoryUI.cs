using UnityEngine;
using UnityEngine.UI;

public class LevelVictoryUI : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject victoryPanel;
    public CanvasGroup victoryCanvasGroup;
    
    [Header("UI Text Elements")]
    public Text victoryTitle;
    public Text levelNameText;
    public Text congratulationsText;
    public Text nextLevelText;
    
    [Header("UI Buttons")]
    public Button restartButton;
    public Button nextLevelButton;
    public Button mainMenuButton;
    public Button exitGameButton;
    
    [Header("Victory Settings")]
    public string victoryTitleText = "¡NIVEL COMPLETADO!";
    public string congratulationsTextContent = "¡Excelente trabajo!";
    public bool pauseGameOnVictory = true;
    public bool showNextLevelButton = true;
    
    [Header("Animation")]
    public bool animatePanel = true;
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    public AudioClip victorySound;
    public AudioClip buttonClickSound;
    
    private string currentLevelName;
    private string nextLevelName;
    private AudioSource audioSource;
    private bool isShowing = false;
    
    void Awake()
    {
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (victorySound != null || buttonClickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Configurar panel inicial
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        
        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.alpha = 0f;
        }
        
        // Configurar botones
        SetupButtons();
    }
    
    void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => OnRestartLevel());
        }
        
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(() => OnNextLevel());
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() => OnMainMenu());
        }
        
        if (exitGameButton != null)
        {
            exitGameButton.onClick.AddListener(() => OnExitGame());
        }
    }
    
    public void ShowVictoryPanel(string levelName, string nextLevel = null)
    {
        if (isShowing) return;
        
        currentLevelName = levelName;
        nextLevelName = nextLevel;
        isShowing = true;
        
        // Pausar juego si está configurado
        if (pauseGameOnVictory)
        {
            Time.timeScale = 0f;
        }
        
        // Reproducir sonido de victoria
        PlayVictorySound();
        
        // Actualizar textos
        UpdateUITexts();
        
        // Configurar botones
        ConfigureButtons();
        
        // Mostrar panel
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        
        // Animar entrada
        if (animatePanel && victoryCanvasGroup != null)
        {
            StartCoroutine(AnimatePanel(true));
        }
        else if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.alpha = 1f;
        }
    }
    
    void UpdateUITexts()
    {
        if (victoryTitle != null)
        {
            victoryTitle.text = victoryTitleText;
        }
        
        if (levelNameText != null)
        {
            levelNameText.text = !string.IsNullOrEmpty(currentLevelName) ? 
                $"Nivel: {currentLevelName}" : "";
        }
        
        if (congratulationsText != null)
        {
            congratulationsText.text = congratulationsTextContent;
        }
        
        if (nextLevelText != null)
        {
            if (!string.IsNullOrEmpty(nextLevelName))
            {
                nextLevelText.text = $"Siguiente: {nextLevelName}";
            }
            else
            {
                nextLevelText.text = "";
            }
        }
    }
    
    void ConfigureButtons()
    {
        // Configurar botón de siguiente nivel
        if (nextLevelButton != null)
        {
            bool hasNextLevel = !string.IsNullOrEmpty(nextLevelName);
            nextLevelButton.gameObject.SetActive(showNextLevelButton && hasNextLevel);
            nextLevelButton.interactable = hasNextLevel;
        }
    }
    
    void PlayVictorySound()
    {
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }
    }
    
    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    System.Collections.IEnumerator AnimatePanel(bool show)
    {
        if (victoryCanvasGroup == null) yield break;
        
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Usar unscaled para que funcione con Time.timeScale = 0
            float progress = elapsed / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            victoryCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            yield return null;
        }
        
        victoryCanvasGroup.alpha = endAlpha;
        
        if (!show && victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }
    
    // Métodos de botones
    
    public void OnRestartLevel()
    {
        PlayButtonSound();
        Debug.Log("Restarting level...");
        
        // Restaurar timeScale antes de cambiar escena
        Time.timeScale = 1f;
        
        // Recargar escena actual
        string sceneName = !string.IsNullOrEmpty(currentLevelName) ? 
            currentLevelName : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        SceneLoader.LoadScene(sceneName);
    }
    
    public void OnNextLevel()
    {
        if (string.IsNullOrEmpty(nextLevelName))
        {
            Debug.LogWarning("Next level name not set!");
            return;
        }
        
        PlayButtonSound();
        Debug.Log($"Loading next level: {nextLevelName}");
        
        // Restaurar timeScale antes de cambiar escena
        Time.timeScale = 1f;
        
        SceneLoader.LoadScene(nextLevelName);
    }
    
    public void OnMainMenu()
    {
        PlayButtonSound();
        Debug.Log("Returning to main menu...");
        
        // Restaurar timeScale antes de cambiar escena
        Time.timeScale = 1f;
        
        // Cargar menú principal
        SceneLoader.LoadScene("MainMenu");
    }
    
    public void OnExitGame()
    {
        PlayButtonSound();
        Debug.Log("Exiting game...");
        
        // Restaurar timeScale
        Time.timeScale = 1f;
        
        // Salir del juego
        SceneLoader.Quit();
    }
    
    public void HideVictoryPanel()
    {
        if (!isShowing) return;
        
        if (animatePanel && victoryCanvasGroup != null)
        {
            StartCoroutine(AnimatePanel(false));
        }
        else
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }
        
        // Restaurar timeScale
        if (pauseGameOnVictory)
        {
            Time.timeScale = 1f;
        }
        
        isShowing = false;
    }
    
    // Métodos públicos para personalización
    
    public void SetVictoryTexts(string title, string congratulations, string nextLevel = null)
    {
        victoryTitleText = title;
        congratulationsTextContent = congratulations;
        
        if (isShowing)
        {
            UpdateUITexts();
        }
    }
    
    public void SetNextLevel(string nextLevel)
    {
        nextLevelName = nextLevel;
        
        if (isShowing)
        {
            UpdateUITexts();
            ConfigureButtons();
        }
    }
    
    // Método para mostrar el panel manualmente (para testing)
    [ContextMenu("Test Show Victory Panel")]
    public void TestShowVictoryPanel()
    {
        ShowVictoryPanel("TestLevel", "NextTestLevel");
    }
    
    void OnDestroy()
    {
        // Asegurar que el timeScale se restaure
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}