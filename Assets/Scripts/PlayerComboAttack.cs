using UnityEngine;

public class PlayerComboAttack : PlayerAttackBase
{
    [Header("连招专属设置")]
    public float comboResetDuration = 1.5f;
    private int comboIndex = 0;
    private float lastAttackTime = 0f;

    // 🎯 ✨ 新增：伤害判定所需的配置
    [Header("伤害判定设置（新）")]
    public Transform attackPoint;      // 攻击中心点（在玩家前方建一个空物体拖进来，不填默认用玩家自身中心）
    public float attackRange = 1.2f;    // 攻击距离/半径
    public LayerMask enemyLayers;       // 敌人所在的图层（用于过滤，防止砍到自己或背景）

    protected override void Start()
    {
        base.Start();
    }

    void Update()
    {
        if (comboIndex > 0 && Time.time - lastAttackTime > comboResetDuration)
        {
            ResetCombo();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (CanAttack())
            {
                ExecuteAttack();
            }
        }
    }

    public override void ExecuteAttack()
    {
        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;

        // 1. 🎬 触发对应的攻击动画
        int animIndex = comboIndex + 1;
        if (anim != null)
        {
            anim.SetTrigger("Attack" + animIndex);
            Debug.Log($"<color=cyan>【玩家攻击】使出第 {animIndex} 剑！</color>");
        }

        // 2. 🎯 ✨ 新增核心：执行圆形范围命中检测
        // 如果没有配置特殊的攻击点，就默认以玩家自身坐标为中心检测
        Vector3 detectCenter = attackPoint != null ? attackPoint.position : transform.position;

        // 划出一个圆形区域，抓取里面所有属于 enemyLayers 图层的碰撞体
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(detectCenter, attackRange, enemyLayers);

        // 遍历所有被砍中的敌人
        foreach (Collider2D enemy in hitEnemies)
        {
            // 获取敌人身上继承自 Character 的组件（比如 Gebuling 哥布林）
            Character enemyCharacter = enemy.GetComponent<Character>();

            if (enemyCharacter != null)
            {
                // 💥 真正调用伤害方法，传入玩家自身的攻击力（characterComponent.damage）
                enemyCharacter.TakeDamage(characterComponent.damage);
                Debug.Log($"<color=red>🎯 成功击中 {enemyCharacter.characterName}！</color>");
            }
        }

        // 3. 推进连招状态
        comboIndex++;
        if (comboIndex >= 3)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        if (comboIndex > 0) Debug.Log("<color=gray>【连招重置】回到第一剑。</color>");
        comboIndex = 0;
    }

    // 🎨 ✨ 新增：在编辑器里画出攻击范围红圈，方便你调大小
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 detectCenter = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(detectCenter, attackRange);
    }
}