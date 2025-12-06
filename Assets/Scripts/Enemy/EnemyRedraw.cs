using UnityEngine;

public class EnemyRedraw : MonoBehaviour
{
    public enum State { Normal, Platform, Ally, Dead }
    public State CurrentState { get; private set; } = State.Normal;
    
    [Header("Redraw Settings")]
    public bool canBeRedrawn = true;  // Habilitado por defecto para pruebas
    
    [Header("Visual Effects")]
    public Color conversionFlashColor = Color.yellow;
    public float conversionFlashDuration = 0.5f;
    public Color allyIndicatorColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    
    [Header("Particle Effects")]
    public ParticleSystem allyConversionParticles;
    public ParticleSystem deathParticles;
    public GameObject allyConversionPrefab;
    public GameObject deathEffectPrefab;

    Material _mat;
    Material _originalMat;
    Coroutine _conversionEffect;

    void Awake()
    {
        var rend = GetComponent<Renderer>();
        if (rend)
        {
            _mat = new Material(Shader.Find("Standard"));
            _mat.color = new Color(0.8f, 0.1f, 0.1f);
            _originalMat = new Material(_mat);  // Guardar material original
            rend.sharedMaterial = _mat;
        }
    }

    public bool CanRedraw => CurrentState == State.Normal && canBeRedrawn;
    public bool CanKill => CurrentState == State.Normal; // Siempre se puede matar si está vivo

    public void ApplyRedraw(bool toAlly)
    {
        if (!CanRedraw) return;
        
        // Mostrar efecto visual de conversión
        ShowConversionEffect();
        
        if (toAlly) ConvertToAlly(); else ConvertToPlatform();
    }
    
    public void ApplyDeath()
    {
        if (!CanKill) return;
        
        CurrentState = State.Dead;
        
        // Mostrar efecto de partículas de muerte
        ShowDeathParticles();
        
        // Desactivar patrol y colisiones
        var patrol = GetComponent<EnemyPatrol>();
        if (patrol) patrol.enabled = false;
        
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        
        // Regenerar tinta al jugador
        var inkDrawer = FindFirstObjectByType<InkDrawer>();
        if (inkDrawer) inkDrawer.RegenerateInk(20f);
        
        // Destruir después de las partículas
        Destroy(gameObject, 2f);
    }
    
    void ShowConversionEffect()
    {
        if (_conversionEffect != null)
        {
            StopCoroutine(_conversionEffect);
        }
        _conversionEffect = StartCoroutine(ConversionFlashEffect());
    }
    
