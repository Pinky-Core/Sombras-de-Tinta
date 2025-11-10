using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelFinishTrigger : MonoBehaviour
{
    [Header("Level Finish Settings")]
    [Tooltip("Nombre de la escena actual (debe coincidir con Build Settings)")]
    public string currentLevelName;
    [Tooltip("Nombre de la siguiente escena a desbloquear (opcional)")]
    public string nextLevelName;
    [Tooltip("Si debe desbloquear automáticamente el siguiente nivel")]
    public bool autoUnlockNextLevel = true;
    
    [Header("Visual Effects")]
    public ParticleSystem finishParticles;
    public AudioClip finishSound;
    public GameObject celebrationEffect;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private bool levelCompleted = false;
    private AudioSource audioSource;
    
    void Start()
    {
        // Configurar trigger
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
        
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && finishSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Auto-detectar nombre del nivel actual si no está asignado
        if (string.IsNullOrEmpty(currentLevelName))
        {
            currentLevelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Level Finish Trigger setup for: {currentLevelName}");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Verificar si es el jugador
        if (IsPlayer(other) && !levelCompleted)
        {
            CompleteLevel();
        }
    }
    
    bool IsPlayer(Collider other)
    {
        // Verificar por tag primero
        if (other.CompareTag("Player"))
            return true;
            
        // Verificar por componentes comunes del jugador
        if (other.GetComponent<PlayerController3D>() != null)
            return true;
            
        if (other.GetComponent<PlayerControllerSide3D>() != null)
            return true;
            
        return false;
    }
    
    void CompleteLevel()
    {
        levelCompleted = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"Level {currentLevelName} completed!");
        }
        
        // Efectos visuales y audio
        PlayFinishEffects();
        
        // Actualizar progreso del nivel
        UpdateLevelProgress();
        
        // Mostrar panel de victoria
        ShowVictoryPanel();

        TutorialEventBus.RaiseTaskCompleted(TutorialTaskIds.FinishRoute);
    }
    
    void PlayFinishEffects()
    {
        // Partículas
        if (finishParticles != null)
        {
            finishParticles.Play();
        }
        
        // Sonido
        if (audioSource != null && finishSound != null)
        {
            audioSource.PlayOneShot(finishSound);
        }
        
        // Efecto de celebración
        if (celebrationEffect != null)
        {
            celebrationEffect.SetActive(true);
        }
    }
    
    void UpdateLevelProgress()
    {
        // Obtener build index del nivel actual
        int currentBuildIndex = GetBuildIndexByName(currentLevelName);
        
        if (currentBuildIndex >= 0)
        {
            // Desbloquear el siguiente nivel
            LevelProgress.UnlockUpTo(currentBuildIndex + 1);
            
            if (showDebugInfo)
            {
                Debug.Log($"Level progress updated. Build index {currentBuildIndex} completed.");
                Debug.Log($"Max unlocked level: {LevelProgress.GetMaxUnlocked()}");
            }
        }
        else
        {
            Debug.LogWarning($"Scene '{currentLevelName}' not found in Build Settings!");
        }
        
        // Si hay un nivel específico siguiente, desbloquearlo
        if (autoUnlockNextLevel && !string.IsNullOrEmpty(nextLevelName))
        {
            int nextBuildIndex = GetBuildIndexByName(nextLevelName);
            if (nextBuildIndex >= 0)
            {
                LevelProgress.UnlockUpTo(nextBuildIndex);
                if (showDebugInfo)
                {
                    Debug.Log($"Next level '{nextLevelName}' unlocked!");
                }
            }
        }
    }
    
    void ShowVictoryPanel()
    {
        // Buscar el LevelVictoryUI en la escena
        var victoryUI = FindFirstObjectByType<LevelVictoryUI>();
        
        if (victoryUI != null)
        {
            victoryUI.ShowVictoryPanel(currentLevelName, nextLevelName);
        }
        else
        {
            Debug.LogWarning("LevelVictoryUI not found in scene! Create one to show victory panel.");
            
            // Fallback: pausar el juego
            Time.timeScale = 0f;
        }
    }
    
    int GetBuildIndexByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return -1;
        
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return i;
        }
        return -1;
    }
    
    // Método público para completar nivel manualmente (por código)
    public void ForceCompleteLevel()
    {
        if (!levelCompleted)
        {
            CompleteLevel();
        }
    }
    
    // Método para resetear el trigger (útil para testing)
    public void ResetTrigger()
    {
        levelCompleted = false;
        
        if (celebrationEffect != null)
        {
            celebrationEffect.SetActive(false);
        }
        
        if (finishParticles != null)
        {
            finishParticles.Stop();
        }
    }
    
    void OnDrawGizmos()
    {
        // Dibujar el área del trigger en el editor
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = levelCompleted ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (collider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }
}
