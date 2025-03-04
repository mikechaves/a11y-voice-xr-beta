// TextInputSimulator.cs
// This script provides a UI for simulating voice input through text

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi;
using Meta.WitAi.Configuration;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;

public class TextInputSimulator : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_InputField textInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private GameObject simulatorPanel;
    
    [Header("Wit Configuration")]
    // Removed WitConfiguration field as it is not available and not used
    
    // Reference to voice service
    private VoiceService voiceService;
    
    // Common test phrases
    [Header("Quick Test Phrases")]
    [SerializeField] private string[] testPhrases = new string[]
    {
        "start therapy",
        "next step",
        "end session",
        "calibrate voice"
    };
    
    void Start()
    {
        // Find voice service in scene
        voiceService = FindObjectOfType<VoiceService>();
        
        if (voiceService == null)
        {
            Debug.LogError("No VoiceService found in scene. TextInputSimulator won't work.");
            responseText.text = "ERROR: No VoiceService found";
            return;
        }
        
        // Set up UI
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitText);
        }
        
        // Create quick test buttons
        CreateTestButtons();
        
        // Show simulator panel
        if (simulatorPanel != null)
        {
            simulatorPanel.SetActive(true);
        }
    }
    
    // Creates buttons for quick test phrases
    private void CreateTestButtons()
    {
        if (simulatorPanel == null) return;
        
        // Get or create a panel for the buttons
        Transform buttonPanel = simulatorPanel.transform.Find("QuickTestPanel");
        if (buttonPanel == null)
        {
            GameObject panel = new GameObject("QuickTestPanel");
            panel.transform.SetParent(simulatorPanel.transform, false);
            buttonPanel = panel.transform;
            
            // Add layout component
            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
        
        // Create buttons for each test phrase
        foreach (string phrase in testPhrases)
        {
            GameObject buttonObj = new GameObject(phrase.Replace(" ", "_") + "_Button");
            buttonObj.transform.SetParent(buttonPanel, false);
            
            // Add button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Add image component for background
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1.0f);
            
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = phrase;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            // Set layout
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 40);
            
            // Set text layout
            RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0, 0);
            textRectTransform.anchorMax = new Vector2(1, 1);
            textRectTransform.offsetMin = new Vector2(5, 5);
            textRectTransform.offsetMax = new Vector2(-5, -5);
            
            // Add click listener
            string currentPhrase = phrase; // Capture for lambda
            button.onClick.AddListener(() => {
                if (textInput != null) textInput.text = currentPhrase;
                SubmitText();
            });
        }
    }
    
    // Submit the text in the input field
    public void SubmitText()
    {
        if (textInput == null || string.IsNullOrEmpty(textInput.text))
        {
            Debug.LogWarning("No text to submit");
            responseText.text = "Please enter text to simulate voice input";
            return;
        }
        
        string text = textInput.text;
        Debug.Log($"Submitting text: {text}");
        
        // Update response
        responseText.text = $"Processing: \"{text}\"";
        
        // Send to Wit
        SendToWit(text);
    }
    
    // Send text to Wit.ai
    private void SendToWit(string text)
    {
        if (voiceService == null)
        {
            Debug.LogError("Cannot send text - VoiceService is null");
            responseText.text = "ERROR: VoiceService not available";
            return;
        }
        
        try
        {
            // Use the VoiceService to activate transcription with the provided text
            voiceService.Activate(text);
            
            Debug.Log("Text sent to Wit.ai via Activate");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error sending text to Wit: {e.Message}");
            responseText.text = $"ERROR: {e.Message}";
        }
    }
    
    // Handle the Wit.ai response
    private void HandleResponse(WitResponseNode response)
    {
        Debug.Log("Received response from Wit.ai");
        
        // Extract intent information
        string intentInfo = "No intent detected";
        
        WitResponseNode intents = response["intents"];
        if (intents != null && intents.Count > 0)
        {
            string intent = intents[0]["name"].Value;
            float confidence = intents[0]["confidence"].AsFloat;
            intentInfo = $"Intent: {intent} (Confidence: {confidence:F2})";
            
            // Extract entities if any
            WitResponseClass entities = response["entities"] as WitResponseClass;
            if (entities != null && entities.Count > 0)
            {
                intentInfo += "\n\nEntities:";
                foreach (KeyValuePair<string, WitResponseNode> kvp in entities)
                {
                    string name = kvp.Key;
                    string value = kvp.Value[0]["value"].Value;
                    intentInfo += $"\n- {name}: {value}";
                }
            }
        }
        
        // Update UI
        responseText.text = intentInfo;
    }
    
    // Handle errors
    private void HandleError(string error, string message)
    {
        Debug.LogError($"Wit.ai error: {error} - {message}");
        responseText.text = $"ERROR: {error}\n{message}";
    }
    
    // Toggle the simulator panel
    public void ToggleSimulator()
    {
        if (simulatorPanel != null)
        {
            simulatorPanel.SetActive(!simulatorPanel.activeSelf);
        }
    }
}