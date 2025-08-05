using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TutorialStep
{
    public Button targetButton;        // button to highlight in this step
    public string description;         // text for bottom panel
}

public class TutorialGuide : MonoBehaviour
{
    [SerializeField] CanvasGroup[] nonEssentialUI; // groups of UI to hide during steps
    [SerializeField] GameObject bottomPanel;
    [SerializeField] TMP_Text bottomText;
    [SerializeField] TutorialStep[] steps;

    public void ShowTutorial()
    {
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        foreach (var step in steps)
        {
            // store original state of non-essential UI and hide them
            var states = new List<(CanvasGroup cg, float alpha, bool interactable, bool blocksRaycasts)>();
            foreach (var cg in nonEssentialUI)
            {
                if (cg == null) continue;
                states.Add((cg, cg.alpha, cg.interactable, cg.blocksRaycasts));
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            // enable and highlight the step's button
            Outline outline = null;
            if (step.targetButton != null)
            {
                step.targetButton.gameObject.SetActive(true);
                step.targetButton.interactable = true;
                outline = step.targetButton.GetComponent<Outline>();
                if (outline == null) outline = step.targetButton.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(5f, 5f);
            }

            // update bottom panel text
            if (bottomPanel != null) bottomPanel.SetActive(true);
            if (bottomText   != null) bottomText.text = step.description;

            // wait for button click
            bool clicked = false;
            if (step.targetButton != null)
                step.targetButton.onClick.AddListener(() => clicked = true);

            while (!clicked)
                yield return null;

            // cleanup listeners and highlight
            if (step.targetButton != null)
            {
                step.targetButton.onClick.RemoveAllListeners();
                if (outline != null) Destroy(outline);
            }

            // restore bottom panel
            if (bottomPanel != null) bottomPanel.SetActive(false);

            // restore previously hidden UI elements
            foreach (var st in states)
            {
                st.cg.alpha = st.alpha;
                st.cg.interactable = st.interactable;
                st.cg.blocksRaycasts = st.blocksRaycasts;
            }
        }
    }
}