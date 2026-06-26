using UnityEngine;

[DefaultExecutionOrder(50)]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float dashDistance = 3f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 0.5f;
    [SerializeField] float jumpSpeed = 8f;
    [SerializeField] float jumpDuration = 0.4f;
    [SerializeField] float jumpCooldown = 0.3f;
    [Tooltip("用于计算相对移动方向；留空则自动使用 Main Camera")]
    [SerializeField] Transform cameraTransform;

    Rigidbody2D rb;
    // 自动从 cameraTransform 获取；Main Camera 上的 CameraFollow2D，用于旋转期间锁定移动
    CameraFollow2D cameraFollow;
    PlayerInputReader input;
    Vector2 movement;
    bool isRunning;
    bool isDashing;
    bool isJumping;
    float dashTimer;
    float dashCooldownTimer;
    float jumpTimer;
    float jumpCooldownTimer;
    Vector2 dashWorldDirection;
    Vector2 jumpWorldDirection;
    Vector2 lastFacingInput = Vector2.down;

    public bool IsRunning => isRunning;
    public bool IsDashing => isDashing;
    public bool IsJumping => isJumping;
    public float CurrentSpeed => rb != null ? rb.velocity.magnitude : 0f;
    public Vector2 MoveInput => movement;
    public Vector2 FacingInput => lastFacingInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            cameraFollow = cameraTransform.GetComponent<CameraFollow2D>();

        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    void Update()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;

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

        if (isJumping)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0f)
            {
                isJumping = false;
                jumpCooldownTimer = jumpCooldown;
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

        movement = input != null ? input.MoveInput : ReadLegacyMoveInput();
        if (movement.sqrMagnitude > 0f)
            lastFacingInput = SnapTo6Directions(movement);

        isRunning = input != null ? input.IsRunning : Input.GetKey(KeyCode.LeftShift);

        bool dashPressed = input != null ? input.DashPressedThisFrame : Input.GetKeyDown(KeyCode.Space);
        if (dashPressed && dashCooldownTimer <= 0f && movement.sqrMagnitude > 0f)
            TryStartDash();

        bool jumpPressed = input != null ? input.JumpPressedThisFrame : Input.GetKeyDown(KeyCode.LeftControl);
        if (jumpPressed && jumpCooldownTimer <= 0f && !isDashing)
            TryStartJump();
    }

    static Vector2 ReadLegacyMoveInput()
    {
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (move.sqrMagnitude > 1f)
            move.Normalize();
        return move;
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

        if (isJumping)
        {
            rb.velocity = jumpWorldDirection * jumpSpeed;
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

    void TryStartJump()
    {
        Vector2 worldDir = lastFacingInput.sqrMagnitude > 0.001f
            ? WorldDirectionFromInput(lastFacingInput)
            : Vector2.down;

        if (worldDir.sqrMagnitude < 0.001f)
            worldDir = Vector2.down;
        else
            worldDir.Normalize();

        jumpWorldDirection = worldDir;
        isJumping = true;
        jumpTimer = jumpDuration;
    }

    Vector2 GetWorldMoveDirection()
    {
        if (movement.sqrMagnitude < 0.001f)
            return Vector2.zero;

        return WorldDirectionFromInput(movement);
    }

    Vector2 WorldDirectionFromInput(Vector2 input)
    {
        if (cameraTransform == null)
            return input;

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

        Vector3 worldMove = right * input.x + forward * input.y;
        if (worldMove.sqrMagnitude > 1f)
            worldMove.Normalize();

        return new Vector2(worldMove.x, worldMove.y);
    }

    bool IsCameraRotating() => cameraFollow != null && cameraFollow.IsRotating;

    static Vector2 SnapTo6Directions(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f)
            return input;

        if (input.y < 0f && Mathf.Abs(input.x) > 0.001f)
            return new Vector2(Mathf.Sign(input.x), 0f);

        if (input.y > 0f && Mathf.Abs(input.x) > 0.001f)
        {
            var diagonal = new Vector2(Mathf.Sign(input.x), 1f);
            return diagonal.normalized;
        }

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return new Vector2(Mathf.Sign(input.x), 0f);

        return new Vector2(0f, Mathf.Sign(input.y));
    }
}
