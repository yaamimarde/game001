using UnityEngine;

[CreateAssetMenu(fileName = "GebulingStats", menuName = "Game001/Enemy Stats/Gebuling")]
public class GebulingStatsSO : CharacterStatsSO
{
    void Reset()
    {
        characterName = "哥布林";
        maxHp = 100;
        damage = 100;
        defense = 10;
        attackType = AttackType.Melee;
    }
}
