using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : MonoBehaviour
{
    [SerializeField] private int DialogueIndex;
    private int index = 1;
    private int dialoguePoint = 0;

    [SerializeField] private int changeCount = 0;
    [SerializeField] private int[] changePoint;

    private bool interactionToggle = false;

    public void Interaction()
    {
        DialogueManager.Instance.StartDialogue(DialogueIndex, index, gameObject);
        if (changePoint.Length > index - 1)
        {
            ChangeDialogue();
        }
    }

    private void ChangeDialogue()
    {
        dialoguePoint++;
        if (changeCount < changePoint.Length && changePoint[changeCount] <= dialoguePoint)
        {
            index++;
            changeCount++;
        }
    }
    public void InteractionToggle()
    {
        interactionToggle = !interactionToggle;
    }
}

