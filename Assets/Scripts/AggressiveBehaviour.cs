using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AggressiveBehaviour : MonoBehaviour
{
    private Character characterComponent;
    public float attackRange;

    [Header("攻击节奏设置")]
    public float attackInterval = 1f;    // 每次攻击的间隔（每秒攻击一次）
    public float restDuration = 2f;      // 3次攻击后的停顿时间
    public int maxAttackCount = 3;       // 一个循环内的最大攻击次数

    // 内部控制变量
    private int currentAttackCount = 0;  // 当前循环已经攻击了几次
    private float timer = 0f;            // 通用计时器

    // 定义状态
    public enum AIState { Attacking, Cooldown, Resting }
    private AIState currentState = AIState.Attacking;
    public AIState CurrentState => currentState;
    // 假设引入一个玩家引用用于测试距离，你也可以用你之前的获取玩家方法
    public Transform playerTransform;

    void Start()
    {
        characterComponent = GetComponent<Character>();
        if (characterComponent != null)
        {
            if (characterComponent.attack == "近战") attackRange = 1.5f;
            else if (characterComponent.attack == "远程") attackRange = 6.0f;
            else attackRange = 2.0f;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 只有当玩家在攻击范围内时，才执行攻击节奏逻辑
        if (distanceToPlayer <= attackRange)
        {
            ExecuteAttackRhythm();
        }
        else
        {
            // 如果玩家离开了范围，可以根据需求重置计数器，或者保持原样
            // 这里选择保持计时，但你可以根据实际游戏体验调整
        }
    }

    // 核心：控制攻击节奏的方法
    void ExecuteAttackRhythm()
    {
        switch (currentState)
        {
            case AIState.Attacking:
                // 1. 执行攻击
                PerformActualAttack();
                currentAttackCount++;

                // 2. 检查是否达到了3次攻击
                if (currentAttackCount >= maxAttackCount)
                {
                    // 达到了3次，进入大停顿状态
                    currentState = AIState.Resting;
                    timer = restDuration; // 设置停顿倒计时 2 秒
                    currentAttackCount = 0; // 重置攻击计数，为下一个循环准备
                    Debug.Log($"<color=yellow>【循环结束】已经攻击了 {maxAttackCount} 次，开始停顿 {restDuration} 秒...</color>");
                }
                else
                {
                    // 没到3次，进入普通的单次攻击冷却
                    currentState = AIState.Cooldown;
                    timer = attackInterval; // 设置单次冷却倒计时 1 秒
                }
                break;

            case AIState.Cooldown:
                // 1秒普通冷却倒计时
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    currentState = AIState.Attacking; // 冷却时间到，可以进行下一次攻击
                }
                break;

            case AIState.Resting:
                // 3次攻击后的2秒大停顿倒计时
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    currentState = AIState.Attacking; // 停顿时间到，开启新一轮循环
                    Debug.Log("<color=green>【新循环开始】停顿结束，重新开始攻击！</color>");
                }
                break;
        }
    }

    // 具体的攻击表现
    void PerformActualAttack()
    {
        if (characterComponent == null) return;

        // 读取 Character 里的数据和攻击类型进行具体的伤害输出
        if (characterComponent.attack == "近战")
        {
            Debug.Log($"{characterComponent.characterName} 发动了【近战挥砍】！造成 {characterComponent.damage} 点伤害。(当前攻击序列: {currentAttackCount + 1}/{maxAttackCount})");
        }
        else if (characterComponent.attack == "远程")
        {
            Debug.Log($"{characterComponent.characterName} 发动了【远程射击】！造成 {characterComponent.damage} 点伤害。(当前攻击序列: {currentAttackCount + 1}/{maxAttackCount})");
        }
    }
}