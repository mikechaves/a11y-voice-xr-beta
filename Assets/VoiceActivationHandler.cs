// File: VoiceActivationHandler.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Json;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi.Requests;

public class VoiceActivationHandler : MonoBehaviour
{
    [Header("Voice Recognition")]
    [SerializeField] private Wit wit;
    [SerializeField] private float activationTimeout = 10f;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject listeningIndicator;
    [SerializeField] private Image listeningProgress;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioClip startListeningSound;
    [SerializeField] private AudioClip stopListeningSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioSource feedbackAudio;
    
    private Coroutine timeoutCoroutine;
    private bool isListening = false;
    
    // For voice detection calibration
    private float voiceThreshold = 0.02f; // Default value
    private bool isCalibrated = false;
    
    void Start()
    {
        Debug.Log("VoiceActivationHandler initializing...");
        
        // Find Wit component if not assigned
        if (wit == null)
        {
            wit = GetComponent<Wit>();
            if (wit == null)
            {
                wit = FindObjectOfType<Wit>();
                Debug.Log("Attempting to find Wit component in scene: " + (wit != null ? "Found" : "Not found"));
            }
        }
        
        // Register for Wit.ai events
        if (wit != null)
        {
            Debug.Log("Wit component found. Registering events...");
            wit.VoiceEvents.OnSend.AddListener((request) => OnListeningStart(request));
            wit.VoiceEvents.OnResponse.AddListener(OnListeningComplete);
            wit.VoiceEvents.OnError.AddListener(OnListeningError);
            wit.VoiceEvents.OnAborted.AddListener(OnListeningAborted);
            
            // Log Wit configuration
            Debug.Log($"Wit configuration - Active: {wit.Active}, IsRequestActive: {wit.IsRequestActive}");
        }
        else
        {
            Debug.LogError("Wit component not assigned to VoiceActivationHandler!");
        }
        
        // Initialize UI elements
        if (listeningIndicator != null)
            listeningIndicator.SetActive(false);
            
        if (listeningProgress != null)
            listeningProgress.fillAmount = 0f;
            
        if (statusText != null)
            statusText.text = "Say a command to begin";
    }
    
