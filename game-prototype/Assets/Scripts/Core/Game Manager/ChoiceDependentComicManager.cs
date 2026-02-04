using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChoiceDependentComicManager : GeneralComicManager
{
    [Header("Choice Configuration")]
    [Tooltip("Assign objects here that should only be visible if their name matches the Selected Drink or Selected Activity.")]
    public List<GameObject> choiceDependentObjects;

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

        foreach (GameObject obj in choiceDependentObjects)
        {
            if (obj == null) continue;

            // Normalize Object Name
            string objName = obj.name.Trim().ToLower();

            // Check for match
            bool isDrinkMatch = !string.IsNullOrEmpty(drinkKey) && objName.Contains(drinkKey);
            bool isActivityMatch = !string.IsNullOrEmpty(activityKey) && objName.Contains(activityKey);

            // Enable if it matches either selection, otherwise disable permanently
            bool shouldBeActive = isDrinkMatch || isActivityMatch;

            obj.SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                Debug.Log($"[ChoiceDependentComicManager] Keeping '{obj.name}' active.");
            }
            else
            {
                obj.SetActive(false);
            }
        }
    }
}
