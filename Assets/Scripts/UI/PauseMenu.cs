using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public bool showCursorOnResume = true; // Solicitud: volver a mostrar cursor al salir del menú de pausa
    bool _paused;

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (InputProvider.PauseDown())
        {
            if (_paused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        _paused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        _paused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (showCursorOnResume) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
        else { Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    }

    // Botón: Reiniciar nivel actual
    public void Restart()
    {
        Time.timeScale = 1f;
        _paused = false;
        if (pausePanel) pausePanel.SetActive(false);
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        SceneLoader.Reload();
    }

    // Botón: Salir del juego
    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneLoader.Quit();
    }
}
