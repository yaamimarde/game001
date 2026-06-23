using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float tiltAngle = -45f;
    [SerializeField] float followDistance = 14f;
    [SerializeField] Vector3 lookAtOffset = Vector3.zero;
    [SerializeField] float smoothTime = 0.15f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        Vector3 focusPoint = target.position + lookAtOffset;
        Vector3 desiredPosition = focusPoint + rotation * Vector3.back * followDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = rotation;
    }
}
