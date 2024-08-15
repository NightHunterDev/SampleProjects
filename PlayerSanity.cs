using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerSanity : MonoBehaviour
{
    [Header("Sanity Settings")]
    public float sanity = 0;
    public float sanityIncreaseRate = 10f;
    public float maxSanity = 100f;
    public float sanityIncreaseDelay = 1f; 
    private float sanityIncreaseTimer = 0f;

    [Header("Camera")]
    public Camera playerCamera;
    public float increasedFOV60 = 65f;  
    public float increasedFOV100 = 70f; 

    [Header("Post Processing Effects")]
    public Volume postProcessVolume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private float originalSaturation;
    public float vignetteIntensitySanity60 = 0.469f;
    public float vignetteIntensitySanity100 = 0.469f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip sanityIncreaseClip;
    public AudioReverbFilter audioReverbFilter;
    public AudioReverbPreset sanity50Reverb = AudioReverbPreset.Psychotic;

    [Header("Sanity 100 Effects")]
    public AudioClip sanity100Clip;
    public AudioSource sanity100AudioSource;
    public AudioReverbFilter sanity100ReverbFilter;

    [Header("Camera Shake Settings")]
    public float shakeIntensity = 0.5f;
    public float shakeSpeed = 1.0f;

    private bool isSanity100Triggered = false;
    private Vector3 originalCameraPosition;
    private float sanity100Timer = 0f;
    private const float sanity100Duration = 40f;  

    private void Start()
    {
        if (postProcessVolume.profile.TryGet<Vignette>(out vignette))
        {
            vignette.intensity.value = 0f;
        }
        else
        {
            Debug.LogError("Vignette component not found in PostProcessVolume.");
        }

        if (postProcessVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            originalSaturation = colorAdjustments.saturation.value;
        }
        else
        {
            Debug.LogError("ColorAdjustments component not found in PostProcessVolume.");
        }

        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
        }
    }

    private void Update()
    {
        CheckSanity();
        if (isSanity100Triggered)
        {
            sanity100Timer += Time.deltaTime;
            ShakeCamera();

            if (sanity100Timer >= sanity100Duration)
            {
                ResetSanity100Effects();
            }
        }
    }

    private void CheckSanity()
    {
        if (IsLookingBack())
        {
            
            sanityIncreaseTimer += Time.deltaTime;
            if (sanityIncreaseTimer >= sanityIncreaseDelay)
            {
                IncreaseSanity();
                sanityIncreaseTimer = 0f; 
            }
        }
        else
        {
            sanityIncreaseTimer = 0f; 
        }

        if (sanity >= 10 && sanity < 50)
        {
            ApplyVignetteEffect(vignetteIntensitySanity60);
            PlaySanityIncreaseAudio();
        }

        if (sanity >= 50 && sanity < 100)
        {
            ApplyReverbFilter(sanity50Reverb);
        }

        if (sanity >= 60 && sanity < 100)
        {
            
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, increasedFOV60, Time.deltaTime * 2f);
            }
        }

        if (sanity >= 100 && !isSanity100Triggered)
        {
            TriggerSanity100Effects();
        }

        if (sanity == 0 && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(audioSource, 2f));
        }
    }

    private bool IsLookingBack()
    {
       
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraUp = playerCamera.transform.up;

       
        if (Vector3.Dot(cameraForward, Vector3.down) > 0.5f)
        {
            return true; 
        }

        return false; 
    }

    private void IncreaseSanity()
    {
        sanity = Mathf.Clamp(sanity + sanityIncreaseRate, 0, maxSanity);
    }

    private void ApplyVignetteEffect(float targetIntensity)
    {
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Time.deltaTime * 2f);
        }
    }

    private void PlaySanityIncreaseAudio()
    {
        if (audioSource != null)
        {
            audioSource.clip = sanityIncreaseClip;
            audioSource.loop = true;

            if (!audioSource.isPlaying && sanity >= 10)
            {
                audioSource.Play();
            }
        }
    }

    private void ApplyReverbFilter(AudioReverbPreset reverbPreset)
    {
        if (audioReverbFilter != null)
        {
            audioReverbFilter.reverbPreset = reverbPreset;
        }
    }

    private void TriggerSanity100Effects()
    {
        isSanity100Triggered = true;
        sanity100Timer = 0f;

        if (sanity100AudioSource != null)
        {
            sanity100AudioSource.clip = sanity100Clip;
            sanity100AudioSource.Play();
        }

        if (sanity100ReverbFilter != null)
        {
            sanity100ReverbFilter.reverbPreset = AudioReverbPreset.Psychotic;
        }

        
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = increasedFOV100;
        }

        
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, -100f, Time.deltaTime * 2f);
        }
    }

    private void ShakeCamera()
    {
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = originalCameraPosition + Random.insideUnitSphere * shakeIntensity;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, originalCameraPosition, Time.deltaTime * shakeSpeed);
        }
    }

    private void ResetSanity100Effects()
    {
        isSanity100Triggered = false;

        
        if (sanity100AudioSource != null)
        {
            sanity100AudioSource.Stop();
        }

        
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, Time.deltaTime * 2f);
        }

        
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, originalSaturation, Time.deltaTime * 2f);
        }

        
        if (audioSource != null)
        {
            StartCoroutine(FadeOutAudio(audioSource, 2f));
        }

        
        if (audioReverbFilter != null)
        {
            audioReverbFilter.reverbPreset = AudioReverbPreset.Off;
        }

        
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = 60f; 
        }
    }

    private System.Collections.IEnumerator FadeOutAudio(AudioSource audioSource, float fadeTime)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;

            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }
}
