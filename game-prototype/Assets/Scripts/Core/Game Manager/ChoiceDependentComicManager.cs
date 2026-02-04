using UnityEngine;
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

        // It does NOT match a selected choice.
        // Check if it is a "Hidden Choice" variant.
        // If it contains ANY choice keyword, but wasn't the selected one (checked above), then it must be a rejected choice.
        
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
             // It is a choice-related object, but it didn't match the selected choice.
             // Therefore, it is the "Hidden" / "Rejected" variant -> DISABLE IT.
             obj.SetActive(false);
             elem.targetObj = null; // Removing reference stops the base class from processing it
             Debug.Log($"[ChoiceDependentComicManager] Disabling '{obj.name}' (Identifying as hidden choice variant).");
        }
        else
        {
             // It is NOT a choice-related object (does not contain any choice keywords).
             // Therefore, it is a Background/Common object -> KEEP ACTIVE
             obj.SetActive(true);
             // Debug.Log($"[ChoiceDependentComicManager] Keeping '{obj.name}' active (Background object).");
        }
    }

    // Removed the choiceDependentObjects loop section as we now use FilterElementList
    /*
        foreach (GameObject obj in choiceDependentObjects)
        {
            // ...
        }
    */
}
