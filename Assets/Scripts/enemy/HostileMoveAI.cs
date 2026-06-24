using UnityEngine;

/// <summary>
/// 独立敌对移动 AI（不依赖 AggressiveBehaviour / NpcMove）。
/// 近战：135° 视野、加权追击、环绕、出生点徘徊；远程：风筝、避障、反卡死。
/// </summary>
public class HostileMoveAI : MonoBehaviour
{
    public enum CombatType { Auto, Melee, Ranged }

    enum MoveState
    {
        IdlePatrol,
        MeleeChase,
        MeleeOrbit,
        RangedKite,
        StuckRecovery
    }

    [Header("目标引用")]
    [SerializeField] Transform playerTransform;

    [Header("战斗类型")]
    [SerializeField] CombatType combatType = CombatType.Auto;

    [Header("通用移动")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float detectionRadius = 8f;
    [SerializeField] float fieldOfView = 135f;
    [SerializeField] LayerMask obstacleLayer = ~0;
    [SerializeField] float obstacleCheckDistance = 1.2f;

    [Header("攻击停步")]
    [Tooltip("进入此距离内停止移动；为 0 时根据 Character.attack 自动推断")]
    [SerializeField] float attackStopRange;

    [Header("近战")]
    [SerializeField] float orbitDistance = 1.8f;
    [SerializeField] float chaseLostBuffer = 2f;
    [SerializeField] float idlePatrolRadius = 2f;
    [SerializeField] float idleMaxDistanceFromSpawn = 3f;
    [SerializeField] float idleRetargetInterval = 2f;

    [Header("远程")]
    [SerializeField] float preferredRange = 5f;
    [SerializeField] float minSafeRange = 3f;

    [Header("反卡死")]
    [SerializeField] float stuckCheckDuration = 0.5f;
    [SerializeField] float stuckMinMove = 0.05f;
    [SerializeField] float stuckRecoveryTime = 0.4f;

    static readonly (float angle, float weight)[] MeleeChaseWeights =
    {
        (0f, 10f),
        (15f, 7f), (-15f, 7f),
        (30f, 5f), (-30f, 5f),
        (45f, 3f), (-45f, 3f)
    };

    static readonly (float angle, float weight)[] RangedKiteWeights =
    {
        (0f, 10f),
        (30f, 6f), (-30f, 6f),
        (60f, 4f), (-60f, 4f),
        (90f, 2f), (-90f, 2f)
    };

    Rigidbody2D rb;
    Character character;

    Vector3 spawnPosition;
    Vector2 facingDirection = Vector2.up;
    Vector2 idleTarget;
    float idleRetargetTimer;
    float lostTargetTimer;
    float stuckTimer;
    float stuckRecoveryTimer;
    Vector3 stuckCheckStartPos;
    MoveState currentState = MoveState.IdlePatrol;
    MoveState stateBeforeStuck;
    Vector2 stuckEscapeDirection;
    bool isMelee;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        character = GetComponent<Character>();
    }

