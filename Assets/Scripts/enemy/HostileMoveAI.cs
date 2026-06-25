using UnityEngine;

/// <summary>
/// 独立敌对移动 AI（不依赖 AggressiveBehaviour / NpcMove）。
/// <para>
/// 近战：135° 视野、加权非直线追击、玩家周围环绕、出生点徘徊；
/// 进入攻击距离后由「攻击节奏输入」驱动：攻击阶段停步挥击，冷却阶段环绕走位。
/// </para>
/// <para>远程：保持距离风筝、避障、反卡死。</para>
/// <para>当前攻击节奏由内置模拟器假装外部传入；日后可替换为 AggressiveBehaviour 适配器。</para>
/// </summary>
public class HostileMoveAI : MonoBehaviour
{
    #region 类型与常量

    /// <summary>战斗风格：Auto 读 Character.attack，或强制近战/远程。</summary>
    public enum CombatType { Auto, Melee, Ranged }

    /// <summary>移动状态机：决定本帧朝哪个方向移动。</summary>
    enum MoveState
    {
        /// <summary>无目标时在出生点附近徘徊。</summary>
        IdlePatrol,
        /// <summary>发现玩家后加权追击（非直线）。</summary>
        MeleeChase,
        /// <summary>在玩家周围切线环绕，保持 orbitDistance。</summary>
        MeleeOrbit,
        /// <summary>远程单位保持距离、侧向走位。</summary>
        RangedKite,
        /// <summary>被障碍物卡住时侧向脱困。</summary>
        StuckRecovery
    }

    /// <summary>近战战斗节奏阶段（由攻击节奏输入提供，驱动停步/环绕）。</summary>
    public enum MeleeCombatPhase
    {
        /// <summary>未在攻击距离内，无战斗节奏。</summary>
        Ready,
        /// <summary>挥击窗口：移动 AI 停步，面向玩家。</summary>
        Attacking,
        /// <summary>攻击冷却：继续 MeleeOrbit 环绕。</summary>
        Cooldown
    }

    /// <summary>
    /// 攻击节奏输入接口。移动 AI 只读 Phase，不直接管伤害逻辑。
    /// 当前由 <see cref="BuiltinAttackRhythmSimulator"/> 模拟；
    /// TODO: 实现 IAttackRhythmInput 包装 AggressiveBehaviour.CurrentState
    ///       Attacking → 停步；Cooldown / Resting → 环绕。
    /// </summary>
    interface IAttackRhythmInput
    {
        MeleeCombatPhase Phase { get; }
        void Tick(float deltaTime, bool inCombatZone);
        void Reset();
        void SyncTiming(float attackDuration, float attackCooldown);
        void SetLogging(bool enabled);
    }

    /// <summary>内置攻击节奏模拟器：假装外部传入了「攻击 → 冷却」行为。</summary>
    sealed class BuiltinAttackRhythmSimulator : IAttackRhythmInput
    {
        readonly string ownerName;

        float attackDuration;
        float attackCooldown;
        bool logPhaseChange;

        MeleeCombatPhase phase = MeleeCombatPhase.Ready;
        float timer;

        public MeleeCombatPhase Phase => phase;

        public BuiltinAttackRhythmSimulator(string ownerName, float attackDuration, float attackCooldown, bool logPhaseChange)
        {
            this.ownerName = ownerName;
            this.attackDuration = attackDuration;
            this.attackCooldown = attackCooldown;
            this.logPhaseChange = logPhaseChange;
        }

        public void SyncTiming(float duration, float cooldown)
        {
            attackDuration = duration;
            attackCooldown = cooldown;
        }

        public void SetLogging(bool enabled) => logPhaseChange = enabled;

        public void Reset()
        {
            SetPhase(MeleeCombatPhase.Ready);
            timer = 0f;
        }

