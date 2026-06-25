using System;
using System.Collections;
using UnityEngine;

public enum AttackType { Melee, Ranged, Custom }

public abstract class Character : MonoBehaviour
{
    [Header("基础属性")]
    public string characterName;
    public int hp;
    public int maxHp = 100;
    public int damage;
    public AttackType attackType;
    public int defense;

    [Header("运行时")]
    public bool useGameSessionStats = true;

    protected virtual bool IsPlayerCharacter => false;

    public event Action<int, int> OnDamaged;
    public event Action OnDied;

    protected virtual void Start()
    {
        ApplySessionStatsIfNeeded();
    }

    public void ApplySessionStatsIfNeeded()
    {
        if (!useGameSessionStats) return;
        if (GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;

        var progression = GameManager.Instance.Session.Progression;
        if (progression == null) return;

        var stats = progression.Stats;
        maxHp = progression.GetEffectiveMaxHp();
        hp = stats.currentHp;
        damage = progression.GetEffectiveDamage();
        defense = progression.GetEffectiveDefense();
        characterName = GameManager.Instance.Session.Save.playerName;
    }

    public void TakeDamage(int damageValue)
    {
        if (hp <= 0) return;

        int realDamage = Mathf.Max(1, damageValue - defense);
        hp -= realDamage;
        Debug.Log($"{characterName} 受到了 {realDamage} 点伤害，剩余血量: {hp}");

        OnDamaged?.Invoke(realDamage, hp);

        if (GameManager.Instance?.Session?.Progression != null && IsPlayerCharacter)
            GameManager.Instance.Session.Progression.RegisterDamageTaken(realDamage);

        SyncHpToSession();

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    protected void SyncHpToSession()
    {
        if (IsPlayerCharacter && GameManager.Instance != null)
            GameManager.Instance.SyncPlayerHpFromCharacter(this);
    }

    protected virtual void Die()
    {
        Debug.Log($"<color=red>【死亡】{characterName} 倒下了！</color>");

        OnDied?.Invoke();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("Die");

        StartCoroutine(DestroyAfterAnimation(2f));
    }

    IEnumerator DestroyAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
