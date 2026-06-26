using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// 【摄像机脚本】CameraFollowSimple
// 挂载：Main Camera
// 功能：平面平滑跟随，无倾斜、无旋转
// ============================================================

/// <summary>
/// 简单 2D 相机跟随：锁定 XY 平面位置，保持固定 Z 与朝向。
/// </summary>
public class CameraFollowSimple : MonoBehaviour
{
    #region 序列化参数

    [SerializeField] Transform target;
    [Tooltip("相对目标的位置偏移，Z 通常为负值（如 -10）")]
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] float smoothTime = 0.15f;
    [Tooltip("勾选后相机始终朝向默认方向（无倾斜）")]
    [SerializeField] bool lockRotation = true;

    #endregion

    Vector3 velocity;

    #region 目标查找

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureTarget();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureTarget();

    void EnsureTarget()
    {
        if (target != null)
            return;

        if (PlayerBootstrap.PlayerTransform != null)
        {
            target = PlayerBootstrap.PlayerTransform;
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    #endregion

    #region 跟随（LateUpdate）

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        if (smoothTime <= 0f)
            transform.position = desiredPosition;
        else
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref velocity, smoothTime);

        if (lockRotation)
            transform.rotation = Quaternion.identity;
    }

    #endregion
}
