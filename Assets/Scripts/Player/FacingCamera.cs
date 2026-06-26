using UnityEngine;

// ============================================================
// 【摄像机相关】FacingCamera（Billboard）
// 挂载：Player / 敌人 / 树的 Sprite 子对象（勿挂在 Rigidbody2D 根对象）
// 功能：精灵仅同步相机 Z 轴旋转，避免倾斜物理碰撞体
// ============================================================

[DefaultExecutionOrder(100)]
public class FacingCamera : MonoBehaviour
{
    UnityEngine.Camera mainCamera;

    void Awake()
    {
        mainCamera = UnityEngine.Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = UnityEngine.Camera.main;

        if (mainCamera == null)
            return;

        float z = mainCamera.transform.eulerAngles.z;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }
}