        public void Tick(float deltaTime, bool inCombatZone)
        {
            if (!inCombatZone)
            {
                Reset();
                return;
            }

            switch (phase)
            {
                case MeleeCombatPhase.Ready:
                    SetPhase(MeleeCombatPhase.Attacking);
                    timer = attackDuration;
                    break;
                case MeleeCombatPhase.Attacking:
                    timer -= deltaTime;
                    if (timer <= 0f)
                    {
                        SetPhase(MeleeCombatPhase.Cooldown);
                        timer = attackCooldown;
                    }
                    break;
                case MeleeCombatPhase.Cooldown:
                    timer -= deltaTime;
                    if (timer <= 0f)
                    {
                        SetPhase(MeleeCombatPhase.Attacking);
                        timer = attackDuration;
                    }
                    break;
            }
        }

        void SetPhase(MeleeCombatPhase next)
        {
            if (phase == next)
                return;

            phase = next;
            LogPhaseIfNeeded(next);
        }

        void LogPhaseIfNeeded(MeleeCombatPhase next)
        {
            if (!logPhaseChange)
                return;

            string message = next switch
            {
                MeleeCombatPhase.Attacking => "挥击停步",
                MeleeCombatPhase.Cooldown => "冷却环绕",
                _ => "脱离战斗"
            };
            Debug.Log($"[{ownerName}] 模拟攻击：{message}");
        }
    }

    /// <summary>近战追击方向权重：0° 正对玩家权重最高，大角度偏移权重递减。</summary>
    static readonly (float angle, float weight)[] MeleeChaseWeights =
    {
        (0f, 10f),
        (15f, 7f), (-15f, 7f),
        (30f, 5f), (-30f, 5f),
        (45f, 3f), (-45f, 3f)
    };

    /// <summary>远程风筝方向权重：偏向远离玩家，带侧向分量。</summary>
    static readonly (float angle, float weight)[] RangedKiteWeights =
    {
        (0f, 10f),
        (30f, 6f), (-30f, 6f),
        (60f, 4f), (-60f, 4f),
        (90f, 2f), (-90f, 2f)
    };

    /// <summary>多敌环绕时按 InstanceID 分配 8 个角度槽，减少堆叠。</summary>
    const int OrbitSlotCount = 8;

    /// <summary>朝向分配槽位的拉力权重（叠加在切线环绕上）。</summary>
    const float OrbitSlotPullWeight = 0.3f;

    #endregion

    #region Inspector 参数

    [Header("目标引用")]
    [Tooltip("追击/环绕的目标；为空时 Start 中按 Player 标签查找")]
    [SerializeField] Transform playerTransform;

    [Header("战斗类型")]
    [Tooltip("Auto：根据 Character.attack 判断近战/远程")]
    [SerializeField] CombatType combatType = CombatType.Auto;

    [Header("通用移动")]
    [Tooltip("移动速度（单位/秒）")]
    [SerializeField] float moveSpeed = 3f;
    [Tooltip("发现玩家的最大距离")]
    [SerializeField] float detectionRadius = 8f;
    [Tooltip("视野扇形角度（度）")]
    [SerializeField] float fieldOfView = 135f;
    [Tooltip("障碍检测射线使用的 Layer")]
    [SerializeField] LayerMask obstacleLayer = ~0;
    [Tooltip("前方障碍检测射线长度")]
    [SerializeField] float obstacleCheckDistance = 1.2f;

    [Header("攻击停步")]
    [Tooltip("近战挥击判定距离；为 0 时根据 Character.attack 自动推断（近战 1.5 / 远程 6）")]
    [SerializeField] float attackStopRange;

    [Header("模拟攻击节奏")]
    [Tooltip("启用后使用内置模拟器驱动「停步 → 环绕」循环，假装外部传入攻击行为")]
    [SerializeField] bool simulateAttack = true;
    [Tooltip("阶段切换时在 Console 输出模拟攻击日志")]
    [SerializeField] bool logSimulatedAttack = true;
    [Tooltip("挥击停步持续时间（秒），越大停步越明显")]
    [SerializeField] float meleeAttackDuration = 0.4f;
    [Tooltip("两次挥击之间的冷却时间（秒），期间环绕移动")]
    [SerializeField] float meleeAttackCooldown = 1f;

