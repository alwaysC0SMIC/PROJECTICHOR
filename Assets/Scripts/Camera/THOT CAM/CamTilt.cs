using UnityEngine;

public class CamTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    public float maxXTilt = 10f; // Max tilt in degrees for X axis (up/down)
    public float maxYTilt = 10f; // Max tilt in degrees for Y axis (left/right)
    public float tiltSpeed = 5f; // How quickly the tilt responds
    public float recenterSpeed = 2f; // How quickly the camera returns to original rotation

    private Vector3 lastPosition;
    private Quaternion originalRotation;
    private Vector2 velocity;

    void Start()
    {
        originalRotation = transform.localRotation;
        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate velocity in world space
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        // Convert world delta to local space (local velocity)
        Vector3 localDelta = transform.InverseTransformDirection(delta);

        // Store velocity for smoothing (x = local X, y = local Y)
        velocity = Vector2.Lerp(velocity, new Vector2(localDelta.x, localDelta.y), Time.deltaTime * tiltSpeed);

        // Calculate target tilt angles
        float tiltX = Mathf.Clamp(velocity.y * maxXTilt * 2.5f, -maxXTilt * 2.5f, maxXTilt * 2.5f); // Moving on local Y tilts X
        float tiltY = Mathf.Clamp(velocity.x * maxYTilt, -maxYTilt, maxYTilt);  // Moving on local X tilts Y

        // Create target rotation
        Quaternion targetTilt = originalRotation * Quaternion.Euler(tiltX, tiltY, 0);

        // Smoothly interpolate to target tilt, then recenter when not moving
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetTilt, Time.deltaTime * tiltSpeed);

        // If velocity is very small, recenter to original rotation
        if (velocity.magnitude < 0.01f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, Time.deltaTime * recenterSpeed);
        }
    }
}
