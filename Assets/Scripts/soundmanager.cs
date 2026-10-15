using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundmanager : MonoBehaviour
{
    public static soundmanager sound_instance;
    public AudioSource audioSource;
    [Header("介面")]
    public AudioClip hoverAudio;
    public AudioClip pressAudio;
    public AudioClip correctAudio;
    public AudioClip wrongAudio;
    public AudioClip warningAudio;
    [Header("玩家")]
    public AudioClip jumpAudio;
    public AudioClip hurtAudio;
    public AudioClip throwAudio;
    public AudioClip swallowAudio;
    public AudioClip heartAudio;
    public AudioClip biggerAudio;
    public AudioClip crackAudio;
    public AudioClip swimAudio;
    [Header("口水")]
    public AudioClip floorAudio;
    public AudioClip attackAudio;
    [Header("寶箱")]
    public AudioClip openAudio;
    public AudioClip flagAudio;
    [Header("敵人")]
    public AudioClip hurt0Audio;
    public AudioClip hurt1Audio;
    public AudioClip hurt2Audio;
    public AudioClip cakeAudio;
    public AudioClip counterAudio;
    public AudioClip doorAudio;
    
    private void Awake()
    {
        sound_instance=this;
    }
    private void OnDestroy()
    {
        if(sound_instance==this)
        {
            sound_instance=null;
        }
    }
    public void Jumpaudio()
    {
        PlayEffect(jumpAudio);
    }
    public void Hurtaudio()
    {
        PlayEffect(hurtAudio);
    }
    public void Throwaudio()
    {
        PlayEffect(throwAudio);
    }
    public void Flooraudio()
    {
        PlayEffect(floorAudio);
    }
    public void Attackaudio()
    {
        PlayEffect(attackAudio);
    }
    public void Openaudio()
    {
        PlayEffect(openAudio);
    }
    public void Hoveraudio()
    {
        PlayEffect(hoverAudio);
    }

    public void Pressaudio()
    {
        PlayEffect(pressAudio);
    }
    public void Correctaudio()
    {
        PlayEffect(correctAudio);
    }
    public void Wrongaudio()
    {
        PlayEffect(wrongAudio);
    }
    public void Hurt0audio()
    {
        PlayEffect(hurt0Audio);
    }
    public void Hurt1audio()
    {
        PlayEffect(hurt1Audio);
    }
    public void Hurt2audio()
    {
        PlayEffect(hurt2Audio);
    }
    public void Cakeaudio()
    {
        PlayEffect(cakeAudio);
    }
    public void SwallowAudio()
    {
        PlayEffect(swallowAudio);
    }
    public void HeartAudio()
    {
        PlayEffect(heartAudio);
    }
    public void FlagAudio()
    {
        PlayEffect(flagAudio);
    }
    public void BiggerAudio()
    {
        PlayEffect(biggerAudio);
    }
    public void WarningAudio()
    {
        PlayEffect(warningAudio);
    }
    public void CrackAudio()
    {
        PlayEffect(crackAudio);
    }
    public void SwimAudio()
    {
        PlayEffect(swimAudio);
    }
    public void CounterAudio()
    {
        PlayEffect(counterAudio);
    }
    public void DoorAudio()
    {
        PlayEffect(doorAudio);
    }
    public void VictoryAudio()
    {
        PlayEffect(correctAudio);
    }
    public void FailureAudio()
    {
        PlayEffect(wrongAudio);
    }

    private void PlayEffect(AudioClip clip)
    {
        if(audioSource==null || clip==null)
        {
            return;
        }
        // UI and gameplay events can happen close together. PlayOneShot keeps
        // the earlier feedback audible instead of replacing it mid-sound.
        audioSource.PlayOneShot(clip,GameAccessibilitySettings.EffectsVolumeLinear);
    }
}