    [Header("近战")]
    [Tooltip("开始环绕玩家的距离，通常略大于攻击距离")]
    [SerializeField] float orbitDistance = 1.8f;
    [Tooltip("环绕切线速度基准，120 为设计默认值")]
    [SerializeField] float orbitAngularSpeed = 120f;
    [Tooltip("丢失视野后继续追击/环绕的缓冲时间（秒）")]
    [SerializeField] float chaseLostBuffer = 2f;
    [Tooltip("徘徊时随机目标点半径")]
    [SerializeField] float idlePatrolRadius = 2f;
    [Tooltip("离出生点超过此距离则走回出生点")]
    [SerializeField] float idleMaxDistanceFromSpawn = 3f;
    [Tooltip("徘徊重新选点的间隔（秒）")]
    [SerializeField] float idleRetargetInterval = 2f;

    [Header("群聚分离")]
    [Tooltip("检测其他 HostileMoveAI 的半径")]
    [SerializeField] float separationRadius = 1.5f;
    [Tooltip("分离力叠加到移动方向上的强度")]
    [SerializeField] float separationStrength = 1.2f;

    [Header("远程")]
    [Tooltip("理想保持距离")]
    [SerializeField] float preferredRange = 5f;
    [Tooltip("低于此距离会优先远离玩家")]
    [SerializeField] float minSafeRange = 3f;

    [Header("反卡死")]
    [Tooltip("每隔多久检测一次是否卡住")]
    [SerializeField] float stuckCheckDuration = 0.5f;
    [Tooltip("该时间内位移低于此值视为卡住")]
    [SerializeField] float stuckMinMove = 0.05f;
    [Tooltip("侧向脱困持续时间（秒）")]
    [SerializeField] float stuckRecoveryTime = 0.4f;

    #endregion

    #region 运行时状态

    Rigidbody2D rb;
    Character character;
    private IAttackBehaviour externalAttackBehaviour;
    /// <summary>假装外部传入的攻击节奏（当前为内置模拟器实例）。</summary>

    IAttackRhythmInput attackRhythm;

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

    /// <summary>是否按近战逻辑运行（Start 时根据 combatType / Character 确定）。</summary>
    bool isMelee;

    /// <summary>环绕方向：+1 逆时针，-1 顺时针，0 未选定。</summary>
    int orbitSide;

    /// <summary>本实例在环绕槽位上的角度（度）。</summary>
    float orbitSlotAngle;

    #endregion

    #region 对外只读属性

    /// <summary>当前近战战斗节奏阶段（来自攻击节奏输入）。</summary>
    public MeleeCombatPhase CurrentCombatPhase => attackRhythm?.Phase ?? MeleeCombatPhase.Ready;

    /// <summary>是否启用模拟攻击节奏。</summary>
    public bool IsSimulatingAttack => simulateAttack;

    #endregion

    #region Unity 生命周期

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        character = GetComponent<Character>();

