using UnityEngine;

public class Gebuling : Character
{
    [SerializeField] CharacterStatsSO statsOverride;

    protected override void Start()
    {
        base.Start();

        if (statsOverride != null)
        {
            characterName = statsOverride.characterName;
            maxHp = statsOverride.maxHp;
            hp = statsOverride.maxHp;
            damage = statsOverride.damage;
            defense = statsOverride.defense;
            attackType = statsOverride.attackType;
        }
        else
        {
            characterName = "哥布林";
            hp = 100;
            maxHp = 100;
            damage = 100;
            attackType = AttackType.Melee;
            defense = 10;
        }

        useGameSessionStats = false;
    }

    protected override void Die()
    {
        Debug.Log("<color=yellow>【NPC死亡】怪物倒下，关闭 AI 组件。</color>");

        AggressiveBehaviour attackAI = GetComponent<AggressiveBehaviour>();
        if (attackAI != null) attackAI.enabled = false;

        HostileMoveAI moveAI = GetComponent<HostileMoveAI>();
        if (moveAI != null) moveAI.enabled = false;

        base.Die();
    }
}
