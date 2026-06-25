using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Gebuling : Character
{
    void Start()
    {
        characterName = "哥布林";
        hp = 100;
        damage = 100;
        attackType = AttackType.Melee;
        defense = 10;
    }

    protected override void Die()
    {
        Debug.Log("<color=yellow>【NPC死亡】哥布林脑死亡，正在停止所有AI行为。</color>");

        // 1.禁用攻击状态机
        AggressiveBehaviour attackAI = GetComponent<AggressiveBehaviour>();
        if (attackAI != null) attackAI.enabled = false;

        // 2. 禁用移动AI（把挂载在你怪身上的脚本都禁掉）
        HostileMoveAI moveAI = GetComponent<HostileMoveAI>();
        if (moveAI != null) moveAI.enabled = false;

        NpcMove oldMoveAI = GetComponent<NpcMove>();
        if (oldMoveAI != null) oldMoveAI.enabled = false;

        // 3. 继承基类的通用死亡逻辑（去物理碰撞、播动画、2秒后消失）
        base.Die();
    }
}