using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(101)]
public class PlayerAnimation : MonoBehaviour
{
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");

    Animator animator;
    PlayerMovement2D movement;
    PlayerComboAttack comboAttack;
    Vector2? facingOverride;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponentInParent<PlayerMovement2D>();
        comboAttack = GetComponentInParent<PlayerComboAttack>();
    }

    void Update()
    {
        if (animator == null || movement == null)
            return;

        animator.SetFloat(SpeedHash, movement.CurrentSpeed);
        animator.SetBool(IsRunningHash, movement.IsRunning);
        animator.SetBool(IsDashingHash, movement.IsDashing);
        animator.SetBool(IsJumpingHash, movement.IsJumping);

        Vector2 facing = facingOverride ?? movement.FacingInput;
        animator.SetFloat(MoveXHash, facing.x);
        animator.SetFloat(MoveYHash, facing.y);
    }

    public void LockFacingForAttack()
    {
        if (movement != null)
            facingOverride = movement.FacingInput;
    }

    public void ClearFacingOverride()
    {
        facingOverride = null;
    }

    public void OnFootstep() { }

    public void OnDashImpact() { }

    public void OnAttackHit()
    {
        comboAttack?.ApplyHitDamage();
    }
}
