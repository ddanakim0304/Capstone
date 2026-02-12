using UnityEngine;
using System.Collections;

public class PrototypeAnimationManager : MiniGameManager
{
    [Tooltip("Assign the 5 objects here")]
    public GameObject[] animationFrames;
    public float switchInterval = 0.5f;

    void Start()
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            StartCoroutine(AnimateObjects());
        }
    }

    private IEnumerator AnimateObjects()
    {
        for (int i = 0; i < animationFrames.Length; i++)
        {
            // Enable the current frame and disable others
            for (int j = 0; j < animationFrames.Length; j++)
            {
                if (animationFrames[j] != null)
                {
                    animationFrames[j].SetActive(j == i);
                }
            }

            yield return new WaitForSeconds(switchInterval);
        }

        WinGame();
    }
}
