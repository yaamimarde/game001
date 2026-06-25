using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarriorPlayer : Character
{
    void Start()
    {
        characterName = "战士";
        hp = 1000;
        damage = 400;
        defense = 20;
        attackType = AttackType.Melee;
    }

    protected override void Die()
    {
        // 1. 立刻禁用玩家的移动控制脚本，让键盘输入失效
        MovePlayer moveScript = GetComponent<MovePlayer>();
        if (moveScript != null)
        {
            moveScript.enabled = false;
        }

        Debug.Log("<color=darkred>【GAME OVER】玩家已阵亡，停止一切键盘输入控制。</color>");

        // 2. 继承基类的通用死亡逻辑（去物理碰撞、播动画、2秒后消失）
        base.Die();
    }
}