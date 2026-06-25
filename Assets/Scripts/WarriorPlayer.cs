using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarriorPlayer : Character
{
    void Start()
    {
        // 游戏开始时，直接给继承过来的属性赋值
        characterName = "战士";
        hp = 1000;
        damage = 40;
        defense = 20;

        // ✨ 优化：将原本的字符串修改为类型安全的枚举类型
        attackType = AttackType.Melee;
    }
}