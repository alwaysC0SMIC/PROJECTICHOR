using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public class GameplayCameraManager : MonoBehaviour
{
    [BoxGroup("Gameplay Cameras")]
    [SerializeField] private CinemachineCamera gameplayVirtualCam;
    [SerializeField] private CinemachineCamera cinematicVirtualCam; // Renamed from buildVirtualCam
    [SerializeField] private PanAndZoom gameplayCam;
    [SerializeField] private Interactor interactor;

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
        //Debug.Log("Gameplay camera disabled via debug button.");
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
        if (evt.uiPage == Enum_UIMenuPage.Gameplay)
        {
            gameplayCam.enabled = true;
            gameplayVirtualCam.Priority = 10;
            if (cinematicVirtualCam != null)
                cinematicVirtualCam.Priority = 0;

            interactor.enabled = true;
        }
        else if (evt.uiPage == Enum_UIMenuPage.Build)
        {
            gameplayVirtualCam.Priority = 0;
            if (cinematicVirtualCam != null)
                cinematicVirtualCam.Priority = 0; // Cinematic cam is not used for now

            gameplayCam.enabled = false;
            
            interactor.enabled = true;
        }
        else
        {
            gameplayCam.enabled = false;
            
            interactor.enabled = false;
        }
    }
}
