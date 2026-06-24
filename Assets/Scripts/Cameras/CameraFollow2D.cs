using UnityEngine;

/// <summary>
/// 2.5D 透视相机：跟随目标，X 轴倾斜俯视，Z 轴轨道旋转（Q/E）。
/// 位置与旋转在 LateUpdate 更新，确保在玩家移动之后执行。
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("X 轴俯仰角，负值表示从上往下看")]
    [SerializeField] float tiltAngle = -45f;
    [Tooltip("相机与焦点之间的直线距离")]
    [SerializeField] float followDistance = 14f;
    [Tooltip("焦点相对目标位置的偏移")]
    [SerializeField] Vector3 lookAtOffset = Vector3.zero;
    [SerializeField] float smoothTime = 0.15f;
    [Tooltip("Q/E 绕 Z 轴旋转的速度（度/秒）")]
    [SerializeField] float rotateSpeed = 90f;

    // 当前 Z 轴轨道角（度），Q 减小、E 增大
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

        // 先绕 Z 轴轨道旋转，再绕 X 轴倾斜，得到 2.5D 观察姿态
        Quaternion zOrbit = Quaternion.AngleAxis(orbitAngle, Vector3.forward);
        Quaternion xTilt = Quaternion.Euler(tiltAngle, 0f, 0f);

        Vector3 focusPoint = target.position + lookAtOffset;
        // 从焦点沿倾斜后的后方偏移 followDistance，再应用轨道旋转
        Vector3 offset = zOrbit * (xTilt * Vector3.back * followDistance);
        Vector3 desiredPosition = focusPoint + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = zOrbit * xTilt;
    }
}
