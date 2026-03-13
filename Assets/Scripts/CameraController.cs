using UnityEngine;

/// <summary>
/// Orbital or follow camera controller.
/// Can operate in Follow mode (third-person) or Orbit mode (around a target point).
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Follow,
        Orbit
    }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private CameraMode mode = CameraMode.Follow;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 2f, -5f);
    [SerializeField] private float followSmoothTime = 0.1f;
    [SerializeField] private bool lookAtTarget = true;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitDistance = 5f;
    [SerializeField] private float orbitSensitivity = 3f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoomDistance = 1f;
    [SerializeField] private float maxZoomDistance = 20f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.2f;

    private Vector3 _smoothVelocity;
    private float _yaw;
    private float _pitch;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraController: No target assigned. Searching for player...");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        switch (mode)
        {
            case CameraMode.Follow:
                UpdateFollowCamera();
                break;
            case CameraMode.Orbit:
                UpdateOrbitCamera();
                break;
        }
    }

    private void UpdateFollowCamera()
    {
        Vector3 desiredPosition = target.TransformPoint(followOffset);

        // Collision check
        if (Physics.SphereCast(target.position, collisionRadius, (desiredPosition - target.position).normalized,
            out RaycastHit hit, followOffset.magnitude, collisionMask))
        {
            desiredPosition = hit.point + hit.normal * collisionRadius;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _smoothVelocity, followSmoothTime);

        if (lookAtTarget)
            transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    private void UpdateOrbitCamera()
    {
        // Input
        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // Scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        orbitDistance -= scroll * zoomSpeed;
        orbitDistance = Mathf.Clamp(orbitDistance, minZoomDistance, maxZoomDistance);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPosition = target.position - rotation * Vector3.forward * orbitDistance;

        // Collision
        if (Physics.SphereCast(target.position, collisionRadius, -rotation * Vector3.forward,
            out RaycastHit hit, orbitDistance, collisionMask))
        {
            desiredPosition = target.position - rotation * Vector3.forward * (hit.distance - collisionRadius);
        }

        transform.position = desiredPosition;
        transform.LookAt(target.position);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetMode(CameraMode newMode)
    {
        mode = newMode;
    }
}
