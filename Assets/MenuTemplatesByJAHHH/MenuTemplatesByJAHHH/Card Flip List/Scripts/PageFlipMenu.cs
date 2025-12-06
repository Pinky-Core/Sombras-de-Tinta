using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MenuTemplatesByJAHHH
{

    public class PageFlipMenu : MonoBehaviour
    {
        private Queue<GameObject> pageQueue;
        private List<GameObject> menuPages;

        [SerializeField] private float zOffset = 0.2f; // Distance between each card
        [SerializeField] private float scaleOffset = 0.05f; // Scale difference between cards

        [SerializeField] private List<Button> forwardButtons;
        [SerializeField] private List<Button> backButtons;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button controlsBackButton;
        [SerializeField] private GameObject controlsCanvas;

        [SerializeField] private AudioManager audioManager;

        void Start()
        {

            menuPages = new List<GameObject>(GetComponentsInChildren<Canvas>(true)
                                             .Select(c => c.gameObject)
                                             .Where(c => c != controlsCanvas));
            pageQueue = new Queue<GameObject>(menuPages);
            startButton.onClick.AddListener(StartGame);
            quitButton.onClick.AddListener(QuitGame);
            controlsButton.onClick.AddListener(OpenControlsCanvas);
            controlsBackButton.onClick.AddListener(CloseControlsCanvas);


            controlsCanvas.SetActive(false);


            for (int i = 0; i < menuPages.Count; i++)
            {
                int index = i;
                forwardButtons[i].onClick.AddListener(() => GoForwardPage(menuPages[index]));
                backButtons[i].onClick.AddListener(() => GoBackPage(menuPages[index]));
            }

            UpdatePageOrder();
        }

        private void UpdatePageOrder()
        {
            int order = 0;
            foreach (GameObject page in pageQueue)
            {
                Canvas canvas = page.GetComponent<Canvas>();
                if (canvas != null)
                {

                    page.transform.localPosition = new Vector3(0, 0, -zOffset * order);

                    // Update scale for depth perception
                    page.transform.localScale = Vector3.one * (1 - (order * scaleOffset));


                    canvas.sortingOrder = order;
                }
                order++;
                PlaySwitchSound();
            }
        }

        private void GoForwardPage(GameObject currentPage)
        {

            GameObject firstPage = pageQueue.Dequeue();
            pageQueue.Enqueue(firstPage);

            UpdatePageOrder();
            PlayClickSound();
        }


        private void GoBackPage(GameObject currentPage)
        {
            if (pageQueue.Last() == currentPage)
            {

                GameObject lastPage = pageQueue.Last();
                pageQueue = new Queue<GameObject>(new[] { lastPage }.Concat(pageQueue.Where(p => p != lastPage)));

                UpdatePageOrder();
                PlayClickSound();
            }
        }

        private void StartGame()
        {
            //SceneManager.LoadScene("Level");      CHANGE THIS TO THE SCENE YOU WANT TO LOAD
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

        private void OpenControlsCanvas()
        {
            controlsCanvas.SetActive(true);
        }

        private void CloseControlsCanvas()
        {

            controlsCanvas.SetActive(false);
        }

        private void PlayClickSound()
        {
            audioManager.PlaySound(AudioTracks.buttonClickSound);

        }

        private void PlaySwitchSound()
        {
            audioManager.PlaySound(AudioTracks.screenSwitchSound);
        }
    }
}
