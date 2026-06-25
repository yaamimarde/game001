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
    public bool ShouldStopMovement => currentState == AIState.Cooldown || currentState == AIState.Resting;

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

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 如果在攻击范围内，执行攻击节奏
        if (distanceToPlayer <= attackRange)
        {
            ExecuteAttackRhythm();
        }
    }

    void ExecuteAttackRhythm()
    {
        switch (currentState)
        {
            case AIState.Attacking:
                PerformActualAttack();
                currentAttackCount++;

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
                break;

            case AIState.Cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0f) currentState = AIState.Attacking;
                break;

            case AIState.Resting:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    currentState = AIState.Attacking;
                    Debug.Log("<color=green>NPC 休息完毕，眼神恢复犀利！</color>");
                }
                break;
        }
    }

    void PerformActualAttack()
    {
        if (characterComponent == null) return;
        Debug.Log($"{characterComponent.characterName} 发动了攻击！造成 {characterComponent.damage} 点伤害。(进度: {currentAttackCount + 1}/{maxAttackCount})");
    }
}