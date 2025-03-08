// VoiceManager.cs
// This script coordinates all voice-related components

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi;
using Meta.WitAi.Data.Configuration;

public class VoiceManager : MonoBehaviour
{
    [Header("Voice Components")]
    [SerializeField] private Wit wit;
    [SerializeField] private WitConfiguration witConfig;
    [SerializeField] private VoiceActivationHandler voiceHandler;
    [SerializeField] private VoiceServiceTester voiceTester;
    
    [Header("UI Elements")]
    [SerializeField] private Button activateVoiceButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI resultsText;
    
    // Debugging flag
    [SerializeField] private bool enableDetailedLogging = true;
    
    private void Start()
    {
        Debug.Log("VoiceManager initializing...");
        
        // Find components if not assigned
        if (wit == null)
        {
            wit = GetComponent<Wit>();
            if (wit == null)
            {
                wit = FindObjectOfType<Wit>();
                LogMessage("Searching for Wit component: " + (wit != null ? "Found" : "Not Found"));
            }
        }
        
        if (witConfig == null && wit != null)
        {
            // Try to get configuration using reflection since the property name might be different
            try
            {
                // Common property names used for configuration in various Wit versions
                string[] possibleFieldNames = new string[] { "Configuration", "configuration", "Config", "config", "witConfiguration" };
                
                System.Reflection.FieldInfo field = null;
                foreach (var fieldName in possibleFieldNames)
                {
                    field = wit.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        witConfig = field.GetValue(wit) as WitConfiguration;
                        if (witConfig != null)
                        {
                            LogMessage($"Found Wit configuration through field '{fieldName}'");
                            break;
                        }
                    }
                }
                
                if (witConfig == null)
                {
                    // If still not found, try to find it in the scene
                    witConfig = FindObjectOfType<WitConfiguration>();
                    LogMessage("Attempting to find WitConfiguration in scene: " + (witConfig != null ? "Found" : "Not Found"));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting Wit configuration: {e.Message}");
            }
        }
        
        if (voiceHandler == null)
        {
            voiceHandler = FindObjectOfType<VoiceActivationHandler>();
            LogMessage("Searching for VoiceActivationHandler: " + (voiceHandler != null ? "Found" : "Not Found"));
        }
        
        if (voiceTester == null)
        {
            voiceTester = GetComponent<VoiceServiceTester>();
            if (voiceTester == null)
            {
                voiceTester = FindObjectOfType<VoiceServiceTester>();
                LogMessage("Searching for VoiceServiceTester: " + (voiceTester != null ? "Found" : "Not Found"));
            }
        }
        
        // Set up voice tester if found
        if (voiceTester != null)
        {
            LogMessage("Setting up VoiceServiceTester");
            
            // Use reflection to set the fields
            System.Type type = voiceTester.GetType();
            
            if (wit != null)
            {
                SetPrivateField(type, voiceTester, "voiceService", wit);
                LogMessage("Set VoiceService in VoiceServiceTester");
            }
            
            if (witConfig != null)
            {
                SetPrivateField(type, voiceTester, "witConfig", witConfig);
                LogMessage("Set WitConfig in VoiceServiceTester");
            }
            
            if (activateVoiceButton != null)
            {
                SetPrivateField(type, voiceTester, "activateButton", activateVoiceButton);
                LogMessage("Set ActivateButton in VoiceServiceTester");
            }
            
            if (statusText != null)
            {
                SetPrivateField(type, voiceTester, "statusText", statusText);
                LogMessage("Set StatusText in VoiceServiceTester");
            }
            
            if (resultsText != null)
            {
                SetPrivateField(type, voiceTester, "resultText", resultsText);
                LogMessage("Set ResultText in VoiceServiceTester");
            }
        }
        
        // Set up the activate button
        if (activateVoiceButton != null)
        {
            activateVoiceButton.onClick.AddListener(ActivateVoice);
            LogMessage("Set up ActivateVoice button click handler");
        }
        
        // Force initialization by calling Start method on VoiceServiceTester
        if (voiceTester != null)
        {
            voiceTester.SendMessage("Start");
            LogMessage("Called Start method on VoiceServiceTester");
        }
        
        // Activate voice after a short delay
        Invoke("ActivateVoiceDelayed", 5f);
    }
    
    // Helper method to set private fields using reflection
    private void SetPrivateField(System.Type type, object obj, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
            LogMessage($"Successfully set {fieldName} field");
        }
        else
        {
            Debug.LogError($"Could not find field {fieldName} on {type.Name}");
        }
    }
    
    // Log with conditional output based on logging flag
    private void LogMessage(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[VoiceManager] {message}");
        }
    }
    
    // Manual activation method 
    public void ActivateVoice()
    {
        LogMessage("Activating voice manually...");
        
        // Try all possible methods to activate voice
        if (wit != null && !wit.Active)
        {
            LogMessage("Activating Wit directly");
            wit.Activate();
        }
        
        if (voiceHandler != null)
        {
            LogMessage("Activating via VoiceActivationHandler");
            voiceHandler.ActivateVoiceRecognition();
        }
        
        if (voiceTester != null)
        {
            LogMessage("Activating via VoiceServiceTester");
            voiceTester.SendMessage("ToggleListening");
        }
        
        // Test with a direct simulation
        if (witConfig != null && voiceTester != null)
        {
            LogMessage("Simulating voice input: 'Calibrate voice'");
            voiceTester.SendMessage("SimulateVoiceInput", "Calibrate voice");
        }
    }
    
    // Delayed activation to allow all systems to initialize
    private void ActivateVoiceDelayed()
    {
        LogMessage("Running delayed voice activation");
        ActivateVoice();
    }
    
    // Simulate specific voice commands for testing
    public void SimulateVoiceCommand(string command)
    {
        if (voiceTester != null)
        {
            LogMessage($"Simulating voice command: '{command}'");
            voiceTester.SendMessage("SimulateVoiceInput", command);
        }
    }
    
    // Update function for handling keyboard shortcuts
    private void Update()
    {
        // Spacebar to activate voice
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LogMessage("Spacebar pressed - activating voice");
            ActivateVoice();
        }
        
        // Number keys for testing specific commands
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SimulateVoiceCommand("Calibrate voice");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SimulateVoiceCommand("Begin therapy");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SimulateVoiceCommand("Next step");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            SimulateVoiceCommand("End session");
        }
    }
}