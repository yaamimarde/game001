using UnityEngine;

/// <summary>
/// 玩家在 XY 平面上的四向移动，基于 Rigidbody2D，无重力。
/// 输入方向相对摄像机朝向转换，Q/E 旋转相机后 W/A/S/D 仍对应屏幕上下左右。
/// </summary>
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [Tooltip("用于计算相对移动方向；留空则自动使用 Main Camera")]
    [SerializeField] Transform cameraTransform;

    Rigidbody2D rb;
    Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();
    }

    void FixedUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            rb.velocity = movement * moveSpeed;
            return;
        }

        // 将相机 forward/right 投影到 XY 平面，使输入始终对齐当前画面
        Vector3 forward = cameraTransform.forward;
        forward.z = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.up;
        else
            forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.z = 0f;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        else
            right.Normalize();

        Vector3 worldMove = right * movement.x + forward * movement.y;
        if (worldMove.sqrMagnitude > 1f)
            worldMove.Normalize();

        rb.velocity = worldMove * moveSpeed;
    }
}
