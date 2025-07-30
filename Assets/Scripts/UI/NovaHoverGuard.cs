using Nova;
using UnityEngine;

public class NovaHoverGuard : MonoBehaviour
{
    public static int ActiveHoverCount { get; private set; }

    [SerializeField] UIBlock _block;

    private void Awake()
    {
        _block = GetComponent<UIBlock>();
        if (_block == null) return;

        _block.AddGestureHandler<Gesture.OnHover>(_ => ActiveHoverCount++);
        _block.AddGestureHandler<Gesture.OnUnhover>(_ => ActiveHoverCount = Mathf.Max(0, ActiveHoverCount - 1));
    }

    public static bool IsOverNovaUI => ActiveHoverCount > 0;
}
