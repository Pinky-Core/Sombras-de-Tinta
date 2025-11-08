using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float shootCost = 10f;  // Costo de tinta por disparo
    public float shootRange = 15f;  // Rango de disparo
    public float shootForce = 20f;  // Fuerza del proyectil
    public GameObject inkProjectilePrefab;  // Prefab del proyectil (se creará dinámicamente si es null)
    public float damagePerShot = 1f; // Daño aplicado a enemigos animados
    
    [Header("Visual Effects")]
    public float muzzleFlashDuration = 0.1f;
    public Color inkColor = Color.black;
    
    private InkDrawer _inkDrawer;
    private Transform _shootPoint;
    private LineRenderer _muzzleFlash;
    
    void Awake()
    {
        _inkDrawer = GetComponent<InkDrawer>();
        if (_inkDrawer == null)
        {
            Debug.LogError("PlayerShooting requires InkDrawer component!");
            enabled = false;
            return;
        }
        
        // Crear punto de disparo
        _shootPoint = new GameObject("ShootPoint").transform;
        _shootPoint.SetParent(transform);
        _shootPoint.localPosition = new Vector3(0, 0.5f, 0.5f);  // Frente del jugador
        
        // Crear línea de muzzle flash
        _muzzleFlash = gameObject.AddComponent<LineRenderer>();
        var spriteDef = Shader.Find("Sprites/Default");
        var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        _muzzleFlash.material = new Material(urpUnlit ? urpUnlit : spriteDef);
        _muzzleFlash.startColor = inkColor;
        _muzzleFlash.endColor = inkColor;
        _muzzleFlash.startWidth = 0.1f;
        _muzzleFlash.endWidth = 0.05f;
        _muzzleFlash.enabled = false;
    }
    
    void Update()
    {
        if (InputProvider.ShootDown() && _inkDrawer.currentInk >= shootCost)
        {
            Shoot();
        }
    }
    
    void Shoot()
    {
        // Verificar si hay suficiente tinta
        if (!_inkDrawer.CanDraw(shootCost)) return;
        
        // Consumir tinta
        _inkDrawer.currentInk -= shootCost;
        
        // Crear proyectil
        Vector3 shootDirection = transform.forward;
        Vector3 shootPosition = _shootPoint.position;
        
        // Raycast para detectar enemigos
        RaycastHit hit;
        if (Physics.Raycast(shootPosition, shootDirection, out hit, shootRange))
        {
            // Verificar si es un enemigo
            var enemy = hit.collider.GetComponent<EnemyRedraw>();
            var patrol = hit.collider.GetComponentInParent<EnemyPatrol>();
            if (enemy != null && enemy.CurrentState == EnemyRedraw.State.Normal)
            {
                if (patrol != null)
                {
                    // Aplicar daño y no destruir inmediatamente (animaciones)
                    patrol.ApplyDamage(damagePerShot);
                }
                else
                {
                    // Matar enemigo inmediato si no tiene sistema de vida/animación
                    KillEnemy(enemy);
                }
            }
            else
            {
                // Crear proyectil que impacta en la superficie
                CreateProjectile(shootPosition, hit.point);
            }
        }
        else
        {
            // Crear proyectil que vuela hasta el rango máximo
            CreateProjectile(shootPosition, shootPosition + shootDirection * shootRange);
        }
        
        // Mostrar muzzle flash
        ShowMuzzleFlash();
    }
    
    void CreateProjectile(Vector3 start, Vector3 end)
    {
        // Crear proyectil dinámico si no hay prefab
        GameObject projectile;
        if (inkProjectilePrefab != null)
        {
            projectile = Instantiate(inkProjectilePrefab, start, Quaternion.identity);
        }
        else
        {
            // Crear proyectil simple
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "InkProjectile";
            projectile.transform.localScale = Vector3.one * 0.2f;
            
            // Configurar material
            var renderer = projectile.GetComponent<Renderer>();
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(sh ? sh : Shader.Find("Standard"));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", inkColor); else mat.color = inkColor;
            var tex = _inkDrawer != null ? _inkDrawer.inkTexture : null;
            if (tex)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                else mat.mainTexture = tex;
            }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.4f);
            renderer.material = mat;
            
            // Configurar collider
            var collider = projectile.GetComponent<Collider>();
            collider.isTrigger = true;
            
            // Agregar script de proyectil
            var inkProjectile = projectile.AddComponent<InkProjectile>();
            inkProjectile.damage = 1f;
            inkProjectile.lifetime = 3f;
        }
        
        // Configurar movimiento del proyectil
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
        }
        
        // Configurar física del proyectil
        rb.mass = 0.1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        
        Vector3 direction = (end - start).normalized;
        rb.linearVelocity = direction * shootForce;
        
        // El proyectil se destruirá automáticamente por su script InkProjectile
    }
    
    void KillEnemy(EnemyRedraw enemy)
    {
        // Dar tinta al matar enemigo usando el método público
        float inkReward = shootCost * 0.8f;  // Recuperar el 80% de la tinta gastada
        _inkDrawer.RegenerateInk(inkReward);
        
        // Destruir enemigo
        Destroy(enemy.gameObject);
    }
    
    void ShowMuzzleFlash()
    {
        _muzzleFlash.enabled = true;
        _muzzleFlash.SetPosition(0, _shootPoint.position);
        _muzzleFlash.SetPosition(1, _shootPoint.position + transform.forward * 0.5f);
        
        // Deshabilitar después de un frame
        Invoke(nameof(HideMuzzleFlash), muzzleFlashDuration);
    }
    
    void HideMuzzleFlash()
    {
        _muzzleFlash.enabled = false;
    }
}