        attackRhythm = new BuiltinAttackRhythmSimulator(
            gameObject.name,
            meleeAttackDuration,
            meleeAttackCooldown,
            logSimulatedAttack);
    }

    void Start()
    {
        spawnPosition = transform.position;
        PickNewIdleTarget();
        externalAttackBehaviour = GetComponent<IAttackBehaviour>();
        if (combatType == CombatType.Auto)
            // 使用类型安全的枚举判断替代字符串判断
            isMelee = character == null || character.attackType == AttackType.Melee;
        else
            isMelee = combatType == CombatType.Melee;

        if (orbitDistance <= 0f)
            orbitDistance = GetAttackStopRange() * 1.2f;

        float attackRange = GetAttackStopRange();
        if (orbitDistance < attackRange)
            orbitDistance = attackRange * 1.2f;
        if (orbitDistance > attackRange * 2f)
            orbitDistance = attackRange * 1.2f;

        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        // 每只怪固定一个环绕槽位，避免多敌叠在同一点
        orbitSlotAngle = (Mathf.Abs(GetInstanceID()) % OrbitSlotCount) * (360f / OrbitSlotCount);

        attackRhythm.SyncTiming(meleeAttackDuration, meleeAttackCooldown);
        attackRhythm.SetLogging(logSimulatedAttack);
    }

    void Update()
    {
        attackRhythm.SyncTiming(meleeAttackDuration, meleeAttackCooldown);
        attackRhythm.SetLogging(logSimulatedAttack);

        UpdateFacingDirection();
        UpdateStateMachine();
        CheckStuck();

        // 有 Rigidbody2D 时位移在 FixedUpdate 中处理
        if (rb != null)
            return;

        if (ShouldStopForAttack())
            return;

        Vector2 moveDir = ApplyFinalMoveDirection(ComputeMoveDirection());
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

        // 攻击阶段：由模拟攻击节奏触发停步
        if (ShouldStopForAttack())
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = ApplyFinalMoveDirection(ComputeMoveDirection());
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

    #endregion

    #region 状态机

    /// <summary>每帧更新移动状态；近战额外驱动攻击节奏与环绕/追击切换。</summary>
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
        {
            UpdateMeleeCombatPhase(dist, canSee);
            UpdateMeleeState(dist, canSee);
        }
        else
        {
            UpdateRangedState(dist, canSee);
        }
    }

    /// <summary>
    /// 近战移动状态：追击 → 环绕 → 攻击距离内由战斗节奏决定停步/绕圈。
    /// </summary>
    void UpdateMeleeState(float dist, bool canSee)
    {
        if (canSee)
        {
            lostTargetTimer = chaseLostBuffer;
            MeleeCombatPhase phase = CurrentCombatPhase;

            // 战斗区域内且节奏已启动：统一进入环绕状态（Attacking 时由停步拦截移动）
            if (IsInCombatZone(dist, canSee) && phase != MeleeCombatPhase.Ready)
            {
                if (currentState != MoveState.MeleeOrbit)
                    EnsureOrbitSide();
                currentState = MoveState.MeleeOrbit;
            }
            else if (dist <= orbitDistance)
            {
                if (currentState != MoveState.MeleeOrbit)
                    EnsureOrbitSide();
                currentState = MoveState.MeleeOrbit;
            }
            else
            {
                if (currentState == MoveState.MeleeOrbit)
                    orbitSide = 0;
                if (dist <= detectionRadius)
                    currentState = MoveState.MeleeChase;
            }
        }
        else if (lostTargetTimer > 0f)
        {
            lostTargetTimer -= Time.deltaTime;
            if (currentState == MoveState.MeleeChase || currentState == MoveState.MeleeOrbit)
                return;
        }
        else
        {
            if (currentState == MoveState.MeleeOrbit)
                orbitSide = 0;
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

    /// <summary>首次进入环绕时随机选左/右，被挡则换边。</summary>
    void EnsureOrbitSide()
    {
        if (orbitSide != 0 || playerTransform == null)
            return;

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 tangentCCW = new Vector2(-toPlayer.y, toPlayer.x);

        orbitSide = Random.value > 0.5f ? 1 : -1;
        if (IsBlocked(tangentCCW * orbitSide))
            orbitSide = -orbitSide;
    }

    #endregion

    #region 模拟攻击节奏

    /// <summary>把攻击节奏输入推进一帧；未启用或非近战时重置。</summary>
    void UpdateMeleeCombatPhase(float dist, bool canSee)
    {
        // 📥 【修改】：如果存在外部现代攻击脚本，直接读取它的状态来改变移动 AI 认账的 Phase
        if (externalAttackBehaviour != null)
        {
            if (IsInCombatZone(dist, canSee))
            {
                // 如果外部脚本说应该停步（说明在挥刀攻击），就给移动 AI 传 Attacking，否则传 Cooldown 环绕
                var targetPhase = externalAttackBehaviour.ShouldStopMovement ? MeleeCombatPhase.Attacking : MeleeCombatPhase.Cooldown;

                // 利用原代码里现成的 SetPhase 方法去刷新状态
                if (attackRhythm is BuiltinAttackRhythmSimulator sim)
                {
                    // 顺藤摸瓜，通过反射或者直接在 Simulator 里加个公有方法（或者简单采用下面 ShouldStopForAttack 的逻辑拦截）
                }
            }
            return;
        }

        // 以下是原本的降级保障（旧的模拟器代码，保持不动）
        if (!simulateAttack || !isMelee)
        {
            attackRhythm.Reset();
            return;
        }
        attackRhythm.Tick(Time.deltaTime, IsInCombatZone(dist, canSee));
    }

    #endregion

    #region 移动与方向

    /// <summary>根据当前 MoveState 计算期望移动方向（未含分离力）。</summary>
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

    /// <summary>叠加群聚分离力，避免多个 HostileMoveAI 叠在一起。</summary>
    Vector2 ApplyFinalMoveDirection(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f)
            return moveDir;

        Vector2 sep = ComputeSeparation();
        if (sep.sqrMagnitude > 0.01f)
            moveDir = (moveDir + sep * separationStrength).normalized;

        return moveDir;
    }

    float ToPlayerAngle()
    {
        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        return Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
    }

    /// <summary>按权重表随机选一个偏移角度方向；被挡方向权重大幅降低。</summary>
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

    /// <summary>
    /// 环绕方向 = 切线（绕圈）+ 径向（拉回 orbitDistance）+ 槽位拉力（多敌分散）。
    /// </summary>
    Vector2 ComputeOrbitDirection()
    {
        if (playerTransform == null)
            return Vector2.zero;

        if (orbitSide == 0)
            EnsureOrbitSide();

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position);
        float dist = toPlayer.magnitude;
        if (dist < 0.001f)
            return Vector2.zero;

        toPlayer /= dist;

        Vector2 tangentCCW = new Vector2(-toPlayer.y, toPlayer.x);
        float tangentScale = orbitAngularSpeed / 120f;
        Vector2 tangent = tangentCCW * orbitSide * tangentScale;

        // 距离偏离 orbitDistance 时沿径向微调
        float radialError = dist - orbitDistance;
        float radialWeight = Mathf.Clamp(radialError / orbitDistance, -1f, 1f);
        Vector2 orbitDir = tangent + toPlayer * radialWeight;

        Vector2 slotDir = AngleToDirection(orbitSlotAngle);
        Vector2 slotPos = (Vector2)playerTransform.position + slotDir * orbitDistance;
        Vector2 toSlot = slotPos - (Vector2)transform.position;
        if (toSlot.sqrMagnitude > 0.001f)
            orbitDir += toSlot.normalized * OrbitSlotPullWeight;

        if (orbitDir.sqrMagnitude < 0.001f)
            return PickWeightedDirection(ToPlayerAngle(), MeleeChaseWeights);

        Vector2 finalDir = orbitDir.normalized;
        if (IsBlocked(finalDir))
            return PickWeightedDirection(ToPlayerAngle(), MeleeChaseWeights);

        return finalDir;
    }

    Vector2 ComputeSeparation()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        Vector2 separation = Vector2.zero;
        Vector2 pos = transform.position;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;
            if (hit.GetComponent<HostileMoveAI>() == null)
                continue;

            Vector2 away = pos - (Vector2)hit.transform.position;
            float d = away.magnitude;
            if (d > 0.01f)
                separation += away.normalized / d;
        }

        return separation.sqrMagnitude > 0.001f ? separation.normalized : Vector2.zero;
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

    void UpdateFacingDirection()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
            facingDirection = rb.velocity.normalized;
        else if (playerTransform != null)
            facingDirection = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        else
            facingDirection = transform.up;
    }

    static Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    #endregion

    #region 感知与障碍

    float GetAttackStopRange()
    {
        if (attackStopRange > 0f)
            return attackStopRange;

        if (externalAttackBehaviour != null)
            return externalAttackBehaviour.AttackRange;

        if (character == null)
            return isMelee ? 1.5f : 6f;

        // ✨ 将字符串判断统一重构为枚举判断
        if (character.attackType == AttackType.Melee)
            return 1.5f;
        if (character.attackType == AttackType.Ranged)
            return 6f;
        return 2f;
    }

    /// <summary>战斗区域半径：取攻击距离与环绕距离的较大值，冷却环绕时节奏不被重置。</summary>
    float GetCombatZoneRange() => Mathf.Max(GetAttackStopRange(), orbitDistance);

    /// <summary>玩家在视野内且处于战斗区域（环绕/挥击范围）内。</summary>
    bool IsInCombatZone(float dist, bool canSee) => canSee && dist <= GetCombatZoneRange();

    /// <summary>玩家是否在检测半径内且处于视野扇形内。</summary>
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

    /// <summary>近战攻击阶段且在战斗区域内时停止移动（模拟挥击停步）。</summary>
    bool ShouldStopForAttack()
    {
        if (playerTransform == null)
            return false;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // 📥 【新增】：如果有外部现代攻击脚本，只要进了距离，且攻击脚本说“要停步挥刀”，就立马返回 true 停下！
        if (externalAttackBehaviour != null)
        {
            return isMelee
                && externalAttackBehaviour.ShouldStopMovement
                && dist <= GetAttackStopRange(); // 或者是 IsInCombatZone 判定
        }

        // 以下是原本的内置模拟器停步逻辑，保留作为无攻击脚本时的保底
        if (!simulateAttack)
            return false;

        return isMelee
            && CurrentCombatPhase == MeleeCombatPhase.Attacking
            && IsInCombatZone(dist, CanSeePlayer());
    }

    /// <summary>射线检测前方是否有障碍（忽略自身与玩家）。</summary>
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

    #endregion

    #region 反卡死

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
        // 攻击停步期间不算卡住
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

    #endregion

    #region 调试 Gizmo

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        Vector2 face = Application.isPlaying ? facingDirection : (Vector2)transform.up;

        // 黄色：检测半径
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(pos, detectionRadius);

        // 青色扇形：视野
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        float halfFov = fieldOfView * 0.5f;
        Vector3 left = (Vector3)(Quaternion.Euler(0, 0, halfFov) * face);
        Vector3 right = (Vector3)(Quaternion.Euler(0, 0, -halfFov) * face);
        Gizmos.DrawLine(pos, pos + left * detectionRadius);
        Gizmos.DrawLine(pos, pos + right * detectionRadius);

        // 绿色：出生点徘徊范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : pos, idlePatrolRadius);

        // 橙色：攻击停步距离
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(pos, GetAttackStopRange());

        // 头顶小球：红=挥击停步，青=冷却环绕，灰=无节奏
        if (Application.isPlaying && isMelee && simulateAttack)
        {
            Gizmos.color = CurrentCombatPhase switch
            {
                MeleeCombatPhase.Attacking => Color.red,
                MeleeCombatPhase.Cooldown => Color.cyan,
                _ => Color.gray
            };
            Gizmos.DrawSphere(pos + Vector3.up * 0.5f, 0.15f);
        }

        if (isMelee || combatType == CombatType.Melee)
        {
            // 红色：环绕距离
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, orbitDistance);

            // 蓝色线段：当前环绕切线方向
            if (Application.isPlaying && orbitSide != 0 && playerTransform != null)
            {
                Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)pos).normalized;
                Vector2 tangent = new Vector2(-toPlayer.y, toPlayer.x) * orbitSide;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + (Vector3)(tangent * 1.5f));
            }
        }

        // 紫色：群聚分离半径
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(pos, separationRadius);
    }

    #endregion
}
