using UnityEngine;
using System.Collections; // Required for Coroutines

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
            DontDestroyOnLoad(gameObject); // This object survives scene changes!
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- BGM LOGIC ---

    public void PlayBGM(AudioClip musicClip, float volume = 1.0f)
    {
        // If the requested music is ALREADY playing, do nothing. 
        // The music will keep playing as if the scene change never happened.
        if (bgmSource.clip == musicClip && bgmSource.isPlaying) 
        {
            return; 
        }

        // If the clip is different, we switch.
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

    // --- SFX LOGIC ---
    public void PlaySFX(AudioClip clip, AudioPan pan, float volume = 1.0f, bool loop = false)
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
        source.volume = startVolume; // Reset volume for next use
    }
}