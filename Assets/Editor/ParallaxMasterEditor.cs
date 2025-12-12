#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParallaxMaster))]
public class ParallaxMasterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var master = (ParallaxMaster)target;

        EditorGUILayout.LabelField("Global Parallax Controller", EditorStyles.boldLabel);

        master.parallaxEnabled = EditorGUILayout.Toggle("Parallax Enabled", master.parallaxEnabled);
        master.globalIntensity = EditorGUILayout.Slider("Global Intensity", master.globalIntensity, 0f, 2f);
        master.affectVerticalMovement = EditorGUILayout.Toggle("Vertical Movement", master.affectVerticalMovement);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fade Settings", EditorStyles.boldLabel);
        master.enableFade = EditorGUILayout.Toggle("Enable Fade", master.enableFade);

        if (master.enableFade)
        {
            master.fadeStartZ = EditorGUILayout.FloatField("Fade Start Z", master.fadeStartZ);
            master.fadeEndZ = EditorGUILayout.FloatField("Fade End Z", master.fadeEndZ);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Scan Scene for Parallax Layers"))
        {
            master.FindAllLayers();
        }

        if (GUI.changed)
            EditorUtility.SetDirty(master);
    }
}
#endif