    void Start()
    {
        spawnPosition = transform.position;
        PickNewIdleTarget();

        if (combatType == CombatType.Auto)
            isMelee = character == null || character.attack == "近战";
        else
            isMelee = combatType == CombatType.Melee;

        if (orbitDistance <= 0f)
            orbitDistance = GetAttackStopRange() * 1.2f;

        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    void Update()
    {
        UpdateFacingDirection();
        UpdateStateMachine();
        CheckStuck();

        if (rb != null)
            return;

        if (ShouldStopForAttack())
            return;

        Vector2 moveDir = ComputeMoveDirection();
        if (currentState == MoveState.StuckRecovery)
            moveDir = stuckEscapeDirection;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.position += (Vector3)(moveDir.normalized * moveSpeed * Time.deltaTime);
            facingDirection = moveDir.normalized;
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (ShouldStopForAttack())
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = ComputeMoveDirection();
        if (currentState == MoveState.StuckRecovery)
            moveDir = stuckEscapeDirection;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            rb.velocity = moveDir.normalized * moveSpeed;
            facingDirection = moveDir.normalized;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    float GetAttackStopRange()
    {
        if (attackStopRange > 0f)
            return attackStopRange;

        if (character == null)
            return isMelee ? 1.5f : 6f;

        if (character.attack == "近战")
            return 1.5f;
        if (character.attack == "远程")
            return 6f;
        return 2f;
    }

    void UpdateFacingDirection()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
            facingDirection = rb.velocity.normalized;
        else if (playerTransform != null)
            facingDirection = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        else
            facingDirection = transform.up;
    }

    void UpdateStateMachine()
    {
        if (playerTransform == null)
        {
            currentState = MoveState.IdlePatrol;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool canSee = CanSeePlayer();

        if (currentState == MoveState.StuckRecovery)
        {
            stuckRecoveryTimer -= Time.deltaTime;
            if (stuckRecoveryTimer <= 0f)
                currentState = stateBeforeStuck;
            return;
        }

        if (isMelee)
            UpdateMeleeState(dist, canSee);
        else
            UpdateRangedState(dist, canSee);
    }

    void UpdateMeleeState(float dist, bool canSee)
    {
        if (canSee)
        {
            lostTargetTimer = chaseLostBuffer;

            if (dist <= orbitDistance)
                currentState = MoveState.MeleeOrbit;
            else if (dist <= detectionRadius)
                currentState = MoveState.MeleeChase;
        }
        else if (lostTargetTimer > 0f)
        {
            lostTargetTimer -= Time.deltaTime;
            if (currentState == MoveState.MeleeChase || currentState == MoveState.MeleeOrbit)
                return;
        }
        else
        {
            currentState = MoveState.IdlePatrol;
        }

        if (currentState == MoveState.IdlePatrol)
            UpdateIdlePatrolLogic();
    }

    void UpdateRangedState(float dist, bool canSee)
    {
        if (canSee && dist <= detectionRadius)
        {
            lostTargetTimer = chaseLostBuffer;
            currentState = MoveState.RangedKite;
        }
        else if (lostTargetTimer > 0f && currentState == MoveState.RangedKite)
        {
            lostTargetTimer -= Time.deltaTime;
        }
        else
        {
            currentState = MoveState.IdlePatrol;
            UpdateIdlePatrolLogic();
        }
    }

    void UpdateIdlePatrolLogic()
    {
        idleRetargetTimer -= Time.deltaTime;
        if (idleRetargetTimer <= 0f || Vector2.Distance(transform.position, idleTarget) < 0.2f)
            PickNewIdleTarget();

        float distFromSpawn = Vector2.Distance(transform.position, spawnPosition);
        if (distFromSpawn > idleMaxDistanceFromSpawn)
            idleTarget = spawnPosition;
    }

    void PickNewIdleTarget()
    {
        Vector2 offset = Random.insideUnitCircle * idlePatrolRadius;
        idleTarget = (Vector2)spawnPosition + offset;
        idleRetargetTimer = idleRetargetInterval;
    }

    Vector2 ComputeMoveDirection()
    {
        switch (currentState)
        {
            case MoveState.IdlePatrol:
                return ((Vector2)idleTarget - (Vector2)transform.position).normalized;
            case MoveState.MeleeChase:
                return PickWeightedDirection(ToPlayerAngle(), MeleeChaseWeights);
            case MoveState.MeleeOrbit:
                return ComputeOrbitDirection();
            case MoveState.RangedKite:
                return ComputeRangedKiteDirection();
            default:
                return Vector2.zero;
        }
    }

    float ToPlayerAngle()
    {
        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        return Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
    }

    Vector2 PickWeightedDirection(float baseAngleDeg, (float angle, float weight)[] weightTable)
    {
        float totalWeight = 0f;
        var candidates = new (float angle, float weight)[weightTable.Length];

        for (int i = 0; i < weightTable.Length; i++)
        {
            float angle = baseAngleDeg + weightTable[i].angle;
            float w = weightTable[i].weight;
            Vector2 dir = AngleToDirection(angle);

            if (IsBlocked(dir))
                w *= 0.05f;

            candidates[i] = (angle, w);
            totalWeight += w;
        }

        if (totalWeight <= 0f)
            return AngleToDirection(baseAngleDeg);

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < candidates.Length; i++)
        {
            cumulative += candidates[i].weight;
            if (roll <= cumulative)
                return AngleToDirection(candidates[i].angle);
        }

        return AngleToDirection(baseAngleDeg);
    }

    Vector2 ComputeOrbitDirection()
    {
        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, playerTransform.position);

        Vector2 tangentLeft = new Vector2(-toPlayer.y, toPlayer.x);
        Vector2 tangentRight = new Vector2(toPlayer.y, -toPlayer.x);

        bool leftBlocked = IsBlocked(tangentLeft);
        bool rightBlocked = IsBlocked(tangentRight);

        Vector2 orbitDir;
        if (!leftBlocked && rightBlocked)
            orbitDir = tangentLeft;
        else if (leftBlocked && !rightBlocked)
            orbitDir = tangentRight;
        else if (!leftBlocked && !rightBlocked)
            orbitDir = Random.value > 0.5f ? tangentLeft : tangentRight;
        else
            return PickWeightedDirection(ToPlayerAngle(), MeleeChaseWeights);

        if (dist < orbitDistance * 0.8f)
            orbitDir = -toPlayer;

        return orbitDir.normalized;
    }

