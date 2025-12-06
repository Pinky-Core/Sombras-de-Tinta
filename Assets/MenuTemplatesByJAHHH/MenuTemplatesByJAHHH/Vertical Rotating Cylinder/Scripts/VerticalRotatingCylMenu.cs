using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MenuTemplatesByJAHHH
{

    public class VerticalRotatingMenuCylinder : MonoBehaviour
    {
        [SerializeField] private GameObject menuCylinder;

        [SerializeField] private GameObject controlsCanvas;

        [SerializeField] private Button buttonStart;
        [SerializeField] private Button buttonOptions;
        [SerializeField] private Button buttonCredits;
        [SerializeField] private Button buttonQuit;
        [SerializeField] private Button buttonS1Back;
        [SerializeField] private Button buttonS2Back;
        [SerializeField] private Button buttonS3Back;
        [SerializeField] private Button startButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button controlsBackButton;

        [SerializeField] private float smoothRotation = 5f;
        [SerializeField] private float tiltAngle = 0f;

        [SerializeField] private AudioManager audioManager;


        void Start()
        {
            buttonStart.onClick.AddListener(GoToScreen1);
            buttonOptions.onClick.AddListener(GoToScreen2);
            buttonCredits.onClick.AddListener(GoToScreen3);
            buttonQuit.onClick.AddListener(QuitGame);
            buttonS1Back.onClick.AddListener(GoBackToMainScreen);
            buttonS2Back.onClick.AddListener(GoBackToMainScreen);
            buttonS3Back.onClick.AddListener(GoBackToMainScreen);
            startButton.onClick.AddListener(StartGame);
            controlsBackButton.onClick.AddListener(CloseControlsCanvas);
            controlsButton.onClick.AddListener(OpenControlsCanvas);

            controlsCanvas.SetActive(false);

            menuCylinder.transform.rotation = Quaternion.Euler(0, 0, 90);
            PlaySwitchSound();
        }


        void Update()
        {

            RotateCylinder();

        }

        private void RotateCylinder()
        {
            Quaternion target = Quaternion.Euler(0, tiltAngle, 0);
            menuCylinder.transform.rotation = Quaternion.Slerp(menuCylinder.transform.rotation, target, Time.deltaTime * smoothRotation);

        }

        private void GoToScreen1()
        {
            tiltAngle += 90f;
            PlayClickSound();
            PlaySwitchSound();
        }

        private void GoToScreen2()
        {
            tiltAngle += 180f;
            PlayClickSound();
            PlaySwitchSound();
        }

        private void GoToScreen3()
        {
            tiltAngle += 270f;
            PlayClickSound();
            PlaySwitchSound();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
           
            UnityEditor.EditorApplication.isPlaying = false;
#else
        // If in a build, quit the application
        Application.Quit();
#endif
        }

        private void GoBackToMainScreen()
        {
            tiltAngle = 0f;
            PlayClickSound();
            PlaySwitchSound();
        }

        private void PlayClickSound()
        {
            audioManager.PlaySound(AudioTracks.buttonClickSound);

        }

        private void PlaySwitchSound()
        {
            audioManager.PlaySound(AudioTracks.screenSwitchSound);
        }

        private void StartGame()
        {
            //SceneManager.LoadScene("Level");      CHANGE THIS TO THE SCENE YOU WANT TO LOAD
        }

        private void OpenControlsCanvas()
        {
            controlsCanvas.SetActive(true);
        }

        private void CloseControlsCanvas()
        {

            controlsCanvas.SetActive(false);
        }

    }
}
