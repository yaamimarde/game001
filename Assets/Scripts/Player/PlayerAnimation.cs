using UnityEngine;

/// <summary>
/// 根据 PlayerMovement2D 状态驱动 Animator 参数；提供 Animation Event 回调。
/// 水平左走/左跑复用右向序列帧，通过 SpriteRenderer.flipX 镜像。
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(101)]
public class PlayerAnimation : MonoBehaviour
{
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");

    Animator animator;
    PlayerMovement2D movement;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (animator == null || movement == null)
            return;

        animator.SetFloat(SpeedHash, movement.CurrentSpeed);
        animator.SetBool(IsRunningHash, movement.IsRunning);
        animator.SetBool(IsDashingHash, movement.IsDashing);

        Vector2 facing = movement.FacingInput;
        float animMoveX = facing.x != 0f ? Mathf.Abs(facing.x) : 0f;
        animator.SetFloat(MoveXHash, animMoveX);
        animator.SetFloat(MoveYHash, facing.y);
    }

    void LateUpdate()
    {
        if (spriteRenderer == null || movement == null)
            return;

        Vector2 facing = movement.FacingInput;
        if (facing.x < 0f)
            spriteRenderer.flipX = true;
        else if (facing.x > 0f)
            spriteRenderer.flipX = false;
    }

    public void OnFootstep()
    {
        Debug.Log("Footstep");
    }

    public void OnDashImpact()
    {
        Debug.Log("Dash impact");
    }

    public void OnAttackHit()
    {
        Debug.Log("Attack hit");
    }
}
