using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
namespace MenuTemplatesByJAHHH
{
    public enum AudioTracks { screenSwitchSound, buttonClickSound }

    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Components")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip screenSwitchSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioMixer audioMixer; 

        [Header("Exposed Parameters")]
        [SerializeField] private string masterVolumeParameter = "MasterVolume"; 
        [SerializeField] private string sfxVolumeParameter = "SFXVolume"; 

        [Header("UI Sliders")]
        [SerializeField] private Slider mainVolumeSlider; 
        [SerializeField] private Slider sfxVolumeSlider; 

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioMixer == null)
            {
                Debug.LogError("Audio Mixer not assigned in the Inspector!");
            }

           
            InitializeSliders();
        }

        public void PlaySound(AudioTracks track)
        {
            if (audioSource != null)
            {
                AudioClip clipToPlay = null;

                switch (track)
                {
                    case AudioTracks.screenSwitchSound:
                        clipToPlay = screenSwitchSound;
                        break;
                    case AudioTracks.buttonClickSound:
                        clipToPlay = buttonClickSound;
                        break;
                }

                if (clipToPlay != null)
                {
                    audioSource.PlayOneShot(clipToPlay);
                }
                else
                {
                    Debug.LogWarning("AudioClip not assigned for " + track);
                }
            }
        }

        
        public void SetMainVolume(float value)
        {
            if (audioMixer != null)
            {
                float volume = Mathf.Log10(value) * 20; 
                audioMixer.SetFloat(masterVolumeParameter, volume);
            }
        }

        
        public void SetSFXVolume(float value)
        {
            if (audioMixer != null)
            {
                float volume = Mathf.Log10(value) * 20; 
                audioMixer.SetFloat(sfxVolumeParameter, volume);
            }
            else
            {
                Debug.LogWarning("Audio Mixer is not assigned!");
            }
        }

        private void InitializeSliders()
        {
            
            if (mainVolumeSlider != null)
            {
                mainVolumeSlider.minValue = 0;
                mainVolumeSlider.maxValue = 1;  
                mainVolumeSlider.value = 1;     
                mainVolumeSlider.onValueChanged.AddListener(SetMainVolume);

                
                if (audioMixer.GetFloat(masterVolumeParameter, out float masterVolume))
                {
                    mainVolumeSlider.value = Mathf.Pow(10, masterVolume / 20); 
                }
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0;
                sfxVolumeSlider.maxValue = 1;  
                sfxVolumeSlider.value = 1;     
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

               
                if (audioMixer.GetFloat(sfxVolumeParameter, out float sfxVolume))
                {
                    sfxVolumeSlider.value = Mathf.Pow(10, sfxVolume / 20);
                }
            }
        }
    }
}
