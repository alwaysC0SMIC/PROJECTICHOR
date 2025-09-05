using Nova;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public class NovaHoverGuard : MonoBehaviour
{
    public static int ActiveHoverCount { get; private set; }
    public static bool IsBeingInteractedWith { get; private set; }
    
#if UNITY_EDITOR
    private static string lastClickTime = "Never";
#endif

    [SerializeField] UIBlock _block;

#if UNITY_EDITOR
    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📊 Active Hover Count")]
    private static int Debug_ActiveHoverCount => ActiveHoverCount;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🖥 Is Over Nova UI")]
    private static bool Debug_IsOverNovaUI => IsOverNovaUI;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎯 This Block")]
    private UIBlock Debug_ThisBlock => _block;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📍 Block Position")]
    [DisplayAsString]
    private string Debug_BlockInfo => _block != null ? 
        $"Active: {_block.gameObject.activeInHierarchy} | Enabled: {_block.enabled}" : "No Block";

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🖱 Last Click Reset")]
    [DisplayAsString]
    private static string Debug_LastClickTime => lastClickTime;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎯 Being Interacted With")]
    private static bool Debug_IsBeingInteractedWith => IsBeingInteractedWith;

    [BoxGroup("Debug Controls"), GUIColor(1f, 0.2f, 0.2f)]
    [Button(ButtonSizes.Large, Name = "Reset Hover Count", Icon = SdfIconType.XCircle)]
    private static void Debug_ResetHoverCount()
    {
        ActiveHoverCount = 0;
    }

    [BoxGroup("Debug Controls"), GUIColor(0.2f, 0.7f, 1f)]
    [Button(ButtonSizes.Medium, Name = "Force Hover", Icon = SdfIconType.Plus)]
    private static void Debug_ForceHover()
    {
        ActiveHoverCount++;
    }

    [BoxGroup("Debug Controls"), GUIColor(1f, 0.5f, 0.2f)]
    [Button(ButtonSizes.Medium, Name = "Force Unhover", Icon = SdfIconType.Dash)]
    private static void Debug_ForceUnhover()
    {
        ActiveHoverCount = Mathf.Max(0, ActiveHoverCount - 1);
    }

    [BoxGroup("Debug Controls"), GUIColor(0.8f, 0.3f, 1f)]
    [Button(ButtonSizes.Medium, Name = "Simulate Click", Icon = SdfIconType.Mouse)]
    private static void Debug_SimulateClick()
    {
        ActiveHoverCount = 0;
        lastClickTime = System.DateTime.Now.ToString("HH:mm:ss");
    }

    [BoxGroup("Debug Controls"), GUIColor(0.8f, 0.3f, 1f)]
    [Button(ButtonSizes.Medium, Name = "Reset Interaction", Icon = SdfIconType.XSquare)]
    private static void Debug_ResetInteraction()
    {
        IsBeingInteractedWith = false;
    }
#endif

    private void Awake()
    {
        if (_block == null)
            _block = GetComponent<UIBlock>();
        if (_block == null) return;

        _block.AddGestureHandler<Gesture.OnHover>(_ => {
            ActiveHoverCount++;
        });
        
        _block.AddGestureHandler<Gesture.OnUnhover>(_ => {
            ActiveHoverCount = Mathf.Max(0, ActiveHoverCount - 1);
        });

        // Add press/drag handlers to track active interaction
        _block.AddGestureHandler<Gesture.OnPress>(_ => {
            IsBeingInteractedWith = true;
        });

        _block.AddGestureHandler<Gesture.OnRelease>(_ => {
            IsBeingInteractedWith = false;
        });

        _block.AddGestureHandler<Gesture.OnDrag>(_ => {
            IsBeingInteractedWith = true;
        });

        // Add click handler to prevent stuck hover states
        _block.AddGestureHandler<Gesture.OnClick>(_ => {
#if UNITY_EDITOR
            lastClickTime = System.DateTime.Now.ToString("HH:mm:ss");
#endif
            // Reset hover count when clicked to prevent stuck states
            ActiveHoverCount = 0;
            IsBeingInteractedWith = false;
        });
    }

    private void Update()
    {
        // Safety check: if mouse is up and we're still being interacted with, reset
        if (IsBeingInteractedWith && !Input.GetMouseButton(0))
        {
            IsBeingInteractedWith = false;
        }
    }

    public static bool IsOverNovaUI => ActiveHoverCount > 0 || IsBeingInteractedWith;
}
