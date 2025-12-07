using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MenuTemplatesByJAHHH
{

    public class BookFlipMenu : MonoBehaviour
    {
        [SerializeField] private GameObject[] pages; 
        [SerializeField] private Button forwardButton; 
        [SerializeField] private Button backButton;
        [SerializeField] private float flipDuration = 1.0f;

        private bool isFlipping = false;
        private int currentPageIndex = 0;

        [SerializeField] private Button controlsButton;
        [SerializeField] private Button controlsBackButton;
        [SerializeField] private GameObject controlsCanvas;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private AudioManager audioManager;

        // ⚡ NUEVO: sonidos aleatorios para pasar página
        [SerializeField] private AudioClip[] pageFlipSounds;
        [SerializeField] private float pageFlipVolume = 1f;

        [SerializeField, Tooltip("Si está activo, deshabilita este componente si faltan referencias en lugar de crashear.")]
        private bool disableOnMissingReferences = true;

        void Start()
        {
            if (!ValidateReferences())
            {
                if (disableOnMissingReferences) { enabled = false; return; }
            }

            controlsButton.onClick.AddListener(OpenControlsCanvas);
            controlsBackButton.onClick.AddListener(CloseControlsCanvas);
            startButton.onClick.AddListener(StartGame);
            quitButton.onClick.AddListener(QuitGame);

            controlsCanvas.SetActive(false);

            if (forwardButton == null || backButton == null)
            {
                Debug.LogError("Buttons not assigned in the Inspector!");
                return;
            }

            forwardButton.onClick.AddListener(GoForwardPage);
            backButton.onClick.AddListener(GoBackPage);

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == 0);
                SetPagePivot(pages[i], new Vector2(0, 0.5f));
            }

            backButton.interactable = false;
        }

        bool ValidateReferences()
        {
            if (controlsButton == null || controlsBackButton == null || startButton == null || quitButton == null || controlsCanvas == null)
            {
                Debug.LogError("BookFlipMenu: faltan referencias a botones/canvas en el inspector.", this);
                return false;
            }
            if (pages == null || pages.Length == 0)
            {
                Debug.LogError("BookFlipMenu: no hay páginas asignadas.", this);
                return false;
            }
            return true;
        }

        private void GoForwardPage()
        {
            if (isFlipping || currentPageIndex >= pages.Length - 1)
                return;

            PlayRandomPageFlipSound();
            StartCoroutine(FlipPage(true));
        }

        private void GoBackPage()
        {
            if (isFlipping || currentPageIndex <= 0)
                return;

            PlayRandomPageFlipSound();
            StartCoroutine(FlipPage(false));
        }

        private IEnumerator FlipPage(bool forward)
        {
            isFlipping = true;

            GameObject currentPage = pages[currentPageIndex];
            int targetPageIndex = forward ? currentPageIndex + 1 : currentPageIndex - 1;
            GameObject targetPage = pages[targetPageIndex];

            targetPage.SetActive(true);

            float elapsedTime = 0f;
            float startAngle = forward ? 0f : 180f;
            float endAngle = forward ? 180f : 0f;

            while (elapsedTime < flipDuration)
            {
                elapsedTime += Time.deltaTime;
                float angle = Mathf.Lerp(startAngle, endAngle, elapsedTime / flipDuration);
                currentPage.transform.localRotation = Quaternion.Euler(0, angle, 0);
                yield return null;
            }

            currentPage.transform.localRotation = Quaternion.Euler(0, endAngle, 0);

            if (forward)
            {
                if (currentPageIndex < pages.Length - 1)
                {
                    currentPage.transform.SetSiblingIndex(0);
                }
            }

            currentPageIndex = targetPageIndex;

            targetPage.transform.localRotation = Quaternion.Euler(0, 0, 0);

            for (int i = 0; i <= currentPageIndex; i++)
            {
                pages[i].SetActive(true);
            }

            UpdatePageOrder();
            UpdateButtons();
            isFlipping = false;
        }

        private void UpdatePageOrder()
        {
            for (int i = 0; i < pages.Length - 1; i++)
            {
                if (i == currentPageIndex)
                {
                    pages[i].transform.SetSiblingIndex(pages.Length - 2);
                }
                else
                {
                    pages[i].transform.SetSiblingIndex(i);
                }
            }
        }

        private void UpdateButtons()
        {
            backButton.interactable = currentPageIndex > 0;
            forwardButton.interactable = currentPageIndex < pages.Length - 1;
        }

        private void SetPagePivot(GameObject page, Vector2 pivot)
        {
            RectTransform rectTransform = page.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.pivot = pivot;
            }
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
            if (audioManager != null) audioManager.PlaySound(AudioTracks.buttonClickSound);
        }

        private void PlaySwitchSound()
        {
            if (audioManager != null) audioManager.PlaySound(AudioTracks.screenSwitchSound);
        }

        // ⚡ NUEVO: reproducir un sonido aleatorio de pasar página
        private void PlayRandomPageFlipSound()
        {
            if (audioManager == null || pageFlipSounds == null || pageFlipSounds.Length == 0)
                return;

            int index = Random.Range(0, pageFlipSounds.Length);
            AudioClip clip = pageFlipSounds[index];

            audioManager.PlaySound(clip);
        }

        private void StartGame()
        {
            //SceneManager.LoadScene("Level");  
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

}
