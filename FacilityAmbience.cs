using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class FacilityAmbience : MonoBehaviour
{
    [Header("Settings")]
    public AudioSource audioSource;          
    public List<AudioClip> audioClips;       // List of Audio Clips to Play
    public float audioClipCooldown = 5f;     // Time Waiting Between Each Audio Clip

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioClips.Count > 0 && audioSource != null)
        {
            StartCoroutine(PlayAmbience());
        }
    }

    IEnumerator PlayAmbience()
    {
        while (true)
        {
            // This Pick's a random Audio Clip
            int randomClipIndex = Random.Range(0, audioClips.Count);
            audioSource.clip = audioClips[randomClipIndex];
            audioSource.Play();

            // Wait Until The Clip Finish 
            yield return new WaitForSeconds(audioSource.clip.length + audioClipCooldown);
        }
    }
}
