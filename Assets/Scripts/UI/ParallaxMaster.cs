using UnityEngine;
using System.Collections.Generic;

public class ParallaxMaster : MonoBehaviour
{
    public static ParallaxMaster Instance;

    [Header("Global Settings")]
    [Range(0f, 2f)]
    public float globalIntensity = 1f;

    public bool parallaxEnabled = true;
    public bool affectVerticalMovement = false;

    [Header("Camera Target")]
    public Transform followTarget;

    [Header("Fade Settings")]
    public bool enableFade = false;
    public float fadeStartZ = 5f;
    public float fadeEndZ = 25f;

    private Vector3 lastPos;
    private List<ParallaxController> layers = new List<ParallaxController>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (followTarget == null)
            followTarget = Camera.main.transform;

        lastPos = followTarget.position;

        FindAllLayers();
    }

    void LateUpdate()
    {
        if (!parallaxEnabled || followTarget == null)
        {
            lastPos = followTarget.position;
            return;
        }

        Vector3 delta = followTarget.position - lastPos;

        foreach (var layer in layers)
        {
            if (layer == null) continue;
            layer.ApplyParallax(delta, globalIntensity, affectVerticalMovement);
        }

        lastPos = followTarget.position;
    }

    public void FindAllLayers()
    {
        layers.Clear();
        layers.AddRange(FindObjectsOfType<ParallaxController>());
    }

    public float GetFadeMultiplier(float z)
    {
        if (!enableFade) return 1f;

        if (z <= fadeStartZ) return 1f;
        if (z >= fadeEndZ) return 0f;

        return 1f - ((z - fadeStartZ) / (fadeEndZ - fadeStartZ));
    }
}
