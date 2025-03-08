// VoiceServiceTester.cs
// This script provides an alternative to using Wit directly

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Configuration;

public class VoiceServiceTester : MonoBehaviour
{
    [Header("Voice Service")]
    [SerializeField] private VoiceService voiceService;
    [SerializeField] private WitConfiguration witConfig;
    
    [Header("UI Components")]
    [SerializeField] private Button activateButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI resultText;
    
    private bool isListening = false;
    
    private void Start()
    {
        // Set up the button listener
        if (activateButton != null)
        {
            activateButton.onClick.AddListener(ToggleListening);
        }
        
        // Check if we have voice service
        if (voiceService == null)
        {
            // Try to find it in the scene
            voiceService = FindObjectOfType<VoiceService>();
            
            // If still null, add a warning
            if (voiceService == null)
            {
                Debug.LogWarning("No VoiceService found in the scene. Voice commands won't work.");
                if (statusText != null) statusText.text = "No Voice Service Available";
                return;
            }
        }
        
        // Register for events
        voiceService.VoiceEvents.OnStartListening.AddListener(OnStartedListening);
        voiceService.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        voiceService.VoiceEvents.OnError.AddListener(OnError);
        voiceService.VoiceEvents.OnResponse.AddListener(OnResponse);
        
        // Initial status
        if (statusText != null) statusText.text = "Ready. Press button to test voice.";
    }
    
    public void ToggleListening()
    {
        if (!isListening)
        {
            // Start listening
            if (voiceService != null)
            {
                voiceService.Activate();
            }
            else
            {
                Debug.LogError("Cannot activate - VoiceService is null");
                if (statusText != null) statusText.text = "Error: No Voice Service";
            }
        }
        else
        {
            // Stop listening
            if (voiceService != null)
            {
                voiceService.Deactivate();
            }
        }
    }
    
    private void OnStartedListening()
    {
        isListening = true;
        Debug.Log("Started listening");
        if (statusText != null) statusText.text = "Listening...";
        if (activateButton != null) activateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop Listening";
    }
    
    private void OnStoppedListening()
    {
        isListening = false;
        Debug.Log("Stopped listening");
        if (statusText != null) statusText.text = "Processing...";
        if (activateButton != null) activateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Listening";
    }
    
    private void OnError(string error, string message)
    {
        Debug.LogError($"Voice error: {error} - {message}");
        isListening = false;
        if (statusText != null) statusText.text = $"Error: {error}";
        if (activateButton != null) activateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Listening";
    }
    
    // Simple helper method that just returns false
    // This ensures we always add our listeners
    private bool ShouldAddListener()
    {
        return true;
    }
    
    private void OnResponse(WitResponseNode response)
    {
        Debug.Log("Got response: " + response.ToString());
        
        // Process the response
        if (resultText != null)
        {
            // Try to extract intent
            string intent = "No intent detected";
            float confidence = 0;
            
            WitResponseNode intents = response["intents"];
            if (intents != null && intents.Count > 0)
            {
                intent = intents[0]["name"].Value;
                confidence = intents[0]["confidence"].AsFloat;
                resultText.text = $"Intent: {intent}\nConfidence: {confidence:F2}";
            }
            else
            {
                resultText.text = "No intent detected in response";
            }
        }
        
        if (statusText != null) statusText.text = "Ready. Press button to test voice.";
    }
    
    // Simpler implementation for voice input simulation
    public void SimulateVoiceInput(string textInput)
    {
        Debug.Log($"VoiceServiceTester: Simulating voice input: '{textInput}'");
        
        // Update status text
        if (statusText != null)
            statusText.text = $"Simulating: \"{textInput}\"";
        
        // Find voice service if needed
        if (voiceService == null)
        {
            voiceService = FindObjectOfType<VoiceService>();
            Debug.Log($"Looking for VoiceService: {(voiceService != null ? "Found" : "Not found")}");
        }
        
        // Try to use the voice service
        if (voiceService != null)
        {
            try
            {
                // Activate with text input
                Debug.Log($"Activating VoiceService with text: '{textInput}'");
                voiceService.Activate(textInput);
                
                // Update status
                if (statusText != null)
                    statusText.text = $"Processing: \"{textInput}\"";
            }
            catch (System.Exception e)
            {
                // Log error
                Debug.LogError($"Error activating voice service: {e.Message}");
                
                // Update status
                if (statusText != null)
                    statusText.text = $"Error: {e.Message}";
            }
        }
        else
        {
            // No voice service found
            Debug.LogError("Cannot simulate voice input - Voice Service not found");
            
            // Update status
            if (statusText != null)
                statusText.text = "Error: Voice Service not available";
        }
    }
}