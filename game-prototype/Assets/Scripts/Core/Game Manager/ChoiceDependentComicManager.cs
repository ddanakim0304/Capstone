using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class ChoiceDependentComicManager : GeneralComicManager
{
    [Header("Filtering Settings")]
    [Tooltip("List of keywords that identify an object as being part of a choice group. Objects NOT containing any of these will be treated as common/background objects and kept active.")]
    public List<string> choiceKeywords = new List<string> { "coffee", "tea", "book", "game" };

    protected override void Start()
    {
        // Apply filtering before the base Start() initializes and plays the scene
        ApplyChoiceFiltering();
        base.Start();
    }

    private void ApplyChoiceFiltering()
    {
        if (MainGameFlowManager.Instance == null)
        {
            Debug.LogWarning("[ChoiceDependentComicManager] MainGameFlowManager instance not found! No filtering applied.");
            return;
        }

        string selectedDrink = MainGameFlowManager.Instance.SelectedDrink;
        string selectedActivity = MainGameFlowManager.Instance.SelectedActivity;

        // Debugging filters: if no selection is made, force default values
        if (string.IsNullOrEmpty(selectedActivity)) 
        {
             selectedActivity = "book";
        }

        if (string.IsNullOrEmpty(selectedDrink)) 
        {
             selectedDrink = "coffee";
        }

        // Normalize Selection Strings
        string drinkKey = selectedDrink != null ? selectedDrink.Trim().ToLower() : "";
        string activityKey = selectedActivity != null ? selectedActivity.Trim().ToLower() : "";

        Debug.Log($"[ChoiceDependentComicManager] Applying filter. Selected Drink: '{drinkKey}', Selected Activity: '{activityKey}'");

        if (comicPanels == null) return;

        // Iterate through all panels and all elements
        foreach (var panel in comicPanels)
        {
             if (panel == null) continue;
             
             // Process elements
             FilterElementList(panel.elements, drinkKey, activityKey);
             
             // Process choice elements
             FilterElementList(panel.choiceElements, drinkKey, activityKey);
             
             // Process result element
             if (panel.resultElement != null)
             {
                 FilterSingleElement(panel.resultElement, drinkKey, activityKey);
             }
        }
    }

    private void FilterElementList(List<ComicElement> elements, string drinkKey, string activityKey)
    {
        if (elements == null) return;
        foreach (var elem in elements)
        {
            FilterSingleElement(elem, drinkKey, activityKey);
        }
    }

    private void FilterSingleElement(ComicElement elem, string drinkKey, string activityKey)
    {
        if (elem == null || elem.targetObj == null) return;

        GameObject obj = elem.targetObj;
        string objName = obj.name.Trim().ToLower();

        // Check if it matches the SELECTED choices
        bool isDrinkMatch = !string.IsNullOrEmpty(drinkKey) && objName.Contains(drinkKey);
        bool isActivityMatch = !string.IsNullOrEmpty(activityKey) && objName.Contains(activityKey);
        
        if (isDrinkMatch || isActivityMatch)
        {
             // It matches a selected choice -> KEEP ACTIVE
             obj.SetActive(true);
             Debug.Log($"[ChoiceDependentComicManager] Keeping '{obj.name}' active (Matches selection).");
             return;
        }

        bool isAnyChoiceObject = false;
        foreach(string keyword in choiceKeywords)
        {
            if (!string.IsNullOrEmpty(keyword) && objName.Contains(keyword.ToLower()))
            {
                isAnyChoiceObject = true;
                break;
            }
        }

        if (isAnyChoiceObject)
        {
             obj.SetActive(false);
             elem.targetObj = null;
             Debug.Log($"[ChoiceDependentComicManager] Disabling '{obj.name}' (Identifying as hidden choice variant).");
        }
        else
        {
             obj.SetActive(true);
        }
    }

    // Override WinGame to handle choice-dependent scene routing
    protected override void WinGame()
    {
        // Prevent winning multiple times
        if (isGameWon) return;
        isGameWon = true;

        if (MainGameFlowManager.Instance == null)
        {
            Debug.LogError("[ChoiceDependentComicManager] MainGameFlowManager not found! Cannot proceed.");
            return;
        }

        string selectedActivity = MainGameFlowManager.Instance.SelectedActivity;
        string targetScene = "";
        
        if (!string.IsNullOrEmpty(selectedActivity))
        {
            string activityKey = selectedActivity.ToLower();
            
            if (activityKey == "game")
            {
                targetScene = "3 Choice-Game";
            }
            else if (activityKey == "book")
            {
                targetScene = "2 Choice-Book";
            }
        }

        if (!string.IsNullOrEmpty(targetScene))
        {
            Debug.Log($"[ChoiceDependentComicManager] Comic sequence complete! Loading scene based on activity choice: {targetScene} (Activity: {selectedActivity})");
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            // Fallback to base behavior if no valid activity selected
            Debug.LogWarning($"[ChoiceDependentComicManager] No valid activity selected ({selectedActivity}). Using fallback scene loading.");
            base.WinGame();
        }
    }

}
