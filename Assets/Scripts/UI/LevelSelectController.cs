using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectController : MonoBehaviour
{
    [Header("UI")]
    public Transform content;               // Contenedor de botones
    public Button levelButtonPrefab;        // Prefab de botón simple (Texto + onClick)
    public string levelPrefix = "Level_";   // Filtra por nombre

    void Start()
    {
        Populate();
    }

    public void Populate()
    {
        if (!content || !levelButtonPrefab) return;
        // Limpiar anteriores
        for (int i = content.childCount - 1; i >= 0; --i) Destroy(content.GetChild(i).gameObject);

        int maxUnlocked = LevelProgress.GetMaxUnlocked();
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; ++i)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!sceneName.StartsWith(levelPrefix)) continue;

            var btn = Instantiate(levelButtonPrefab, content);
            btn.name = $"Btn_{sceneName}";
            btn.GetComponentInChildren<Text>().text = sceneName;
            int idx = i;
            bool unlocked = LevelProgress.IsUnlocked(idx);
            btn.interactable = unlocked;
            btn.onClick.AddListener(() => SceneLoader.LoadScene(idx));
        }
    }
}
