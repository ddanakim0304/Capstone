using UnityEngine;
using System.Collections.Generic;

public enum ComicAnimType { None, FadeIn, Pulse, Shake, SlideInFromLeft, SlideInFromRight }

// Reusable audio entry. Give each one a unique id so you can track what's playing.
[System.Serializable]
public class ComicMusic
{
    public string id = "Music";
    [Tooltip("The audio clip to play. Leave empty to skip.")]
    public AudioClip music;
    public AudioPan musicPan = AudioPan.Center;
    [Range(0f, 1f)]
    public float musicVolume = 1.0f;
    public bool musicLoop = false;
    [Tooltip("Fade-out duration in seconds (used by onEndMusic or manual StopFade).")]
    public float fadeDuration = 0.5f;
}

[System.Serializable]
public class ComicElement
{
    public string name = "Element";
    public GameObject targetObj;
    public ComicAnimType animationType;
    public float duration = 0.5f;
    public float delayBefore = 0f;
    public float magnitude = 0.1f;
    public bool waitForCompletion = true;

    [Header("Audio (Optional)")]
    [Tooltip("Sounds to play when this element's animation begins.")]
    public List<ComicMusic> onPlayMusic;
    
    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public Vector3 originalPos;
    [HideInInspector] public SpriteRenderer cachedRenderer; // Kept for legacy or single-access
    [HideInInspector] public SpriteRenderer[] cachedRenderers;
    [HideInInspector] public SpriteMask[] cachedMasks;
    [HideInInspector] public float[] originalMaskCutoffs;
}

[System.Serializable]
public class ComicPanel
{
    public string panelName = "Panel";

    [Header("Standard Animation")]
    public List<ComicElement> elements;
    
    [Header("Panel Audio (Optional)")]
    [Tooltip("Music entries to play when this panel begins.")]
    public List<ComicMusic> onStartMusic;
    [Tooltip("Music entries to fade out when this panel ends (match pan to what was started).")]
    public List<ComicMusic> onEndMusic;

    [Header("Choice Settings")]
    public bool isChoicePanel = false;
    
    [Tooltip("0 = Player 1, 1 = Player 2")]
    public int playerIndex = 0;

    public List<ComicElement> choiceElements;
    public ComicElement resultElement;

    [Header("Choice Timing")]
    public float delayBeforeChoices = 0.5f;

    [Header("Choice Audio (Optional)")]
    [Tooltip("Sound to play each time the player navigates left/right between choices.")]
    public ComicMusic choiceChangeSound;

    [Header("Timing")]
    public float delayAfterPanel = 1.0f;
}