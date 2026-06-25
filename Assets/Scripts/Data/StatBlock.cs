using System;
using UnityEngine;

[Serializable]
public class StatBlock
{
    public int strength = 1;
    public int dexterity = 1;
    public int toughness = 1;
    public int athletics = 1;
    public int melee = 1;
    public int ranged = 1;

    public float strengthXp;
    public float dexterityXp;
    public float toughnessXp;
    public float athleticsXp;
    public float meleeXp;
    public float rangedXp;

    public int maxHp = 100;
    public int currentHp = 100;
    public int baseDamage = 10;
    public int baseDefense = 5;

    public static StatBlock CreateDefault()
    {
        return new StatBlock();
    }

    public StatBlock Clone()
    {
        return (StatBlock)MemberwiseClone();
    }
}
