using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    Melee,      // 近战
    Ranged,     // 远程
    Custom      // 自定义/特殊
}

public abstract class Character : MonoBehaviour
{
    [Header("基础属性")]
    public string characterName;
    public int hp;
    public int damage;
    public AttackType attackType; // 使用枚举代替字符串
    public int defense;


    // 你也可以写一个普通方法，子类可以直接继承使用
    public void TakeDamage(int damageValue)
    {
        // 简单的减伤公式
        int realDamage = Mathf.Max(1, damageValue - defense);
        hp -= realDamage;
        Debug.Log($"{characterName} 受到 {realDamage} 点伤害，剩余生命: {hp}");
    }
}