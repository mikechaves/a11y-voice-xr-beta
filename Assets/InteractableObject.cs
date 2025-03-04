using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    public UnityEvent onClick; // Use this for button actions in the Inspector

    public void OnHover()
    {
        // Optional: Add visual feedback (e.g., highlight the button)
        Debug.Log("Hovering over " + gameObject.name);
    }

    public void OnClick()
    {
        onClick?.Invoke();
        Debug.Log("Clicked " + gameObject.name);
    }
}