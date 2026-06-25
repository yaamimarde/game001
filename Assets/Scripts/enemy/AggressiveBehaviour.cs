using UnityEngine;

public class AggressiveBehaviour : MonoBehaviour, IAttackBehaviour
{
    Character characterComponent;

    [HideInInspector]
    public float attackRange;
    public float AttackRange => attackRange;

    [Header("攻击节奏设置")]
    public float attackInterval = 1f;
    public float restDuration = 2f;
    public int maxAttackCount = 3;

    int currentAttackCount;
    float timer;

    public enum AIState { Attacking, Cooldown, Resting }
    AIState currentState = AIState.Attacking;

    public bool ShouldStopMovement =>
        currentState == AIState.Attacking || currentState == AIState.Resting;

    public Transform playerTransform;

    void Start()
    {
        if (playerTransform == null)
        {
            if (PlayerBootstrap.PlayerTransform != null)
                playerTransform = PlayerBootstrap.PlayerTransform;
            else
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }
        }

        characterComponent = GetComponent<Character>();
        if (characterComponent != null)
        {
            attackRange = characterComponent.attackType switch
            {
                AttackType.Melee => 1.5f,
                AttackType.Ranged => 6.0f,
                _ => 2.0f
            };
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == AIState.Cooldown || currentState == AIState.Resting)
            UpdateTimers();
        else if (currentState == AIState.Attacking && distanceToPlayer <= attackRange)
            TriggerActualAttack();
    }

    void UpdateTimers()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            bool wasResting = currentState == AIState.Resting;
            currentState = AIState.Attacking;
            if (wasResting)
                Debug.Log("<color=green>NPC 休息完毕，眼神恢复犀利！</color>");
        }
    }

    void TriggerActualAttack()
    {
        PerformActualAttack();
        currentAttackCount++;

        if (currentAttackCount >= maxAttackCount)
        {
            currentState = AIState.Resting;
            timer = restDuration;
            currentAttackCount = 0;
        }
        else
        {
            currentState = AIState.Cooldown;
            timer = attackInterval;
        }
    }

    void PerformActualAttack()
    {
        if (characterComponent == null || playerTransform == null) return;

        Character targetCharacter = playerTransform.GetComponent<Character>();
        targetCharacter?.TakeDamage(characterComponent.damage);
    }
}
