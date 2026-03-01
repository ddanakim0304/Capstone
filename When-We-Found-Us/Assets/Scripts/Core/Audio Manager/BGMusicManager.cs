using UnityEngine;

public class SceneMusicSetup : MonoBehaviour
{
    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float volume = 1.0f;

    private void Start()
    {
        if (backgroundMusic != null)
        {
            AudioManager.Instance.PlayBGM(backgroundMusic, volume);
        }
    }
}