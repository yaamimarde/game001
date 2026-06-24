using UnityEngine;

/// <summary>
/// 让 Sprite 始终面向摄像机（Billboard），用于 2.5D 俯视下的角色/物体显示。
/// 执行顺序设为 100，确保在 CameraFollow2D 的 LateUpdate 之后同步旋转。
/// </summary>
[DefaultExecutionOrder(100)]
public class FacingCamera : MonoBehaviour
{
    Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        transform.rotation = mainCamera.transform.rotation;
    }
}
