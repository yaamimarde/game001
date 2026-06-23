using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float tiltAngle = -45f;
    [SerializeField] float followDistance = 14f;
    [SerializeField] Vector3 lookAtOffset = Vector3.zero;
    [SerializeField] float smoothTime = 0.15f;
    [SerializeField] float rotateSpeed = 90f;

    float orbitAngle;
    Vector3 velocity;

    void Update()
    {
        if (Input.GetKey(KeyCode.Q))
            orbitAngle -= rotateSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            orbitAngle += rotateSpeed * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Quaternion zOrbit = Quaternion.AngleAxis(orbitAngle, Vector3.forward);
        Quaternion xTilt = Quaternion.Euler(tiltAngle, 0f, 0f);

        Vector3 focusPoint = target.position + lookAtOffset;
        Vector3 offset = zOrbit * (xTilt * Vector3.back * followDistance);
        Vector3 desiredPosition = focusPoint + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = zOrbit * xTilt;
    }
}
