using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameTime : MonoBehaviour
{
    [TabGroup("Settings", "⚙️ Settings")]
    [TitleGroup("Settings/Time Control")]
    [Header("Time Settings")]
    [SerializeField] private float gameTimeScale = 1.0f;
    [SerializeField] private bool isPaused = false;
    
    [TabGroup("Settings", "⚙️ Settings")]
    [TitleGroup("Settings/Debug")]
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    #if UNITY_EDITOR
    [TabGroup("Settings", "⚙️ Settings")]
    [TitleGroup("Settings/Editor")]
    [Header("Editor Controls")]
    [SerializeField] private bool showEditorControls = false;
    #endif
    
    // Static instance for global access
    public static GameTime Instance { get; private set; }
    
    // Time values
    private float currentGameTime = 0f;
    private float lastFrameGameTime = 0f;
    private float gameDeltaTime = 0f;
    
    // Properties for easy access (similar to Unity's Time class)
    public static float DeltaTime => Instance != null ? Instance.gameDeltaTime : Time.deltaTime;
    public static float TimeScale => Instance != null ? Instance.gameTimeScale : 1.0f;
    public static float TotalTime => Instance != null ? Instance.currentGameTime : Time.time;
    public static bool IsPaused => Instance != null ? Instance.isPaused : false;
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Time Scale Controls")]
    [ButtonGroup("Debug/Time Scale Controls/Quick Scale")]
    [Button("0.1x", ButtonSizes.Medium), GUIColor(0.8f, 0.4f, 0.4f)]
    private void SetTimeScale01() => SetTimeScale(0.1f);
    
    [ButtonGroup("Debug/Time Scale Controls/Quick Scale")]
    [Button("0.25x", ButtonSizes.Medium), GUIColor(0.8f, 0.6f, 0.4f)]
    private void SetTimeScale025() => SetTimeScale(0.25f);
    
    [ButtonGroup("Debug/Time Scale Controls/Quick Scale")]
    [Button("0.5x", ButtonSizes.Medium), GUIColor(0.8f, 0.8f, 0.4f)]
    private void SetTimeScale05() => SetTimeScale(0.5f);
    
    [ButtonGroup("Debug/Time Scale Controls/Quick Scale")]
    [Button("1x", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    private void SetTimeScale1() => SetTimeScale(1.0f);
    
    [ButtonGroup("Debug/Time Scale Controls/Quick Scale")]
    [Button("2x", ButtonSizes.Medium), GUIColor(0.4f, 0.6f, 0.8f)]
    private void SetTimeScale2() => SetTimeScale(2.0f);
    
    [ButtonGroup("Debug/Time Scale Controls/Quick Scale")]
    [Button("3x", ButtonSizes.Medium), GUIColor(0.6f, 0.4f, 0.8f)]
    private void SetTimeScale3() => SetTimeScale(3.0f);
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Pause Controls")]
    [Button(ButtonSizes.Large, Name = "@GetPauseButtonText()")]
    [GUIColor("@GetPauseButtonColor()")]
    private void TogglePauseButton() => TogglePause();
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Special Effects")]
    [ButtonGroup("Debug/Special Effects/Effects")]
    [Button("Slow Motion", ButtonSizes.Medium), GUIColor(0.7f, 0.5f, 0.9f)]
    private void TriggerSlowMotion() => SlowMotion(0.3f, 2.0f);
    
    [ButtonGroup("Debug/Special Effects/Effects")]
    [Button("Bullet Time", ButtonSizes.Medium), GUIColor(0.9f, 0.3f, 0.3f)]
    private void TriggerBulletTime() => BulletTime(0.1f, 1.0f);
    
    [ButtonGroup("Debug/Special Effects/Effects")]
    [Button("Reset Scale", ButtonSizes.Medium), GUIColor(0.5f, 0.8f, 0.5f)]
    private void ResetScaleButton() => ResetTimeScale();
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Custom Controls")]
    [HorizontalGroup("Debug/Custom Controls/Custom Scale")]
    [LabelWidth(80)]
    [SerializeField] private float customTimeScale = 1.0f;
    
    [HorizontalGroup("Debug/Custom Controls/Custom Scale")]
    [Button("Apply Custom Scale", ButtonSizes.Medium), GUIColor(0.4f, 0.7f, 0.9f)]
    private void ApplyCustomTimeScale() => SetTimeScale(customTimeScale);
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Transition Controls")]
    [HorizontalGroup("Debug/Transition Controls/Transition")]
    [LabelWidth(100)]
    [SerializeField] private float transitionTarget = 0.5f;
    
    [HorizontalGroup("Debug/Transition Controls/Transition")]
    [LabelWidth(80)]
    [SerializeField] private float transitionDuration = 1.0f;
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Transition Controls")]
    [Button("Start Smooth Transition", ButtonSizes.Large), GUIColor(0.6f, 0.8f, 0.6f)]
    private void StartTransition() => TransitionTimeScale(transitionTarget, transitionDuration);
    
    [TabGroup("Debug", "🔧 Debug Controls")]
    [TitleGroup("Debug/Runtime Info")]
    [ShowInInspector, ReadOnly, LabelText("Current Delta Time")]
    private float RuntimeDeltaTime => DeltaTime;
    
    [ShowInInspector, ReadOnly, LabelText("Current Time Scale")]
    private float RuntimeTimeScale => TimeScale;
    
    [ShowInInspector, ReadOnly, LabelText("Total Game Time")]
    private float RuntimeTotalTime => TotalTime;
    
    [ShowInInspector, ReadOnly, LabelText("Is Paused")]
    private bool RuntimeIsPaused => IsPaused;
    
    [ShowInInspector, ReadOnly, LabelText("Is Effectively Stopped")]
    private bool RuntimeIsEffectivelyStopped => IsEffectivelyStopped();
    
    // Helper methods for Odin buttons
    private string GetPauseButtonText()
    {
        return IsPaused ? "▶️ Resume Game Time" : "⏸️ Pause Game Time";
    }
    
    private Color GetPauseButtonColor()
    {
        return IsPaused ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
    }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Update()
    {
        UpdateGameTime();
    }
    
    private void UpdateGameTime()
    {
        if (isPaused)
        {
            gameDeltaTime = 0f;
            return;
        }
        
        // Calculate game delta time based on real delta time and game time scale
        gameDeltaTime = Time.deltaTime * gameTimeScale;
        
        // Update total game time
        lastFrameGameTime = currentGameTime;
        currentGameTime += gameDeltaTime;
        
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            Debug.Log($"[GameTime] Scale: {gameTimeScale:F2}, DeltaTime: {gameDeltaTime:F4}, TotalTime: {currentGameTime:F2}");
        }
    }
    
    #region Public Control Methods
    
    /// <summary>
    /// Set the game time scale (1.0 = normal speed, 0.5 = half speed, 2.0 = double speed)
    /// </summary>
    public static void SetTimeScale(float scale)
    {
        if (Instance != null)
        {
            Instance.gameTimeScale = Mathf.Max(0f, scale); // Prevent negative time scale
            
            if (Instance.showDebugInfo)
            {
                Debug.Log($"[GameTime] Time scale set to: {scale}");
            }
        }
    }
    
    /// <summary>
    /// Pause the game time
    /// </summary>
    public static void Pause()
    {
        if (Instance != null)
        {
            Instance.isPaused = true;
            
            if (Instance.showDebugInfo)
            {
                Debug.Log("[GameTime] Game time paused");
            }
        }
    }
    
    /// <summary>
    /// Resume the game time
    /// </summary>
    public static void Resume()
    {
        if (Instance != null)
        {
            Instance.isPaused = false;
            
            if (Instance.showDebugInfo)
            {
                Debug.Log("[GameTime] Game time resumed");
            }
        }
    }
    
    /// <summary>
    /// Toggle pause state
    /// </summary>
    public static void TogglePause()
    {
        if (Instance != null)
        {
            if (Instance.isPaused)
                Resume();
            else
                Pause();
        }
    }
    
    /// <summary>
    /// Gradually change time scale over duration (useful for smooth transitions)
    /// </summary>
    public static void TransitionTimeScale(float targetScale, float duration)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.TransitionTimeScaleCoroutine(targetScale, duration));
        }
    }
    
    /// <summary>
    /// Apply slow motion for a specific duration
    /// </summary>
    public static void SlowMotion(float slowScale = 0.3f, float duration = 2.0f)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.TemporaryTimeScaleCoroutine(slowScale, duration));
        }
    }
    
    /// <summary>
    /// Apply bullet time effect
    /// </summary>
    public static void BulletTime(float bulletTimeScale = 0.1f, float duration = 1.0f)
    {
        SlowMotion(bulletTimeScale, duration);
    }
    
    /// <summary>
    /// Reset time scale to normal (1.0)
    /// </summary>
    public static void ResetTimeScale()
    {
        SetTimeScale(1.0f);
    }
    
    #endregion
    
    #region Coroutines
    
    private System.Collections.IEnumerator TransitionTimeScaleCoroutine(float targetScale, float duration)
    {
        float startScale = gameTimeScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; // Use real time for transition
            float t = elapsed / duration;
            gameTimeScale = Mathf.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        gameTimeScale = targetScale;
        
        if (showDebugInfo)
        {
            Debug.Log($"[GameTime] Transition complete. Time scale: {targetScale}");
        }
    }
    
    private System.Collections.IEnumerator TemporaryTimeScaleCoroutine(float tempScale, float duration)
    {
        float originalScale = gameTimeScale;
        gameTimeScale = tempScale;
        
        if (showDebugInfo)
        {
            Debug.Log($"[GameTime] Temporary time scale: {tempScale} for {duration} seconds");
        }
        
        // Wait for the duration using real time
        yield return new WaitForSecondsRealtime(duration);
        
        gameTimeScale = originalScale;
        
        if (showDebugInfo)
        {
            Debug.Log($"[GameTime] Restored time scale to: {originalScale}");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Get game time equivalent of a real-world duration
    /// </summary>
    public static float GetGameTimeDuration(float realDuration)
    {
        if (Instance != null && Instance.gameTimeScale > 0)
        {
            return realDuration / Instance.gameTimeScale;
        }
        return realDuration;
    }
    
    /// <summary>
    /// Get real time equivalent of a game time duration
    /// </summary>
    public static float GetRealTimeDuration(float gameTimeDuration)
    {
        if (Instance != null)
        {
            return gameTimeDuration * Instance.gameTimeScale;
        }
        return gameTimeDuration;
    }
    
    /// <summary>
    /// Check if time is effectively stopped (paused or very slow)
    /// </summary>
    public static bool IsEffectivelyStopped()
    {
        return IsPaused || (Instance != null && Instance.gameTimeScale < 0.01f);
    }
    
    /// <summary>
    /// Get interpolation value for smooth movement using game time
    /// </summary>
    public static float GetLerpSpeed(float speed)
    {
        return speed * DeltaTime;
    }
    
    #endregion
    
    #region Unity Editor Integration
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Clamp time scale in editor
        gameTimeScale = Mathf.Max(0f, gameTimeScale);
    }
    
    void OnGUI()
    {
        if (!showEditorControls || !Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Game Time Controls");
        
        GUILayout.Label($"Time Scale: {gameTimeScale:F2}");
        gameTimeScale = GUILayout.HorizontalSlider(gameTimeScale, 0f, 3f);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.1x")) SetTimeScale(0.1f);
        if (GUILayout.Button("0.5x")) SetTimeScale(0.5f);
        if (GUILayout.Button("1x")) SetTimeScale(1.0f);
        if (GUILayout.Button("2x")) SetTimeScale(2.0f);
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button(isPaused ? "Resume" : "Pause"))
        {
            TogglePause();
        }
        
        if (GUILayout.Button("Slow Motion"))
        {
            SlowMotion(0.3f, 2.0f);
        }
        
        if (GUILayout.Button("Bullet Time"))
        {
            BulletTime(0.1f, 1.0f);
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endif
    
    #endregion
}
