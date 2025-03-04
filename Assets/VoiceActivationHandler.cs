// File: VoiceActivationHandler.cs

using System.Collections;
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
        // Register for Wit.ai events
        if (wit != null)
        {
            wit.VoiceEvents.OnSend.AddListener((request) => OnListeningStart(request));
            wit.VoiceEvents.OnResponse.AddListener(OnListeningComplete);
            wit.VoiceEvents.OnError.AddListener(OnListeningError);
            wit.VoiceEvents.OnAborted.AddListener(OnListeningAborted);
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
        if (wit != null && !isListening)
        {
            wit.Activate();
        }
    }
    
    // For voice calibration (simple implementation)
    public void CalibrateVoice()
    {
        if (statusText != null)
            statusText.text = "Please speak normally for 5 seconds...";
            
        StartCoroutine(PerformCalibration());
    }
    
    private IEnumerator PerformCalibration()
    {
        // In a real implementation, you would:
        // 1. Record audio levels for a few seconds
        // 2. Analyze to find comfortable threshold
        // 3. Set parameters for Wit or your voice detection
        
        // Simulated calibration for MVP
        yield return new WaitForSeconds(5f);
        
        // Set a simulated threshold (in a real implementation, this would be calculated)
        voiceThreshold = 0.03f;
        isCalibrated = true;
        
        if (statusText != null)
            statusText.text = "Calibration complete! Your voice threshold is set.";
    }
    
    // Audio visualization (if needed)
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isListening) return;
        
        // Calculate volume level for visualization
        float sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            sum += Mathf.Abs(data[i]);
        }
        
        float average = sum / data.Length;
        
        // You could use this to visualize audio levels
        // or for additional feedback during calibration
    }
}