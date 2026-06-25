using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // 强制要求挂载刚体，防止空指针报错
public class NpcMove : MonoBehaviour
{
    [Header("目标引用")]
    public Transform playerTransform;

    [Header("通用移动设置")]
    public float moveSpeed = 3f;

    [Header("AI 状态半径")]
    public float chaseRadius = 5f;

    [Header("绕圆巡逻设置")]
    public float circleRadius = 2f;
    public float circleSpeed = 2f;

    [Header("追击缓冲设置")]
    public float lostTargetBufferTime = 2f;

    private Vector3 patrolCenter;
    private float angle = 0f;
    private float lostTargetTimer = 0f;

    private AggressiveBehaviour aggressiveScript;
    private Rigidbody2D rb;
    private float actualAttackRange = 0f;

    void Start()
    {
        patrolCenter = transform.position;
        aggressiveScript = GetComponent<AggressiveBehaviour>();
        rb = GetComponent<Rigidbody2D>();

        // 规范 2D 物理设定：防止受物理碰撞导致NPC产生诡异的Z轴旋转
        rb.freezeRotation = true;
        rb.gravityScale = 0f; // 2D俯视角游戏通常关闭重力
    }

    void Update()
    {
        if (playerTransform == null)
        {
            PatrolInCircle();
            return;
        }

        if (aggressiveScript != null)
        {
            actualAttackRange = aggressiveScript.attackRange;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 判断：是否在攻击距离内，或者攻击脚本强制要求停下（比如处于攻击后摇/冷却）
        bool shouldStop = distanceToPlayer <= actualAttackRange || (aggressiveScript != null && aggressiveScript.ShouldStopMovement);

        if (shouldStop)
        {
            lostTargetTimer = lostTargetBufferTime;
            patrolCenter = transform.position;
            angle = 0f;

            rb.velocity = Vector2.zero; // 停止物理惯性
            return;
        }

        // 状态状态状态切换
        if (distanceToPlayer <= chaseRadius)
        {
            lostTargetTimer = lostTargetBufferTime;
            ChasePlayer();
        }
        else
        {
            if (lostTargetTimer > 0f)
            {
                lostTargetTimer -= Time.deltaTime;
                ChasePlayer();
            }
            else
            {
                PatrolInCircle();
            }
        }
    }

    // 修复：补全了追击移动逻辑，改用 Rigidbody2D 安全移动
    void ChasePlayer()
    {
        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 targetPosition = (Vector2)transform.position + direction * moveSpeed * Time.deltaTime;

        rb.MovePosition(targetPosition);

        patrolCenter = transform.position; // 实时刷新巡逻锚点
        angle = 0f;
    }

    // 优化：巡逻逻辑同样改用物理安全的 MovePosition
    void PatrolInCircle()
    {
        angle += circleSpeed * Time.deltaTime;
        float x = Mathf.Cos(angle) * circleRadius;
        float y = Mathf.Sin(angle) * circleRadius;
        Vector3 targetPatrolPosition = patrolCenter + new Vector3(x, y, 0f);

        rb.velocity = Vector2.zero;

        Vector2 nextPos = Vector3.MoveTowards(transform.position, targetPatrolPosition, moveSpeed * Time.deltaTime);
        rb.MovePosition(nextPos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, actualAttackRange);

        Gizmos.color = Color.green;
        if (Application.isPlaying) Gizmos.DrawWireSphere(patrolCenter, circleRadius);
        else Gizmos.DrawWireSphere(transform.position, circleRadius);
    }
}