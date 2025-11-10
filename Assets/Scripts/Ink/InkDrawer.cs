using System.Collections.Generic;
using UnityEngine;

public class InkDrawer : MonoBehaviour
{
    [Header("Ink Settings")]
    public float maxInk = 100f;
    public float currentInk = 100f;
    public float costPerMeter = 5f;
    public float brushRadius = 0.25f;
    public float minStep = 0.2f;
    public LayerMask drawMask = ~0; // everything by default

    [Header("Placement")]
    public float offsetFromSurface = 0.01f;

    [Header("2.5D Drawing")]
    public bool useFixedZPlane = true;
    public float fixedZ = 0f;
    
    [Header("Ink Material")]
    public Color inkColor = Color.black;
    public bool useCustomMaterial = false;
    public Material inkMaterialOverride; // Asigna un material URP/Lit aquí si quieres textura propia
    public Texture2D inkTexture;        // Textura opcional si se crea el material en runtime
    
    [Header("Shape Detection")]
    public bool enableShapeDetection = true;
    public float shapeDetectionRadius = 3f; // Radio para detectar enemigos cerca del trazo

    [Header("Ink VFX")]
    [Tooltip("Sistema de partículas para simular gotas que caen del trazo.")]
    public ParticleSystem inkDropletPrefab;
    [Range(0f, 1f)] public float dropletSpawnChance = 0.7f;
    public Vector3 dropletOffset = new Vector3(0f, -0.25f, 0f);

    [Header("Combat Feedback")]
    [Tooltip("Daño base que hace una raya estándar (enemigos con más vida necesitarán varias).")]
    public float lineDamage = 1f;
    [Tooltip("Duración del shake de cámara al matar un enemigo.")]
    public float killShakeDuration = 0.25f;
    [Tooltip("Intensidad del shake de cámara al matar un enemigo.")]
    public float killShakeIntensity = 0.35f;

    Vector3 _lastPoint;
    bool _drawing;
    Material _inkMat;
    ShapeDetector _shapeDetector;
    List<GameObject> _currentTraceInk = new List<GameObject>();

