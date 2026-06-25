using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType { Melee, Ranged, Custom }

public abstract class Character : MonoBehaviour
{
    [Header("基础属性")]
    public string characterName;
    public int hp;
    public int damage;
    public AttackType attackType;
    public int defense;

    public void TakeDamage(int damageValue)
    {
        if (hp <= 0) return; // 如果已经死了，不再重复受击

        int realDamage = Mathf.Max(1, damageValue - defense);
        hp -= realDamage;
        Debug.Log($"{characterName} 受到了 {realDamage} 点伤害，剩余血量: {hp}");

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    // 💀 死亡虚方法
    protected virtual void Die()
    {
        Debug.Log($"<color=red>【死亡】{characterName} 倒下了！</color>");

        // 1.立刻关闭碰撞体，防止死尸卡位或重复挨打
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 2.如果有刚体，让它停下来，不要继续漂移
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 3.尝试获取 Animator 并触发 "Die" 动画
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }
        else
        {
            Debug.LogWarning($"{characterName} 身上没有找到 Animator 组件，无法播放死亡动画！");
        }

        // 4.开启协程，2秒后销毁物体（你可以把 2f 改成你动画的实际长度）
        StartCoroutine(DestroyAfterAnimation(2f));
    }

    // 🌟 核心：延迟销毁协程
    private IEnumerator DestroyAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 让物体消失
        Destroy(gameObject);

        // 💡 提示：如果你的游戏需要“怪物复活池”或者考虑性能优化，
        // 以后可以把 Destroy(gameObject) 改成对象池回收，或者 gameObject.SetActive(false);
    }
}