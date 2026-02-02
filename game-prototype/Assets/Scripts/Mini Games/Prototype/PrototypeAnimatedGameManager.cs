using UnityEngine;
using System.Collections;

public class PrototypeAnimatedGameManager : MiniGameManager
{
    [Header("Character Appearance")]
    [Tooltip("The character GameObject that will slowly appear")]
    public GameObject characterObject;
    [Tooltip("Duration for the character to fully appear (in seconds)")]
    public float appearanceDuration = 2f;

    [Header("Sprite Animation")]
    [Tooltip("Two sprites that will iterate for the animation")]
    public GameObject[] animationSprites = new GameObject[2];
    [Tooltip("Duration for one complete cycle through both sprites")]
    public float cycleDuration = 0.8f;
    [Tooltip("Number of times to cycle through the sprites")]
    public int cycleCount = 5;

    private bool animationComplete = false;

    void Start()
    {
        // Ensure character starts invisible and animation sprites are hidden
        SetupInitialState();
        
        // Start the animation sequence
        StartCoroutine(PlayAnimationSequence());
    }

    private void SetupInitialState()
    {
        if (characterObject != null)
        {
            // Start with character completely transparent
            SetObjectAlpha(characterObject, 0f);
        }

        // Hide animation sprites initially
        for (int i = 0; i < animationSprites.Length; i++)
        {
            if (animationSprites[i] != null)
            {
                animationSprites[i].SetActive(false);
            }
        }
    }

    private IEnumerator PlayAnimationSequence()
    {
        // Phase 1: Character appearance animation
        yield return StartCoroutine(CharacterAppearAnimation());

        // Phase 2: Sprite cycling animation
        yield return StartCoroutine(SpriteCyclingAnimation());

        // Phase 3: Complete the game and move to next scene
        CompleteAnimation();
    }

    private IEnumerator CharacterAppearAnimation()
    {
        if (characterObject == null)
        {
            Debug.LogWarning("Character object not assigned! Skipping appearance animation.");
            yield break;
        }

        float elapsedTime = 0f;
        
        while (elapsedTime < appearanceDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / appearanceDuration;
            
            // Smooth ease-in animation
            float alpha = Mathf.SmoothStep(0f, 1f, progress);
            SetObjectAlpha(characterObject, alpha);
            
            yield return null;
        }

        // Ensure character is fully visible
        SetObjectAlpha(characterObject, 1f);
        
        Debug.Log("Character appearance animation complete!");
    }

    private IEnumerator SpriteCyclingAnimation()
    {
        if (animationSprites == null || animationSprites.Length < 2)
        {
            Debug.LogWarning("Not enough animation sprites assigned! Skipping sprite animation.");
            yield break;
        }

        // Validate sprites
        bool hasValidSprites = false;
        for (int i = 0; i < animationSprites.Length; i++)
        {
            if (animationSprites[i] != null)
            {
                hasValidSprites = true;
                break;
            }
        }

        if (!hasValidSprites)
        {
            Debug.LogWarning("No valid animation sprites found! Skipping sprite animation.");
            yield break;
        }

        float timePerSprite = cycleDuration / 2f; // Two sprites per cycle
        
        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            Debug.Log($"Starting sprite cycle {cycle + 1}/{cycleCount}");
            
            // Show first sprite
            if (animationSprites[0] != null)
            {
                animationSprites[0].SetActive(true);
            }
            if (animationSprites[1] != null)
            {
                animationSprites[1].SetActive(false);
            }
            
            yield return new WaitForSeconds(timePerSprite);
            
            // Show second sprite
            if (animationSprites[0] != null)
            {
                animationSprites[0].SetActive(false);
            }
            if (animationSprites[1] != null)
            {
                animationSprites[1].SetActive(true);
            }
            
            yield return new WaitForSeconds(timePerSprite);
        }

        // Hide both sprites after animation
        for (int i = 0; i < animationSprites.Length; i++)
        {
            if (animationSprites[i] != null)
            {
                animationSprites[i].SetActive(false);
            }
        }
        
        Debug.Log("Sprite cycling animation complete!");
    }

    private void CompleteAnimation()
    {
        if (animationComplete) return;
        
        animationComplete = true;
        
        Debug.Log("Prototype animation sequence completed! Moving to Chapter 1...");
        
        // Call the inherited WinGame method to proceed to next scene
        WinGame();
    }

    private void SetObjectAlpha(GameObject obj, float alpha)
    {
        if (obj == null) return;

        // Try to find SpriteRenderer first
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
            return;
        }

        // Try to find CanvasGroup for UI elements
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        // Try to find Image component for UI
        UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            return;
        }

        Debug.LogWarning($"Could not find SpriteRenderer, CanvasGroup, or Image component on {obj.name} to set alpha!");
    }

    // Optional: Allow manual testing in editor
    [ContextMenu("Test Animation Sequence")]
    private void TestAnimationSequence()
    {
        if (Application.isPlaying)
        {
            StopAllCoroutines();
            SetupInitialState();
            StartCoroutine(PlayAnimationSequence());
        }
    }
}