    void Awake()
    {
        currentInk = Mathf.Clamp(currentInk, 0f, maxInk);
        
        // Inicializar detector de formas
        _shapeDetector = gameObject.GetComponent<ShapeDetector>();
        if (_shapeDetector == null && enableShapeDetection)
        {
            _shapeDetector = gameObject.AddComponent<ShapeDetector>();
        }
        // Crear/usar material compatible con URP
        if (inkMaterialOverride)
        {
            _inkMat = new Material(inkMaterialOverride);
        }
        else
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            var shader = urp ? urp : Shader.Find("Standard");
            _inkMat = new Material(shader);
            if (inkTexture)
            {
                if (_inkMat.HasProperty("_BaseMap")) _inkMat.SetTexture("_BaseMap", inkTexture);
                else _inkMat.mainTexture = inkTexture;
            }
            if (_inkMat.HasProperty("_BaseColor")) _inkMat.SetColor("_BaseColor", inkColor);
            else _inkMat.color = inkColor;
        }
    }

    void Update()
    {
        if (Camera.main == null) return;

        // Start/stop drawing with left mouse
        if (InputProvider.LeftMouseDown())
        {
            _drawing = true;
            _lastPoint = Vector3.positiveInfinity; // force place first
            _currentTraceInk.Clear();
            if (_shapeDetector != null) _shapeDetector.StartTrace();
            TutorialEventBus.RaiseTaskCompleted(TutorialTaskIds.DrawLine);
        }
        if (InputProvider.LeftMouseUp())
        {
            _drawing = false;
            
            // Detectar enemigo al finalizar trazo (mecánica simplificada)
            if (enableShapeDetection && _currentTraceInk.Count > 0)
            {
                ProcessSimpleLineDetection();
            }
        }

        if (_drawing && currentInk > 0f)
        {
            Ray ray = Camera.main.ScreenPointToRay(InputProvider.MouseScreenPosition());
            Vector3? placePoint = null;

            // Intentar detectar enemigo SIEMPRE (independiente del modo de dibujo)
            if (Physics.Raycast(ray, out RaycastHit hitEnemy, 200f, drawMask, QueryTriggerInteraction.Ignore))
            {
                var enemy = hitEnemy.collider.GetComponentInParent<EnemyRedraw>();
                if (enemy != null)
                {
                    if (enemy.CanRedraw)
                    {
                        float costRedraw = Mathf.Max(5f, brushRadius * costPerMeter * 2f);
                        if (currentInk >= costRedraw)
                        {
                            // Sin Shift = plataforma, Con Shift = aliado (coherente con HUD)
                            bool toAlly = InputProvider.ShiftHeld();
                            enemy.ApplyRedraw(toAlly);
                            if (toAlly)
                            {
                                TutorialEventBus.RaiseTaskCompleted(TutorialTaskIds.ConvertAlly);
                            }
                            currentInk -= costRedraw;
                            _drawing = false; // Consumir el trazo y detener dibujo para evitar múltiples conversiones
                            _lastPoint = Vector3.positiveInfinity; // Reset para el próximo trazo

                            ShowRedrawFeedback(hitEnemy.point);
                            Debug.Log(toAlly ? "Enemy converted to ally!" : "Enemy converted to platform!");
                        }
                        else
                        {
                            Debug.Log("Not enough ink to redraw enemy");
                        }
                    }
                    else
                    {
                        Debug.Log("Enemy cannot be redrawn");
                    }
                    return; // No colocar tinta si estamos interactuando con enemigo
                }
            }

            // Si no estamos redibujando un enemigo, calcular punto de tinta
            if (useFixedZPlane)
            {
                // Proyectar al plano Z fijo para 2.5D
                if (Mathf.Abs(ray.direction.z) > 1e-4f)
                {
                    float t = (fixedZ - ray.origin.z) / ray.direction.z;
                    if (t > 0f)
                    {
                        Vector3 projectedPoint = ray.origin + ray.direction * t;
                        projectedPoint.y = Mathf.Max(projectedPoint.y, 0.5f);
                        placePoint = projectedPoint;
                    }
                }
            }
            else if (Physics.Raycast(ray, out RaycastHit hit, 200f, drawMask, QueryTriggerInteraction.Ignore))
            {
                // Solo colocar tinta en superficies que no sean el suelo
                if (!hit.collider.CompareTag("Ground") && hit.point.y > 0.1f)
                {
                    placePoint = hit.point + hit.normal * offsetFromSurface;
                }
            }

            if (placePoint.HasValue)
            {
                Vector3 point = placePoint.Value;
                if (useFixedZPlane) point.z = fixedZ;

                float step = Mathf.Max(minStep, brushRadius * 0.75f);
                if (!float.IsFinite(_lastPoint.x) || Vector3.Distance(point, _lastPoint) >= step)
                {
                    float dist = float.IsFinite(_lastPoint.x) ? Vector3.Distance(point, _lastPoint) : step;
                    float cost = dist * costPerMeter;
                    if (currentInk >= cost)
                    {
                        var inkSphere = PlaceInkSphere(point);
                        _currentTraceInk.Add(inkSphere);
                        currentInk -= cost;
                        _lastPoint = point;
                        
                        // Agregar punto al detector de formas
                        if (_shapeDetector != null) _shapeDetector.AddPoint(point);
                    }
                    else if (currentInk > 0.01f)
                    {
                        var inkSphere = PlaceInkSphere(point);
                        _currentTraceInk.Add(inkSphere);
                        currentInk = 0f;
                        
                        // Agregar punto al detector de formas
                        if (_shapeDetector != null) _shapeDetector.AddPoint(point);
                    }
                }
            }
        }

        // La tinta solo se regenera matando enemigos (removido refill manual)
        // if (InputProvider.RefillDown()) currentInk = maxInk;  // Comentado para el prototipo final
    }

    GameObject PlaceInkSphere(Vector3 position)
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "InkPiece";
        s.transform.position = position;
        s.transform.localScale = Vector3.one * (brushRadius * 2f);
        
        // Configurar material con color personalizado
        var rend = s.GetComponent<Renderer>();
        if (rend) 
        {
            var mat = _inkMat != null ? _inkMat : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", inkColor); else mat.color = inkColor;
            if (inkTexture)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", inkTexture);
                else mat.mainTexture = inkTexture;
            }
            rend.material = mat;
        }
        
        // Configurar collider
        var col = s.GetComponent<Collider>();
        var pm = new PhysicsMaterial();
        pm.frictionCombine = PhysicsMaterialCombine.Average;
        pm.bounceCombine = PhysicsMaterialCombine.Minimum;
        pm.dynamicFriction = 0.6f;
        pm.staticFriction = 0.8f;
        col.material = pm;
        s.layer = gameObject.layer; // keep same layer as owner

        SpawnInkDroplets(position);
        
        return s;
    }

    void SpawnInkDroplets(Vector3 position)
    {
        if (inkDropletPrefab == null)
        {
            return;
        }

        if (dropletSpawnChance < 1f && Random.value > dropletSpawnChance)
        {
            return;
        }

        Vector3 spawnPos = position + dropletOffset;
        ParticleSystem droplets = Instantiate(inkDropletPrefab, spawnPos, Quaternion.identity);
        droplets.Play();
        var main = droplets.main;
        float lifetime = main.duration + main.startLifetime.constantMax + 0.5f;
        Destroy(droplets.gameObject, Mathf.Max(0.1f, lifetime));
    }

    // Método público para regenerar tinta (solo desde sistema de combate)
    public void RegenerateInk(float amount)
    {
        currentInk += amount;
        currentInk = Mathf.Clamp(currentInk, 0f, maxInk);
    }
    
    // Método para verificar si se puede dibujar
    public bool CanDraw(float cost)
    {
        return currentInk >= cost;
    }
    
    // Método para obtener el porcentaje de tinta restante
    public float GetInkPercentage()
    {
        return currentInk / maxInk;
    }
    
    // Método para mostrar feedback visual de redraw
    void ShowRedrawFeedback(Vector3 position)
    {
        // Crear efecto visual temporal en el punto de redraw
        GameObject feedback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        feedback.name = "RedrawFeedback";
        feedback.transform.position = position;
        feedback.transform.localScale = Vector3.one * 0.5f;
        
        // Configurar material de feedback
        var rend = feedback.GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 1f, 0f, 0.8f);  // Amarillo brillante
            mat.SetFloat("_Emission", 1f);
            rend.material = mat;
        }
        
        // Remover collider
        var col = feedback.GetComponent<Collider>();
        if (col) Destroy(col);
        
        // Destruir después de un tiempo
        Destroy(feedback, 0.3f);
    }
    
    void ProcessSimpleLineDetection()
    {
        Debug.Log($"ProcessSimpleLineDetection called with {_currentTraceInk.Count} ink points");
        
        // Calcular centro del trazo para buscar enemigos cerca
        Vector3 traceCenter = Vector3.zero;
        int validPoints = 0;
        
        foreach (var ink in _currentTraceInk)
        {
            if (ink != null)
            {
                traceCenter += ink.transform.position;
                validPoints++;
            }
        }
        
        Debug.Log($"Valid ink points: {validPoints}");
        
        if (validPoints == 0) 
        {
            Debug.Log("No valid ink points found");
            return;
        }
        
        traceCenter /= validPoints;
        Debug.Log($"Trace center: {traceCenter}, Detection radius: {shapeDetectionRadius}");
        
        // Buscar enemigos en el área del trazo
        Collider[] nearbyColliders = Physics.OverlapSphere(traceCenter, shapeDetectionRadius, drawMask);
        Debug.Log($"Found {nearbyColliders.Length} nearby colliders");
        
        EnemyRedraw targetEnemy = null;
        
        foreach (var col in nearbyColliders)
        {
            Debug.Log($"Checking collider: {col.name}");
            var enemy = col.GetComponentInParent<EnemyRedraw>();
            if (enemy != null)
            {
                Debug.Log($"Found enemy: {enemy.name}, CanRedraw: {enemy.CanRedraw}, CanKill: {enemy.CanKill}");
                targetEnemy = enemy;
                break;
            }
        }
        
        if (targetEnemy != null)
        {
            bool actionPerformed = false;
            
            // Sin Shift = matar enemigo, Con Shift = convertir a aliado
            if (InputProvider.ShiftHeld())
            {
                if (targetEnemy.CanRedraw)
                {
                    targetEnemy.ApplyRedraw(true); // Shift + raya = aliado
                    actionPerformed = true;
                    TutorialEventBus.RaiseTaskCompleted(TutorialTaskIds.ConvertAlly);
                    Debug.Log("Shift + raya - Enemigo convertido a aliado!");
                }
                else
                {
                    Debug.Log("Enemy found but cannot be redrawn");
                }
            }
            else
            {
                if (targetEnemy.CanKill)
                {
                    bool killed = ApplyLineDamage(targetEnemy);
                    if (killed)
                    {
                        actionPerformed = true;
                        TutorialEventBus.RaiseTaskCompleted(TutorialTaskIds.KillWithLine);
                        Debug.Log("Raya sobre enemigo - Enemigo eliminado!");
                    }
                    else
                    {
                        Debug.Log("Raya detectada pero el enemigo aún sigue con vida.");
                    }
                }
                else
                {
                    Debug.Log("Enemy found but cannot be killed");
                }
            }
            
            // Si se realizó una acción, eliminar el rastro de tinta
            if (actionPerformed)
            {
                ClearCurrentTrace();
            }
        }
        else
        {
            Debug.Log("Raya detectada pero no hay enemigos cerca.");
        }
    }
    
    void ClearCurrentTrace()
    {
        // Destruir todas las esferas de tinta del trazo actual
        foreach (var ink in _currentTraceInk)
        {
            if (ink != null)
            {
                Destroy(ink);
            }
        }
        _currentTraceInk.Clear();
        
        if (_shapeDetector != null)
        {
            _shapeDetector.ClearTrace();
        }
    }

    void TriggerKillFeedback()
    {
        if (killShakeDuration <= 0f || killShakeIntensity <= 0f)
        {
            return;
        }

        CameraShake.Instance?.Shake(killShakeDuration, killShakeIntensity);
    }

    bool ApplyLineDamage(EnemyRedraw enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        float damage = Mathf.Max(0.01f, lineDamage);
        var enemyPatrol = enemy.GetComponent<EnemyPatrol>();
        if (enemyPatrol != null)
        {
            bool killed = enemyPatrol.ApplyDamage(damage);
            if (killed)
            {
                TriggerKillFeedback();
            }
            return killed;
        }

        enemy.ApplyDeath();
        TriggerKillFeedback();
        return true;
    }

    void OnGUI()
    {
        const float w = 300f;
        const float h = 24f;
        Rect r = new Rect(10, 10, w, h);
        GUI.Box(r, $"Tinta: {Mathf.CeilToInt(currentInk)} / {Mathf.CeilToInt(maxInk)}  (Matar enemigos regenera tinta)");
        
        // Mostrar controles
        Rect r2 = new Rect(10, 40, w, h);
        GUI.Box(r2, "Raya sobre enemigo = Matar | Shift + Raya = Aliado");
    }
}
