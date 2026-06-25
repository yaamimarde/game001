using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ✨ 优化：继承 IAttackBehaviour 接口
public class AggressiveBehaviour : MonoBehaviour, IAttackBehaviour
{
    private Character characterComponent;

    [HideInInspector]
    public float attackRange;
    public float AttackRange => attackRange; // ✨ 实现接口属性

    [Header("攻击节奏设置")]
    public float attackInterval = 1f;
    public float restDuration = 2f;
    public int maxAttackCount = 3;

    private int currentAttackCount = 0;
    private float timer = 0f;

    public enum AIState { Attacking, Cooldown, Resting }
    private AIState currentState = AIState.Attacking;

    // ✨ 实现接口属性：对外暴露停步状态
    // 修改后：Attacking 代表挥刀不能动，Resting 代表大虚弱不能动；而 Cooldown（小冷却）期间允许移动环绕走位
    public bool ShouldStopMovement => currentState == AIState.Attacking || currentState == AIState.Resting;

    public Transform playerTransform;

    void Start()
    {
        characterComponent = GetComponent<Character>();
        if (characterComponent != null)
        {
            switch (characterComponent.attackType)
            {
                case AttackType.Melee:
                    attackRange = 1.5f;
                    break;
                case AttackType.Ranged:
                    attackRange = 6.0f;
                    break;
                default:
                    attackRange = 2.0f;
                    break;
            }
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. 计算与玩家的实际距离
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 2. ✨核心重构：时间流转绝对独立！
        // 无论怪物因为环绕走位走到哪里，冷却(Cooldown)和休息(Resting)的计时器必须在后台雷打不动地推进。
        if (currentState == AIState.Cooldown || currentState == AIState.Resting)
        {
            UpdateTimers();
        }
        // 3. 只有当处于 Attacking 状态（即技能准备就绪）时，才去判定距离是否触发攻击
        else if (currentState == AIState.Attacking)
        {
            if (distanceToPlayer <= attackRange)
            {
                TriggerActualAttack();
            }
        }
    }

    /// <summary>
    /// ✨ 新增：纯粹的计时器更新逻辑（不包含攻击行为触发）
    /// </summary>
    void UpdateTimers()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            currentState = AIState.Attacking;
            if (currentState == AIState.Resting)
            {
                Debug.Log("<color=green>NPC 休息完毕，眼神恢复犀利！</color>");
            }
        }
    }

    /// <summary>
    /// ✨ 新增：纯粹的触发一击逻辑
    /// </summary>
    void TriggerActualAttack()
    {
        // 执行实际伤害/Log
        PerformActualAttack();
        currentAttackCount++;

        // 判断是进入小 CD 还是大休息
        if (currentAttackCount >= maxAttackCount)
        {
            currentState = AIState.Resting;
            timer = restDuration;
            currentAttackCount = 0;
            Debug.Log($"<color=yellow>{characterComponent.characterName} 连招打完，进入大休息 {restDuration} 秒...</color>");
        }
        else
        {
            currentState = AIState.Cooldown;
            timer = attackInterval;
        }
    }
    void PerformActualAttack()
    {
        // 安全拦截
        if (characterComponent == null || playerTransform == null) return;

        Debug.Log($"<color=orange>{characterComponent.characterName} 发动了攻击！(进度: {currentAttackCount + 1}/{maxAttackCount})</color>");

        // 通过玩家的 Transform 获取它身上的 Character 基类组件
        Character targetCharacter = playerTransform.GetComponent<Character>();

        if (targetCharacter != null)
        {
            // 调用玩家的受击方法，传入当前怪物自身的伤害值（characterComponent.damage）
            targetCharacter.TakeDamage(characterComponent.damage);
        }
        else
        {
            Debug.LogError($"在目标 {playerTransform.name} 上没有找到 Character 组件！无法造成伤害！");
        }
    }
}