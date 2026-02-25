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

    // --- SFX LOGIC (Same as before) ---
    public void PlaySFX(AudioClip clip, AudioPan pan)
    {
        if (clip == null) return;

        switch (pan)
        {
            // Pan -1
            case AudioPan.Left: sfxLeftSource.PlayOneShot(clip); break;
            // Pan 1
            case AudioPan.Right: sfxRightSource.PlayOneShot(clip); break;
            // Pan 0
            case AudioPan.Center: sfxCenterSource.PlayOneShot(clip); break;
        }
    }
}