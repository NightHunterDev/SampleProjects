using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class FacilityAmbience : MonoBehaviour
{
    [Header("Settings")]
    public AudioSource audioSource;          // The audio source to play the clips
    public List<AudioClip> audioClips;       // List of audio clips to choose from
    public float audioClipCooldown = 5f;     // Time to wait before playing the next clip

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
            // Pick a random audio clip
            int randomClipIndex = Random.Range(0, audioClips.Count);
            audioSource.clip = audioClips[randomClipIndex];
            audioSource.Play();

            // Wait for the clip to finish and the cooldown
            yield return new WaitForSeconds(audioSource.clip.length + audioClipCooldown);
        }
    }
}
