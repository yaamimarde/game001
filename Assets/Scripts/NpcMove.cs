using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcMove : MonoBehaviour
{
    [Header("目标引用")]
    public Transform playerTransform; // 玩家的 Transform 坐标

    [Header("通用移动设置")]
    public float moveSpeed = 3f;      // 移动速度

    [Header("AI 状态半径")]
    public float chaseRadius = 5f;    // 警戒半径（玩家进入此半径则开始往玩家走）

    [Header("绕圆巡逻设置")]
    public float circleRadius = 2f;   // 绕圈巡逻的圆半径
    public float circleSpeed = 2f;    // 绕圈的速度

    [Header("追击缓冲设置")]
    public float lostTargetBufferTime = 2f; // 离开范围后多追的时间（秒）

    private Vector3 patrolCenter;     // 巡逻圆心的初始位置
    private float angle = 0f;         // 当前旋转的角度
    private float lostTargetTimer = 0f;

    // 获取同物体上的攻击行为脚本组件
    private AggressiveBehaviour aggressiveScript;
    private float actualAttackRange = 0f; // 实际的攻击距离

    void Start()
    {
        patrolCenter = transform.position;
        aggressiveScript = GetComponent<AggressiveBehaviour>();
    }

    void Update()
    {
        // 1. 安全检查：如果没有玩家，老老实实巡逻
        if (playerTransform == null)
        {
            PatrolInCircle();
            return;
        }

        // 2. 实时同步外部脚本计算出的攻击距离
        if (aggressiveScript != null)
        {
            actualAttackRange = aggressiveScript.attackRange;
        }

        // 3. 计算 NPC 与玩家之间的实际距离
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // ================== 核心修改：双重最高优先级拦截 ==================
        // 判定条件1：距离小于等于攻击距离
        // 判定条件2：✨ 新增：哪怕因为物理挤压距离稍微拉开了一丁点，只要攻击脚本不在“准备攻击(Attacking)”状态（即在冷却或大停顿中），就绝不允许移动！
        bool isAttackingState = (aggressiveScript != null && aggressiveScript.CurrentState != AggressiveBehaviour.AIState.Attacking);

        if (distanceToPlayer <= actualAttackRange || isAttackingState)
        {
            lostTargetTimer = lostTargetBufferTime;
            patrolCenter = transform.position;
            angle = 0f;

            // 如果你挂了 Rigidbody 2D，强行把它的物理速度清零！
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            return; // 关键：跳过后续所有移动脚本
        }
        // =============================================================

        // 4. 状态判断逻辑（只有当不在攻击范围内，且不在攻击冷却/停顿中时，才会走到这里）
        if (distanceToPlayer <= chaseRadius)
        {
            // 玩家在警戒半径内，但在攻击范围外 -> 刷新计时器，并追击
            lostTargetTimer = lostTargetBufferTime;
            ChasePlayer();
        }
        else
        {
            // 玩家离开了警戒半径
            if (lostTargetTimer > 0f)
            {
                // 在2秒宽限期内 -> 继续追击
                lostTargetTimer -= Time.deltaTime;
                ChasePlayer();
            }
            else
            {
                // 超过2秒了 -> 彻底放弃，换成绕圆巡逻
                PatrolInCircle();
            }
        }
    }
    void ChasePlayer()
    {
        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        patrolCenter = transform.position;
        angle = 0f;
    }

    // 状态 B：绕圆巡逻
    void PatrolInCircle()
    {
        angle += circleSpeed * Time.deltaTime;
        float x = Mathf.Cos(angle) * circleRadius;
        float y = Mathf.Sin(angle) * circleRadius;
        Vector3 targetPatrolPosition = patrolCenter + new Vector3(x, y, 0f);

        // 巡逻时为了防止刚体残留速度干扰，用 MovePosition 或者直接清空速度后赋值
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        transform.position = Vector3.MoveTowards(transform.position, targetPatrolPosition, moveSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        // 橙色圈代表实际攻击范围
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, actualAttackRange);

        Gizmos.color = Color.green;
        if (Application.isPlaying) Gizmos.DrawWireSphere(patrolCenter, circleRadius);
        else Gizmos.DrawWireSphere(transform.position, circleRadius);
    }
}