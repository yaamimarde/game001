using UnityEngine;

public class PlayerComboAttack : PlayerAttackBase
{
    [Header("连招专属设置")]
    public float comboResetDuration = 1.5f;

    [Header("伤害判定设置")]
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public LayerMask enemyLayers;
    [SerializeField] bool useAnimationHitEvent = true;

    static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    static readonly int Attack3Hash = Animator.StringToHash("Attack3");

    int comboIndex;
    float lastAttackTime;
    bool pendingHit;

    PlayerMovement2D movement;
    PlayerAnimation playerAnimation;
    PlayerInputReader input;

    protected override void Start()
    {
        base.Start();
        movement = GetComponent<PlayerMovement2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
        input = GetComponent<PlayerInputReader>();
    }

    void Update()
    {
        if (comboIndex > 0 && Time.time - lastAttackTime > comboResetDuration)
            ResetCombo();

        if (GetAttackPressed() && CanAttack())
            ExecuteAttack();
    }

    bool GetAttackPressed()
    {
        if (input != null)
            return input.AttackPressedThisFrame;

        return Input.GetMouseButtonDown(0);
    }

    public override bool CanAttack()
    {
        if (!base.CanAttack())
            return false;

        if (movement != null && (movement.IsDashing || movement.IsJumping))
            return false;

        return true;
    }

    public override void ExecuteAttack()
    {
        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;
        pendingHit = true;

        if (playerAnimation != null)
            playerAnimation.LockFacingForAttack();

        int animIndex = comboIndex + 1;
        SetAttackTrigger(animIndex);

        if (!useAnimationHitEvent)
            ApplyHitDamage();
    }

    void SetAttackTrigger(int index)
    {
        if (anim == null)
            return;

        var hash = index switch
        {
            1 => Attack1Hash,
            2 => Attack2Hash,
            3 => Attack3Hash,
            _ => 0
        };

        if (hash != 0 && HasParameter(anim, hash))
            anim.SetTrigger(hash);
    }

    static bool HasParameter(Animator animator, int nameHash)
    {
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == nameHash)
                return true;
        }

        return false;
    }

    public void ApplyHitDamage()
    {
        if (!pendingHit)
            return;

        pendingHit = false;

        Vector3 detectCenter = attackPoint != null ? attackPoint.position : transform.position;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(detectCenter, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            Character enemyCharacter = enemy.GetComponent<Character>();
            if (enemyCharacter == null)
                continue;

            enemyCharacter.TakeDamage(characterComponent.damage);
            GameManager.Instance?.Session?.Progression?.RegisterMeleeHit();
        }

        comboIndex++;
        if (comboIndex >= 3)
            ResetCombo();
    }

    void ResetCombo()
    {
        comboIndex = 0;
        pendingHit = false;
        playerAnimation?.ClearFacingOverride();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 detectCenter = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(detectCenter, attackRange);
    }
}
