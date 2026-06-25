using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerAttackBase : MonoBehaviour
{
    protected Character characterComponent;
    protected Animator anim;

    [Header("基类公共设置")]
    public float attackCooldown = 0.5f; // 两次攻击之间的基础冷却
    protected float nextAttackTime = 0f;  // 下一次允许攻击的时间戳

    protected virtual void Start()
    {
        // 自动获取挂在同一个玩家身上的组件
        characterComponent = GetComponent<Character>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 公共判定：当前是否可以发动攻击/技能
    /// </summary>
    public virtual bool CanAttack()
    {
        // 1. 血量必须大于0；2. CD 必须已经转完
        return (characterComponent != null && characterComponent.hp > 0 && Time.time >= nextAttackTime);
    }

    /// <summary>
    /// 核心虚方法：所有子类技能/攻击的具体实现
    /// </summary>
    public abstract void ExecuteAttack();
}
