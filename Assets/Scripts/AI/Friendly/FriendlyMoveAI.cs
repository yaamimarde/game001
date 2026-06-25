using UnityEngine;

public class FriendlyMoveAI : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float followDistance = 2f;
    [SerializeField] float detectRange = 6f;
    [SerializeField] LayerMask enemyLayers;

    Rigidbody2D rb;
    Transform player;
    CompanionOrder order = CompanionOrder.Follow;
    Transform combatTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        var p = FindObjectOfType<WarriorPlayer>();
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null || GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        switch (order)
        {
            case CompanionOrder.Hold:
                rb.velocity = Vector2.zero;
                break;
            case CompanionOrder.AttackTarget:
            case CompanionOrder.Follow:
                if (TryFindEnemy(out Transform enemy))
                {
                    MoveToward(enemy.position, moveSpeed * 1.1f);
                }
                else
                {
                    float dist = Vector2.Distance(transform.position, player.position);
                    if (dist > followDistance)
                        MoveToward(player.position, moveSpeed);
                    else
                        rb.velocity = Vector2.zero;
                }
                break;
        }
    }

    public void SetOrder(CompanionOrder newOrder)
    {
        order = newOrder;
    }

    bool TryFindEnemy(out Transform enemy)
    {
        enemy = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRange, enemyLayers);
        float best = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit.GetComponent<Character>() == null) continue;
            float d = Vector2.SqrMagnitude(hit.transform.position - transform.position);
            if (d < best)
            {
                best = d;
                enemy = hit.transform;
            }
        }
        return enemy != null;
    }

    void MoveToward(Vector3 target, float speed)
    {
        Vector2 dir = ((Vector2)(target - transform.position)).normalized;
        rb.velocity = dir * speed;
    }
}