    // Called when Wit starts listening
    private void OnListeningStart(VoiceServiceRequest request)
    {
        isListening = true;
        
        // Play start sound
        if (feedbackAudio != null && startListeningSound != null)
            feedbackAudio.PlayOneShot(startListeningSound);
        
        // Show listening indicator
        if (listeningIndicator != null)
            listeningIndicator.SetActive(true);
            
        if (statusText != null)
            statusText.text = "Listening...";
        
        // Start timeout coroutine
        if (timeoutCoroutine != null)
            StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ListeningTimeout());
    }
    
    // Called when Wit successfully processes speech
    private void OnListeningComplete(WitResponseNode response)
    {
        isListening = false;
        
        // Play stop sound
        if (feedbackAudio != null && stopListeningSound != null)
            feedbackAudio.PlayOneShot(stopListeningSound);
        
        // Hide listening indicator
        if (listeningIndicator != null)
            listeningIndicator.SetActive(false);
            
        if (statusText != null)
            statusText.text = "Command received";
        
        // Stop timeout coroutine
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
    }
    
    // Called when Wit encounters an error
    private void OnListeningError(string error, string message)
    {
        isListening = false;
        
        // Play error sound
        if (feedbackAudio != null && errorSound != null)
            feedbackAudio.PlayOneShot(errorSound);
        
        // Hide listening indicator
        if (listeningIndicator != null)
            listeningIndicator.SetActive(false);
            
        if (statusText != null)
            statusText.text = "Sorry, I didn't catch that";
        
        // Stop timeout coroutine
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
        
        // Show error feedback
        Debug.LogWarning($"Wit Error: {error} - {message}");
    }
    
    // Called when listening is aborted
    private void OnListeningAborted()
    {
        isListening = false;
        
        // Hide listening indicator
        if (listeningIndicator != null)
            listeningIndicator.SetActive(false);
            
        if (statusText != null)
            statusText.text = "Listening canceled";
        
        // Stop timeout coroutine
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
    }
    
    // Timeout for listening
    private IEnumerator ListeningTimeout()
    {
        float elapsed = 0f;
        
        while (elapsed < activationTimeout)
        {
            elapsed += Time.deltaTime;
            
            // Update progress bar
            if (listeningProgress != null)
                listeningProgress.fillAmount = elapsed / activationTimeout;
                
            yield return null;
        }
        
        // Timeout reached, abort listening
        if (isListening && wit != null)
        {
            wit.Deactivate();
            
            if (statusText != null)
                statusText.text = "I didn't hear anything. Please try again.";
                
            isListening = false;
            
            // Hide listening indicator
            if (listeningIndicator != null)
                listeningIndicator.SetActive(false);
        }
    }
    
    // For manual activation via button (useful for testing)
    public void ActivateVoiceRecognition()
    {
        Debug.Log("Attempting to activate voice recognition...");
        
        // Find Wit component if not assigned
        if (wit == null)
        {
            wit = FindObjectOfType<Wit>();
            Debug.Log("Trying to find Wit component: " + (wit != null ? "Found" : "Not found"));
        }
        
        if (wit != null)
        {
            if (!isListening)
            {
                Debug.Log("Activating Wit...");
                wit.Activate();
                
                // Update UI in case events don't fire
                if (statusText != null)
                    statusText.text = "Listening...";
                
                if (listeningIndicator != null)
                    listeningIndicator.SetActive(true);
                
                // Force a timeout in case callbacks don't work
                StartCoroutine(ForceListeningTimeout());
            }
            else
            {
                Debug.Log("Already listening, cannot activate again");
            }
        }
        else
        {
            Debug.LogError("Cannot activate voice - Wit component is null");
        }
    }
    
    // Force a timeout in case callbacks don't work
    private IEnumerator ForceListeningTimeout()
    {
        yield return new WaitForSeconds(15f);
        
        if (isListening)
        {
            Debug.Log("Force-stopping listening due to timeout");
            isListening = false;
            
            if (listeningIndicator != null)
                listeningIndicator.SetActive(false);
                
            if (statusText != null)
                statusText.text = "Listening timed out. Please try again.";
        }
    }
    
    // For voice calibration implementation
    public void CalibrateVoice()
    {
        if (statusText != null)
            statusText.text = "Please speak normally for 5 seconds...";
            
        StartCoroutine(PerformCalibration());
    }
    
    private List<float> calibrationSamples = new List<float>();
    private bool isCalibrating = false;
    
    private IEnumerator PerformCalibration()
    {
        // Reset calibration data
        calibrationSamples.Clear();
        isCalibrating = true;
        
        // Collect audio samples for 5 seconds
        float calibrationTime = 5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < calibrationTime)
        {
            elapsedTime += Time.deltaTime;
            
            // Update UI
            if (listeningProgress != null)
                listeningProgress.fillAmount = elapsedTime / calibrationTime;
                
            yield return null;
        }
        
        // Calculate threshold based on samples
        if (calibrationSamples.Count > 0)
        {
            // Sort samples and find the 70th percentile for a good threshold
            calibrationSamples.Sort();
            int percentileIndex = Mathf.FloorToInt(calibrationSamples.Count * 0.7f);
            voiceThreshold = calibrationSamples[percentileIndex];
            
            // Ensure minimum threshold and add small buffer
            voiceThreshold = Mathf.Max(voiceThreshold, 0.01f) * 1.2f;
        }
        else
        {
            // Fallback if no samples were collected
            voiceThreshold = 0.03f;
        }
        
        isCalibrated = true;
        isCalibrating = false;
        
        Debug.Log($"Voice calibration complete. Threshold set to: {voiceThreshold}");
        
        if (statusText != null)
            statusText.text = $"Calibration complete! Your voice threshold is set to {voiceThreshold:F3}";
    }
    
    // Audio processing for visualization and calibration
    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Calculate volume level from audio data
        float sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            sum += Mathf.Abs(data[i]);
        }
        
        float average = sum / data.Length;
        
        // If calibrating, add to samples
        if (isCalibrating)
        {
            calibrationSamples.Add(average);
        }
        
        // If listening, can use for visual feedback
        if (isListening && listeningProgress != null)
        {
            // Visualize current audio level compared to threshold
            float levelRatio = Mathf.Clamp01(average / voiceThreshold);
            
            // Update listening indicator (visual feedback)
            if (levelRatio > 0.1f && listeningProgress != null)
            {
                listeningProgress.fillAmount = Mathf.Lerp(listeningProgress.fillAmount, levelRatio, Time.deltaTime * 10f);
            }
            
            // Use calibration status to adjust sensitivity
            if (isCalibrated)
            {
                // If we're calibrated, we can be more precise about detecting speech
                if (levelRatio > 0.7f && statusText != null)
                {
                    statusText.text = "Hearing you clearly...";
                }
            }
        }
    }
}