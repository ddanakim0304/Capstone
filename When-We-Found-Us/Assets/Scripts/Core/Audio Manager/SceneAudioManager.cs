using UnityEngine;
public class SceneAudioManager : MonoBehaviour
{
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float volume = 1.0f;
    public bool loop = true;

    private void Start()
    {
        if (bgmClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(bgmClip, AudioPan.Center, volume, loop);
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX(AudioPan.Center);
        }
    }
}
