using UnityEngine;
using System.Collections;

public enum AudioPan { Left, Right, Center }

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxLeftSource;
    public AudioSource sfxRightSource;
    public AudioSource sfxCenterSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(AudioClip musicClip, float volume = 1.0f)
    {
        // If the requested music is ALREADY playing, do nothing. 
        if (bgmSource.clip == musicClip && bgmSource.isPlaying) 
        {
            return; 
        }

        // If the clip is different then switch.
        StartCoroutine(FadeSwitchMusic(musicClip, volume));
    }

    // A smooth fader to switch tracks without jarring cuts
    private IEnumerator FadeSwitchMusic(AudioClip newClip, float volume)
    {
        float fadeTime = 1.0f;
        float startVolume = bgmSource.volume;

        // Fade Out
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // Fade In
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, volume, t / fadeTime);
            yield return null;
        }
        bgmSource.volume = volume;
    }
    public void PlaySFX(AudioClip clip, AudioPan pan, float volume = 1.0f, bool loop = false, float startTime = 0f)
    {
        if (clip == null) return;

        AudioSource source = pan switch
        {
            AudioPan.Left   => sfxLeftSource,
            AudioPan.Right  => sfxRightSource,
            _               => sfxCenterSource,
        };

        if (loop)
        {
            // For looping we must assign the clip directly so loop works
            source.clip = clip;
            source.volume = volume;
            source.loop = true;
            source.Play();
            if (startTime > 0f) source.time = Mathf.Clamp(startTime, 0f, clip.length);
        }
        else
        {
            source.loop = false;
            source.PlayOneShot(clip, volume);
        }
    }

    public void StopSFX(AudioPan pan)
    {
        switch (pan)
        {
            case AudioPan.Left: sfxLeftSource.Stop(); break;
            case AudioPan.Right: sfxRightSource.Stop(); break;
            case AudioPan.Center: sfxCenterSource.Stop(); break;
        }
    }

    public void SetSFXVolume(AudioPan pan, float volume)
    {
        AudioSource source = pan switch
        {
            AudioPan.Left   => sfxLeftSource,
            AudioPan.Right  => sfxRightSource,
            _               => sfxCenterSource,
        };
        source.volume = volume;
    }

    public void FadeOutSFX(AudioPan pan, float fadeTime = 0.5f)
    {
        StartCoroutine(FadeOutSFXCoroutine(pan, fadeTime));
    }

    private IEnumerator FadeOutSFXCoroutine(AudioPan pan, float fadeTime)
    {
        AudioSource source = pan switch
        {
            AudioPan.Left   => sfxLeftSource,
            AudioPan.Right  => sfxRightSource,
            _               => sfxCenterSource,
        };

        float startVolume = source.volume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }
        source.Stop();
        // Reset volume for next use
        source.volume = startVolume; 
    }
}