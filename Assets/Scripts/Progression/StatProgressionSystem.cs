using UnityEngine;

[CreateAssetMenu(fileName = "StatProgressionConfig", menuName = "Game/Stat Progression Config")]
public class StatProgressionConfig : ScriptableObject
{
    public float xpPerLevel = 100f;
    public int hpPerToughness = 10;
    public int damagePerStrength = 2;
    public int damagePerMelee = 3;
    public int defensePerToughness = 1;
    public float moveSpeedPerAthletics = 0.05f;
    public float attackCooldownReductionPerDex = 0.01f;
}

public class StatProgressionSystem
{
    public StatBlock Stats { get; private set; }
    readonly StatProgressionConfig config;
    float distanceAccumulator;

    public StatProgressionSystem(StatBlock stats)
    {
        Stats = stats ?? StatBlock.CreateDefault();
        config = DefaultGameContent.GetStatConfig();
        RecalculateDerivedStats();
    }

    public int GetEffectiveDamage()
    {
        int bonus = 0;
        if (config != null)
        {
            bonus += Stats.strength * config.damagePerStrength;
            bonus += Stats.melee * config.damagePerMelee;
        }
        return Stats.baseDamage + bonus;
    }

    public int GetEffectiveDefense()
    {
        int bonus = config != null ? Stats.toughness * config.defensePerToughness : 0;
        return Stats.baseDefense + bonus;
    }

    public int GetEffectiveMaxHp()
    {
        int bonus = config != null ? Stats.toughness * config.hpPerToughness : 0;
        return Stats.maxHp + bonus;
    }

    public void RegisterMeleeHit()
    {
        AddXp(ref Stats.strengthXp, ref Stats.strength);
        AddXp(ref Stats.meleeXp, ref Stats.melee);
        AddXp(ref Stats.dexterityXp, ref Stats.dexterity);
        RecalculateDerivedStats();
        GameEventBus.RaiseStatChanged();
    }

    public void RegisterDamageTaken(int amount)
    {
        AddXp(ref Stats.toughnessXp, ref Stats.toughness, amount * 0.5f);
        RecalculateDerivedStats();
        GameEventBus.RaiseStatChanged();
    }

    public void RegisterDistanceMoved(float distance)
    {
        distanceAccumulator += distance;
        if (distanceAccumulator < 1f) return;

        int meters = (int)distanceAccumulator;
        distanceAccumulator -= meters;
        AddXp(ref Stats.athleticsXp, ref Stats.athletics, meters * 0.2f);
        GameEventBus.RaiseStatChanged();
    }

    void AddXp(ref float xp, ref int level, float amount = 1f)
    {
        float threshold = config != null ? config.xpPerLevel : 100f;
        xp += amount;
        while (xp >= threshold)
        {
            xp -= threshold;
            level++;
        }
    }

    public void RecalculateDerivedStats()
    {
        Stats.maxHp = GetEffectiveMaxHp();
        Stats.currentHp = Mathf.Min(Stats.currentHp, Stats.maxHp);
    }
}
