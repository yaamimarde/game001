using UnityEngine;
public class CompanionCombatAI : MonoBehaviour, IAttackBehaviour
{
    [SerializeField] float attackRange = 1.2f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] LayerMask enemyLayers;

    Character self;
    float nextAttackTime;

    public float AttackRange => attackRange;
    public bool ShouldStopMovement => Time.time < nextAttackTime + 0.2f;

    void Awake()
    {
        self = GetComponent<Character>();
    }

    void Update()
    {
        if (self == null || self.hp <= 0) return;
        if (Time.time < nextAttackTime) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayers);
        foreach (var hit in hits)
        {
            var target = hit.GetComponent<Character>();
            if (target == null || target == self) continue;

            target.TakeDamage(self.damage);
            nextAttackTime = Time.time + attackCooldown;
            break;
        }
    }
}
