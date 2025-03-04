// Add this script to your scene to ensure proper camera setup
using UnityEngine;

public class MRCameraSetup : MonoBehaviour
{
    void Start()
    {
        // Find all cameras in the scene
        Camera[] cameras = FindObjectsOfType<Camera>();
        
        // Disable any camera that's not part of the XR Rig
        foreach (Camera cam in cameras)
        {
            if (!cam.transform.IsChildOf(transform))
            {
                Debug.Log("Disabling non-XR camera: " + cam.name);
                cam.gameObject.SetActive(false);
            }
        }
    }
}