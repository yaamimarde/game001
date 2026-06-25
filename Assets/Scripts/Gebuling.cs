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
        damage = 10;
        attackType = AttackType.Melee; // 类型安全赋值
        defense = 10;
    }
}