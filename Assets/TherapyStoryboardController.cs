// File: TherapyStoryboardController.cs
// Updated for Meta Quest 3 voice interaction

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi;
using Meta.WitAi.Json;

public class TherapyStoryboardController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject calibrationScene;
    [SerializeField] private GameObject therapyStartScene;
    [SerializeField] private GameObject exerciseScene;
    [SerializeField] private GameObject errorHandlingScene;
    [SerializeField] private GameObject endSessionScene;
    
    [Header("Components")]
    [SerializeField] private Wit wit;
    [SerializeField] private FeedbackSystem feedbackSystem;
    
    [Header("Voice Activation")]
    [SerializeField] private float autoActivationDelay = 1.5f;
    [SerializeField] private float reactivationDelay = 0.5f;
    [SerializeField] private bool autoActivateOnStart = true;
    [SerializeField] private GameObject voiceIndicator;
    
    [Header("Confidence Thresholds")]
    [SerializeField] private float highConfidenceThreshold = 0.85f;
    [SerializeField] private float mediumConfidenceThreshold = 0.65f;
    [SerializeField] private float minConfidenceThreshold = 0.5f;
    
    // Current state of the storyboard flow
    private StoryboardState currentState = StoryboardState.Calibration;
    
    // For handling calibration results
    private bool isCalibrated = false;
    private float voiceThreshold = 0f;
    
    // For calibration phrases
    [Header("Calibration")]
    [SerializeField] private string[] calibrationPhrases = new string[3] 
    {
        "Hello, voice system",
        "Start my therapy",
        "This is a test"
    };
    [SerializeField] private int currentCalibrationPhrase = 0;

    [Header("Calibration UI")]
    [SerializeField] private TextMeshProUGUI calibrationPhraseText;
    [SerializeField] private TextMeshProUGUI calibrationInstructionText;
    [SerializeField] private Slider voiceLevelSlider;
    [SerializeField] private Image[] progressDots;
    [SerializeField] private float microphoneVisualUpdateInterval = 0.05f;
    [SerializeField] private Image voiceStatusIndicator;
    private float lastMicrophoneLevel = 0f;
    private Coroutine microphoneVisualizationCoroutine;
    private Coroutine voiceActivationCoroutine;
    
    // For exercise progression
    [Header("Exercise Progression")]
    [SerializeField] private string[] exerciseInstructions = new string[]
    {
        "Gently raise your shoulders and hold for 5 seconds.",
        "Slowly turn your head to the left, then to the right.",
        "Stretch your arms forward as comfortable for you.",
        "Take a deep breath in, and slowly exhale.",
        "Gently rotate your shoulders forward, then backward."
    };
    [SerializeField] private int currentExerciseIndex = 0;
    
    // For Mixed Reality settings
    [Header("Mixed Reality")]
    [SerializeField] private Transform uiAnchor;
    [SerializeField] private float uiDistance = 1.0f;
    [SerializeField] private bool followHeadPosition = true;
    
    // Keep track of misunderstood commands for error handling demo
    private int consecutiveErrors = 0;
    
    // For debug display
    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private GameObject debugPanel;
    
    // Store last recognized entities and traits
    private Dictionary<string, string> extractedEntities = new Dictionary<string, string>();
    private Dictionary<string, string> extractedTraits = new Dictionary<string, string>();
    
    // For storing pending confirmation intent
    private string pendingConfirmationIntent = null;
    
    // Processing voice command
    private bool isProcessingVoiceCommand = false;
    
    void Start()
    {
        // Register for Wit.ai events
        if (wit != null)
        {
            wit.VoiceEvents.OnResponse.AddListener(OnWitResponse);
            wit.VoiceEvents.OnError.AddListener(OnWitError);
            wit.VoiceEvents.OnStartListening.AddListener(OnStartListening);
            wit.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
            wit.VoiceEvents.OnAborted.AddListener(OnAborted);
        }
        else
        {
            Debug.LogError("Wit component not assigned!");
        }
        
        // Initialize dictionaries
        extractedEntities = new Dictionary<string, string>();
        extractedTraits = new Dictionary<string, string>();
        
        // Set up initial scene
        SwitchToState(StoryboardState.Calibration);
        
        // Hide debug panel in builds
        if (debugPanel != null)
        {
            debugPanel.SetActive(Application.isEditor);
        }
        
        // Auto-start voice recognition
        if (autoActivateOnStart)
        {
            ActivateVoiceWithDelay(autoActivationDelay);
        }
        
        // Position UI appropriately for MR
        PositionUIForMixedReality();

        // Force activate voice after a short delay
        StartCoroutine(ForceActivateVoice());
    }

    private IEnumerator ForceActivateVoice()
    {
        yield return new WaitForSeconds(2.0f);
        
        Debug.Log("🎤 FORCING VOICE ACTIVATION");
        if (wit != null)
        {
            wit.Activate();
            Debug.Log("🎤 WIT ACTIVATED: " + wit.Active);
        }
        else
        {
            Debug.LogError("🎤 WIT IS NULL");
        }
    }
    
    void Update()
    {
        // Check every 60 frames (~1 second) if voice recognition is active
        if (wit != null && !wit.Active && !isProcessingVoiceCommand && Time.frameCount % 60 == 0)
        {
            // Auto-restart voice recognition if it's not active and we're not processing a command
            ActivateVoiceWithDelay(0.1f);
        }
        
        // Update voice status indicator
        UpdateVoiceStatusIndicator();
        
        // If we're in follow head mode, update UI position
        if (followHeadPosition && uiAnchor != null)
        {
            PositionUIForMixedReality();
        }
    }
    
    // Position UI appropriately for mixed reality
    private void PositionUIForMixedReality()
    {
        if (uiAnchor != null)
        {
            // Find main camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Position UI in front of user
                Vector3 position = mainCamera.transform.position + mainCamera.transform.forward * uiDistance;
                
                // Only use forward direction, maintain Y height
                position.y = uiAnchor.position.y;
                
                // Update position
                uiAnchor.position = position;
                
                // Make UI face the user
                uiAnchor.rotation = Quaternion.LookRotation(
                    uiAnchor.position - new Vector3(mainCamera.transform.position.x, uiAnchor.position.y, mainCamera.transform.position.z), 
                    Vector3.up
                );
            }
        }
    }
    
    // Voice status events
    private void OnStartListening()
    {
        Debug.Log("🎤 WIT STARTED LISTENING");

        Debug.Log("Started listening");
        if (voiceStatusIndicator != null)
        {
            voiceStatusIndicator.color = Color.green;
        }
    }
    
    private void OnStoppedListening()
    {
        Debug.Log("🎤 WIT STOPPED LISTENING");
        
        Debug.Log("Stopped listening");
        if (voiceStatusIndicator != null)
        {
            voiceStatusIndicator.color = Color.yellow;
        }
    }
    
    private void OnAborted()
    {
        Debug.Log("Listening aborted");
        if (voiceStatusIndicator != null)
        {
            voiceStatusIndicator.color = Color.red;
        }
        
        // Reactivate voice after abort
        ActivateVoiceWithDelay(reactivationDelay);
    }
    
    // Update voice status indicator
    private void UpdateVoiceStatusIndicator()
    {
        if (voiceStatusIndicator != null)
        {
            if (wit != null && wit.Active)
            {
                // Pulsate when active
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
                voiceStatusIndicator.color = Color.Lerp(new Color(0.2f, 0.8f, 0.2f), new Color(0.8f, 1f, 0.8f), pulse);
            }
            else if (isProcessingVoiceCommand)
            {
                voiceStatusIndicator.color = Color.yellow;
            }
            else
            {
                voiceStatusIndicator.color = Color.gray;
            }
        }
        
        if (voiceIndicator != null)
        {
            voiceIndicator.SetActive(wit != null && wit.Active);
        }
    }
    
    // Activate voice recognition with delay
    private void ActivateVoiceWithDelay(float delay)
    {
        if (voiceActivationCoroutine != null)
        {
            StopCoroutine(voiceActivationCoroutine);
        }
        
        voiceActivationCoroutine = StartCoroutine(ActivateVoiceAfterDelay(delay));
    }
    
    // Coroutine to activate voice after delay
    private IEnumerator ActivateVoiceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (wit != null && !wit.Active && !isProcessingVoiceCommand)
        {
            Debug.Log("Auto-activating voice recognition");
            wit.Activate();
        }
        
        voiceActivationCoroutine = null;
    }
    
    // Switch to specified storyboard state
    private void SwitchToState(StoryboardState newState)
    {
        // Deactivate all scenes first
        calibrationScene?.SetActive(false);
        therapyStartScene?.SetActive(false);
        exerciseScene?.SetActive(false);
        errorHandlingScene?.SetActive(false);
        endSessionScene?.SetActive(false);
        
        // Activate appropriate scene based on new state
        switch (newState)
        {
            case StoryboardState.Calibration:
                calibrationScene?.SetActive(true);
                ShowCalibrationPrompt();
                break;
                
            case StoryboardState.StartTherapy:
                therapyStartScene?.SetActive(true);
                feedbackSystem.ShowFeedback("Calibration complete. Your voice threshold is set. Let's get started! Say 'Begin therapy' when you're ready.");
                UpdateVoiceCommandsText("Voice Commands Available:\n• \"Start therapy\"\n• \"Calibrate voice\"\n• \"Help\"");
                break;
                
            case StoryboardState.ExerciseInProgress:
                exerciseScene?.SetActive(true);
                ShowCurrentExercise();
                UpdateVoiceCommandsText("Voice Commands Available:\n• \"Next step\"\n• \"Previous step\"\n• \"Repeat\"\n• \"Pause\"\n• \"End session\"");
                break;
                
            case StoryboardState.ErrorHandling:
                errorHandlingScene?.SetActive(true);
                feedbackSystem.ShowFeedback("I'm sorry, I missed that. Did you mean 'Next Step' or 'End Session'?", FeedbackSystem.FeedbackType.Error);
                UpdateVoiceCommandsText("Please say clearly:\n• \"Next step\"\n• \"End session\"\n• \"Help\"");
                break;
                
            case StoryboardState.EndSession:
                endSessionScene?.SetActive(true);
                feedbackSystem.ShowFeedback("Therapy session saved. Would you like to clear voice data or keep it for improvements?");
                UpdateVoiceCommandsText("Voice Commands Available:\n• \"Yes\"\n• \"No\"\n• \"Help\"");
                break;
        }
        
        // Update current state
        currentState = newState;
    }
    
    // Update voice commands text based on current state
    private void UpdateVoiceCommandsText(string commands)
    {
        GameObject currentScene = null;
        
        // Get the active scene GameObject
        switch (currentState)
        {
            case StoryboardState.Calibration:
                currentScene = calibrationScene;
                break;
            case StoryboardState.StartTherapy:
                currentScene = therapyStartScene;
                break;
            case StoryboardState.ExerciseInProgress:
                currentScene = exerciseScene;
                break;
            case StoryboardState.ErrorHandling:
                currentScene = errorHandlingScene;
                break;
            case StoryboardState.EndSession:
                currentScene = endSessionScene;
                break;
        }
        
        // Find and update the commands text
        if (currentScene != null)
        {
            TextMeshProUGUI[] textComponents = currentScene.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in textComponents)
            {
                if (text.name.Contains("Command") || text.name.Contains("Help"))
                {
                    text.text = commands;
                    break;
                }
            }
        }
    }
    
    // Process Wit.ai responses
    private void OnWitResponse(WitResponseNode response)
    {
        Debug.Log("🎤 WIT RESPONSE RECEIVED: " + response.ToString());

        // Set processing flag
        isProcessingVoiceCommand = true;
        
        // Log the full response for debugging
        Debug.Log("Wit Response: " + response.ToString());
        
        // Clear previous entities and traits
        extractedEntities.Clear();
        extractedTraits.Clear();
        
        // Get intent and confidence
        string intent = "";
        float confidence = 0f;
        
        WitResponseNode intents = response["intents"];
        if (intents.Count == 0)
        {
            HandleMisunderstoodCommand();
            // Reactivate voice after a short delay
            ActivateVoiceWithDelay(reactivationDelay);
            isProcessingVoiceCommand = false;
            return;
        }
        
        intent = intents[0]["name"].Value;
        confidence = intents[0]["confidence"].AsFloat;
        
        // Display debug info
        if (debugText != null)
        {
            debugText.text = $"Intent: {intent}\nConfidence: {confidence:F2}";
        }
        
        // Extract entities
        WitResponseNode entities = response["entities"];
        if (entities != null)
        {
            // Iterate through entity types properly
            foreach (string entityName in entities.ChildNodeNames)
            {
                WitResponseNode entityValues = entities[entityName];
                if (entityValues != null && entityValues.Count > 0)
                {
                    // Get the first value of this entity type
                    string entityValue = entityValues[0]["value"].Value;
                    
                    // Store the entity value
                    extractedEntities[entityName] = entityValue;
                    
                    // Add to debug display
                    if (debugText != null)
                    {
                        debugText.text += $"\nEntity: {entityName}={entityValue}";
                    }
                }
            }
        }
        
        // Extract traits
        WitResponseNode traits = response["traits"];
        if (traits != null)
        {
            // Iterate through trait types properly
            foreach (string traitName in traits.ChildNodeNames)
            {
                WitResponseNode traitValues = traits[traitName];
                if (traitValues != null && traitValues.Count > 0)
                {
                    // Get the first value of this trait type
                    string traitValue = traitValues[0]["value"].Value;
                    
                    // Store the trait value
                    extractedTraits[traitName] = traitValue;
                    
                    // Add to debug display
                    if (debugText != null)
                    {
                        debugText.text += $"\nTrait: {traitName}={traitValue}";
                    }
                }
            }
        }
        
        // Process based on confidence level
        if (confidence >= highConfidenceThreshold)
        {
            // High confidence - process normally
            ProcessIntent(intent);
        }
        else if (confidence >= mediumConfidenceThreshold)
        {
            // Medium confidence - ask for confirmation
            AskForConfirmation(intent);
        }
        else if (confidence >= minConfidenceThreshold)
        {
            // Low confidence - give specific guidance
            HandleLowConfidenceIntent(intent);
        }
        else
        {
            // Very low confidence - general error message
            HandleMisunderstoodCommand();
        }
        
        // Reactivate voice after a short delay
        ActivateVoiceWithDelay(reactivationDelay);
        
        // Clear processing flag
        isProcessingVoiceCommand = false;
    }
    
    // Process intent based on current state
    private void ProcessIntent(string intent)
    {
        Debug.Log($"Processing intent: {intent} in state: {currentState}");
        
        // Haptic feedback for Quest controllers (if available)
        TriggerHapticFeedback();
        
        // If we're confirming something, handle confirmation intents specially
        if (pendingConfirmationIntent != null)
        {
            if (intent == "confirmation_yes")
            {
                // Process the pending intent that was confirmed
                ProcessIntent(pendingConfirmationIntent);
                pendingConfirmationIntent = null;
                return;
            }
            else if (intent == "confirmation_no")
            {
                pendingConfirmationIntent = null;
                feedbackSystem.ShowFeedback("I understand. What would you like to do instead?");
                return;
            }
        }
        
        // Handle different intents
        switch (intent)
        {
            case "therapy_start":
                HandleTherapyStart();
                break;
                
            case "therapy_end":
                HandleTherapyEnd();
                break;
                
            case "therapy_pause":
                HandleTherapyPause();
                break;
                
            case "therapy_resume":
                HandleTherapyResume();
                break;
                
            case "therapy_next":
                HandleTherapyNext();
                break;
                
            case "therapy_previous":
                HandleTherapyPrevious();
                break;
                
            case "therapy_repeat":
                HandleTherapyRepeat();
                break;
                
            case "calibration_start":
                HandleCalibrationStart();
                break;
                
            case "calibration_confirm":
                HandleCalibrationConfirm();
                break;
                
            case "help_request":
                HandleHelpRequest();
                break;
                
            case "confirmation_yes":
                HandleConfirmationYes();
                break;
                
            case "confirmation_no":
                HandleConfirmationNo();
                break;
                
            case "feedback_positive":
                HandlePositiveFeedback();
                break;
                
            case "feedback_negative":
                HandleNegativeFeedback();
                break;
                
            case "emergency_stop":
                HandleEmergencyStop();
                break;
                
            default:
                // Check if this is a state-specific intent
                switch (currentState)
                {
                    case StoryboardState.Calibration:
                        HandleCalibrationResponse(intent, 1.0f); // Process as high confidence for calibration
                        break;
                    default:
                        HandleMisunderstoodCommand();
                        break;
                }
                break;
        }
    }
    
    // Provide haptic feedback when commands are recognized (for Quest controllers)
    private void TriggerHapticFeedback()
    {
        // Check if OVRInput is available (for Quest)
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (OVRInput.IsControllerConnected(OVRInput.Controller.Touch))
        {
            OVRInput.SetControllerVibration(0.3f, 0.3f, OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(0.3f, 0.3f, OVRInput.Controller.LTouch);
        }
        #endif
    }
    
    // Ask for confirmation when medium confidence
    private void AskForConfirmation(string intent)
    {
        string intentName = GetFriendlyIntentName(intent);
        
        // Store the intent that needs confirmation
        pendingConfirmationIntent = intent;
        
        // Ask for confirmation
        feedbackSystem.ShowFeedback($"I think you want to {intentName}. Is that correct?");
        UpdateVoiceCommandsText("Please confirm:\n• \"Yes\"\n• \"No\"");
    }
    
    // Convert intent name to user-friendly text
    private string GetFriendlyIntentName(string intent)
    {
        switch (intent)
        {
            case "therapy_start": return "start the therapy session";
            case "therapy_end": return "end the session";
            case "therapy_pause": return "pause the session";
            case "therapy_resume": return "resume the session";
            case "therapy_next": return "go to the next step";
            case "therapy_previous": return "go back to the previous step";
            case "therapy_repeat": return "repeat the instructions";
            case "calibration_start": return "start voice calibration";
            case "calibration_confirm": return "confirm the calibration";
            case "help_request": return "get help";
            case "emergency_stop": return "stop immediately";
            default: return intent.Replace("_", " ");
        }
    }
    
    // Process intents with low confidence
    private void HandleLowConfidenceIntent(string intent)
    {
        switch (intent)
        {
            case "therapy_next":
                feedbackSystem.ShowFeedback("I think you want to move to the next step. Please say 'Next step' clearly if that's correct.", FeedbackSystem.FeedbackType.Neutral);
                break;
                
            case "therapy_end":
                feedbackSystem.ShowFeedback("I think you want to end the session. Please say 'End session' clearly if that's correct.", FeedbackSystem.FeedbackType.Neutral);
                break;
                
            case "therapy_start":
                feedbackSystem.ShowFeedback("I think you want to start therapy. Please say 'Begin therapy' or 'Start therapy' clearly.", FeedbackSystem.FeedbackType.Neutral);
                break;
                
            default:
                HandleMisunderstoodCommand();
                break;
        }
    }
    
    // Handle Wit.ai errors
    private void OnWitError(string error, string message)
    {
        feedbackSystem.ShowFeedback("I'm having trouble understanding. Could you please try again?", FeedbackSystem.FeedbackType.Error);
        consecutiveErrors++;
        
        // If multiple consecutive errors, offer more help
        if (consecutiveErrors >= 3)
        {
            switch (currentState)
            {
                case StoryboardState.Calibration:
                    feedbackSystem.ShowFeedback("Let's try calibration again. Please speak clearly and directly.", FeedbackSystem.FeedbackType.Error);
                    currentCalibrationPhrase = 0;
                    ShowCalibrationPrompt();
                    break;
                    
                case StoryboardState.StartTherapy:
                    feedbackSystem.ShowFeedback("To start therapy, please say 'Begin therapy' or 'Start session' clearly.", FeedbackSystem.FeedbackType.Error);
                    break;
                    
                case StoryboardState.ExerciseInProgress:
                    feedbackSystem.ShowFeedback("You can say 'Next step' to continue or 'End session' to finish. Would you like to try again?", FeedbackSystem.FeedbackType.Error);
                    break;
                    
                case StoryboardState.EndSession:
                    feedbackSystem.ShowFeedback("Would you like to keep your voice data? Please say 'Yes' or 'No' clearly.", FeedbackSystem.FeedbackType.Error);
                    break;
            }
            
            consecutiveErrors = 0;
        }
        
        // Reactivate voice after error
        ActivateVoiceWithDelay(reactivationDelay);
    }
    
    #region Intent Handler Methods
    
    private void HandleTherapyStart()
    {
        if (currentState == StoryboardState.StartTherapy || currentState == StoryboardState.Calibration)
        {
            // Check for any exercise customization from entities
            string intensity = GetEntityValue("Intensity", "moderate");
            
            feedbackSystem.ShowFeedback($"Starting a {intensity} therapy session. I'll guide you through the exercises.", FeedbackSystem.FeedbackType.Success);
            SwitchToState(StoryboardState.ExerciseInProgress);
            consecutiveErrors = 0;
        }
        else
        {
            feedbackSystem.ShowFeedback("We're already in a therapy session. Say 'Next step' to continue or 'End session' to finish.");
        }
    }
    
    private void HandleTherapyEnd()
    {
        if (currentState == StoryboardState.ExerciseInProgress)
        {
            feedbackSystem.ShowFeedback("Ending your session. Great job today!", FeedbackSystem.FeedbackType.Success);
            SwitchToState(StoryboardState.EndSession);
            consecutiveErrors = 0;
        }
        else
        {
            feedbackSystem.ShowFeedback("There's no active therapy session to end.");
        }
    }
    
    private void HandleTherapyPause()
    {
        if (currentState == StoryboardState.ExerciseInProgress)
        {
            feedbackSystem.ShowFeedback("Session paused. Take your time. Say 'Resume' when you're ready to continue.", FeedbackSystem.FeedbackType.Success);
            // In a real implementation, you would pause timers, animations, etc.
            consecutiveErrors = 0;
        }
        else
        {
            feedbackSystem.ShowFeedback("There's no active therapy session to pause.");
        }
    }
    
    private void HandleTherapyResume()
    {
        feedbackSystem.ShowFeedback("Resuming your session. Let's continue where we left off.", FeedbackSystem.FeedbackType.Success);
        // In a real implementation, you would resume timers, animations, etc.
        consecutiveErrors = 0;
    }
    
    private void HandleTherapyNext()
    {
        if (currentState == StoryboardState.ExerciseInProgress)
        {
            // Advance to next exercise
            currentExerciseIndex++;
            if (currentExerciseIndex < exerciseInstructions.Length)
            {
                // Get sentiment trait if available
                string sentiment = GetTraitValue("Sentiment", "neutral");
                
                // Provide feedback based on sentiment
                if (sentiment == "positive")
                {
                    feedbackSystem.ShowFeedback("Great job! I can tell you're doing well. Moving to the next exercise.", FeedbackSystem.FeedbackType.Success);
                }
                else if (sentiment == "negative")
                {
                    feedbackSystem.ShowFeedback("Moving to the next exercise. Remember, it's okay to go at your own pace.", FeedbackSystem.FeedbackType.Success);
                }
                else
                {
                    feedbackSystem.ShowFeedback("Moving to the next exercise.", FeedbackSystem.FeedbackType.Success);
                }
                
                ShowCurrentExercise();
            }
            else
            {
                feedbackSystem.ShowFeedback("You've completed all exercises. Say 'End session' when you're ready.", FeedbackSystem.FeedbackType.Success);
            }
            
            consecutiveErrors = 0;
        }
        else
        {
            feedbackSystem.ShowFeedback("You need to start a therapy session first. Say 'Start therapy' to begin.");
        }
    }
    
    private void HandleTherapyPrevious()
    {
        if (currentState == StoryboardState.ExerciseInProgress && currentExerciseIndex > 0)
        {
            currentExerciseIndex--;
            feedbackSystem.ShowFeedback("Going back to the previous exercise.", FeedbackSystem.FeedbackType.Success);
            ShowCurrentExercise();
            consecutiveErrors = 0;
        }
        else
        {
            if (currentExerciseIndex == 0)
            {
                feedbackSystem.ShowFeedback("You're at the first exercise. There's no previous step to go back to.");
            }
            else
            {
                feedbackSystem.ShowFeedback("You need to be in an active therapy session to go back a step.");
            }
        }
    }
    
    private void HandleTherapyRepeat()
    {
        feedbackSystem.ShowFeedback("Let me repeat the instructions for this step.", FeedbackSystem.FeedbackType.Success);
        ShowCurrentExercise();
        consecutiveErrors = 0;
    }
    
    private void HandleCalibrationStart()
    {
        currentState = StoryboardState.Calibration;
        currentCalibrationPhrase = 0;
        feedbackSystem.ShowFeedback("Starting voice calibration. Please say each phrase as it appears on screen.", FeedbackSystem.FeedbackType.Success);
        SwitchToState(StoryboardState.Calibration);
        consecutiveErrors = 0;
    }
    
    private void HandleCalibrationConfirm()
    {
        if (currentState == StoryboardState.Calibration)
        {
            currentCalibrationPhrase++;
            
            if (currentCalibrationPhrase < calibrationPhrases.Length)
            {
                // Continue calibration
                ShowCalibrationPrompt();
            }
            else
            {
                // Calibration complete
                isCalibrated = true;
                feedbackSystem.ShowFeedback("Calibration complete! Your voice threshold is set. Let's get started!", FeedbackSystem.FeedbackType.Success);
                SwitchToState(StoryboardState.StartTherapy);
            }
            
            consecutiveErrors = 0;
        }
    }
    
    private void HandleHelpRequest()
    {
        // Check for specific assistance type
        string assistanceType = GetEntityValue("Assistance_Type", "general");
        
        switch (assistanceType)
        {
            case "instructions":
                feedbackSystem.ShowFeedback("Here are detailed instructions for this exercise: follow the visual guide and say 'Next' when ready to proceed.", FeedbackSystem.FeedbackType.Neutral);
                break;
                
            case "modification":
                feedbackSystem.ShowFeedback("You can modify this exercise by reducing the range of motion if needed. Just do what feels comfortable for you.", FeedbackSystem.FeedbackType.Neutral);
                break;
                
            case "simplification":
                feedbackSystem.ShowFeedback("Let's simplify this. Just focus on the basic movement and don't worry about perfect form for now.", FeedbackSystem.FeedbackType.Neutral);
                break;
                
            default:
                // General help based on current state
                switch (currentState)
                {
                    case StoryboardState.Calibration:
                        feedbackSystem.ShowFeedback("During calibration, just clearly say the phrases shown on screen. This helps the system recognize your voice.", FeedbackSystem.FeedbackType.Neutral);
                        break;
                        
                    case StoryboardState.StartTherapy:
                        feedbackSystem.ShowFeedback("Say 'Start therapy' to begin your session, or 'Calibrate voice' if you need to recalibrate.", FeedbackSystem.FeedbackType.Neutral);
                        break;
                        
                    case StoryboardState.ExerciseInProgress:
                        feedbackSystem.ShowFeedback("You can say 'Next' to move forward, 'Previous' to go back, 'Pause' to take a break, or 'End session' to finish.", FeedbackSystem.FeedbackType.Neutral);
                        break;
                        
                    case StoryboardState.EndSession:
                        feedbackSystem.ShowFeedback("Your session is complete. Say 'Yes' to save your progress data or 'No' to discard it.", FeedbackSystem.FeedbackType.Neutral);
                        break;
                }
                break;
        }
        
        consecutiveErrors = 0;
    }
    
    private void HandleConfirmationYes()
    {
        if (pendingConfirmationIntent != null)
        {
            // Process the confirmed intent
            ProcessIntent(pendingConfirmationIntent);
            pendingConfirmationIntent = null;
        }
        else if (currentState == StoryboardState.EndSession)
        {
            feedbackSystem.ShowFeedback("Thank you. Your session data has been saved for future improvements.", FeedbackSystem.FeedbackType.Success);
            StartCoroutine(ResetAfterDelay(5f));
        }
        else
        {
            feedbackSystem.ShowFeedback("Thank you for confirming. What would you like to do next?", FeedbackSystem.FeedbackType.Success);
        }
        
        consecutiveErrors = 0;
    }
    
    private void HandleConfirmationNo()
    {
        if (pendingConfirmationIntent != null)
        {
            feedbackSystem.ShowFeedback("I understand. What would you like to do instead?", FeedbackSystem.FeedbackType.Neutral);
            pendingConfirmationIntent = null;
        }
        else if (currentState == StoryboardState.EndSession)
        {
            feedbackSystem.ShowFeedback("No problem. Your session data has been discarded to protect your privacy.", FeedbackSystem.FeedbackType.Success);
            StartCoroutine(ResetAfterDelay(5f));
        }
        else
        {
            feedbackSystem.ShowFeedback("I understand. What would you like to do next?", FeedbackSystem.FeedbackType.Neutral);
        }
        
        consecutiveErrors = 0;
    }
    
    private void HandlePositiveFeedback()
    {
        // Get sentiment information for more personalized response
        string sentiment = GetTraitValue("Sentiment", "positive");
        
        if (sentiment == "positive")
        {
            feedbackSystem.ShowFeedback("I'm so glad to hear that! It's wonderful that you're having a good experience.", FeedbackSystem.FeedbackType.Success);
        }
        else
        {
            feedbackSystem.ShowFeedback("Thank you for the positive feedback. I'm here to help make your therapy experience better.", FeedbackSystem.FeedbackType.Success);
        }
        
        consecutiveErrors = 0;
    }
    
    private void HandleNegativeFeedback()
    {
        // Check for body part or pain level entities
        string bodyPart = GetEntityValue("Body_Part", "");
        string painLevel = GetEntityValue("Pain_Level", "");
        
        if (!string.IsNullOrEmpty(bodyPart) && !string.IsNullOrEmpty(painLevel))
        {
            feedbackSystem.ShowFeedback($"I understand you're experiencing {painLevel} discomfort in your {bodyPart}. Let's modify this exercise to make it more comfortable.", FeedbackSystem.FeedbackType.Error);
        }
        else if (!string.IsNullOrEmpty(bodyPart))
        {
            feedbackSystem.ShowFeedback($"I understand your {bodyPart} is causing you difficulty. Let's adapt this exercise.", FeedbackSystem.FeedbackType.Error);
        }
        else
        {
            feedbackSystem.ShowFeedback("I'm sorry to hear you're having difficulty. Would you like to try a different exercise or modify this one?", FeedbackSystem.FeedbackType.Error);
        }
        
        consecutiveErrors = 0;
    }
    
    private void HandleEmergencyStop()
    {
        feedbackSystem.ShowFeedback("STOPPING ALL ACTIVITIES IMMEDIATELY. Are you okay?", FeedbackSystem.FeedbackType.Error);
        // In a real implementation, you would immediately halt all activities, animations, etc.
        consecutiveErrors = 0;
        
        // Return to start menu after a brief pause
        StartCoroutine(ResetAfterDelay(5f));
    }
    
    #endregion
    
    // Handle calibration responses
    private void HandleCalibrationResponse(string intent, float confidence)
    {
        if (confidence >= minConfidenceThreshold)
        {
            currentCalibrationPhrase++;
            
            if (currentCalibrationPhrase < calibrationPhrases.Length)
            {
                // Continue calibration
                ShowCalibrationPrompt();
                
                // Update instruction
                if (calibrationInstructionText != null)
                {
                    calibrationInstructionText.text = "Great! Now try the next phrase.";
                }
            }
            else
            {
                // Calibration complete
                isCalibrated = true;
                
                // Update UI
                if (calibrationPhraseText != null)
                {
                    calibrationPhraseText.text = "Calibration Complete!";
                }
                
                if (calibrationInstructionText != null)
                {
                    calibrationInstructionText.text = "Your voice is now calibrated. Moving to therapy...";
                }
                
                // Stop microphone visualization
                if (microphoneVisualizationCoroutine != null)
                {
                    StopCoroutine(microphoneVisualizationCoroutine);
                }
                
                // Wait a moment before transitioning to next state
                StartCoroutine(DelayedStateTransition(StoryboardState.StartTherapy, 2.0f));
            }
        }
        else
        {
            // Retry current phrase
            feedbackSystem.ShowFeedback("I didn't quite catch that. Let's try again.", FeedbackSystem.FeedbackType.Error);
            ShowCalibrationPrompt();
            
            // Update instruction
            if (calibrationInstructionText != null)
            {
                calibrationInstructionText.text = "Please speak a bit more clearly.";
            }
        }
    }

    // Wait before transitioning to a new state
    private IEnumerator DelayedStateTransition(StoryboardState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        SwitchToState(newState);
    }
    
    // Show the current calibration prompt
    private void ShowCalibrationPrompt()
    {
        if (currentCalibrationPhrase < calibrationPhrases.Length)
        {
            // Update phrase text
            if (calibrationPhraseText != null)
            {
                calibrationPhraseText.text = $"\"{calibrationPhrases[currentCalibrationPhrase]}\"";
            }
            
            // Update instruction text
            if (calibrationInstructionText != null)
            {
                calibrationInstructionText.text = "Please speak the phrase clearly when ready";
            }
            
            // Update progress dots
            UpdateProgressDots();
            
            // Show feedback using feedback system
            feedbackSystem.ShowFeedback($"Please say: \"{calibrationPhrases[currentCalibrationPhrase]}\"", FeedbackSystem.FeedbackType.Neutral);
            UpdateVoiceCommandsText("Voice Command:\n• Say the phrase above clearly");
            
            // Start microphone visualization
            StartMicrophoneVisualization();
        }
    }

    // Update progress dots to show current state
    private void UpdateProgressDots()
    {
        if (progressDots != null && progressDots.Length > 0)
        {
            for (int i = 0; i < progressDots.Length; i++)
            {
                if (progressDots[i] != null)
                {
                    // Current dot is green, completed dots are dark green, upcoming dots are gray
                    if (i == currentCalibrationPhrase)
                    {
                        progressDots[i].color = new Color(0.29f, 0.69f, 0.31f); // Green
                    }
                    else if (i < currentCalibrationPhrase)
                    {
                        progressDots[i].color = new Color(0.13f, 0.55f, 0.13f); // Dark Green
                    }
                    else
                    {
                        progressDots[i].color = new Color(0.67f, 0.67f, 0.67f); // Gray
                    }
                }
            }
        }
    }

    // Start microphone level visualization
    private void StartMicrophoneVisualization()
    {
        // Stop any existing coroutine
        if (microphoneVisualizationCoroutine != null)
        {
            StopCoroutine(microphoneVisualizationCoroutine);
        }
        
        // Start new visualization coroutine
        microphoneVisualizationCoroutine = StartCoroutine(UpdateMicrophoneVisualization());
    }

    // Coroutine to simulate microphone level changes
    private IEnumerator UpdateMicrophoneVisualization()
    {
        // Reset slider
        if (voiceLevelSlider != null)
        {
            voiceLevelSlider.value = 0;
        }
        
        while (true)
        {
            // In a real implementation, you would get the actual microphone level here
            // For testing, we'll simulate random fluctuations
            float randomFluctuation = UnityEngine.Random.Range(-0.1f, 0.1f);
            lastMicrophoneLevel = Mathf.Clamp01(lastMicrophoneLevel + randomFluctuation);
            
            // When Wit is actively listening, show higher levels
            if (wit != null && wit.Active)
            {
                lastMicrophoneLevel = Mathf.Lerp(lastMicrophoneLevel, 0.7f, 0.3f);
            }
            else
            {
                lastMicrophoneLevel = Mathf.Lerp(lastMicrophoneLevel, 0.1f, 0.3f);
            }
            
            // Update the slider
            if (voiceLevelSlider != null)
            {
                voiceLevelSlider.value = lastMicrophoneLevel;
            }
            
            yield return new WaitForSeconds(microphoneVisualUpdateInterval);
        }
    }
    
    // Show the current exercise instructions
    private void ShowCurrentExercise()
    {
        if (currentExerciseIndex < exerciseInstructions.Length)
        {
            feedbackSystem.ShowFeedback(exerciseInstructions[currentExerciseIndex], FeedbackSystem.FeedbackType.Success);
        }
    }
    
    // Handle misunderstood commands
    private void HandleMisunderstoodCommand()
    {
        consecutiveErrors++;
        
        if (consecutiveErrors >= 3)
        {
            // After multiple consecutive errors, provide more explicit help
            SwitchToState(StoryboardState.ErrorHandling);
            consecutiveErrors = 0;
        }
        else
        {
            // General error feedback
            feedbackSystem.ShowFeedback("I didn't understand. Could you please try again?", FeedbackSystem.FeedbackType.Error);
        }
    }
    
    // Helper method to get entity value with fallback
    private string GetEntityValue(string entityName, string fallback)
    {
        if (extractedEntities.ContainsKey(entityName))
        {
            return extractedEntities[entityName];
        }
        return fallback;
    }
    
    // Helper method to get trait value with fallback
    private string GetTraitValue(string traitName, string fallback)
    {
        if (extractedTraits.ContainsKey(traitName))
        {
            return extractedTraits[traitName];
        }
        return fallback;
    }
    
    // Reset the experience after a delay
    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Reset variables
        currentCalibrationPhrase = 0;
        currentExerciseIndex = 0;
        consecutiveErrors = 0;
        
        // Return to start
        SwitchToState(StoryboardState.Calibration);
    }
    
    // For manually activating voice recognition (e.g., via a button)
    public void ActivateVoiceRecognition()
    {
        if (wit != null && !wit.Active)
        {
            wit.Activate();
        }
    }
    
    // Storyboard states
    public enum StoryboardState
    {
        Calibration,
        StartTherapy,
        ExerciseInProgress,
        ErrorHandling,
        EndSession
    }

    // For UI button trigger
    public void SimulateCalibrationConfirmation()
    {
        // This public method can be called directly from a UI button
        HandleCalibrationResponse("calibration_confirm", 1.0f);
    }
    
    // When application pauses or resumes (e.g., when Quest goes to sleep/wakes up)
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) // App resuming
        {
            // Reactivate voice when app resumes
            ActivateVoiceWithDelay(1.0f);
        }
    }
}