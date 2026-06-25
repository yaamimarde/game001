using UnityEngine;
public class CompanionCharacter : Character
{
    RecruitTemplate template;
    FriendlyMoveAI moveAi;
    CompanionCombatAI combatAi;

    public void Initialize(RecruitTemplate recruitTemplate, CompanionSave save)
    {
        template = recruitTemplate;
        characterName = recruitTemplate.displayName;

        var stats = save?.stats ?? recruitTemplate.baseStats.Clone();
        maxHp = stats.maxHp;
        hp = stats.currentHp;
        damage = stats.baseDamage;
        defense = stats.baseDefense;
        attackType = AttackType.Melee;

        moveAi = GetComponent<FriendlyMoveAI>();
        combatAi = GetComponent<CompanionCombatAI>();
        if (combatAi == null)
            combatAi = gameObject.AddComponent<CompanionCombatAI>();
    }

    public StatBlock GetStatBlock()
    {
        return new StatBlock
        {
            maxHp = maxHp,
            currentHp = hp,
            baseDamage = damage,
            baseDefense = defense
        };
    }

    protected override void Die()
    {
        base.Die();
    }
}
