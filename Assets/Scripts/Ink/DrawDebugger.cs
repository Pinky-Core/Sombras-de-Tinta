using UnityEngine;

public class DrawDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;
    public bool forceEnableRedraw = false;
    
    InkDrawer _inkDrawer;
    ShapeDetector _shapeDetector;
    
    void Start()
    {
        _inkDrawer = FindFirstObjectByType<InkDrawer>();
        _shapeDetector = FindFirstObjectByType<ShapeDetector>();
        
        if (showDebugLogs)
        {
            Debug.Log($"InkDrawer found: {_inkDrawer != null}");
            Debug.Log($"ShapeDetector found: {_shapeDetector != null}");
            
            if (_inkDrawer != null)
            {
                Debug.Log($"Shape detection enabled: {_inkDrawer.enableShapeDetection}");
                Debug.Log($"Detection radius: {_inkDrawer.shapeDetectionRadius}");
                Debug.Log($"Current ink: {_inkDrawer.currentInk}");
            }
        }
        
        // Forzar habilitación de redraw en todos los enemigos si está activado
        if (forceEnableRedraw)
        {
            var enemies = FindObjectsByType<EnemyRedraw>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.canBeRedrawn = true;
                if (showDebugLogs)
                    Debug.Log($"Enabled redraw for {enemy.name}");
            }
        }
    }
    
    void Update()
    {
        if (!showDebugLogs) return;
        
        // Debug de input
        if (InputProvider.LeftMouseDown())
        {
            Debug.Log("Mouse down detected");
        }
        
        if (InputProvider.LeftMouseUp())
        {
            Debug.Log("Mouse up detected");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Mostrar radio de detección de enemigos
        var enemies = FindObjectsByType<EnemyRedraw>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.canBeRedrawn)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(enemy.transform.position, 3f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(enemy.transform.position, Vector3.one * 0.5f);
            }
        }
        
        // Mostrar radio de detección del InkDrawer
        if (_inkDrawer != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(InputProvider.MouseScreenPosition());
            mouseWorldPos.z = 0; // Para 2.5D
            Gizmos.DrawWireSphere(mouseWorldPos, _inkDrawer.shapeDetectionRadius);
        }
    }
    
    // Método para test manual desde inspector
    [ContextMenu("Test Circle Detection")]
    public void TestCircleDetection()
    {
        var enemies = FindObjectsByType<EnemyRedraw>(FindObjectsSortMode.None);
        if (enemies.Length > 0)
        {
            var enemy = enemies[0];
            if (enemy.CanRedraw)
            {
                enemy.ApplyRedraw(true); // Círculo = aliado
                Debug.Log("Manual circle test - Enemy converted to ally!");
            }
            else
            {
                Debug.Log("Enemy cannot be redrawn. Enable canBeRedrawn first.");
            }
        }
    }
    
    [ContextMenu("Test Cross Detection")]
    public void TestCrossDetection()
    {
        var enemies = FindObjectsByType<EnemyRedraw>(FindObjectsSortMode.None);
        if (enemies.Length > 0)
        {
            var enemy = enemies[0];
            if (enemy.CanKill)
            {
                enemy.ApplyDeath(); // Cruz = muerte
                Debug.Log("Manual cross test - Enemy killed!");
            }
            else
            {
                Debug.Log("Enemy cannot be killed.");
            }
        }
    }
    
    [ContextMenu("Enable Redraw on All Enemies")]
    public void EnableRedrawOnAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyRedraw>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.canBeRedrawn = true;
            Debug.Log($"Enabled redraw for {enemy.name}");
        }
    }
}