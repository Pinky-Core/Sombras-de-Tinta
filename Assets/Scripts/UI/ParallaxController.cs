using UnityEngine;

[ExecuteAlways]
public class ParallaxController : MonoBehaviour
{
    [Header("Automatic Depth")]
    public bool autoDepth = true;

    [Range(0f, 1f)]
    public float manualStrength = 0.3f;

    [Header("Smoothing")]
    public float smoothness = 10f;

    [Header("Infinite Scrolling (Optional)")]
    public bool infiniteX = false;
    public bool infiniteY = false;

    private SpriteRenderer sr;
    private float depthFactor;
    private Vector3 startPos;
    private float spriteWidth;
    private float spriteHeight;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;

        if (sr)
        {
            spriteWidth = sr.bounds.size.x;
            spriteHeight = sr.bounds.size.y;
        }

        DetectDepth();
    }

    void DetectDepth()
    {
        if (!autoDepth || Camera.main == null)
        {
            depthFactor = manualStrength;
            return;
        }

        float dist = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        depthFactor = Mathf.Clamp(dist * 0.05f, 0.01f, 0.8f);
    }

    public void ApplyParallax(Vector3 delta, float globalIntensity, bool vertical)
    {
        float factor = depthFactor * globalIntensity;
        Vector3 move = new Vector3(delta.x * factor, vertical ? delta.y * factor : 0f, 0f);

        Vector3 targetPos = transform.position + move;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            1f - Mathf.Exp(-smoothness * Time.deltaTime)
        );

        // Fade por distancia del ParallaxMaster
        if (ParallaxMaster.Instance != null && sr != null)
        {
            float fade = ParallaxMaster.Instance.GetFadeMultiplier(Mathf.Abs(transform.position.z));
            Color c = sr.color;
            c.a = fade;
            sr.color = c;
        }

        HandleInfiniteScrolling();
    }

    void HandleInfiniteScrolling()
    {
        if (!sr) return;

        Transform cam = Camera.main.transform;

        if (infiniteX)
        {
            float diffX = Mathf.Abs(cam.position.x - transform.position.x);
            if (diffX >= spriteWidth) transform.position += Vector3.right * Mathf.Sign(cam.position.x - transform.position.x) * spriteWidth;
        }

        if (infiniteY)
        {
            float diffY = Mathf.Abs(cam.position.y - transform.position.y);
            if (diffY >= spriteHeight) transform.position += Vector3.up * Mathf.Sign(cam.position.y - transform.position.y) * spriteHeight;
        }
    }
}
