using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarriorPlayer :Character
{

    void Start()
    {
        // 游戏开始时，直接给继承过来的属性赋值
        characterName = "战士";
        hp = 1000;
        attack = "近战";
        damage = 40;
        defense = 20;
    }
}