using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Rotate and zoom an object with mouse input at runtime.
/// Attach this to the target object.
/// </summary>
public class MouseObjectTransformController : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Degrees per pixel while dragging with left mouse button.")]
    public float rotationSpeed = 0.2f;

    [Header("Zoom (Scale)")]
    [Tooltip("Scroll sensitivity for scaling.")]
    public float zoomSpeed = 0.15f;

    [Tooltip("Minimum uniform scale.")]
    public float minScale = 0.25f;

    [Tooltip("Maximum uniform scale.")]
    public float maxScale = 3f;

    private Vector3 _lastPointerPosition;

    private void Update()
    {
        HandleRotation();
        HandleZoom();
    }

    private void HandleRotation()
    {
        if (IsLeftMousePressed())
        {
            Vector2 delta = GetPointerDelta();

            // Horizontal drag: yaw around world Y axis
            transform.Rotate(Vector3.up, delta.x * rotationSpeed, Space.World);
            // Vertical drag: pitch around local X axis
            transform.Rotate(Vector3.right, -delta.y * rotationSpeed, Space.Self);
        }

        _lastPointerPosition = GetPointerPosition();
    }

    private void HandleZoom()
    {
        float scroll = GetScrollDelta();
        if (Mathf.Abs(scroll) < 1e-6f)
        {
            return;
        }

        float current = transform.localScale.x;
        float target = current * (1f + scroll * zoomSpeed);
        float clamped = Mathf.Clamp(target, minScale, maxScale);
        transform.localScale = Vector3.one * clamped;
    }

    private bool IsLeftMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private Vector3 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        return _lastPointerPosition;
#else
        return Input.mousePosition;
#endif
    }

    private Vector2 GetPointerDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }
        return Vector2.zero;
#else
        Vector3 current = Input.mousePosition;
        return current - _lastPointerPosition;
#endif
    }

    private float GetScrollDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            // Match legacy Input.GetAxis("Mouse ScrollWheel") scale roughly
            return Mouse.current.scroll.ReadValue().y * 0.01f;
        }
        return 0f;
#else
        return Input.GetAxis("Mouse ScrollWheel");
#endif
    }
}
