// File: TherapySessionManager.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;

public class TherapySessionManager : MonoBehaviour
{
    // References to UI elements
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private AudioSource feedbackAudio;
    
    // Reference to Wit.ai component
    [Header("Voice Recognition")]
    [SerializeField] private Wit wit;
    
    // Therapy session states
    private enum TherapyState
    {
        Calibration,
        Ready,
        InProgress,
        Paused,
        Completed
    }
    
    // Current therapy state
    private TherapyState currentState = TherapyState.Calibration;
    
    // Current step in the therapy sequence
    private int currentStep = 0;
    
    // Therapy instructions (would be populated with actual exercise instructions)
    private string[] therapySteps = new string[]
    {
        "Welcome to your therapy session. Say 'Begin therapy' when you're ready.",
        "Gently raise your shoulders and hold for 5 seconds.",
        "Now slowly turn your head to the left, then to the right.",
        "Great job! Now try to stretch your arms forward if comfortable.",
        "For the last exercise, take a deep breath in, and slowly exhale.",
        "Congratulations! You've completed today's session. Say 'End session' when ready."
    };
    
    void Start()
    {
        // Initialize UI
        UpdateUI();
        
        // Register for Wit.ai events
        if (wit != null)
        {
            wit.VoiceEvents.OnResponse.AddListener(OnWitResponse);
            wit.VoiceEvents.OnError.AddListener(OnWitError);
            wit.VoiceEvents.OnSend.AddListener(OnWitRequestCreated);
        }
        else
        {
            Debug.LogError("Wit component not assigned to TherapySessionManager!");
        }
        
        // Start with calibration message
        ShowFeedback("Let's calibrate your voice. Please say 'Calibrate voice' clearly.");
    }
    
    // Handle voice commands from Wit.ai
    private void OnWitResponse(WitResponseNode response)
    {
        // Hide the activation feedback once we get a response
        feedbackPanel.GetComponent<Animator>().SetBool("Listening", false);
        
        // Get the intent with the highest confidence
        WitResponseNode intents = response["intents"];
        if (intents.Count == 0)
        {
            HandleUnrecognizedCommand();
            return;
        }
        
        string intent = intents[0]["name"].Value;
        float confidence = intents[0]["confidence"].AsFloat;
        
        // Only process if confidence is above threshold
        if (confidence < 0.7f)
        {
            HandleLowConfidenceCommand(intent);
            return;
        }
        
        // Process different intents
        switch (intent)
        {
            case "therapy_start":
                StartTherapy();
                break;
                
            case "therapy_next":
                NextStep();
                break;
                
            case "therapy_end":
                EndSession();
                break;
                
            case "calibration":
                CompleteCalibration();
                break;
                
            default:
                HandleUnrecognizedCommand();
                break;
        }
    }
    
    // Called when Wit.ai encounters an error
    private void OnWitError(string error, string message)
    {
        feedbackPanel.GetComponent<Animator>().SetBool("Listening", false);
        ShowFeedback("I'm having trouble understanding. Could you please try again?");
    }
    
    // Called when a new Wit request is created (voice activation)
    private void OnWitRequestCreated(VoiceServiceRequest request)
    {
        // Show feedback that we're listening
        feedbackPanel.GetComponent<Animator>().SetBool("Listening", true);
        ShowFeedback("I'm listening...");
    }
    
    // Handles commands with low confidence score
    private void HandleLowConfidenceCommand(string intent)
    {
        switch (intent)
        {
            case "therapy_next":
                ShowFeedback("I think you said 'Next step'. Is that correct? Please say it again clearly.");
                break;
                
            case "therapy_end":
                ShowFeedback("Did you want to end the session? If so, please say 'End session' clearly.");
                break;
                
            default:
                ShowFeedback("I didn't quite catch that. Could you please try again?");
                break;
        }
    }
    
    // Handles completely unrecognized commands
    private void HandleUnrecognizedCommand()
    {
        ShowFeedback("I'm sorry, I didn't understand. Please try again or say 'Help' for options.");
    }
    
    // Completes the calibration process
    private void CompleteCalibration()
    {
        currentState = TherapyState.Ready;
        ShowFeedback("Calibration complete! Your voice threshold is set. Say 'Begin therapy' when you're ready to start.");
    }
    
    // Starts the therapy session
    private void StartTherapy()
    {
        if (currentState == TherapyState.Ready || currentState == TherapyState.Calibration)
        {
            currentState = TherapyState.InProgress;
            currentStep = 1; // Start at the first actual exercise
            UpdateUI();
            ShowFeedback("Therapy session started. " + therapySteps[currentStep]);
        }
        else
        {
            ShowFeedback("We're already in a therapy session. Say 'Next step' to continue or 'End session' to finish.");
        }
    }
    
    // Advances to the next therapy step
    private void NextStep()
    {
        if (currentState == TherapyState.InProgress)
        {
            currentStep++;
            
            if (currentStep < therapySteps.Length)
            {
                UpdateUI();
                ShowFeedback("Great job! " + therapySteps[currentStep]);
            }
            else
            {
                currentStep = therapySteps.Length - 1; // Stay on last step
                ShowFeedback("You've completed all exercises. Say 'End session' when you're ready to finish.");
            }
        }
        else
        {
            ShowFeedback("The therapy hasn't started yet. Say 'Begin therapy' to start.");
        }
    }
    
    // Ends the therapy session
    private void EndSession()
    {
        if (currentState == TherapyState.InProgress)
        {
            currentState = TherapyState.Completed;
            ShowFeedback("Therapy session saved. Would you like to clear your voice data or keep it for improvements?");
            
            // You would add user choice handling here
            
            Invoke("ResetToReady", 5f); // Reset after 5 seconds
        }
        else
        {
            ShowFeedback("There's no active therapy session to end.");
        }
    }
    
    // Resets to ready state after session completion
    private void ResetToReady()
    {
        currentState = TherapyState.Ready;
        currentStep = 0;
        UpdateUI();
        ShowFeedback("Ready for a new therapy session. Say 'Begin therapy' when you're ready.");
    }
    
    // Updates the UI based on current state and step
    private void UpdateUI()
    {
        if (instructionText != null)
        {
            instructionText.text = therapySteps[currentStep];
        }
    }
    
    // Shows feedback to the user (both text and voice)
    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            
            // Animate the feedback panel (you'd create this animation)
            if (feedbackPanel != null && feedbackPanel.GetComponent<Animator>() != null)
            {
                feedbackPanel.GetComponent<Animator>().SetTrigger("ShowFeedback");
            }
            
            // Play TTS feedback if available
            // This is where you'd integrate a TTS system
            // For MVP, you could use pre-recorded audio clips
        }
        
        Debug.Log("Feedback: " + message);
    }
    
    // For manual activation (you can add a button for testing)
    public void ActivateWit()
    {
        if (wit != null)
        {
            wit.Activate();
        }
    }
}