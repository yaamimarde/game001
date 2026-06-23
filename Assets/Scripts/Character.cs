using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [Header("基础属性")] // 这个能让 Unity 面板更美观
    public string characterName;
    public int hp;
    public int damage;
    public string attack;// 攻击方式
    public int defense;


    // 你也可以写一个普通方法，子类可以直接继承使用
    public void TakeDamage(int damageValue)
    {
        // 简单的减伤公式
        int realDamage = Mathf.Max(1, damageValue - defense);
        hp -= realDamage;
        Debug.Log($"{characterName} 受到了 {realDamage} 点伤害，剩余血量: {hp}");
    }
}