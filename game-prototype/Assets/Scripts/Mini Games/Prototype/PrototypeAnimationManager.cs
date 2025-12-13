using UnityEngine;
using System.Collections;

public class PrototypeAnimationManager : MonoBehaviour
{
    [Tooltip("Assign the 5 objects here")]
    public GameObject[] animationFrames;
    public float switchInterval = 0.5f;

    private int currentIndex = 0;

    void Start()
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            StartCoroutine(AnimateObjects());
        }
    }

    private IEnumerator AnimateObjects()
    {
        while (true)
        {
            // Enable the current frame and disable others
            for (int i = 0; i < animationFrames.Length; i++)
            {
                if (animationFrames[i] != null)
                {
                    animationFrames[i].SetActive(i == currentIndex);
                }
            }

            yield return new WaitForSeconds(switchInterval);

            // Move to the next frame, looping back to 0
            currentIndex = (currentIndex + 1) % animationFrames.Length;
        }
    }
}