    Vector2 ComputeRangedKiteDirection()
    {
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
        float baseAngle = Mathf.Atan2(awayFromPlayer.y, awayFromPlayer.x) * Mathf.Rad2Deg;

        if (dist < minSafeRange)
            return PickWeightedDirection(baseAngle, RangedKiteWeights);

        if (dist < preferredRange)
            return PickWeightedDirection(baseAngle + (Random.value > 0.5f ? 90f : -90f), RangedKiteWeights);

        if (dist > detectionRadius * 0.8f)
            return Vector2.zero;

        return PickWeightedDirection(baseAngle, RangedKiteWeights);
    }

    bool CanSeePlayer()
    {
        if (playerTransform == null)
            return false;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > detectionRadius)
            return false;

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(facingDirection, toPlayer);
        return angle <= fieldOfView * 0.5f;
    }

    bool ShouldStopForAttack()
    {
        if (playerTransform == null)
            return false;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        return dist <= GetAttackStopRange();
    }

    bool IsBlocked(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return true;

        var hit = Physics2D.Raycast(transform.position, direction.normalized, obstacleCheckDistance, obstacleLayer);
        if (hit.collider == null)
            return false;

        if (hit.collider.gameObject == gameObject)
            return false;

        if (playerTransform != null && hit.collider.transform.IsChildOf(playerTransform))
            return false;

        if (hit.collider.transform == playerTransform)
            return false;

        return true;
    }

    void CheckStuck()
    {
        if (currentState == MoveState.StuckRecovery || currentState == MoveState.IdlePatrol)
            return;

        if (stuckTimer <= 0f)
        {
            stuckCheckStartPos = transform.position;
            stuckTimer = stuckCheckDuration;
            return;
        }

        stuckTimer -= Time.deltaTime;
        if (stuckTimer > 0f)
            return;

        float moved = Vector3.Distance(stuckCheckStartPos, transform.position);
        if (moved < stuckMinMove && !ShouldStopForAttack())
            EnterStuckRecovery();
    }

    void EnterStuckRecovery()
    {
        stateBeforeStuck = currentState;
        currentState = MoveState.StuckRecovery;
        stuckRecoveryTimer = stuckRecoveryTime;
        stuckTimer = stuckCheckDuration;

        Vector2 lastDir = rb != null && rb.velocity.sqrMagnitude > 0.01f
            ? rb.velocity.normalized
            : facingDirection;

        Vector2 perpA = new Vector2(-lastDir.y, lastDir.x);
        Vector2 perpB = new Vector2(lastDir.y, -lastDir.x);

        if (!IsBlocked(perpA))
            stuckEscapeDirection = perpA;
        else if (!IsBlocked(perpB))
            stuckEscapeDirection = perpB;
        else
            stuckEscapeDirection = PickRandomClearDirection();
    }

    Vector2 PickRandomClearDirection()
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector2 dir = AngleToDirection(angle);
            if (!IsBlocked(dir))
                return dir;
        }
        return Random.insideUnitCircle.normalized;
    }

    static Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        Vector2 face = Application.isPlaying ? facingDirection : (Vector2)transform.up;

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(pos, detectionRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        float halfFov = fieldOfView * 0.5f;
        Vector3 left = (Vector3)(Quaternion.Euler(0, 0, halfFov) * face);
        Vector3 right = (Vector3)(Quaternion.Euler(0, 0, -halfFov) * face);
        Gizmos.DrawLine(pos, pos + left * detectionRadius);
        Gizmos.DrawLine(pos, pos + right * detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : pos, idlePatrolRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(pos, GetAttackStopRange());

        if (isMelee || combatType == CombatType.Melee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, orbitDistance);
        }
    }
}
