using UnityEngine;

/// <summary>
/// Example script showing how to use GameTime instead of Unity's Time class
/// This allows the movement to be affected by GameTime's custom time scale
/// </summary>
public class GameTimeExample : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotateSpeed = 90.0f;
    
    [Header("Time Mode")]
    [SerializeField] private bool useGameTime = true;
    [SerializeField] private bool showTimeInfo = false;
    
    private Vector3 originalPosition;
    
    void Start()
    {
        originalPosition = transform.position;
    }
    
    void Update()
    {
        // Choose which time system to use
        float deltaTime = useGameTime ? GameTime.DeltaTime : Time.deltaTime;
        
        // Show time information if enabled
        if (showTimeInfo && Time.frameCount % 60 == 0) // Every 60 frames to avoid spam
        {
            Debug.Log($"[GameTimeExample] GameTime.DeltaTime: {GameTime.DeltaTime:F4}, " +
                     $"Time.deltaTime: {Time.deltaTime:F4}, " +
                     $"Time Scale: {GameTime.TimeScale:F2}, " +
                     $"Paused: {GameTime.IsPaused}");
        }
        
        // Example movement using chosen time system
        MoveExample(deltaTime);
        RotateExample(deltaTime);
    }
    
    private void MoveExample(float deltaTime)
    {
        // Simple back-and-forth movement
        float movement = Mathf.Sin(GameTime.TotalTime * 2.0f) * moveSpeed * deltaTime;
        transform.position = originalPosition + Vector3.right * movement;
    }
    
    private void RotateExample(float deltaTime)
    {
        // Continuous rotation
        transform.Rotate(Vector3.up, rotateSpeed * deltaTime);
    }
    
    /// <summary>
    /// Toggle between GameTime and regular Time
    /// </summary>
    public void ToggleTimeMode()
    {
        useGameTime = !useGameTime;
        Debug.Log($"[GameTimeExample] Now using: {(useGameTime ? "GameTime" : "Unity Time")}");
    }
    
    /// <summary>
    /// Example of using GameTime for delayed actions
    /// </summary>
    public void DelayedAction(float delay)
    {
        StartCoroutine(DelayedActionCoroutine(delay));
    }
    
    private System.Collections.IEnumerator DelayedActionCoroutine(float delay)
    {
        Debug.Log($"[GameTimeExample] Starting delayed action with {delay} second delay");
        
        float elapsed = 0f;
        while (elapsed < delay)
        {
            // Use GameTime for the delay so it respects time scale and pause
            elapsed += GameTime.DeltaTime;
            yield return null;
        }
        
        Debug.Log("[GameTimeExample] Delayed action executed!");
    }
    
    /// <summary>
    /// Example of creating a timer that respects GameTime
    /// </summary>
    public void StartGameTimeTimer(float duration)
    {
        StartCoroutine(GameTimeTimerCoroutine(duration));
    }
    
    private System.Collections.IEnumerator GameTimeTimerCoroutine(float duration)
    {
        float elapsed = 0f;
        Debug.Log($"[GameTimeExample] Timer started for {duration} seconds");
        
        while (elapsed < duration)
        {
            elapsed += GameTime.DeltaTime;
            
            // This will pause when GameTime is paused and speed up/slow down with time scale
            float progress = elapsed / duration;
            
            // Update something based on progress...
            if (showTimeInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[GameTimeExample] Timer progress: {progress:P1}");
            }
            
            yield return null;
        }
        
        Debug.Log("[GameTimeExample] Timer completed!");
    }
}
