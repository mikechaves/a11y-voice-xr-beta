using UnityEngine;
using UnityEngine.EventSystems;
using static OVRInput;
using System.Collections.Generic;

public class ControllerRaycaster : MonoBehaviour
{
    public LineRenderer lineRenderer; // Optional: for visualizing the ray
    public float rayLength = 30f; // Length of the raycast
    public LayerMask interactableLayer; // Layer for buttons or interactable objects
    private TherapyStoryboardController therapyController; // Reference to your main script

    void Start()
    {
        // Find the TherapyStoryboardController in the scene (assuming it's on a root GameObject)
        therapyController = FindObjectOfType<TherapyStoryboardController>();
        if (therapyController == null)
        {
            Debug.LogError("TherapyStoryboardController not found in the scene!");
        }
    }

    void Update()
    {
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;

        Debug.DrawRay(rayOrigin, rayDirection * rayLength, Color.red); // Visible in Scene view for debugging

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayLength, interactableLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, rayOrigin);
                lineRenderer.SetPosition(1, hit.point);
            }

            // Check if the hit object is a UI button or 3D interactable
            GameObject hitObject = hit.collider.gameObject;

            // Option 1: Handle UI Buttons (Canvas-based)
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Camera.main.WorldToScreenPoint(hit.point)
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject.CompareTag("Button")) // Tag your buttons with "Button"
                {
                    // Highlight or prepare for interaction (optional)
                    Debug.Log("Hovering over button: " + result.gameObject.name);

                    // Check for trigger (e.g., trigger button on controller)
                    if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || // Right controller
                        OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)) // Left controller
                    {
                        ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                    }
                    return; // Exit after handling UI interaction
                }
            }

            // Option 2: Handle 3D Buttons (with colliders)
            InteractableObject interactable = hitObject.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                // Highlight or prepare for interaction
                interactable.OnHover();

                // Check for trigger (e.g., trigger button on controller)
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || // Right controller
                    OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)) // Left controller
                {
                    interactable.OnClick();
                }
            }
        }
        else
        {
            Debug.Log("Raycast missed any objects.");
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, rayOrigin);
                lineRenderer.SetPosition(1, rayOrigin);
            }
        }
    }
}