    System.Collections.IEnumerator ConversionFlashEffect()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) yield break;
        
        // Flash de conversión
        var flashMat = new Material(Shader.Find("Standard"));
        flashMat.color = conversionFlashColor;
        flashMat.SetFloat("_Emission", 1f);
        rend.material = flashMat;
        
        yield return new WaitForSeconds(conversionFlashDuration);
        
        // Restaurar material original
        rend.material = _originalMat;
        _conversionEffect = null;
    }

    void ConvertToPlatform()
    {
        CurrentState = State.Platform;
        // Disable patrol
        var patrol = GetComponent<EnemyPatrol>();
        if (patrol) patrol.enabled = false;

        // Replace collider with a box for stable platform
        var cap = GetComponent<CapsuleCollider>();
        if (cap)
        {
            Destroy(cap);
        }
        var box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 1f);
        box.center = new Vector3(0f, 0.5f, 0f);

        // Visual: turn solid black
        var rend = GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.black;
            rend.sharedMaterial = mat;
        }
        gameObject.name = "EnemyPlatform";

        EnsureColliderAndSnap();
    }

    void ConvertToAlly()
    {
        CurrentState = State.Ally;
        var patrol = GetComponent<EnemyPatrol>();
        if (patrol) patrol.enabled = false;
        
        // Mostrar efecto de partículas de conversión
        ShowAllyConversionParticles();

        // Visual: make it dark gray/blue to differentiate
        var rend = GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.1f, 0.3f);
            mat.SetFloat("_Emission", 0.3f);  // Brillo sutil para indicar que es aliado
            rend.sharedMaterial = mat;
        }

        if (!gameObject.TryGetComponent(out AllyFollower ally))
        {
            ally = gameObject.AddComponent<AllyFollower>();
        }
        ally.enabled = true;
        var target = FindPlayerTransform();
        ally.manualTarget = target;
        ally.SetTarget(target);
        
        // Agregar indicador visual permanente (borde o glow)
        AddAllyIndicator();

        EnsureColliderAndSnap();
    }
    
    void AddAllyIndicator()
    {
        // Crear un objeto hijo que actúe como indicador visual
        var indicator = new GameObject("AllyIndicator");
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = Vector3.up * 1.2f;
        indicator.transform.localScale = Vector3.one * 0.3f;
        
        // Crear esfera indicadora
        var indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicatorObj.transform.SetParent(indicator.transform);
        indicatorObj.transform.localPosition = Vector3.zero;
        indicatorObj.transform.localScale = Vector3.one;
        
        // Configurar material del indicador
        var rend = indicatorObj.GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = allyIndicatorColor;
            mat.SetFloat("_Emission", 0.5f);
            rend.material = mat;
        }
        
        // Remover collider del indicador
        var col = indicatorObj.GetComponent<Collider>();
        if (col) Destroy(col);
        
        // Hacer que rote suavemente
        indicatorObj.AddComponent<AllyIndicatorRotator>();
    }
    
    void ShowAllyConversionParticles()
    {
        // Usar prefab si está asignado
        if (allyConversionPrefab != null)
        {
            var effect = Instantiate(allyConversionPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        // Usar sistema de partículas si está asignado
        else if (allyConversionParticles != null)
        {
            allyConversionParticles.transform.position = transform.position;
            allyConversionParticles.Play();
        }
        // Crear sistema de partículas desde código
        else
        {
            CreateCodedAllyParticles();
        }
    }
    
    void ShowDeathParticles()
    {
        // Usar prefab si está asignado
        if (deathEffectPrefab != null)
        {
            var effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        // Usar sistema de partículas si está asignado
        else if (deathParticles != null)
        {
            deathParticles.transform.position = transform.position;
            deathParticles.Play();
        }
        // Crear sistema de partículas desde código
        else
        {
            CreateCodedDeathParticles();
        }
    }

    Transform FindPlayerTransform()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        var p = FindFirstObjectByType<PlayerController3D>();
        if (p != null) return p.transform;
        var p2 = FindFirstObjectByType<PlayerControllerSide3D>();
        return p2 != null ? p2.transform : null;
    }

    void EnsureColliderAndSnap()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
        }

        col.isTrigger = false;

        if (col is CapsuleCollider cap)
        {
            cap.direction = 1;
            cap.radius = Mathf.Max(0.3f, cap.radius);
            cap.height = Mathf.Max(cap.height, cap.radius * 2f);
        }
        else if (col is BoxCollider box)
        {
            Vector3 size = box.size;
            size.y = Mathf.Max(size.y, 0.8f);
            box.size = size;
            box.center = new Vector3(box.center.x, Mathf.Max(box.center.y, size.y * 0.5f), box.center.z);
        }

        SnapToGround();
    }

    void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }
    
    void CreateBasicAllyParticles()
    {
        // Crear varias esferas con efecto de dispersión hacia arriba (efecto positivo)
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.name = "AllyParticle";
            particle.transform.position = transform.position + Vector3.up * 0.5f;
            particle.transform.localScale = Vector3.one * 0.2f;
            
            // Material verde brillante
            var rend = particle.GetComponent<Renderer>();
            if (rend)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 1f, 0.2f, 0.8f);
                mat.SetFloat("_Emission", 1f);
                rend.material = mat;
            }
            
            // Remover collider
            var col = particle.GetComponent<Collider>();
            if (col) Destroy(col);
            
            // Añadir rigidbody para movimiento
            var rb = particle.AddComponent<Rigidbody>();
            rb.useGravity = false;
            Vector3 force = new Vector3(Random.Range(-2f, 2f), Random.Range(3f, 6f), Random.Range(-1f, 1f));
            rb.AddForce(force, ForceMode.Impulse);
            
            // Destruir después de un tiempo
            Destroy(particle, 2f);
        }
    }
    
    void CreateBasicDeathParticles()
    {
        // Crear varias esferas con efecto de explosión (efecto negativo)
        for (int i = 0; i < 12; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.name = "DeathParticle";
            particle.transform.position = transform.position + Vector3.up * 0.5f;
            particle.transform.localScale = Vector3.one * 0.15f;
            
            // Material rojo/naranja
            var rend = particle.GetComponent<Renderer>();
            if (rend)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.3f, 0.1f, 0.9f);
                mat.SetFloat("_Emission", 1f);
                rend.material = mat;
            }
            
            // Remover collider
            var col = particle.GetComponent<Collider>();
            if (col) Destroy(col);
            
            // Añadir rigidbody para movimiento explosivo
            var rb = particle.AddComponent<Rigidbody>();
            rb.useGravity = true;
            Vector3 force = new Vector3(Random.Range(-4f, 4f), Random.Range(2f, 5f), Random.Range(-2f, 2f));
            rb.AddForce(force, ForceMode.Impulse);
            
            // Destruir después de un tiempo
            Destroy(particle, 1.5f);
        }
    }
    
    void CreateCodedAllyParticles()
    {
        // Crear GameObject para el sistema de partículas
        GameObject psObject = new GameObject("AllyConversionParticles");
        psObject.transform.position = transform.position + Vector3.up * 0.5f;
        
        // Agregar componente ParticleSystem
        ParticleSystem ps = psObject.AddComponent<ParticleSystem>();
        
        // Configurar propiedades principales
        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 3f;
        main.startSize = 0.2f;
        main.startColor = new Color(0.2f, 1f, 0.2f, 1f); // Verde brillante
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Configurar material (arreglar partículas rosas)
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Probar diferentes shaders hasta encontrar uno que funcione
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? 
                    Shader.Find("Sprites/Default") ?? 
                    Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
                    Shader.Find("Standard");
        var material = new Material(shader);
        
        // Configurar color según el shader
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.2f, 1f, 0.2f, 1f));
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(0.2f, 1f, 0.2f, 1f));
        else
            material.color = new Color(0.2f, 1f, 0.2f, 1f);
            
        renderer.material = material;
        
        // Configurar emisión
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 30, 50, 0.1f)
        });
        
        // Configurar forma
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        // Configurar velocidad a lo largo del tiempo
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f, 4f);
        
        // Configurar tamaño a lo largo del tiempo (se hace más pequeño)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Configurar color a lo largo del tiempo (fade out)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;
        
        // Destruir después de que termine
        Destroy(psObject, 3f);
    }
    
    void CreateCodedDeathParticles()
    {
        // Crear GameObject para el sistema de partículas
        GameObject psObject = new GameObject("DeathParticles");
        psObject.transform.position = transform.position + Vector3.up * 0.5f;
        
        // Agregar componente ParticleSystem
        ParticleSystem ps = psObject.AddComponent<ParticleSystem>();
        
        // Configurar propiedades principales
        var main = ps.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 5f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.3f, 0.1f, 1f); // Rojo-naranja
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Configurar material (arreglar partículas rosas)
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Probar diferentes shaders hasta encontrar uno que funcione
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? 
                    Shader.Find("Sprites/Default") ?? 
                    Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
                    Shader.Find("Standard");
        var material = new Material(shader);
        
        // Configurar color según el shader
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(1f, 0.3f, 0.1f, 1f));
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(1f, 0.3f, 0.1f, 1f));
        else
            material.color = new Color(1f, 0.3f, 0.1f, 1f);
            
        renderer.material = material;
        
        // Configurar emisión (explosión)
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 60, 80, 0.1f)
        });
        
        // Configurar forma (esfera para explosión)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        
        // Configurar velocidad a lo largo del tiempo
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(3f, 6f);
        
        // Agregar gravedad
        var forceOverLifetime = ps.forceOverLifetime;
        forceOverLifetime.enabled = true;
        forceOverLifetime.y = -9.81f;
        
        // Configurar tamaño a lo largo del tiempo
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 1.2f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Configurar color a lo largo del tiempo (de rojo a amarillo a transparente)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.red, 0.0f), 
                new GradientColorKey(Color.yellow, 0.3f), 
                new GradientColorKey(Color.black, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.8f, 0.5f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // Destruir después de que termine
        Destroy(psObject, 2f);
    }
}
