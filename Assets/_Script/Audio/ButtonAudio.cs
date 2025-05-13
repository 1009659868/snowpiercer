using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonAudio : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip audioClip;
    public void PlayAudio(){
        Debug.Log("Play");
        audioSource.PlayOneShot(audioClip);
    }
}
