using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

namespace MenuTemplatesByJAHHH
{
    public class SpinFlipMenu : MonoBehaviour
    {
        private Queue<GameObject> pageQueue;
        private List<GameObject> menuPages;

        [SerializeField] private float zOffset = 0.2f;
        [SerializeField] private float scaleOffset = 0.05f;
        [SerializeField] private float flipDuration = 0.5f;

        [SerializeField] private List<Button> forwardButtons;
        [SerializeField] private List<Button> backButtons;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button startButton;
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

            quitButton.onClick.AddListener(QuitGame);
            startButton.onClick.AddListener(StartGame);
            controlsButton.onClick.AddListener(OpenControlsCanvas);
            controlsBackButton.onClick.AddListener(CloseControlsCanvas);

            controlsCanvas.SetActive(false);

            
            for (int i = 0; i < menuPages.Count; i++)
            {
                int index = i;
                if (i < forwardButtons.Count)
                {
                    forwardButtons[i].onClick.AddListener(() =>
                    {
                        StartCoroutine(GoForwardPage(menuPages[index]));
                    });
                }
                if (i < backButtons.Count)
                {
                    backButtons[i].onClick.AddListener(() =>
                    {
                        StartCoroutine(GoBackPage(menuPages[index]));
                    });
                }
            }

            UpdatePageOrder();
        }

        void UpdatePageOrder()
        {
            int order = 0;
            foreach (GameObject page in pageQueue)
            {
                Canvas canvas = page.GetComponent<Canvas>();
                if (canvas != null)
                {
                    page.transform.localPosition = new Vector3(0, 0, -zOffset * order);
                    page.transform.localScale = Vector3.one * (1 - (order * scaleOffset));
                    canvas.sortingOrder = order;
                }
                order++;
            }
        }

        private IEnumerator GoForwardPage(GameObject currentPage)
        {
            Debug.Log($"Attempting forward flip for {currentPage.name}");

            
            yield return StartCoroutine(AnimatePageFlip(currentPage, true));

            GameObject firstPage = pageQueue.Dequeue();
            pageQueue.Enqueue(firstPage);

            Debug.Log("Queue after Forward Flip: " + string.Join(", ", pageQueue.Select(p => p.name)));

            UpdatePageOrder();
            PlayClickSound();
        }

        private IEnumerator GoBackPage(GameObject currentPage)
        {
            Debug.Log($"Attempting backward flip for {currentPage.name}");

          
            yield return StartCoroutine(AnimatePageFlip(currentPage, false));

            GameObject lastPage = pageQueue.Last();
            pageQueue = new Queue<GameObject>(new[] { lastPage }.Concat(pageQueue.Take(pageQueue.Count - 1)));

            Debug.Log("Queue after Back Flip: " + string.Join(", ", pageQueue.Select(p => p.name)));

            UpdatePageOrder();
            PlayClickSound();
        }

        private IEnumerator AnimatePageFlip(GameObject page, bool isForward)
        {
            RectTransform rect = page.GetComponent<RectTransform>();
            float elapsedTime = 0f;
            float startAngle = isForward ? 0f : 180f;
            float endAngle = isForward ? 180f : 0f;

         
            Vector3 initialRotation = rect.localRotation.eulerAngles;

            while (elapsedTime < flipDuration)
            {
                elapsedTime += Time.deltaTime;

             
                float angle = Mathf.Lerp(startAngle, endAngle, elapsedTime / flipDuration);
                rect.localRotation = Quaternion.Euler(initialRotation.x, angle, initialRotation.z);
                PlaySwitchSound();
                yield return null;
            }

         
            rect.localRotation = Quaternion.Euler(initialRotation.x, 0f, initialRotation.z);

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
