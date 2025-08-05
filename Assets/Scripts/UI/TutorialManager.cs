using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Tooltip("Optional list of highlight or overlay objects spawned during the tutorial")]
    [SerializeField] private List<GameObject> overlayObjects = new List<GameObject>();

    [Tooltip("Components that were disabled while the tutorial ran and should be re-enabled when finished")]
    [SerializeField] private List<Behaviour> disabledUIElements = new List<Behaviour>();

    [Tooltip("Player control script that should be re-enabled after the tutorial")] 
    [SerializeField] private MonoBehaviour playerController;

    /// <summary>
    /// Runs the tutorial sequence.  At the end of the tutorial we clean up any
    /// temporary visual elements, record that the player has seen the tutorial,
    /// and return control back to the player.
    /// </summary>
    public void ShowTutorial()
    {
        // ... tutorial steps would occur here ...

        EndTutorial();
    }

    /// <summary>
    /// Cleans up tutorial state and hands control back to the player.
    /// </summary>
    void EndTutorial()
    {
        // Remove any highlights/overlays that were created for the tutorial
        foreach (var overlay in overlayObjects)
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
        }
        overlayObjects.Clear();

        // Re-enable any UI elements that were disabled for the tutorial
        foreach (var ui in disabledUIElements)
        {
            if (ui != null)
            {
                ui.enabled = true;
            }
        }
        disabledUIElements.Clear();

        // Store that the tutorial has been seen so it doesn't run again
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        // Hand control back to the player
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        // Camera remains in its current position; no explicit changes needed
    }
}