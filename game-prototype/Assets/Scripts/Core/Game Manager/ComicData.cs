using UnityEngine;
using System.Collections.Generic;

public enum ComicAnimType { None, FadeIn, Pulse, Shake }

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
    
    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public Vector3 originalPos;
    [HideInInspector] public SpriteRenderer cachedRenderer;
}

[System.Serializable]
public class ComicPanel
{
    public string panelName = "Panel";

    [Header("Standard Animation")]
    public List<ComicElement> elements;
    
    [Header("Choice Settings")]
    public bool isChoicePanel = false;
    
    [Tooltip("0 = Player 1, 1 = Player 2")]
    public int playerIndex = 0;

    public List<GameObject> choiceOptions;
    public ComicElement resultElement;

    [Header("Timing")]
    public float delayAfterPanel = 1.0f;
}