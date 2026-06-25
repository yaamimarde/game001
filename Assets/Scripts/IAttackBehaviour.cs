using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackBehaviour
{
    // 获取攻击距离
    float AttackRange { get; }

    // 移动 AI 询问：当前攻击节奏是否要求 NPC 原地停步（比如正在挥刀或前摇）
    bool ShouldStopMovement { get; }
}
