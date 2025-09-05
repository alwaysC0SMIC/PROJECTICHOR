using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public class GameplayCameraManager : MonoBehaviour
{
    [Header("Gameplay Cameras")]
    [SerializeField] private CinemachineCamera gameplayVirtualCam;
    [SerializeField] private CinemachineCamera cinematicVirtualCam; // Renamed from buildVirtualCam
    [SerializeField] private PanAndZoom gameplayCam;
    [SerializeField] private Interactor interactor;

#if UNITY_EDITOR
    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎮 Gameplay Cam Enabled")]
    private bool Debug_GameplayCamEnabled => gameplayCam != null ? gameplayCam.enabled : false;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎯 Allow Input")]
    private bool Debug_AllowInput => gameplayCam != null ? gameplayCam.allowInput : false;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🖱 Interactor Enabled")]
    private bool Debug_InteractorEnabled => interactor != null ? interactor.enabled : false;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📹 Gameplay Cam Priority")]
    private int Debug_GameplayCamPriority => gameplayVirtualCam != null ? gameplayVirtualCam.Priority : -1;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎬 Cinematic Cam Priority")]
    private int Debug_CinematicCamPriority => cinematicVirtualCam != null ? cinematicVirtualCam.Priority : -1;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🖥 Over Nova UI")]
    private bool Debug_OverNovaUI => NovaHoverGuard.IsOverNovaUI;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📊 Camera State")]
    [DisplayAsString]
    private string Debug_CameraState => 
        $"Gameplay: {(gameplayCam?.enabled ?? false)} | " +
        $"Input: {(gameplayCam?.allowInput ?? false)} | " +
        $"Interactor: {(interactor?.enabled ?? false)} | " +
        $"UI Hover: {NovaHoverGuard.IsOverNovaUI}";

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📋 Last UI Page Event")]
    [DisplayAsString]
    private string Debug_LastUIPageEvent { get; set; } = "None";

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🔗 Event Bindings")]
    [DisplayAsString]
    private string Debug_EventBindings => 
        $"Toggle: {(toggleGameplayCamEventBinding != null ? "✓" : "✗")} | " +
        $"Reset: {(resetGameplayCamEventBinding != null ? "✓" : "✗")} | " +
        $"UIPage: {(updateUIPageEventBinding != null ? "✓" : "✗")}";
#endif

#if UNITY_EDITOR
    [BoxGroup("Debug Controls"), GUIColor(0.2f, 0.7f, 1f)]
    [Button(ButtonSizes.Large, Name = "Reset Camera", Icon = SdfIconType.Camera)]
    private void Debug_ResetCamera()
    {
        ResetCamera();
       // Debug.Log("Camera reset via debug button.");
    }

    [BoxGroup("Debug Controls"), GUIColor(0.7f, 1f, 0.2f)]
    [Button(ButtonSizes.Large, Name = "Enable Gameplay Cam", Icon = SdfIconType.Eye)]
    private void Debug_EnableGameplayCam()
    {
        gameplayCam.enabled = true;
        gameplayVirtualCam.Priority = 10;
        if (cinematicVirtualCam != null)
            cinematicVirtualCam.Priority = 0;
        interactor.enabled = true;
        //Debug.Log("Gameplay camera enabled via debug button.");
    }

    [BoxGroup("Debug Controls"), GUIColor(1f, 0.5f, 0.2f)]
    [Button(ButtonSizes.Large, Name = "Disable Gameplay Cam", Icon = SdfIconType.Lock)]
    private void Debug_DisableGameplayCam()
    {
        gameplayCam.enabled = false;
        gameplayVirtualCam.Priority = 0;
        if (cinematicVirtualCam != null)
            cinematicVirtualCam.Priority = 0;
        interactor.enabled = false;
    }

    [BoxGroup("Debug Controls"), GUIColor(0.8f, 0.3f, 0.8f)]
    [Button(ButtonSizes.Medium, Name = "Refresh Debug Info", Icon = SdfIconType.ArrowClockwise)]
    private void Debug_RefreshInfo()
    {
        // This button forces Odin to refresh the debug info display
        // The actual values are shown in the Debug Info section above
    }
#endif

    private EventBinding<ToggleGameplayCamEvent> toggleGameplayCamEventBinding;
    private EventBinding<ResetCameraEvent> resetGameplayCamEventBinding;
    private EventBinding<UpdateUIPageEvent> updateUIPageEventBinding;

    private void Start()
    {
        gameplayVirtualCam.Priority = 10;
        if (cinematicVirtualCam != null)
            cinematicVirtualCam.Priority = 0; // Cinematic cam is not used for now

        // Delay initial state event to ensure all bindings are registered
        StartCoroutine(DelayedInitialState());
    }

    private System.Collections.IEnumerator DelayedInitialState()
    {
        yield return null; // Wait one frame
        //Debug.Log("[GameplayCameraManager] Raising initial gameplay state event");
        EventBus<UpdateUIPageEvent>.Raise(new UpdateUIPageEvent { uiPage = Enum_UIMenuPage.Gameplay });
    }

    #region ASSIGNING/UNASSIGNING EVENTS

    private void OnEnable()
    {
        toggleGameplayCamEventBinding = new EventBinding<ToggleGameplayCamEvent>(ToggleCamera);
        EventBus<ToggleGameplayCamEvent>.Register(toggleGameplayCamEventBinding);

        resetGameplayCamEventBinding = new EventBinding<ResetCameraEvent>(ResetCamera);
        EventBus<ResetCameraEvent>.Register(resetGameplayCamEventBinding);

        updateUIPageEventBinding = new EventBinding<UpdateUIPageEvent>(HandleUIPageEvent);
        EventBus<UpdateUIPageEvent>.Register(updateUIPageEventBinding);

        //Debug.Log("[GameplayCameraManager] Event bindings registered in OnEnable");
    }

    private void OnDisable()
    {
        EventBus<ToggleGameplayCamEvent>.Deregister(toggleGameplayCamEventBinding);
        EventBus<ResetCameraEvent>.Deregister(resetGameplayCamEventBinding);
        EventBus<UpdateUIPageEvent>.Deregister(updateUIPageEventBinding);
    }

    #endregion

    private void ToggleCamera(ToggleGameplayCamEvent evt)
    {
        gameplayCam.enabled = evt.allowCam;
        interactor.enabled = evt.allowCam;
    }

    private void ResetCamera()
    {
        gameplayCam.ResetCam();
    }

    private void HandleUIPageEvent(UpdateUIPageEvent evt)
    {
#if UNITY_EDITOR
        Debug_LastUIPageEvent = $"{evt.uiPage} at {System.DateTime.Now:HH:mm:ss}";
#endif
        
        if (evt.uiPage == Enum_UIMenuPage.Gameplay)
        {
            //Debug.Log($"[GameplayCameraManager] Enabling gameplay mode - gameplayCam.enabled before: {gameplayCam.enabled}");
            
            gameplayVirtualCam.Priority = 10;
            if (cinematicVirtualCam != null)
                cinematicVirtualCam.Priority = 0;
            gameplayCam.enabled = true;
            interactor.enabled = true;
            
            //Debug.Log($"[GameplayCameraManager] Gameplay mode enabled - gameplayCam.enabled after: {gameplayCam.enabled}, allowInput: {gameplayCam.allowInput}");
        }
        else if (evt.uiPage == Enum_UIMenuPage.Build)
        {
            //Debug.Log($"[GameplayCameraManager] Switching to Build mode - disabling gameplayCam");
            
            gameplayVirtualCam.Priority = 0;
            if (cinematicVirtualCam != null)
                cinematicVirtualCam.Priority = 0; // Cinematic cam is not used for now

            gameplayCam.enabled = false;
            
            interactor.enabled = true;
        }
        else
        {
            //Debug.Log($"[GameplayCameraManager] Switching to other mode: {evt.uiPage} - disabling gameplayCam");
            
            gameplayCam.enabled = false;
            
            interactor.enabled = false;
        }
    }
}
