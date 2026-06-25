using UnityEngine;

public class WarriorPlayer : Character
{
    protected override bool IsPlayerCharacter => true;

    protected override void Start()
    {
        base.Start();

        if (GameManager.Instance != null && GameManager.Instance.Session.IsActive)
        {
            ApplySessionStatsIfNeeded();
            if (GetComponent<PlayerProgressionTracker>() == null)
                gameObject.AddComponent<PlayerProgressionTracker>();
        }
        else
        {
            characterName = "战士";
            maxHp = 100;
            hp = 100;
            damage = 10;
            defense = 5;
            attackType = AttackType.Melee;
            useGameSessionStats = false;
        }
    }

    protected override void Die()
    {
        var move2d = GetComponent<PlayerMovement2D>();
        if (move2d != null)
            move2d.enabled = false;

        var attack = GetComponent<PlayerComboAttack>();
        if (attack != null)
            attack.enabled = false;

        Debug.Log("<color=darkred>【GAME OVER】玩家已阵亡。</color>");
        GameManager.Instance?.OnPlayerDeath();
        base.Die();
    }
}
