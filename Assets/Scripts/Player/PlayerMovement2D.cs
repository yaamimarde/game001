using UnityEngine;

/// <summary>
/// 玩家在 XY 平面上的四向移动，基于 Rigidbody2D，无重力。
/// 输入方向相对摄像机朝向转换；相机步进旋转期间禁止移动。
/// Shift 奔跑，Space 定向冲刺（需有方向输入）。
/// </summary>
[DefaultExecutionOrder(50)]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float dashDistance = 3f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 0.5f;
    [Tooltip("用于计算相对移动方向；留空则自动使用 Main Camera")]
    [SerializeField] Transform cameraTransform;

    Rigidbody2D rb;
    CameraFollow2D cameraFollow;
    Vector2 movement;
    bool isRunning;
    bool isDashing;
    float dashTimer;
    float dashCooldownTimer;
    Vector2 dashWorldDirection;
    Vector2 lastFacingInput = Vector2.down;

    public bool IsRunning => isRunning;
    public bool IsDashing => isDashing;
    public float CurrentSpeed => rb != null ? rb.velocity.magnitude : 0f;
    public Vector2 MoveInput => movement;
    public Vector2 FacingInput => lastFacingInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            cameraFollow = cameraTransform.GetComponent<CameraFollow2D>();
    }

    void Update()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashCooldownTimer = dashCooldown;
                rb.velocity = Vector2.zero;
            }
            return;
        }

        if (IsCameraRotating())
        {
            movement = Vector2.zero;
            isRunning = false;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        if (movement.sqrMagnitude > 0f)
            lastFacingInput = SnapTo4Directions(movement);

        isRunning = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.Space)
            && dashCooldownTimer <= 0f
            && movement.sqrMagnitude > 0f)
            TryStartDash();
    }

    void FixedUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            cameraFollow = cameraTransform.GetComponent<CameraFollow2D>();
        }

        if (IsCameraRotating())
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            rb.velocity = dashWorldDirection * (dashDistance / dashDuration);
            return;
        }

        float speed = isRunning ? runSpeed : moveSpeed;

        if (cameraTransform == null)
        {
            rb.velocity = movement * speed;
            return;
        }

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

        rb.velocity = worldMove * speed;
    }

    void TryStartDash()
    {
        Vector2 worldDir = GetWorldMoveDirection();
        if (worldDir.sqrMagnitude < 0.001f)
            return;

        dashWorldDirection = worldDir;
        isDashing = true;
        dashTimer = dashDuration;
    }

    Vector2 GetWorldMoveDirection()
    {
        if (cameraTransform == null)
            return movement;

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

        return new Vector2(worldMove.x, worldMove.y);
    }

    bool IsCameraRotating() => cameraFollow != null && cameraFollow.IsRotating;

    static Vector2 SnapTo4Directions(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return new Vector2(Mathf.Sign(input.x), 0f);
        return new Vector2(0f, Mathf.Sign(input.y));
    }
}
