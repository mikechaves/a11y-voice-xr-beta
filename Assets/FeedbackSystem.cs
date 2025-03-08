// File: FeedbackSystem.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FeedbackSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Image feedbackBackground;
    [SerializeField] private float displayDuration = 3f;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] positiveClips;
    [SerializeField] private AudioClip[] negativeClips;
    [SerializeField] private AudioClip[] neutralClips;
    
    // Simple TTS settings
    [Header("Text-to-Speech")]
    [SerializeField] private bool useTTS = true;
    [SerializeField] private float speechRate = 1.0f;
    
    // Color themes for different feedback types
    [Header("Color Themes")]
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color neutralColor = new Color(0.2f, 0.2f, 0.8f, 0.8f);
    
    private Coroutine activeDisplayCoroutine;
    
    // Store recent feedback messages to avoid repetition
    private Queue<string> recentMessages = new Queue<string>();
    private int maxRecentMessages = 5;
    
    // Alternative phrases for common feedback to add variety
    private Dictionary<string, List<string>> alternativePhrases = new Dictionary<string, List<string>>()
    {
        {
            "error_not_understood", new List<string>
            {
                "I'm sorry, I didn't catch that. Could you try again?",
                "I didn't quite understand. Could you please repeat that?",
                "I missed what you said. One more time, please?",
                "I'm having trouble understanding. Could you speak a bit more clearly?"
            }
        },
        {
            "success_command", new List<string>
            {
                "Got it!",
                "I understand.",
                "Command recognized.",
                "Processing your request."
            }
        },
        {
            "prompt_next", new List<string>
            {
                "What would you like to do next?",
                "Ready for your next command.",
                "Please tell me what you'd like to do next.",
                "I'm ready for your next instruction."
            }
        }
    };
    
    void Start()
    {
        // Initialize the feedback panel as hidden
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }
    
    // Show feedback with automatically determined type
    public void ShowFeedback(string message)
    {
        // Default to neutral feedback
        FeedbackType type = FeedbackType.Neutral;
        
        // Determine type based on message content
        if (message.Contains("sorry") || message.Contains("didn't") || 
            message.Contains("error") || message.Contains("try again"))
        {
            type = FeedbackType.Error;
        }
        else if (message.Contains("great") || message.Contains("good job") || 
                message.Contains("complete") || message.Contains("success"))
        {
            type = FeedbackType.Success;
        }
        
        ShowFeedback(message, type);
    }
    
    // Show feedback with specified type
    public void ShowFeedback(string message, FeedbackType type)
    {
        // Check if this is a key for alternative phrases
        if (alternativePhrases.ContainsKey(message))
        {
            List<string> alternatives = alternativePhrases[message];
            int index = UnityEngine.Random.Range(0, alternatives.Count);
            message = alternatives[index];
        }
        
        // Avoid repetitive messages
        if (recentMessages.Contains(message))
        {
            // Add slight variation to repeated messages
            if (message.EndsWith("?") || message.EndsWith("."))
                message = message.Substring(0, message.Length - 1) + "...";
            else
                message += "...";
        }
        
        // Store message in recent messages queue
        recentMessages.Enqueue(message);
        if (recentMessages.Count > maxRecentMessages)
            recentMessages.Dequeue();
            
        // Update UI
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = message;
            
            // Set background color based on type
            if (feedbackBackground != null)
            {
                switch (type)
                {
                    case FeedbackType.Success:
                        feedbackBackground.color = successColor;
                        break;
                    case FeedbackType.Error:
                        feedbackBackground.color = errorColor;
                        break;
                    case FeedbackType.Neutral:
                    default:
                        feedbackBackground.color = neutralColor;
                        break;
                }
            }
        }
        
        // Play appropriate audio clip
        if (audioSource != null)
        {
            AudioClip[] clipsToUse = null;
            
            switch (type)
            {
                case FeedbackType.Success:
                    clipsToUse = positiveClips;
                    break;
                case FeedbackType.Error:
                    clipsToUse = negativeClips;
                    break;
                case FeedbackType.Neutral:
                default:
                    clipsToUse = neutralClips;
                    break;
            }
            
            if (clipsToUse != null && clipsToUse.Length > 0)
            {
                AudioClip clip = clipsToUse[UnityEngine.Random.Range(0, clipsToUse.Length)];
                if (clip != null)
                    audioSource.PlayOneShot(clip);
            }
        }
        
        // Speak the message if TTS is enabled
        if (useTTS)
        {
            SpeakMessage(message);
        }
        
        // Schedule hiding the feedback
        if (activeDisplayCoroutine != null)
            StopCoroutine(activeDisplayCoroutine);
            
        activeDisplayCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }
    
    // Hide feedback after a delay
    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        
        if (feedbackPanel != null)
        {
            // You could add a fade-out animation here
            feedbackPanel.SetActive(false);
        }
        
        activeDisplayCoroutine = null;
    }
    
    // Placeholder for TTS implementation
    private void SpeakMessage(string message)
    {
        if (!useTTS)
            return;
            
        // Log that we would speak the message with the current speech rate
        Debug.Log($"TTS would say (at rate {speechRate}): {message}");
        
        // In a real implementation, this would be connected to Meta TTS or another TTS system
        // The speech rate would control how fast the message is spoken
        float messageDuration = message.Length * 0.05f / speechRate; // Rough estimate of duration
        
        // In a complete implementation, we would:
        // 1. Configure the TTS service with the speech rate
        // 2. Convert the text to speech
        // 3. Play the speech audio
        
        // For now, just simulate the "speaking" duration
        StartCoroutine(SimulateSpeaking(messageDuration));
    }
    
    // Simulate the speaking duration for feedback
    private IEnumerator SimulateSpeaking(float duration)
    {
        // Simulate the speaking time
        yield return new WaitForSeconds(duration);
        
        // Optionally log when speech is "complete"
        Debug.Log("TTS speech complete");
    }
    
    // Feedback types
    public enum FeedbackType
    {
        Success,
        Error,
        Neutral
    }
}